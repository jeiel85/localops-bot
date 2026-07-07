using System.Runtime.Versioning;
using LocalOpsBot.Core.Notifications;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace LocalOpsBot.Tray.Services;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed class WindowsToastNotificationListener
{
    private readonly HashSet<uint> _seenIds = new();
    private UserNotificationListener? _listener;
    private const int MaxSeenIds = 1000;

    public async Task<UserNotificationListenerAccessStatus> RequestAccessAsync()
    {
        _listener = UserNotificationListener.Current;
        var status = await _listener.RequestAccessAsync();
        return status;
    }

    public async Task<IReadOnlyList<ToastNotificationEvent>> PollAsync(CancellationToken ct)
    {
        if (_listener == null) return Array.Empty<ToastNotificationEvent>();

        var results = new List<ToastNotificationEvent>();
        var notifications = await _listener.GetNotificationsAsync(NotificationKinds.Toast);

        foreach (var n in notifications)
        {
            if (_seenIds.Contains(n.Id)) continue;
            _seenIds.Add(n.Id);

            var (title, body) = ExtractText(n);
            var sourceApp = n.AppInfo?.DisplayInfo?.DisplayName ?? "Unknown";

            var toastEvent = new ToastNotificationEvent(
                Guid.NewGuid().ToString("N"),
                sourceApp,
                title,
                body,
                n.CreationTime.ToUniversalTime(),
                n.Id.ToString(),
                NotificationSensitivity.Normal);

            results.Add(toastEvent);
        }

        TrimSeenIds();
        return results;
    }

    private static (string? Title, string? Body) ExtractText(UserNotification notification)
    {
        try
        {
            var binding = notification.Notification?.Visual?.GetBinding(KnownNotificationBindings.ToastGeneric);
            if (binding == null) return (null, null);

            var texts = binding.GetTextElements();
            var title = texts.Count > 0 ? texts[0]?.Text : null;
            var body = texts.Count > 1 ? string.Join(" ", texts.Skip(1).Select(t => t?.Text).Where(t => t != null)) : null;
            return (title, body);
        }
        catch
        {
            return (null, null);
        }
    }

    private void TrimSeenIds()
    {
        if (_seenIds.Count > MaxSeenIds)
        {
            var toRemove = _seenIds.Count - (MaxSeenIds / 2);
            var oldest = _seenIds.Take(toRemove).ToList();
            foreach (var id in oldest) _seenIds.Remove(id);
        }
    }
}
