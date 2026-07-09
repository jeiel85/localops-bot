using System.Net;
using LocalOpsBot.Infrastructure.Telegram;
using LocalOpsBot.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace LocalOpsBot.Tests.Infrastructure.Telegram;

public sealed class TelegramClientTests
{
    private static TelegramClient CreateClient(
        FakeHttpMessageHandler handler, string token = "123:test")
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var options = Options.Create(new TelegramOptions { BotToken = token });
        return new TelegramClient(http, options);
    }

    [Fact]
    public void IsConfigured_true_when_token_present()
    {
        Assert.True(CreateClient(new FakeHttpMessageHandler(), "123:test").IsConfigured);
    }

    [Fact]
    public async Task Unconfigured_client_does_not_throw_at_construction_but_send_throws()
    {
        var client = CreateClient(new FakeHttpMessageHandler(), token: ""); // no crash here

        Assert.False(client.IsConfigured);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.SendMessageAsync(1, "x", null, CancellationToken.None));
    }

    [Fact]
    public async Task SendMessage_encodes_chat_id_and_text()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""");
        var client = CreateClient(handler);

        await client.SendMessageAsync(123, "hello", null, CancellationToken.None);

        var request = handler.Requests.Single();
        var body = await request.Content!.ReadAsStringAsync();
        Assert.Contains("\"chat_id\":123", body);
        Assert.Contains("\"text\":\"hello\"", body);
        Assert.Contains("bot123:test/sendMessage", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendMessage_with_options_sends_parse_mode()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""");
        var client = CreateClient(handler);

        var opts = new TelegramSendOptions(ParseMode: "Markdown", DisableWebPagePreview: true);
        await client.SendMessageAsync(1, "hi", opts, CancellationToken.None);

        var body = await handler.Requests.Single().Content!.ReadAsStringAsync();
        Assert.Contains("\"parse_mode\":\"Markdown\"", body);
    }

    [Fact]
    public async Task SendMessage_without_options_applies_default_html_parse_mode()
    {
        // Regression: command replies pass null options; they must still receive a
        // parse_mode so <b>/<code> tags render instead of showing up as raw text.
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, """{"ok":true,"result":{"message_id":1}}""");
        var client = CreateClient(handler);

        await client.SendMessageAsync(1, "<b>hi</b>", null, CancellationToken.None);

        var body = await handler.Requests.Single().Content!.ReadAsStringAsync();
        Assert.Contains("\"parse_mode\":\"HTML\"", body);
    }

    [Fact]
    public async Task SendMessage_failure_throws_TelegramApiException()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.Unauthorized, """{"ok":false,"error_code":401,"description":"Unauthorized"}""");
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsAsync<TelegramApiException>(() =>
            client.SendMessageAsync(1, "x", null, CancellationToken.None));

        Assert.Equal(401, ex.HttpStatusCode);
    }

    [Fact]
    public async Task GetUpdates_returns_updates()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, """
            {"ok":true,"result":[{"update_id":100,"message":{"message_id":1,"chat":{"id":1,"type":"private"},"date":1720320000,"text":"/ping"}}]}
            """);
        var client = CreateClient(handler);

        var updates = await client.GetUpdatesAsync(null, 30, CancellationToken.None);

        var update = Assert.Single(updates);
        Assert.Equal(100, update.UpdateId);
        Assert.Equal("/ping", update.Message!.Text);
    }

    [Fact]
    public async Task GetUpdates_passes_offset()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, """{"ok":true,"result":[]}""");
        var client = CreateClient(handler);

        await client.GetUpdatesAsync(500, 30, CancellationToken.None);

        var body = await handler.Requests.Single().Content!.ReadAsStringAsync();
        Assert.Contains("\"offset\":500", body);
    }

    [Fact]
    public async Task GetUpdates_timeout_is_sent()
    {
        var handler = new FakeHttpMessageHandler();
        handler.QueueResponse(HttpStatusCode.OK, """{"ok":true,"result":[]}""");
        var client = CreateClient(handler);

        await client.GetUpdatesAsync(null, 120, CancellationToken.None);

        var body = await handler.Requests.Single().Content!.ReadAsStringAsync();
        Assert.Contains("\"timeout\":120", body);
    }
}
