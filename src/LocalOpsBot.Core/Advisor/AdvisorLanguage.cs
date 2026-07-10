using System.Globalization;

namespace LocalOpsBot.Core.Advisor;

/// <summary>
/// Resolves the reply language for LLM prompts: the explicit config value if set, else the bot's
/// current UI language (the Agent sets it from <c>agent:language</c>, or the OS display language when
/// that is empty), else English. Shared by the advisors so they answer in the same language.
/// </summary>
internal static class AdvisorLanguage
{
    public static string Resolve(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();
        try
        {
            var ui = CultureInfo.CurrentUICulture;
            var neutral = ui.IsNeutralCulture ? ui : ui.Parent;
            var name = neutral.EnglishName;
            return string.IsNullOrWhiteSpace(name) || name.StartsWith("Invariant", StringComparison.Ordinal)
                ? "English" : name;
        }
        catch { return "English"; }
    }
}
