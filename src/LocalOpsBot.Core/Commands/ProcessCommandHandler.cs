using LocalOpsBot.Core.Localization;
using LocalOpsBot.Core.Monitoring;

namespace LocalOpsBot.Core.Commands;

public sealed class ProcessCommandHandler : ICommandHandler
{
    private readonly IProcessCollector _processCollector;
    private readonly IReadOnlyList<ProcessWatchConfig> _watches;

    public string CommandName => "process";
    public string Description => "Watched process status";

    public ProcessCommandHandler(
        IProcessCollector processCollector,
        IEnumerable<ProcessWatchConfig> watches)
    {
        _processCollector = processCollector;
        _watches = watches.ToList();
    }

    public async Task<CommandResult> HandleAsync(BotCommand command, CancellationToken ct)
    {
        if (_watches.Count == 0)
            return new CommandResult(false, Strings.NoProcessWatches);

        var results = await _processCollector.CollectAsync(_watches, ct);
        var lines = new List<string> { $"<b>\u2699 {Strings.ProcessWatchTitle}</b>\n" };

        foreach (var r in results)
        {
            var icon = r.IsRunning ? "\u2705" : "\u274c";
            lines.Add($"{icon} <b>{HtmlEscape(r.WatchName)}</b>");
            lines.Add($"  {Strings.ProcessLabel}: {string.Join(", ", r.ProcessNames)}");
            lines.Add($"  {Strings.StatusWord}: {(r.IsRunning ? Strings.Running : Strings.Missing)} ({Strings.Instances(r.InstanceCount)})");
            lines.Add("");
        }

        return new CommandResult(true, string.Join("\n", lines));
    }

    private static string HtmlEscape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
