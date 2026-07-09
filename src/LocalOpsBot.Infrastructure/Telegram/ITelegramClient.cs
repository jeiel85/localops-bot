using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalOpsBot.Infrastructure.Telegram;

public sealed class UnixTimestampConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64());

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.ToUnixTimeSeconds());
}

public interface ITelegramClient
{
    /// <summary>
    /// False when no bot token is configured yet (a fresh install before onboarding sets it).
    /// Consumers should idle instead of calling the API, which would otherwise fail.
    /// </summary>
    bool IsConfigured { get; }

    Task SendMessageAsync(long chatId, string text, TelegramSendOptions? options, CancellationToken ct);
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken ct);
}

public sealed record TelegramSendOptions(
    string? ParseMode = "HTML",
    bool DisableWebPagePreview = true,
    bool DisableNotification = false);

public sealed record TelegramUpdate(long UpdateId, TelegramMessage? Message);
public sealed record TelegramMessage(long MessageId, TelegramChat Chat, TelegramUser? From, string? Text, [property: JsonConverter(typeof(UnixTimestampConverter))] DateTimeOffset Date);
public sealed record TelegramChat(long Id, string Type, string? Title, string? Username);
public sealed record TelegramUser(long Id, bool IsBot, string? Username, string? FirstName);
