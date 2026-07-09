using LocalOpsBot.Core.Monitoring;

namespace LocalOpsBot.Tests.Fakes;

internal sealed class FakeTemperatureCollector : ITemperatureCollector
{
    public string Name => "FakeTemperature";
    public CollectorResult<TemperatureSnapshot> NextResult { get; set; }
        = CollectorResult<TemperatureSnapshot>.Ok(
            new TemperatureSnapshot(new List<SensorReading>
            {
                new("CPU Package", "Cpu", 72.0),
                new("CPU Core #1", "Cpu", 68.0),
                new("GPU Core", "Gpu", 65.0),
                new("System", "Board", 41.0),
            }),
            DateTimeOffset.UtcNow);

    public Task<CollectorResult<TemperatureSnapshot>> CollectAsync(CancellationToken ct)
        => Task.FromResult(NextResult);
}
