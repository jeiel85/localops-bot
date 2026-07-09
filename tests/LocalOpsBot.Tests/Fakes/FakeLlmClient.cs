using LocalOpsBot.Core.Advisor;

namespace LocalOpsBot.Tests.Fakes;

public sealed class FakeLlmClient : ILlmClient
{
    public LlmResult NextResult { get; set; } = new(true, "Everything looks fine.", null);
    public string? LastPrompt { get; private set; }

    public Task<LlmResult> GenerateAsync(string prompt, CancellationToken ct)
    {
        LastPrompt = prompt;
        return Task.FromResult(NextResult);
    }
}
