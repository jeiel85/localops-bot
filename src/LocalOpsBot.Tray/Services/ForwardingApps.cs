using System.IO;

namespace LocalOpsBot.Tray.Services;

/// <summary>
/// Two small per-user files under <c>%LOCALAPPDATA%\Homebase\</c> that drive notification app
/// selection. Both are user-writable, so tuning which apps forward needs no elevation:
/// <list type="bullet">
/// <item><c>forwarding-apps.txt</c> — the set of app display names that have sent a notification
/// (appended by the poller) so the dashboard can offer them to choose from.</item>
/// <item><c>forwarding-allow.txt</c> — the user's chosen allow-list (written by the dashboard, read
/// live by <see cref="DynamicAppFilter"/>). Empty = forward all.</item>
/// </list>
/// </summary>
internal static class ForwardingApps
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Homebase");
    private static readonly string SeenPath = Path.Combine(Dir, "forwarding-apps.txt");
    private static readonly string AllowPath = Path.Combine(Dir, "forwarding-allow.txt");

    private static readonly object Gate = new();

    // ── Seen apps ───────────────────────────────────────────────────────────────────────────────
    public static void RecordSeenApp(string? app)
    {
        if (string.IsNullOrWhiteSpace(app)) return;
        var name = app.Trim();
        lock (Gate)
        {
            try
            {
                if (ReadLinesNoLock(SeenPath).Contains(name, StringComparer.OrdinalIgnoreCase)) return;
                Directory.CreateDirectory(Dir);
                File.AppendAllText(SeenPath, name + Environment.NewLine);
            }
            catch { /* best-effort: never let recording break the poll loop */ }
        }
    }

    public static IReadOnlyList<string> ReadSeenApps()
    {
        lock (Gate) return ReadLinesNoLock(SeenPath);
    }

    // ── Allow-list ──────────────────────────────────────────────────────────────────────────────
    private static string[] _allowCache = Array.Empty<string>();
    private static DateTime _allowMtime = DateTime.MinValue;

    /// <summary>The current allow-list, re-read only when the file changes (cheap per-poll call).</summary>
    public static IReadOnlyCollection<string> ReadAllowList()
    {
        lock (Gate)
        {
            try
            {
                if (!File.Exists(AllowPath))
                {
                    _allowCache = Array.Empty<string>();
                    _allowMtime = DateTime.MinValue;
                }
                else
                {
                    var mtime = File.GetLastWriteTimeUtc(AllowPath);
                    if (mtime != _allowMtime)
                    {
                        _allowCache = ReadLinesNoLock(AllowPath).ToArray();
                        _allowMtime = mtime;
                    }
                }
            }
            catch { /* keep the last good cache */ }
            return _allowCache;
        }
    }

    public static void WriteAllowList(IEnumerable<string> apps)
    {
        var lines = apps.Where(a => !string.IsNullOrWhiteSpace(a))
                        .Select(a => a.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
        lock (Gate)
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllText(AllowPath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
            // Invalidate the cache so the very next ReadAllowList reflects the write immediately.
            _allowMtime = DateTime.MinValue;
        }
    }

    private static List<string> ReadLinesNoLock(string path)
    {
        try
        {
            if (!File.Exists(path)) return new List<string>();
            return File.ReadAllLines(path).Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        }
        catch { return new List<string>(); }
    }
}
