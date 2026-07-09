namespace LocalOpsBot.Core.Monitoring;

/// <summary>
/// Configuration for hardware temperature collection. Bound from the "temperature" config section.
/// </summary>
public sealed class TemperatureOptions
{
    /// <summary>
    /// When false, temperature collection is skipped entirely and the kernel driver (WinRing0) is
    /// never loaded. Set this to false if antivirus flags the driver or sensors aren't wanted.
    /// </summary>
    public bool Enabled { get; init; } = true;
}
