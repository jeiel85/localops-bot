namespace LocalOpsBot.Core.Monitoring;

/// <summary>
/// Reads CPU/GPU/board temperature sensors. On Windows this needs an elevated process with a
/// kernel driver, so it runs in the Agent only; results are graceful (empty) when no sensors
/// are exposed or the platform can't read them.
/// </summary>
public interface ITemperatureCollector : ICollector<TemperatureSnapshot>
{
}
