using LocalOpsBot.Protocol.Messaging;

namespace LocalOpsBot.Core.Delivery;

public enum NotificationPriority
{
    Info,
    Warning,
    Critical
}

public enum DeliveryPolicy
{
    OriginOnly,
    LocalPreferred,
    TelegramFallback,
    Both,
    LocalOnly,
    TelegramOnly
}

public sealed record NotificationAction(
    string ActionId,
    string DisplayName,
    bool RequiresConfirmation);

public sealed record OutboundNotification(
    Guid NotificationId,
    string Category,
    NotificationPriority Priority,
    string Title,
    string? Body,
    string DedupKey,
    MessageSensitivity Sensitivity,
    DeliveryPolicy DeliveryPolicy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<NotificationAction>? Actions,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record DeliveryStep(
    EndpointAddress Target,
    TimeSpan Delay,
    bool ContinueOnSuccess,
    bool ContinueOnFailure);

public sealed record DeliveryPlan(
    Guid PlanId,
    IReadOnlyList<DeliveryStep> Steps);

public interface IDeliveryPlanner
{
    Task<DeliveryPlan> PlanAsync(
        OutboundNotification notification,
        CancellationToken ct);
}

public interface IOutboundRouter
{
    Task DeliverAsync(
        OutboundNotification notification,
        CancellationToken ct);
}
