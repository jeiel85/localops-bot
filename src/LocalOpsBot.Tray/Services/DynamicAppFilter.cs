using LocalOpsBot.Core.Notifications;

namespace LocalOpsBot.Tray.Services;

/// <summary>
/// App-name filter whose allow-list is read live from the user file (<see cref="ForwardingApps"/>),
/// so the dashboard's app selection takes effect within a poll cycle (~3s) without restarting the
/// tray or elevating. An empty allow-list forwards everything (the "nothing selected yet" default).
/// </summary>
internal sealed class DynamicAppFilter : INotificationFilter
{
    public NotificationFilterResult Evaluate(ToastNotificationEvent notification)
    {
        var allow = ForwardingApps.ReadAllowList();
        if (allow.Count == 0)
            return new NotificationFilterResult(true, null);

        return allow.Contains(notification.SourceApp, StringComparer.OrdinalIgnoreCase)
            ? new NotificationFilterResult(true, null)
            : new NotificationFilterResult(false, "Not in allow list");
    }
}
