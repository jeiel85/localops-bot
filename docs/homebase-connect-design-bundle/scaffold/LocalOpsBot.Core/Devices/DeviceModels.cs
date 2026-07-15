namespace LocalOpsBot.Core.Devices;

public enum DeviceType
{
    Unknown,
    Desktop,
    Laptop,
    Phone,
    Tablet
}

public enum TrustState
{
    Unknown,
    Discovered,
    PairingPending,
    Trusted,
    Revoked
}

public enum DeviceConnectionState
{
    Offline,
    Connecting,
    Connected,
    Degraded
}

public sealed record DeviceRecord(
    string DeviceId,
    string DisplayName,
    DeviceType Type,
    TrustState TrustState,
    DeviceConnectionState ConnectionState,
    string? CertificateFingerprint,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    IReadOnlySet<string> IncomingCapabilities,
    IReadOnlySet<string> OutgoingCapabilities);
