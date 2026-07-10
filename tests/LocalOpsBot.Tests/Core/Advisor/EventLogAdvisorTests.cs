using System;
using System.Threading;
using System.Threading.Tasks;
using LocalOpsBot.Core.Advisor;
using LocalOpsBot.Core.Monitoring;
using LocalOpsBot.Tests.Fakes;
using Xunit;

namespace LocalOpsBot.Tests.Core.Advisor;

public sealed class EventLogAdvisorTests
{
    private static WindowsEventLogItem Event() =>
        new("Application", 42, 1000, "MyApp", "Error",
            new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero), "PC", "The process failed.");

    [Fact]
    public async Task Interpret_returns_trimmed_llm_text_on_success()
    {
        var llm = new FakeLlmClient { NextResult = new(true, "  It means X. Check Y.  ", null) };
        var advisor = new EventLogAdvisor(new LlmAdvisorOptions { Language = "English" }, llm);

        var note = await advisor.InterpretAsync(Event(), CancellationToken.None);

        Assert.Equal("It means X. Check Y.", note);
    }

    [Fact]
    public async Task Interpret_returns_null_when_llm_fails()
    {
        var llm = new FakeLlmClient { NextResult = new(false, "", "server down") };
        var advisor = new EventLogAdvisor(new LlmAdvisorOptions { Language = "English" }, llm);

        Assert.Null(await advisor.InterpretAsync(Event(), CancellationToken.None));
    }

    [Fact]
    public async Task Prompt_includes_event_details_and_reply_language()
    {
        var llm = new FakeLlmClient();
        var advisor = new EventLogAdvisor(new LlmAdvisorOptions { Language = "Korean" }, llm);

        await advisor.InterpretAsync(Event(), CancellationToken.None);

        Assert.Contains("Error", llm.LastPrompt);
        Assert.Contains("MyApp", llm.LastPrompt);
        Assert.Contains("1000", llm.LastPrompt);
        Assert.Contains("The process failed.", llm.LastPrompt);
        Assert.Contains("Korean", llm.LastPrompt);
    }
}
