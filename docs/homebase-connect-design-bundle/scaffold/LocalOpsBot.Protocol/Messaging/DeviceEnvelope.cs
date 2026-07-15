using System.Text.Json;

namespace LocalOpsBot.Protocol.Messaging;

public sealed record EndpointAddress(
    string TransportId,
    string EndpointId,
    string? DeviceId = null);

public sealed record PayloadDescriptor(
    Guid TransferId,
    string? FileName,
    string? MimeType,
    long SizeBytes,
    string? Sha256);

public enum MessageSensitivity
{
    Normal,
    Private,
    Sensitive,
    Secret
}

public enum DeliverySemantics
{
    BestEffort,
    AtLeastOnce,
    Acknowledged
}

public sealed record DeviceEnvelope(
    Guid MessageId,
    int SchemaVersion,
    string MessageType,
    EndpointAddress Source,
    EndpointAddress? Target,
    Guid TraceId,
    Guid? CorrelationId,
    Guid? CausationId,
    int HopCount,
    int MaxHops,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    MessageSensitivity Sensitivity,
    DeliverySemantics Delivery,
    JsonElement Body,
    PayloadDescriptor? Payload,
    IReadOnlyDictionary<string, string>? Metadata)
{
    public void Validate(DateTimeOffset now)
    {
        if (MessageId == Guid.Empty) throw new ArgumentException("MessageId is required.");
        if (TraceId == Guid.Empty) throw new ArgumentException("TraceId is required.");
        if (SchemaVersion != 1) throw new NotSupportedException($"Schema {SchemaVersion} is unsupported.");
        if (string.IsNullOrWhiteSpace(MessageType)) throw new ArgumentException("MessageType is required.");
        if (HopCount < 0 || MaxHops is < 1 or > 8 || HopCount >= MaxHops)
            throw new ArgumentOutOfRangeException(nameof(HopCount));
        if (ExpiresAt is not null && ExpiresAt <= now)
            throw new InvalidOperationException("Message has expired.");
        if (Payload is { SizeBytes: < 0 })
            throw new ArgumentOutOfRangeException(nameof(Payload.SizeBytes));
    }
}
