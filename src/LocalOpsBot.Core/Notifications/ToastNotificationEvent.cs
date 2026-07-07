namespace LocalOpsBot.Core.Notifications;

public enum NotificationSensitivity
{
    Normal,
    Sensitive,
    Blocked
}

public sealed record ToastNotificationEvent(
    string EventId,
    string SourceApp,
    string? Title,
    string? Body,
    DateTimeOffset CreatedAt,
    string RawNotificationId,
    NotificationSensitivity Sensitivity);

public sealed record ToastNotificationPipeMessage(
    int SchemaVersion,
    string Type,
    string EventId,
    string SourceApp,
    string? Title,
    string? Body,
    DateTimeOffset CreatedAt,
    NotificationSensitivity Sensitivity);
