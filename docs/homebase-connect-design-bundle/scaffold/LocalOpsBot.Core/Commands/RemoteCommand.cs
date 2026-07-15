using System.Text.Json;
using LocalOpsBot.Protocol.Messaging;

namespace LocalOpsBot.Core.Commands;

public enum PrincipalKind
{
    TelegramChat,
    PairedDevice,
    LocalUser
}

public enum TrustLevel
{
    Untrusted,
    Allowed,
    Paired,
    LocalAdministrator
}

public enum CommandRiskLevel
{
    ReadOnly,
    UserSessionMutation,
    SystemMutation,
    Destructive
}

public sealed record CommandPrincipal(
    string PrincipalId,
    PrincipalKind Kind,
    string? DeviceId,
    TrustLevel TrustLevel);

public sealed record RemoteCommand(
    Guid RequestId,
    string Name,
    IReadOnlyList<string> Args,
    CommandPrincipal Principal,
    EndpointAddress ReplyTo,
    string RawInput,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record CommandAttachment(
    string Name,
    string? MimeType,
    string LocalPath);

public sealed record RemoteCommandResult(
    bool Success,
    string? UserMessage,
    JsonElement? Data,
    IReadOnlyList<CommandAttachment>? Attachments,
    string? ErrorCode,
    string? ErrorDetail);

public interface IRemoteCommandHandler
{
    string CommandName { get; }
    CommandRiskLevel RiskLevel { get; }

    Task<RemoteCommandResult> HandleAsync(
        RemoteCommand command,
        CancellationToken ct);
}
