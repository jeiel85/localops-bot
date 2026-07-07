using LocalOpsBot.Core.Notifications;

namespace LocalOpsBot.Infrastructure.Notifications;

public sealed class NotificationFilter : INotificationFilter
{
    private readonly string _mode;
    private readonly HashSet<string> _allowApps;
    private readonly HashSet<string> _blockApps;
    private readonly ITextMasker _masker;
    private readonly bool _maskingEnabled;

    public NotificationFilter(
        string mode,
        IReadOnlyList<string> allowApps,
        IReadOnlyList<string> blockApps,
        ITextMasker masker,
        bool maskingEnabled)
    {
        _mode = mode;
        _allowApps = new HashSet<string>(allowApps, StringComparer.OrdinalIgnoreCase);
        _blockApps = new HashSet<string>(blockApps, StringComparer.OrdinalIgnoreCase);
        _masker = masker;
        _maskingEnabled = maskingEnabled;
    }

    public NotificationFilterResult Evaluate(ToastNotificationEvent notification)
    {
        if (_blockApps.Contains(notification.SourceApp))
            return new NotificationFilterResult(false, "Blocked app");

        if (_mode.Equals("AllowList", StringComparison.OrdinalIgnoreCase)
            && _allowApps.Count > 0
            && !_allowApps.Contains(notification.SourceApp))
            return new NotificationFilterResult(false, "Not in allow list");

        return new NotificationFilterResult(true, null);
    }
}
