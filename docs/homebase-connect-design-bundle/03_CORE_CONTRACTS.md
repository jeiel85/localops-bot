# 03. Core Contracts

## 1. DeviceEnvelope

모든 device transport가 사용하는 Homebase 내부 wire-neutral envelope입니다.

```csharp
public sealed record DeviceEnvelope(
    Guid MessageId,
    int SchemaVersion,
    string MessageType,
    EndpointAddress Source,
    EndpointAddress? Target,
    Guid TraceId,
    Guid? CorrelationId,
    Guid? CausationId,
    int HopCount,
    int MaxHops,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    MessageSensitivity Sensitivity,
    DeliverySemantics Delivery,
    JsonElement Body,
    PayloadDescriptor? Payload,
    IReadOnlyDictionary<string, string>? Metadata);
```

필수 검증:

- `SchemaVersion == 1`
- `MessageType`는 빈 문자열 금지
- `MessageId`, `TraceId`는 empty GUID 금지
- `HopCount >= 0`
- `MaxHops` 범위 `1..8`
- `HopCount < MaxHops`
- expired message drop
- payload size와 descriptor 일치
- body 최대 크기 제한

## 2. EndpointAddress

```csharp
public sealed record EndpointAddress(
    string TransportId,
    string EndpointId,
    string? DeviceId = null);
```

예:

```text
telegram:chat:123456789
kdeconnect:device:3a59...
local:session:1
```

## 3. Device model

```csharp
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
```

State transition:

```text
Unknown
  → Discovered
  → PairingPending
  → Trusted
  → Connected
  → Disconnected
  → Revoked
```

금지 전이:

- Revoked → Connected
- Unknown → Trusted
- Discovered → Connected without trust
- Expired pairing → Trusted

## 4. Transport contract

```csharp
public interface IDeviceTransport : IAsyncDisposable
{
    string TransportId { get; }
    TransportCapabilities Capabilities { get; }
    event Func<TransportInboundMessage, CancellationToken, Task>? MessageReceived;
    event Func<TransportDeviceEvent, CancellationToken, Task>? DeviceEventReceived;

    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task<TransportSendResult> SendAsync(
        EndpointAddress target,
        DeviceEnvelope envelope,
        CancellationToken ct);
}
```

Transport implementation rules:

- callback 예외 격리
- start/stop idempotent
- duplicate start 금지
- cancellation 준수
- send timeout 필수
- 실패를 bool로 숨기지 않고 분류된 result 반환
- raw secret logging 금지

## 5. Plugin contract

```csharp
public interface IHomebasePlugin
{
    string PluginId { get; }
    Version PluginVersion { get; }
    IReadOnlySet<string> IncomingMessageTypes { get; }
    IReadOnlySet<string> OutgoingMessageTypes { get; }
    IReadOnlySet<string> RequiredCapabilities { get; }

    ValueTask<PluginActivationResult> CanActivateAsync(
        DeviceContext context,
        CancellationToken ct);

    Task StartAsync(DeviceContext context, CancellationToken ct);
    Task<PluginHandleResult> HandleAsync(
        DeviceContext context,
        DeviceEnvelope envelope,
        CancellationToken ct);
    Task StopAsync(DeviceContext context, CancellationToken ct);
}
```

Plugin lifecycle은 device별입니다. 동일 Plugin class라도 장치마다 별도 context와 설정을 갖습니다.

## 6. Remote command

```csharp
public sealed record RemoteCommand(
    Guid RequestId,
    string Name,
    IReadOnlyList<string> Args,
    CommandPrincipal Principal,
    EndpointAddress ReplyTo,
    string RawInput,
    DateTimeOffset ReceivedAt,
    IReadOnlyDictionary<string, string>? Metadata);
```

```csharp
public sealed record CommandPrincipal(
    string PrincipalId,
    PrincipalKind Kind,
    string? DeviceId,
    TrustLevel TrustLevel);
```

## 7. Command authorization

```csharp
public enum CommandRiskLevel
{
    ReadOnly,
    UserSessionMutation,
    SystemMutation,
    Destructive
}
```

각 Handler가 위험도를 선언합니다.

```csharp
public interface IRemoteCommandHandler
{
    string CommandName { get; }
    CommandRiskLevel RiskLevel { get; }
    Task<RemoteCommandResult> HandleAsync(
        RemoteCommand command,
        CancellationToken ct);
}
```

정책:

| 위험도 | Telegram allowlist | Paired device | 사용자 확인 |
|---|---:|---:|---:|
| ReadOnly | 필요 | 필요 | 불필요 |
| UserSessionMutation | 필요 | 필요 | 설정 가능 |
| SystemMutation | 추가 allowlist | 장치별 허용 | 기본 필요 |
| Destructive | 명시 opt-in | 장치별 허용 | 항상 필요 |

## 8. Structured result

```csharp
public sealed record RemoteCommandResult(
    bool Success,
    string? UserMessage,
    JsonElement? Data,
    IReadOnlyList<CommandAttachment>? Attachments,
    string? ErrorCode,
    string? ErrorDetail);
```

Transport renderer가 `UserMessage`와 `Data`를 각 채널 포맷으로 렌더링합니다.

## 9. Outbound notification

```csharp
public sealed record OutboundNotification(
    Guid NotificationId,
    string Category,
    NotificationPriority Priority,
    string Title,
    string? Body,
    string DedupKey,
    MessageSensitivity Sensitivity,
    DeliveryPolicy DeliveryPolicy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    IReadOnlyList<NotificationAction>? Actions,
    IReadOnlyDictionary<string, string>? Metadata);
```

Alert와 일반 notification을 구분하되 동일 Outbound Router를 사용합니다.
