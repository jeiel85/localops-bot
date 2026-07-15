using System.Text.Json;

namespace LocalOpsBot.Core.Ipc;

public enum SessionMessageKind
{
    Hello,
    Challenge,
    ChallengeResponse,
    Request,
    Response,
    Event,
    Cancel,
    Heartbeat,
    Goodbye
}

public sealed record SessionBridgeMessage(
    int SchemaVersion,
    Guid MessageId,
    SessionMessageKind Kind,
    string Operation,
    Guid? CorrelationId,
    DateTimeOffset SentAt,
    DateTimeOffset? ExpiresAt,
    JsonElement Body);

public sealed record SessionOperationRequest(
    Guid RequestId,
    string Operation,
    JsonElement Body,
    DateTimeOffset? ExpiresAt);

public sealed record SessionOperationResult(
    bool Success,
    JsonElement? Body,
    string? ErrorCode,
    string? Error);

public interface IUserSessionBridge
{
    bool IsAvailable { get; }

    Task<SessionOperationResult> InvokeAsync(
        SessionOperationRequest request,
        CancellationToken ct);

    IAsyncEnumerable<SessionBridgeMessage> SubscribeAsync(
        CancellationToken ct);
}
