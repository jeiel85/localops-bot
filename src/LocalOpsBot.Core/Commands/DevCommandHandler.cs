using LocalOpsBot.Core.Localization;
using LocalOpsBot.Core.Monitoring;

namespace LocalOpsBot.Core.Commands;

public sealed class DevCommandHandler : ICommandHandler
{
    private readonly IHttpEndpointMonitor _httpMonitor;
    private readonly IReadOnlyList<HttpEndpointConfig> _endpoints;

    public string CommandName => "dev";
    public string Description => "HTTP endpoint health status";

    public DevCommandHandler(
        IHttpEndpointMonitor httpMonitor,
        IEnumerable<HttpEndpointConfig> endpoints)
    {
        _httpMonitor = httpMonitor;
        _endpoints = endpoints.ToList();
    }

    public async Task<CommandResult> HandleAsync(BotCommand command, CancellationToken ct)
    {
        if (_endpoints.Count == 0)
            return new CommandResult(false, Strings.NoHttpEndpoints);

        var lines = new List<string> { $"<b>\ud83d\udcbb {Strings.DevEndpointTitle}</b>\n" };

        foreach (var ep in _endpoints)
        {
            var result = await _httpMonitor.CheckAsync(ep, ct);
            var icon = result.Success ? "\u2705" : "\u274c";
            var ms = result.ResponseTimeMs.HasValue ? $" ({result.ResponseTimeMs}ms)" : "";
            lines.Add($"{icon} <b>{HtmlEscape(result.Name)}</b>{ms}");
            lines.Add($"  URL: {HtmlEscape(result.Url)}");
            lines.Add($"  {Strings.StatusWord}: {(result.Success ? Strings.Ok : result.Error)}");
            lines.Add("");
        }

        return new CommandResult(true, string.Join("\n", lines));
    }

    private static string HtmlEscape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
