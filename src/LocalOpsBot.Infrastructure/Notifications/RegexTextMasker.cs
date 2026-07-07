using System.Text.RegularExpressions;
using LocalOpsBot.Core.Notifications;

namespace LocalOpsBot.Infrastructure.Notifications;

public sealed class RegexTextMasker : ITextMasker
{
    private readonly IReadOnlyList<Regex> _patterns;

    public RegexTextMasker(IReadOnlyList<string> patterns)
    {
        _patterns = patterns.Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase)).ToList();
    }

    public string Mask(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var result = input;
        foreach (var regex in _patterns)
            result = regex.Replace(result, MatchEvaluator);
        return result;
    }

    private static string MatchEvaluator(Match match)
    {
        var val = match.Value;
        var sepIdx = val.IndexOfAny(new[] { ':', '=' });
        if (sepIdx >= 0)
        {
            var prefix = val[..(sepIdx + 1)];
            return prefix + new string('*', Math.Min(val.Length - sepIdx - 1, 20));
        }
        return new string('*', Math.Min(val.Length, 20));
    }
}
