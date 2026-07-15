using LocalOpsBot.Core.Devices;
using LocalOpsBot.Protocol.Messaging;

namespace LocalOpsBot.Core.Plugins;

public sealed record DeviceContext(
    DeviceRecord Device,
    IServiceProvider Services);

public sealed record PluginActivationResult(
    bool CanActivate,
    string? Reason = null);

public sealed record PluginHandleResult(
    bool Handled,
    string? ErrorCode = null,
    string? Error = null);

public interface IHomebasePlugin
{
    string PluginId { get; }
    Version PluginVersion { get; }
    IReadOnlySet<string> IncomingMessageTypes { get; }
    IReadOnlySet<string> OutgoingMessageTypes { get; }
    IReadOnlySet<string> RequiredCapabilities { get; }

    ValueTask<PluginActivationResult> CanActivateAsync(
        DeviceContext context,
        CancellationToken ct);

    Task StartAsync(DeviceContext context, CancellationToken ct);

    Task<PluginHandleResult> HandleAsync(
        DeviceContext context,
        DeviceEnvelope envelope,
        CancellationToken ct);

    Task StopAsync(DeviceContext context, CancellationToken ct);
}
