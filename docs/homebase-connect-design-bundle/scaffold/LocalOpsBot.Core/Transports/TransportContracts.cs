using LocalOpsBot.Protocol.Messaging;

namespace LocalOpsBot.Core.Transports;

public sealed record TransportCapabilities(
    bool SupportsDiscovery,
    bool SupportsPairing,
    bool SupportsPayloads,
    bool SupportsAcknowledgements);

public sealed record TransportInboundMessage(
    EndpointAddress Source,
    DeviceEnvelope Envelope);

public sealed record TransportDeviceEvent(
    string DeviceId,
    string EventType,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, string>? Metadata);

public enum TransportSendStatus
{
    Sent,
    Acknowledged,
    EndpointOffline,
    Unauthorized,
    Rejected,
    Failed
}

public sealed record TransportSendResult(
    TransportSendStatus Status,
    string? ErrorCode = null,
    string? Error = null);

public interface IDeviceTransport : IAsyncDisposable
{
    string TransportId { get; }
    TransportCapabilities Capabilities { get; }

    event Func<TransportInboundMessage, CancellationToken, Task>? MessageReceived;
    event Func<TransportDeviceEvent, CancellationToken, Task>? DeviceEventReceived;

    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);

    Task<TransportSendResult> SendAsync(
        EndpointAddress target,
        DeviceEnvelope envelope,
        CancellationToken ct);
}
