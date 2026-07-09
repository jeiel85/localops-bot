using System.Diagnostics.Eventing.Reader;
using System.Runtime.Versioning;
using LocalOpsBot.Core.Monitoring;
using Microsoft.Extensions.Logging;

namespace LocalOpsBot.Infrastructure.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsEventLogWatcher : IEventLogWatcher
{
    // Per-log resume position for the alert poller. Reading resumes strictly AFTER this
    // bookmark, so each poll only scans events written since the last one — instead of
    // walking the whole log and skipping by RecordId every time. Mutated only by PollAsync
    // (the single background poller); ReadRecentAsync never touches it.
    private readonly Dictionary<string, EventBookmark?> _bookmarks = new();

    // Logs currently failing to read, so a persistent failure is logged once (not every poll).
    private readonly HashSet<string> _failingLogs = new(StringComparer.OrdinalIgnoreCase);

    private readonly ILogger<WindowsEventLogWatcher>? _logger;

    // Logger is optional so the watcher can still be constructed directly (e.g. in tests);
    // dependency injection supplies a real logger in the running Agent.
    public WindowsEventLogWatcher(ILogger<WindowsEventLogWatcher>? logger = null)
    {
        _logger = logger;
    }

    public Task<IReadOnlyList<WindowsEventLogItem>> PollAsync(EventLogOptions options, CancellationToken ct)
    {
        var results = new List<WindowsEventLogItem>();
        foreach (var logName in options.Logs)
        {
            ct.ThrowIfCancellationRequested();
            results.AddRange(ReadLog(logName, options));
        }
        return Task.FromResult<IReadOnlyList<WindowsEventLogItem>>(results);
    }

    public Task<IReadOnlyList<WindowsEventLogItem>> ReadRecentAsync(
        EventLogOptions options, int limit, CancellationToken ct)
    {
        var collected = new List<WindowsEventLogItem>();
        foreach (var logName in options.Logs)
        {
            ct.ThrowIfCancellationRequested();
            collected.AddRange(ReadRecentFromLog(logName, options, limit));
        }

        // Newest first across all requested logs, capped at the caller's limit.
        IReadOnlyList<WindowsEventLogItem> recent = collected
            .OrderByDescending(e => e.TimeCreated)
            .Take(limit)
            .ToList();
        return Task.FromResult(recent);
    }

    // ── Alert-poller path (stateful, bookmark-based) ──────────────────────────────
    private List<WindowsEventLogItem> ReadLog(string logName, EventLogOptions options)
    {
        var items = new List<WindowsEventLogItem>();
        try
        {
            // First poll for this log: record the newest event as a baseline and emit
            // nothing, so a restart doesn't replay historical errors as an alert storm.
            if (!_bookmarks.ContainsKey(logName))
            {
                _bookmarks[logName] = ReadBaselineBookmark(logName);
                return items;
            }

            var query = new EventLogQuery(logName, PathType.LogName);
            var bookmark = _bookmarks[logName];

            EventLogReader reader;
            try
            {
                reader = bookmark is null
                    ? new EventLogReader(query)
                    : new EventLogReader(query, bookmark);
            }
            catch (EventLogException ex)
            {
                // The bookmark is no longer valid (log cleared or overwritten). Re-baseline
                // to the current newest event and skip this cycle rather than replay history.
                _logger?.LogInformation(
                    ex, "Event log '{Log}' bookmark was invalidated (log cleared?); re-baselining", logName);
                _bookmarks[logName] = ReadBaselineBookmark(logName);
                NoteHealthy(logName);
                return items;
            }

            using (reader)
            {
                for (var record = reader.ReadEvent(); record != null; record = reader.ReadEvent())
                {
                    using (record)
                    {
                        // Advance the resume point for every event read, even ones filtered out
                        // below, so the next poll never re-scans them.
                        _bookmarks[logName] = record.Bookmark;

                        var item = ToItem(record, logName, options);
                        if (item is not null)
                            items.Add(item);
                    }
                }
            }

            NoteHealthy(logName); // read completed without error
        }
        catch (Exception ex)
        {
            NoteFailure(logName, ex);
        }
        return items;
    }

    // ── /events path (stateless, newest-first, never touches _bookmarks) ──────────
    private List<WindowsEventLogItem> ReadRecentFromLog(string logName, EventLogOptions options, int limit)
    {
        var items = new List<WindowsEventLogItem>();
        try
        {
            var query = new EventLogQuery(logName, PathType.LogName) { ReverseDirection = true };
            using var reader = new EventLogReader(query);
            for (var record = reader.ReadEvent();
                 record != null && items.Count < limit;
                 record = reader.ReadEvent())
            {
                using (record)
                {
                    var item = ToItem(record, logName, options);
                    if (item is not null)
                        items.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            // /events is manual and infrequent, so no dedup needed; a warning is enough.
            _logger?.LogWarning(ex, "Reading recent events from '{Log}' failed", logName);
        }
        return items;
    }

    // Maps a raw record to an emitted item, or null if its level is not in the filter.
    private static WindowsEventLogItem? ToItem(EventRecord record, string logName, EventLogOptions options)
    {
        var level = MapLevel(record);
        if (!options.Levels.Contains(level, StringComparer.OrdinalIgnoreCase))
            return null;

        var msg = record.FormatDescription() ?? string.Empty;
        if (options.MessageMaxChars > 0 && msg.Length > options.MessageMaxChars)
            msg = msg[..options.MessageMaxChars] + "...";

        return new WindowsEventLogItem(
            logName, record.RecordId.GetValueOrDefault(), record.Id,
            record.ProviderName, level,
            record.TimeCreated?.ToUniversalTime() ?? DateTimeOffset.UtcNow,
            record.MachineName, string.IsNullOrEmpty(msg) ? null : msg);
    }

    // The bookmark of the current newest event in the log (null if empty or unreadable),
    // used as the "watch from here" baseline so history is never replayed.
    private static EventBookmark? ReadBaselineBookmark(string logName)
    {
        try
        {
            var newestQuery = new EventLogQuery(logName, PathType.LogName) { ReverseDirection = true };
            using var newestReader = new EventLogReader(newestQuery);
            using var newest = newestReader.ReadEvent();
            return newest?.Bookmark;
        }
        catch
        {
            return null;
        }
    }

    private static string MapLevel(EventRecord record) => record.Level switch
    {
        1 => "Critical",
        2 => "Error",
        3 => "Warning",
        4 => "Information",
        5 => "Verbose",
        _ => record.LevelDisplayName ?? "Unknown"
    };

    // Log a read failure once per log, on the transition to failing, so a persistently
    // unreadable log doesn't spam the log file on every poll.
    private void NoteFailure(string logName, Exception ex)
    {
        if (_failingLogs.Add(logName))
            _logger?.LogWarning(
                ex, "Event log '{Log}' could not be read; further read errors suppressed until it recovers", logName);
    }

    private void NoteHealthy(string logName)
    {
        if (_failingLogs.Remove(logName))
            _logger?.LogInformation("Event log '{Log}' is readable again", logName);
    }
}
