using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LocalOpsBot.Tray.Services;

/// <summary>
/// Persists the <c>notificationForwarding.enabled</c> flag. The Agent's config lives in
/// admin-owned ProgramData, so the write needs elevation — but the JSON is edited here in C#
/// (<see cref="System.Text.Json"/> preserves arrays), and the elevated PowerShell only copies the
/// staged file into place and restarts the service. That deliberately avoids Windows PowerShell
/// 5.1's <c>ConvertFrom/ConvertTo-Json</c> single-element-array unwrap, which would corrupt
/// <c>telegram.allowedChatIds</c> on a round-trip.
/// </summary>
internal static class NotificationForwardingConfigurator
{
    /// <summary>
    /// Writes <c>notificationForwarding.enabled = <paramref name="enabled"/></c> to the ProgramData
    /// config and restarts the Agent so it (un)wires the pipe server. Returns <c>true</c> once the
    /// elevated helper copied the config successfully; <c>false</c> if the user declined the UAC
    /// prompt. Throws if the existing config can't be read (so it is never clobbered).
    /// </summary>
    public static async Task<bool> SetEnabledAsync(bool enabled)
    {
        // 1) Edit the config in-process, preserving everything else (arrays included).
        JsonObject root;
        if (File.Exists(TrayConfig.ConfigPath))
        {
            var text = await File.ReadAllTextAsync(TrayConfig.ConfigPath);
            root = JsonNode.Parse(text) as JsonObject
                   ?? throw new InvalidOperationException("The configuration file is not a JSON object.");
        }
        else
        {
            root = new JsonObject();
        }

        if (root["notificationForwarding"] is not JsonObject forwarding)
        {
            forwarding = new JsonObject();
            root["notificationForwarding"] = forwarding;
        }
        forwarding["enabled"] = enabled;
        // First time the section is created, default to forwarding everything except an explicit
        // block list, so an enabled feature has sensible behaviour without further config.
        forwarding["mode"] ??= "BlockList";
        // Default masking ON: redact OTP codes, passwords and tokens before a notification leaves
        // the machine. Only sets the default when absent, so an explicit user choice is preserved.
        forwarding["maskingEnabled"] ??= true;

        var tempConfig = Path.Combine(Path.GetTempPath(), $"homebase_cfg_{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(tempConfig,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        // 2) Elevated helper: copy the staged file over the admin-owned config, then restart the
        // Agent so the new state takes effect. No JSON parsing in PowerShell — copy only.
        var script = @"$ErrorActionPreference = 'Stop'
$src = '__SRC__'
$configDir = 'C:\ProgramData\Homebase\config'
$configFile = Join-Path $configDir 'appsettings.json'
New-Item -ItemType Directory -Force -Path $configDir | Out-Null
Copy-Item -LiteralPath $src -Destination $configFile -Force
# Restart is best-effort: the config is already written and the service reads it at startup.
try {
    $svc = Get-Service -Name 'Homebase.Agent' -ErrorAction SilentlyContinue
    if ($svc) { Restart-Service -Name 'Homebase.Agent' -Force -ErrorAction SilentlyContinue }
} catch { }
".Replace("__SRC__", tempConfig.Replace("'", "''"));

        var psFile = Path.Combine(Path.GetTempPath(), $"homebase_setfwd_{Guid.NewGuid():N}.ps1");
        await File.WriteAllTextAsync(psFile, script);

        try
        {
            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{psFile}\"")
            {
                // Verb=runas requires UseShellExecute=true. Prompts the single UAC consent.
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            using var proc = Process.Start(psi);
            if (proc is null) return false;
            await proc.WaitForExitAsync();
            return proc.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return false; // user dismissed the UAC prompt — nothing changed
        }
        finally
        {
            TryDelete(psFile);
            TryDelete(tempConfig);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { /* best-effort cleanup */ }
    }
}
