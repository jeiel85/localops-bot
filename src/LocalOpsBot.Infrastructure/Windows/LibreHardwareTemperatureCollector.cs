using System.Runtime.Versioning;
using LibreHardwareMonitor.Hardware;
using LocalOpsBot.Core.Monitoring;
using Microsoft.Extensions.Logging;

namespace LocalOpsBot.Infrastructure.Windows;

/// <summary>
/// Reads temperature sensors via LibreHardwareMonitor. Opening the library loads a kernel driver
/// (WinRing0) to read hardware, so it requires an elevated process — this runs in the Agent
/// (Windows service), never the non-elevated Tray. When the machine exposes no sensors or the
/// driver can't load, collection succeeds with an empty list instead of throwing.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class LibreHardwareTemperatureCollector : ITemperatureCollector, IDisposable
{
    // Sensors sometimes report sentinel values instead of a real reading: 0.0 when the kernel
    // driver isn't loaded (non-elevated), or 255.0 ("0xFF / not ready", seen on GPU memory
    // junction). Keep only physically plausible temperatures so the advisor doesn't misread them.
    private const double MinPlausibleCelsius = 0.0;    // exclusive: 0.0 means "no reading"
    private const double MaxPlausibleCelsius = 150.0;  // above any real PC component temperature

    // LibreHardwareMonitor's Computer is not thread-safe: Open() is expensive and Update()/reads
    // must not overlap. One Computer is opened lazily and every access is serialized on this gate.
    private readonly object _gate = new();
    private readonly TemperatureOptions _options;
    private readonly ILogger<LibreHardwareTemperatureCollector>? _logger;
    private Computer? _computer;
    private bool _disposed;
    private bool _loggedFailure; // guarded by _gate: warn on the first failure, then stay quiet until recovery

    // Options/logger are optional so the collector can be constructed directly (tests, smoke);
    // dependency injection supplies the bound options and a real logger in the running Agent.
    public LibreHardwareTemperatureCollector(
        TemperatureOptions? options = null,
        ILogger<LibreHardwareTemperatureCollector>? logger = null)
    {
        _options = options ?? new TemperatureOptions();
        _logger = logger;
    }

    public string Name => "Temperature";

    public Task<CollectorResult<TemperatureSnapshot>> CollectAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            if (_disposed)
                return Task.FromResult(
                    CollectorResult<TemperatureSnapshot>.Fail("Temperature collector is disposed.", now));

            // Toggle off: return empty without opening the library, so the WinRing0 driver never loads.
            if (!_options.Enabled)
                return Task.FromResult(
                    CollectorResult<TemperatureSnapshot>.Ok(new TemperatureSnapshot([]), now));

            try
            {
                var computer = EnsureOpen();
                var readings = new List<SensorReading>();
                foreach (var hardware in computer.Hardware)
                    CollectFrom(hardware, readings);

                if (_loggedFailure)
                {
                    _logger?.LogInformation("Temperature collection recovered.");
                    _loggedFailure = false;
                }
                return Task.FromResult(
                    CollectorResult<TemperatureSnapshot>.Ok(new TemperatureSnapshot(readings), now));
            }
            catch (Exception ex)
            {
                // No sensors, missing driver, or no elevation: degrade gracefully rather than crash.
                // Log the first failure once (not every poll); a later success logs recovery above.
                if (!_loggedFailure)
                {
                    _logger?.LogWarning(ex, "Temperature collection failed; sensors will be omitted until it recovers.");
                    _loggedFailure = true;
                }
                return Task.FromResult(CollectorResult<TemperatureSnapshot>.Fail(ex.Message, now));
            }
        }
    }

    // Opens the LHM Computer once and reuses it; opening loads the kernel driver and is slow.
    private Computer EnsureOpen()
    {
        if (_computer is { } existing)
            return existing;

        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
        };
        try
        {
            computer.Open();
        }
        catch
        {
            try { computer.Close(); } catch { /* half-open teardown is best-effort */ }
            throw;
        }

        _computer = computer;
        return computer;
    }

    // Reads Temperature sensors off this hardware node and recurses into sub-hardware — board and
    // system temperatures live on the motherboard's SuperIO sub-hardware, not the node itself.
    private static void CollectFrom(IHardware hardware, List<SensorReading> readings)
    {
        hardware.Update();

        var kind = KindOf(hardware.HardwareType);
        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.SensorType != SensorType.Temperature) continue;
            if (sensor.Value is not float value || float.IsNaN(value)) continue;
            if (value <= MinPlausibleCelsius || value > MaxPlausibleCelsius) continue;
            readings.Add(new SensorReading(sensor.Name, kind, value));
        }

        foreach (var sub in hardware.SubHardware)
            CollectFrom(sub, readings);
    }

    private static string KindOf(HardwareType type) => type switch
    {
        HardwareType.Cpu => "Cpu",
        HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel => "Gpu",
        _ => "Board", // Motherboard + SuperIO (where board/system temperatures are exposed)
    };

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _computer?.Close(); }
            catch { /* best-effort teardown */ }
            _computer = null;
        }
    }
}
