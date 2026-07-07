using System.Runtime.Versioning;
using LocalOpsBot.Core.Notifications;
using LocalOpsBot.Infrastructure.Notifications;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Windows.UI.Notifications.Management;

namespace LocalOpsBot.Tray.Services;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed class ToastPollingService : BackgroundService
{
    private readonly WindowsToastNotificationListener _listener;
    private readonly INotificationFilter _filter;
    private readonly ITextMasker _masker;
    private readonly INotificationBridgeClient _bridge;
    private readonly ILogger<ToastPollingService> _logger;

    public ToastPollingService(
        WindowsToastNotificationListener listener,
        INotificationFilter filter,
        ITextMasker masker,
        INotificationBridgeClient bridge,
        ILogger<ToastPollingService> logger)
    {
        _listener = listener;
        _filter = filter;
        _masker = masker;
        _bridge = bridge;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Toast polling service starting");

        var accessStatus = await _listener.RequestAccessAsync();
        if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
        {
            _logger.LogWarning("Toast notification access denied: {Status}", accessStatus);
            return;
        }

        _logger.LogInformation("Toast notification access granted");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(3000, ct);
                var notifications = await _listener.PollAsync(ct);

                foreach (var notification in notifications)
                {
                    var filterResult = _filter.Evaluate(notification);
                    var sensitivity = filterResult.Allowed ? NotificationSensitivity.Normal : NotificationSensitivity.Blocked;

                    var masked = notification with
                    {
                        Title = _masker.Mask(notification.Title ?? string.Empty),
                        Body = _masker.Mask(notification.Body ?? string.Empty),
                        Sensitivity = sensitivity
                    };

                    await _bridge.SendAsync(masked, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Toast polling failed");
            }
        }
    }
}
