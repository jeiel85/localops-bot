namespace LocalOpsBot.Core.Advisor;

/// <summary>Outcome of a single LLM completion. Failures are returned, not thrown, so callers
/// can show a friendly message (server down, model not pulled, timeout) instead of crashing.</summary>
public sealed record LlmResult(bool Ok, string Text, string? Error);

/// <summary>A minimal local-LLM text-completion client (implemented against Ollama's HTTP API).</summary>
public interface ILlmClient
{
    Task<LlmResult> GenerateAsync(string prompt, CancellationToken ct);
}
