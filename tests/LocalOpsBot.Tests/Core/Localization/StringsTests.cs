using System.Globalization;
using LocalOpsBot.Core.Localization;
using Xunit;

namespace LocalOpsBot.Tests.Core.Localization;

public sealed class StringsTests
{
    private static T WithCulture<T>(string culture, Func<T> f)
    {
        var prev = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            return f();
        }
        finally { CultureInfo.CurrentUICulture = prev; }
    }

    [Fact]
    public void Korean_culture_returns_korean()
    {
        Assert.Equal("알 수 없음", WithCulture("ko-KR", () => Strings.Unknown));
    }

    [Fact]
    public void English_culture_returns_english()
    {
        Assert.Equal("unknown", WithCulture("en-US", () => Strings.Unknown));
    }

    [Fact]
    public void Unknown_command_is_localized_both_ways()
    {
        Assert.Contains("알 수 없는 명령", WithCulture("ko-KR", () => Strings.UnknownCommand("advice")));
        Assert.Contains("Unknown command", WithCulture("en-US", () => Strings.UnknownCommand("advice")));
    }
}
