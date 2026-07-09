using LocalOpsBot.Core.Alerts;
using LocalOpsBot.Core.Localization;

namespace LocalOpsBot.Core.Commands;

public sealed class PolicyCommandHandler : ICommandHandler
{
    private readonly IStateStore _stateStore;
    private readonly AlertingOptions _options;

    public string CommandName => "policy";
    public string Description => "Show current alert policy settings";

    public PolicyCommandHandler(IStateStore stateStore, AlertingOptions options)
    {
        _stateStore = stateStore;
        _options = options;
    }

    public async Task<CommandResult> HandleAsync(BotCommand command, CancellationToken ct)
    {
        var mutedUntilStr = await _stateStore.GetAsync("alert.muted_until", ct);
        var mutedInfo = Strings.NoMuteActive;
        if (DateTime.TryParse(mutedUntilStr, out var mutedUntil) && DateTime.UtcNow < mutedUntil)
            mutedInfo = Strings.MutedUntilPolicy($"{mutedUntil:yyyy-MM-dd HH:mm}", $"{(mutedUntil - DateTime.UtcNow).TotalMinutes:F0}");

        var lines = new List<string>
        {
            $"<b>\ud83d\udce1 {Strings.AlertPolicyTitle}</b>\n",
            $"<b>{Strings.MuteStateLabel}:</b> {HtmlEscape(mutedInfo)}",
            $"<b>{Strings.DedupWindowLabel}:</b> {_options.DedupWindowSeconds}s",
            $"<b>{Strings.MaxPerMinLabel}:</b> {_options.MaxMessagesPerMinute}",
            $"<b>{Strings.MaxPerHourLabel}:</b> {_options.MaxMessagesPerHour}",
            $"<b>{Strings.RecoveryAlertsLabel}:</b> {(_options.SendRecoveryAlerts ? Strings.On : Strings.Off)}",
            $"<b>{Strings.CriticalBypassLabel}:</b> {(_options.CriticalAlertsBypassMute ? Strings.Yes : Strings.No)}",
            "",
            Strings.PolicyTip
        };

        return new CommandResult(true, string.Join("\n", lines));
    }

    private static string HtmlEscape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
