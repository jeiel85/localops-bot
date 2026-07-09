using System.Globalization;
using System.Runtime.CompilerServices;

namespace LocalOpsBot.Tests.Support;

/// <summary>
/// Pins the test assembly's default culture to English so locale-dependent bot text is asserted
/// deterministically regardless of the host machine's display language (dev machines here run
/// under ko-KR). Tests that exercise a specific language (e.g. Korean) still override
/// <see cref="CultureInfo.CurrentUICulture"/> explicitly via <see cref="CultureScope"/>.
/// </summary>
internal static class TestCultureInitializer
{
    [ModuleInitializer]
    internal static void SetInvariantEnglishDefault()
    {
        var en = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = en;
        CultureInfo.DefaultThreadCurrentUICulture = en;
    }
}
