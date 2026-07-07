namespace LocalOpsBot.Core.Notifications;

public interface INotificationBridgeClient
{
    Task SendAsync(ToastNotificationEvent notification, CancellationToken ct);
}

public interface INotificationBridgeServer
{
    event Action<ToastNotificationEvent>? NotificationReceived;
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
}
