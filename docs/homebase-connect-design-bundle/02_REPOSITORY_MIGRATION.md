# 02. Repository Migration

## 1. 현재 자산 처리

| 현재 구성 | 처리 | 목표 |
|---|---|---|
| `ICommandRouter` | 유지 후 입력 모델 교체 | 모든 채널이 공통 사용 |
| `BotCommand` | 단계적 폐기 | `RemoteCommand` |
| `CommandResult` | 확장 | 구조화 결과와 attachment |
| `AlertDispatcher` | 내부 Telegram 의존 제거 | `IOutboundRouter` 사용 |
| `ITelegramClient` | 유지 | Telegram Adapter 내부 |
| `TelegramPollingService` | Adapter로 축소 | command ingress |
| `NotificationBridgeServer` | 교체 | 양방향 `SessionBridgeServer` |
| `NotificationBridgeClient` | 교체 | 양방향 `SessionBridgeClient` |
| `AlertPolicy` | 유지 | transport-neutral |
| SQLite V1 | 보존 | V2 device hub tables 추가 |
| Collector | 유지 | 변경 없음 |
| WPF Dashboard | 확장 | Devices, Pairing, Plugins card |

## 2. 안전한 마이그레이션 원칙

1. 기존 Telegram 테스트를 기준선으로 고정합니다.
2. 새 interface를 추가한 뒤 기존 구현을 Adapter로 감쌉니다.
3. 기존 코드를 먼저 삭제하지 않습니다.
4. 새 경로와 기존 경로를 feature flag로 병행합니다.
5. 메시지 수와 결과를 비교한 뒤 새 경로를 기본값으로 바꿉니다.
6. 안정화 후 obsolete 코드를 삭제합니다.

## 3. Branch strategy

```text
main
 └─ feature/device-hub-foundation
     ├─ goal-01-transport-contracts
     ├─ goal-02-telegram-adapter
     ├─ goal-03-outbound-router
     ├─ goal-04-session-bridge
     └─ ...
```

권장 방식:

- GOAL당 PR 1개
- schema migration은 단독 PR
- KDE protocol spike와 production implementation을 분리
- UI 변경은 backend contract 이후 진행

## 4. Solution migration

### Step 1

추가:

```text
src/LocalOpsBot.Protocol/LocalOpsBot.Protocol.csproj
src/LocalOpsBot.Transport.KdeConnect/LocalOpsBot.Transport.KdeConnect.csproj
```

### Step 2

안정화 후 추가:

```text
src/LocalOpsBot.Transport.Telegram/LocalOpsBot.Transport.Telegram.csproj
```

### Step 3

기존 테스트 프로젝트는 유지하되 폴더를 분리합니다.

```text
tests/LocalOpsBot.Tests/
├─ Unit/
├─ Integration/
├─ Contract/
└─ Fixtures/
```

테스트 수가 커지면 추후 프로젝트를 분리합니다.

## 5. Namespace plan

```text
LocalOpsBot.Protocol.Messaging
LocalOpsBot.Protocol.Devices
LocalOpsBot.Core.Devices
LocalOpsBot.Core.Transports
LocalOpsBot.Core.Commands
LocalOpsBot.Core.Delivery
LocalOpsBot.Core.Plugins
LocalOpsBot.Core.Security
LocalOpsBot.Infrastructure.Ipc
LocalOpsBot.Transport.Telegram
LocalOpsBot.Transport.KdeConnect
LocalOpsBot.Transport.KdeConnect.Protocol
LocalOpsBot.Transport.KdeConnect.Pairing
LocalOpsBot.Transport.KdeConnect.Payloads
```

## 6. Compatibility shim

초기에는 다음 변환기를 둡니다.

```csharp
public interface IBotCommandCompatibilityAdapter
{
    RemoteCommand Convert(BotCommand legacy);
}
```

기존 Handler를 한 번에 수정하기 어렵다면:

```csharp
public sealed class LegacyCommandHandlerAdapter : IRemoteCommandHandler
{
    private readonly ICommandHandler _legacy;
}
```

최종 상태에서는 Handler가 `RemoteCommand`를 직접 받습니다.

## 7. AlertDispatcher migration

### Before

```csharp
AlertDispatcher(
    IAlertPolicy,
    IAlertStore,
    ITelegramClient,
    IOptions<TelegramOptions>)
```

### After

```csharp
AlertDispatcher(
    IAlertPolicy,
    IAlertStore,
    IOutboundRouter,
    IAlertNotificationMapper)
```

`IAlertNotificationMapper`는 도메인 `AlertEvent`를 transport-neutral `OutboundNotification`으로 변환합니다.

Telegram HTML formatting은 `TelegramNotificationRenderer`로 이동합니다.

## 8. Program.cs registration

최종 형태:

```csharp
builder.Services.AddLocalOpsProtocol();
builder.Services.AddLocalOpsCore(builder.Configuration);
builder.Services.AddLocalOpsData(builder.Configuration);
builder.Services.AddLocalOpsWindowsInfrastructure(builder.Configuration);
builder.Services.AddHomebaseSessionBridge(builder.Configuration);
builder.Services.AddTelegramTransport(builder.Configuration);
builder.Services.AddKdeConnectTransport(builder.Configuration);
builder.Services.AddHomebasePlugins(builder.Configuration);
```

Hosted Service를 조건부로 직접 나열하지 말고 각 extension이 자신의 lifecycle registration을 책임지게 합니다.
