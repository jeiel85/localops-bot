using LocalOpsBot.Core.Localization;
using LocalOpsBot.Core.Updates;

namespace LocalOpsBot.Core.Commands;

public sealed class UpdateCommandHandler : ICommandHandler
{
    private readonly UpdateService _updater;

    public string CommandName => "update";
    public string Description => "Check for and apply updates";

    public UpdateCommandHandler(UpdateService updater) => _updater = updater;

    public async Task<CommandResult> HandleAsync(BotCommand command, CancellationToken ct)
    {
        var currentVer = _updater.GetCurrentVersionString();

        try
        {
            var info = await _updater.CheckForUpdateAsync(ct);
            if (info == null)
                return new CommandResult(true, $"<b>\u2705 {Strings.UpToDateTitle}</b>\n{Strings.CurrentVersionLabel}: {currentVer}");

            // Download + verify synchronously so we can report success or failure back to
            // the user. Only if that succeeds do we launch the (self-restarting) apply step.
            try
            {
                var zip = await _updater.DownloadUpdateAsync(info, null, ct);
                _updater.ApplyUpdate(zip);
            }
            catch (Exception ex)
            {
                return new CommandResult(true,
                    $"<b>\u26a0\ufe0f {Strings.UpdateFailedTitle}</b>\n" +
                    Strings.UpdateFailedBody($"{info.Version}", ex.Message, currentVer),
                    Error: ex.Message);
            }

            return new CommandResult(true, string.Join("\n", new[]
            {
                $"<b>\ud83d\udce1 {Strings.UpdateDownloadedTitle($"{info.Version}")}</b>",
                $"{Strings.CurrentLabel}: {currentVer}",
                $"{Strings.PublishedLabel}: {info.PublishedAt:yyyy-MM-dd}",
                "",
                Strings.InstallingNow
            }));
        }
        catch (UpdateCheckException ex)
        {
            return new CommandResult(true,
                $"<b>\u26a0\ufe0f {Strings.UpdateCheckFailedTitle}</b>\n" +
                $"{ex.Kind}: {ex.Message}");
        }
    }
}
