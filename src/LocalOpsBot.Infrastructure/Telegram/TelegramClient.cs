using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace LocalOpsBot.Infrastructure.Telegram;

public sealed class TelegramClient : ITelegramClient
{
    private readonly HttpClient _http;
    private readonly string? _baseUrl; // null until a bot token is configured
    private readonly TelegramSendOptions _defaultSendOptions;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool IsConfigured => _baseUrl is not null;

    public TelegramClient(HttpClient http, IOptions<TelegramOptions> options)
    {
        var token = options.Value.BotToken;
        // Support "ENV:VARNAME" indirection so the secret can live in an environment
        // variable instead of being written in plain text into the config file.
        if (!string.IsNullOrEmpty(token) && token.StartsWith("ENV:", StringComparison.Ordinal))
            token = Environment.GetEnvironmentVariable(token.Substring(4)) ?? string.Empty;

        _http = http;
        // No token yet (a fresh install before onboarding sets one): stay unconfigured and let
        // callers idle, rather than throwing here — that would crash-loop the Agent host at start.
        _baseUrl = string.IsNullOrWhiteSpace(token) ? null : $"https://api.telegram.org/bot{token}";
        // Applied whenever a caller passes no explicit options. Without this, command
        // replies went out with no parse_mode and their <b>/<code> tags showed up raw.
        _defaultSendOptions = new TelegramSendOptions(
            options.Value.ParseMode,
            options.Value.DisableWebPagePreview);
    }

    private void EnsureConfigured()
    {
        if (_baseUrl is null)
            throw new InvalidOperationException("Telegram bot token is not configured.");
    }

    public async Task SendMessageAsync(
        long chatId, string text, TelegramSendOptions? sendOptions, CancellationToken ct)
    {
        EnsureConfigured();
        var opts = sendOptions ?? _defaultSendOptions;
        var payload = new Dictionary<string, object?>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["parse_mode"] = opts.ParseMode,
            ["disable_web_page_preview"] = opts.DisableWebPagePreview,
            ["disable_notification"] = opts.DisableNotification
        };

        using var response = await _http.PostAsJsonAsync(
            $"{_baseUrl}/sendMessage", payload, JsonOpts, ct);

        if (!response.IsSuccessStatusCode)
            await ThrowOnError(response, "sendMessage");
    }

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        long? offset, int timeoutSeconds, CancellationToken ct)
    {
        EnsureConfigured();
        var payload = new Dictionary<string, object?>
        {
            ["offset"] = offset,
            ["timeout"] = timeoutSeconds,
            ["allowed_updates"] = new[] { "message" }
        };

        using var response = await _http.PostAsJsonAsync(
            $"{_baseUrl}/getUpdates", payload, JsonOpts, ct);

        if (!response.IsSuccessStatusCode)
        {
            await ThrowOnError(response, "getUpdates");
            return Array.Empty<TelegramUpdate>();
        }

        var wrapper = await response.Content.ReadFromJsonAsync<TelegramResponse>(JsonOpts, ct);
        return wrapper?.Result ?? Array.Empty<TelegramUpdate>();
    }

    private static async Task ThrowOnError(HttpResponseMessage response, string apiMethod)
    {
        var body = await response.Content.ReadAsStringAsync();

        string description;
        try
        {
            var err = JsonSerializer.Deserialize<TelegramErrorResponse>(body, JsonOpts);
            description = err?.Description ?? body;
        }
        catch
        {
            description = body;
        }

        throw new TelegramApiException(
            (int)response.StatusCode,
            $"Telegram {apiMethod} failed ({(int)response.StatusCode}): {description}",
            apiMethod);
    }

    private sealed record TelegramResponse(bool Ok, IReadOnlyList<TelegramUpdate>? Result);
    private sealed record TelegramErrorResponse(bool Ok, int ErrorCode, string? Description);
}
