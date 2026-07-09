using LocalOpsBot.Core.Advisor;
using LocalOpsBot.Tests.Fakes;
using Xunit;

namespace LocalOpsBot.Tests.Core.Advisor;

public sealed class PcStateAdvisorTests
{
    private static PcStateAdvisor NewAdvisor(LlmAdvisorOptions options, FakeLlmClient llm) =>
        new(options, llm,
            new FakeSystemMetricsCollector(),
            new FakeDiskCollector(),
            new FakeNetworkStatusChecker(),
            new FakeAlertStore());

    [Fact]
    public async Task Advise_when_disabled_returns_not_ok()
    {
        var advisor = NewAdvisor(new LlmAdvisorOptions { Enabled = false }, new FakeLlmClient());

        var result = await advisor.AdviseAsync(CancellationToken.None);

        Assert.False(result.Ok);
        Assert.Contains("turned off", result.Error!);
    }

    [Fact]
    public async Task Summary_includes_core_metrics()
    {
        var advisor = NewAdvisor(new LlmAdvisorOptions(), new FakeLlmClient());

        var summary = await advisor.BuildStateSummaryAsync(CancellationToken.None);

        Assert.Contains("CPU", summary);
        Assert.Contains("RAM", summary);
        Assert.Contains("Disk", summary);
    }

    [Fact]
    public async Task Advise_feeds_summary_into_prompt_and_returns_llm_text()
    {
        var llm = new FakeLlmClient { NextResult = new(true, "Looks healthy.", null) };
        var advisor = NewAdvisor(new LlmAdvisorOptions(), llm);

        var result = await advisor.AdviseAsync(CancellationToken.None);

        Assert.True(result.Ok);
        Assert.Equal("Looks healthy.", result.Text);
        Assert.NotNull(llm.LastPrompt);
        Assert.Contains("Current readings", llm.LastPrompt!);
        Assert.Contains("CPU", llm.LastPrompt!);
    }

    [Fact]
    public async Task Advise_propagates_llm_failure()
    {
        var llm = new FakeLlmClient { NextResult = new(false, "", "server down") };
        var advisor = NewAdvisor(new LlmAdvisorOptions(), llm);

        var result = await advisor.AdviseAsync(CancellationToken.None);

        Assert.False(result.Ok);
        Assert.Equal("server down", result.Error);
    }
}
