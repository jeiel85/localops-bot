using LocalOpsBot.Core.Localization;
using LocalOpsBot.Core.Monitoring;

namespace LocalOpsBot.Core.Notifications;

public static class BootMessageFormatter
{
    public static string Format(HostInfoRecord host, DateTimeOffset now)
    {
        var localNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "Korea Standard Time");
        var uptime = host.Uptime;
        var uptimeStr = uptime.Days > 0
            ? $"{uptime.Days}d {uptime.Hours:00}h {uptime.Minutes:00}m"
            : $"{(int)uptime.TotalHours:00}h {uptime.Minutes:00}m {uptime.Seconds:00}s";

        var ip = host.PrimaryIPv4 ?? Strings.Unknown;

        return $"🟢 <b>{Strings.BootDetected(HtmlEscape(host.MachineName))}</b>\n\n"
             + $"{Strings.TimeLabel}: <code>{localNow:yyyy-MM-dd HH:mm:ss 'KST'}</code>\n"
             + $"IP: <code>{HtmlEscape(ip)}</code>\n"
             + $"{Strings.Uptime}: <code>{uptimeStr}</code>";
    }

    private static string HtmlEscape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
