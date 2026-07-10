using System.Net.Http;
using LocalOpsBot.Core.Updates;
using Application = System.Windows.Application;

namespace LocalOpsBot.Tray.Services;

internal enum UpdateOutcome { UpToDate, Declined, Cancelled, Failed, Updating, Busy }

/// <summary>
/// The single owner of the tray's auto-update flow, shared by the tray context menu and the
/// dashboard so both behave identically. Wraps <see cref="UpdateService"/> and drives an
/// <see cref="UpdateProgressWindow"/> for live download progress. Everything here runs on the
/// WPF UI thread; <see cref="Progress{T}"/> callbacks therefore marshal back to it for free.
/// </summary>
internal sealed class UpdateCoordinator
{
    private readonly UpdateService _updater;
    private bool _busy;

    public UpdateCoordinator()
    {
        var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Homebase.Tray/0.1");
        _updater = new UpdateService(http);
    }

    public string CurrentVersion => _updater.GetCurrentVersionString();

    /// <summary>Silent check used by the startup background poll; returns the newer release or null.</summary>
    public Task<UpdateInfo?> CheckAsync(CancellationToken ct) => _updater.CheckForUpdateAsync(ct);

    /// <summary>
    /// Check → confirm → download (with a progress window) → apply (UAC). <paramref name="status"/>
    /// receives short phase strings so the caller can reflect them in its own affordance (the tray
    /// menu item text or the dashboard's update label). Must be called on the UI thread.
    /// </summary>
    public async Task<UpdateOutcome> RunInteractiveAsync(IProgress<string>? status = null)
    {
        if (_busy) return UpdateOutcome.Busy;
        _busy = true;
        try
        {
            status?.Report("Checking…");
            UpdateInfo? info;
            try
            {
                info = await _updater.CheckForUpdateAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                status?.Report("Update check failed");
                MessageDialog.Show("Update Error", $"Update check failed: {ex.Message}");
                return UpdateOutcome.Failed;
            }

            if (info is null)
            {
                status?.Report($"v{CurrentVersion} — up to date");
                MessageDialog.Show("Update Check", $"Homebase is up to date (v{CurrentVersion}).");
                return UpdateOutcome.UpToDate;
            }

            var confirmed = MessageDialog.Show("Update Available",
                $"Update v{info.Version} available!\nPublished: {info.PublishedAt:yyyy-MM-dd}",
                primary: "Download & Install", secondary: "Later");
            if (!confirmed)
            {
                status?.Report($"v{info.Version} available");
                return UpdateOutcome.Declined;
            }

            return await DownloadAndApplyAsync(info, status);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task<UpdateOutcome> DownloadAndApplyAsync(UpdateInfo info, IProgress<string>? status)
    {
        var window = new UpdateProgressWindow();
        window.SetStatus("Downloading the update…");
        window.Show();
        window.Activate();
        try
        {
            string zip;
            try
            {
                status?.Report("Downloading update…");
                var progress = new Progress<int>(p =>
                {
                    window.SetPercent(p);
                    // The checksum verification runs right after the byte loop hits 100%.
                    if (p >= 100) window.SetStatus("Verifying…");
                });
                zip = await _updater.DownloadUpdateAsync(info, progress, window.Token);
            }
            catch (OperationCanceledException)
            {
                status?.Report($"v{info.Version} available");
                return UpdateOutcome.Cancelled;
            }
            catch (Exception ex)
            {
                status?.Report("Update failed");
                MessageDialog.Show("Update Error", $"Download failed: {ex.Message}");
                return UpdateOutcome.Failed;
            }

            // Download + checksum OK. Applying writes Program Files and stops/starts the service,
            // so it needs admin — lock cancellation and launch the elevated helper. Only shut the
            // tray down once that helper is actually running (the user may still decline the UAC).
            window.SetPercent(100);
            window.SetStatus("Installing… approve the administrator prompt.");
            window.LockCancel();
            status?.Report("Installing update…");

            bool started;
            try
            {
                started = _updater.ApplyUpdate(zip, elevate: true);
            }
            catch (Exception ex)
            {
                status?.Report("Update failed");
                MessageDialog.Show("Update Error", $"Couldn't launch the installer: {ex.Message}");
                return UpdateOutcome.Failed;
            }

            if (started)
            {
                // Let the dashboard (which cancels its own close) actually close during shutdown.
                App.IsShuttingDown = true;
                Application.Current.Shutdown();
                return UpdateOutcome.Updating;
            }

            status?.Report($"v{info.Version} available");
            MessageDialog.Show("Update Cancelled",
                "Installing the update needs administrator approval. Nothing was changed — you can try again anytime.");
            return UpdateOutcome.Cancelled;
        }
        finally
        {
            // The user may have already closed the window (cancel); closing again is a no-op we
            // guard defensively rather than tracking close state.
            try { window.Close(); } catch { /* already closed */ }
        }
    }
}
