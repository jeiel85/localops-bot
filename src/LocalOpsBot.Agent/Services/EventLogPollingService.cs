using System.Runtime.Versioning;
using LocalOpsBot.Core.Advisor;
using LocalOpsBot.Core.Alerts;
using LocalOpsBot.Core.Localization;
using LocalOpsBot.Core.Monitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalOpsBot.Agent.Services;

[SupportedOSPlatform("windows")]
public sealed class EventLogPollingService : BackgroundService
{
    private readonly IEventLogWatcher _watcher;
    private readonly EventLogOptions _options;
    private readonly EventAlertPolicy _policy;
    private readonly IAlertDispatcher _dispatcher;
    private readonly CollectorOptions _collectors;
    private readonly IEventLogAdvisor _advisor;
    private readonly ILogger<EventLogPollingService> _logger;
    private readonly string _machineName = Environment.MachineName;

    // Bound the LLM calls per poll cycle so a burst of distinct errors can't stall the loop.
    private const int MaxLlmInterpretationsPerCycle = 3;

    public EventLogPollingService(
        IEventLogWatcher watcher,
        EventLogOptions options,
        IAlertDispatcher dispatcher,
        CollectorOptions collectors,
        IEventLogAdvisor advisor,
        ILogger<EventLogPollingService> logger)
    {
        _watcher = watcher;
        _options = options;
        _policy = new EventAlertPolicy(options);
        _dispatcher = dispatcher;
        _collectors = collectors;
        _advisor = advisor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Event log polling disabled");
            return;
        }

        _logger.LogInformation("Event log polling started: {Logs}", string.Join(", ", _options.Logs));
        var interval = TimeSpan.FromSeconds(Math.Max(5, _collectors.EventLogPollingIntervalSeconds));

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, ct);
                var events = await _watcher.PollAsync(_options, ct);
                if (events.Count == 0) continue;

                _logger.LogInformation("Event log: {Count} new event(s)", events.Count);

                var alerted = 0;
                var interpreted = 0;
                foreach (var e in events)
                {
                    // Level gate + repeat-suppression: keep recurring/low-priority events from flooding.
                    if (!_policy.ShouldAlert(e, DateTimeOffset.UtcNow)) continue;
                    alerted++;

                    var severity = string.Equals(e.Level, "Critical", StringComparison.OrdinalIgnoreCase)
                        ? AlertSeverity.Critical
                        : AlertSeverity.Warning;

                    var title = Strings.EventAlertTitle(Strings.EventLevel(e.Level), e.ProviderName ?? e.LogName);
                    var message = Truncate(e.Message, _options.MessageMaxChars);
                    var body = $"{Strings.EventLogLabel}: {e.LogName} · {Strings.EventIdLabel}: {e.EventId} · {e.TimeCreated:yyyy-MM-dd HH:mm:ss}"
                             + (string.IsNullOrWhiteSpace(message) ? string.Empty : $"\n{message}");

                    // Optional: ask the local LLM what this event means and what to check, and append
                    // it. Best-effort — a down/disabled LLM just returns null and the alert still sends.
                    // Capped per cycle so a burst of distinct errors can't stall the loop on LLM calls.
                    if (_options.LlmInterpret && interpreted < MaxLlmInterpretationsPerCycle)
                    {
                        interpreted++;
                        var note = await _advisor.InterpretAsync(e, ct);
                        if (!string.IsNullOrWhiteSpace(note))
                            body += $"\n\n\U0001f4a1 {note}";
                    }

                    await _dispatcher.DispatchAsync(new AlertEvent(
                        Guid.NewGuid().ToString("N"),
                        "event_log",
                        severity,
                        title,
                        body,
                        $"eventlog:{e.LogName}:{e.RecordId}",
                        e.MachineName ?? _machineName,
                        DateTimeOffset.UtcNow), ct);
                }

                if (alerted < events.Count)
                    _logger.LogInformation(
                        "Event log: alerted {Alerted}/{Total} (others below alert level or repeat-suppressed)",
                        alerted, events.Count);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event log polling failed");
            }
        }
    }

    private static string Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        value = value.Replace("\r\n", " ").Replace('\n', ' ').Trim();
        return value.Length <= max ? value : value.Substring(0, max) + "…";
    }
}
