namespace LocalOpsBot.Core.Commands;

public sealed class PingCommandHandler : ICommandHandler
{
    public string CommandName => "ping";
    public string Description => "Bot health check";

    public Task<CommandResult> HandleAsync(BotCommand command, CancellationToken ct)
    {
        var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            command.ReceivedAt, "Korea Standard Time");
        // 'KST' is a quoted literal — an unquoted K is the offset designator and would render "+09:00ST".
        var response = $"pong\n{Environment.MachineName} | {now:yyyy-MM-dd HH:mm:ss 'KST'}";

        return Task.FromResult(new CommandResult(true, response));
    }
}
