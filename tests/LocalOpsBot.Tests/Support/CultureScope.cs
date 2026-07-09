using System.Globalization;

namespace LocalOpsBot.Tests.Support;

/// <summary>
/// Temporarily pins <see cref="CultureInfo.CurrentCulture"/> and
/// <see cref="CultureInfo.CurrentUICulture"/> for the current async flow, restoring the
/// previous values on dispose. Use in tests that assert language-specific bot text so they
/// stay deterministic regardless of the host machine's display language.
/// </summary>
public sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _prevCulture;
    private readonly CultureInfo _prevUiCulture;

    public CultureScope(string culture)
    {
        _prevCulture = CultureInfo.CurrentCulture;
        _prevUiCulture = CultureInfo.CurrentUICulture;
        var c = CultureInfo.GetCultureInfo(culture);
        CultureInfo.CurrentCulture = c;
        CultureInfo.CurrentUICulture = c;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _prevCulture;
        CultureInfo.CurrentUICulture = _prevUiCulture;
    }
}
