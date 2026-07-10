using LocalOpsBot.Core.Monitoring;

namespace LocalOpsBot.Core.Advisor;

public interface IEventLogAdvisor
{
    /// <summary>
    /// Asks the local LLM to explain an event in a couple of sentences plus one concrete action, in
    /// the bot language. Returns <c>null</c> when interpretation is unavailable (LLM off/unreachable)
    /// so the caller can still send the alert without it.
    /// </summary>
    Task<string?> InterpretAsync(WindowsEventLogItem e, CancellationToken ct);
}

/// <summary>
/// Runs a raised Windows event through the local LLM to add a plain-language "what this means +
/// what to check" note to the alert. Gated by <see cref="EventLogOptions.LlmInterpret"/> at the
/// call site; failures are swallowed (returns null) so a down LLM never blocks the alert.
/// </summary>
public sealed class EventLogAdvisor : IEventLogAdvisor
{
    private readonly LlmAdvisorOptions _options;
    private readonly ILlmClient _llm;

    public EventLogAdvisor(LlmAdvisorOptions options, ILlmClient llm)
    {
        _options = options;
        _llm = llm;
    }

    public async Task<string?> InterpretAsync(WindowsEventLogItem e, CancellationToken ct)
    {
        try
        {
            var result = await _llm.GenerateAsync(BuildPrompt(e), ct);
            return result.Ok && !string.IsNullOrWhiteSpace(result.Text) ? result.Text.Trim() : null;
        }
        catch
        {
            // Interpretation is best-effort context — never let it break the alert.
            return null;
        }
    }

    // Cap the message fed to the model so a pathologically long event can't bloat the prompt.
    private const int MaxMessageChars = 1000;

    private string BuildPrompt(WindowsEventLogItem e) =>
        "You are a concise Windows troubleshooter. A Windows Event Log entry was raised. In 1-3 short " +
        "sentences, explain what it most likely means and one concrete thing to check or do. If it is " +
        "usually harmless, say so. Do not invent details beyond what is given. " +
        $"Write the reply in {AdvisorLanguage.Resolve(_options.Language)}.\n\n" +
        $"Level: {e.Level}\n" +
        $"Source: {e.ProviderName ?? e.LogName}\n" +
        $"Event ID: {e.EventId}\n" +
        $"Log: {e.LogName}\n" +
        $"Message: {Truncate(e.Message, MaxMessageChars)}";

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= max ? value : value.Substring(0, max) + "…";
    }
}
