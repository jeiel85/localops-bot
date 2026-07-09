using LocalOpsBot.Core.Advisor;

namespace LocalOpsBot.Core.Commands;

/// <summary>
/// <c>/advise</c> — asks the local LLM for plain-language health advice about the PC's current
/// state (e.g. "CPU has been pegged, check for a runaway process"). Degrades gracefully with a
/// setup hint when no local LLM is reachable.
/// </summary>
public sealed class AdviseCommandHandler : ICommandHandler
{
    private readonly IPcStateAdvisor _advisor;

    public string CommandName => "advise";
    public string Description => "AI health advice for your PC (local LLM)";

    public AdviseCommandHandler(IPcStateAdvisor advisor) => _advisor = advisor;

    public async Task<CommandResult> HandleAsync(BotCommand command, CancellationToken ct)
    {
        var result = await _advisor.AdviseAsync(ct);

        if (!result.Ok)
        {
            return new CommandResult(true,
                "<b>\U0001f9e0 AI Advisor</b>\n\n" +
                $"⚠️ {HtmlEscape(result.Error ?? "The advisor is unavailable.")}\n\n" +
                "Make sure a local LLM server (Ollama) is running and the model is pulled. " +
                "Use /llm to check the server, or run <code>ollama pull llama3.2:1b</code>.");
        }

        // The model's text is freeform, so escape it — Telegram HTML mode rejects stray &, <, >.
        return new CommandResult(true, "<b>\U0001f9e0 AI Advisor</b>\n\n" + HtmlEscape(result.Text));
    }

    private static string HtmlEscape(string value)
        => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
