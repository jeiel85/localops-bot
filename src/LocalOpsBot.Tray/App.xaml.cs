using System.Windows;
using LocalOpsBot.Tray.Services;

namespace LocalOpsBot.Tray;

public partial class App : Application
{
    private TrayIconManager? _trayIcon;
    private NotificationForwardingHost? _forwardingHost;

    /// <summary>
    /// Set just before a programmatic <see cref="Application.Shutdown()"/> (e.g. an auto-update
    /// restart) so windows that normally cancel their close — the dashboard hides instead of
    /// closing — let the shutdown proceed cleanly.
    /// </summary>
    internal static bool IsShuttingDown { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _trayIcon = new TrayIconManager();

        // Start toast-notification forwarding if the user has enabled it. This is the tray half of
        // the pipeline (listener → pipe → Agent → Telegram); it stays dormant until enabled.
        _forwardingHost = new NotificationForwardingHost();
        _forwardingHost.StartIfEnabled();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _forwardingHost?.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
