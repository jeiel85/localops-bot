# 04. Message Routing

## 1. Inbound pipeline

```text
Transport receives external data
  → Adapter validates external identity
  → Adapter maps to DeviceEnvelope or RemoteCommand
  → ReplayGuard
  → LoopGuard
  → Authorization
  → CommandRouter or DeviceMessageRouter
  → Response routed to ReplyTo endpoint
```

## 2. Outbound pipeline

```text
Domain event
  → OutboundNotification mapper
  → AlertPolicy / QuietHours / Dedup
  → DeliveryPlanner
  → one or more DeliveryAttempt
  → Transport renderer
  → Transport send
  → delivery audit
  → retry or fallback
```

## 3. Delivery policy

```csharp
public enum DeliveryPolicy
{
    OriginOnly,
    LocalPreferred,
    TelegramFallback,
    Both,
    LocalOnly,
    TelegramOnly
}
```

권장 기본값:

| Category | Policy |
|---|---|
| boot | TelegramFallback |
| critical_system | Both |
| warning_system | TelegramFallback |
| info_system | LocalPreferred |
| windows_toast | LocalPreferred |
| android_notification | LocalOnly |
| battery | LocalOnly |
| file_share | LocalOnly |
| command_response | OriginOnly |

## 4. Delivery planner

입력:

- notification priority
- category
- paired device presence
- reachable transport
- quiet hours
- sensitivity
- target preference
- fallback delay

출력:

```csharp
public sealed record DeliveryPlan(
    Guid PlanId,
    IReadOnlyList<DeliveryStep> Steps);
```

```csharp
public sealed record DeliveryStep(
    EndpointAddress Target,
    TimeSpan Delay,
    bool ContinueOnSuccess,
    bool ContinueOnFailure);
```

예:

```text
Warning system alert
1. KDE/Homebase local target immediately
2. If no acknowledgement in 15 seconds, Telegram
```

KDE Connect Android의 `Send Notifications` 호환 구현이 완료되기 전까지는 feature flag로 Telegram 경로를 유지합니다. 호환 구현이 활성화되면 KDE 전달 성공 시 Telegram fallback을 생략하고, 장치가 오프라인이거나 전송이 실패했을 때만 Telegram을 사용합니다.

## 5. Loop prevention

필수 장치:

- `TraceId`
- `CausationId`
- `OriginTransport`
- `OriginDeviceId`
- `HopCount`
- `MaxHops`
- recent trace cache
- self-generated app notification filter

Drop 조건:

```text
trace recently processed
OR hopCount >= maxHops
OR source device == target device
OR message expired
OR origin transport is same and no explicit echo allowed
```

Windows notification source 필터는 기존 앱명 blocklist와 별도로 적용합니다.

```text
Phone Link
KDE Connect
Homebase
Telegram Desktop
```

앱 이름 기반 차단은 보조 수단이며, 주 수단은 trace metadata입니다.

## 6. Replay protection

외부 device message의 `(deviceId, messageId)`를 TTL cache와 DB에 저장합니다.

- memory TTL: 10분
- durable replay window: 보안 명령은 24시간
- duplicate command는 재실행하지 않음
- 이전 결과가 있으면 idempotent response 가능

## 7. Retry

분류:

```csharp
public enum DeliveryFailureKind
{
    TransientNetwork,
    RateLimited,
    EndpointOffline,
    Unauthorized,
    PayloadRejected,
    PermanentProtocol,
    Expired,
    Unknown
}
```

정책:

- Transient: exponential backoff + jitter
- RateLimited: provider retry-after 준수
- EndpointOffline: fallback 검토
- Unauthorized: 재시도 금지
- PermanentProtocol: dead letter
- Expired: drop
- PayloadRejected: 사용자에게 실패 사유

## 8. Queue

초기에는 SQLite 기반 durable outbox를 권장합니다.

```text
outbox_message
outbox_attempt
```

발송 전에 outbox insert, 성공 후 completed 처리합니다.

부팅 알림처럼 중복에 민감한 메시지는 deterministic dedup key를 사용합니다.

## 9. Response correlation

모든 command response는 `RequestId`를 유지합니다.

```text
Telegram update
  → RequestId 생성
  → Command execution
  → result
  → 동일 Telegram chat reply

KDE command packet
  → packet request id
  → result
  → 같은 device session response
```

Transport가 끊긴 경우 결과를 delivery outbox에 넣되, 위험 명령의 결과는 다른 채널로 자동 전환하지 않습니다.
