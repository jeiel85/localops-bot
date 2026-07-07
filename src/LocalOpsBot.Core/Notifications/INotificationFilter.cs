namespace LocalOpsBot.Core.Notifications;

public sealed record NotificationFilterResult(bool Allowed, string? DropReason);

public interface INotificationFilter
{
    NotificationFilterResult Evaluate(ToastNotificationEvent notification);
}
