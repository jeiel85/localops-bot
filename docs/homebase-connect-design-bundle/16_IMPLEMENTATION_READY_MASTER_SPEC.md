# 16. Implementation-Ready Master Specification

## 1. 가장 먼저 제거할 직접 결합

현재 Homebase의 다음 서비스가 Telegram 구현에 직접 묶여 있습니다.

```text
AlertDispatcher
  → ITelegramClient
  → TelegramOptions

NotificationForwardingService
  → ITelegramClient
  → TelegramOptions
```

이 두 경로를 먼저 `IOutboundRouter`로 교체합니다.

Command Handler는 한꺼번에 변경하지 않습니다.

```text
RemoteCommand
  → LegacyRemoteCommandRouter
  → existing ICommandRouter
  → existing ICommandHandler
```

## 2. 1차 추가 프로젝트

```text
src/LocalOpsBot.Protocol/
```

Telegram 코드는 초기 PR에서 이동하지 않습니다. 추상화가 안정화된 뒤 별도 Transport 프로젝트로 분리합니다.

KDE 구현 시 추가:

```text
src/LocalOpsBot.Transport.KdeConnect/
```

## 3. Phase 1 신규 파일

```text
src/LocalOpsBot.Protocol/Messaging/*
src/LocalOpsBot.Core/Commands/RemoteCommand.cs
src/LocalOpsBot.Core/Commands/IRemoteCommandRouter.cs
src/LocalOpsBot.Core/Commands/LegacyRemoteCommandRouter.cs
src/LocalOpsBot.Core/Delivery/*
src/LocalOpsBot.Infrastructure/Telegram/TelegramOutboundChannel.cs
src/LocalOpsBot.Infrastructure/Telegram/TelegramNotificationRenderer.cs
tests/LocalOpsBot.Tests/Protocol/*
tests/LocalOpsBot.Tests/Core/Commands/*
tests/LocalOpsBot.Tests/Core/Delivery/*
```

## 4. Phase 1 기존 파일 수정

```text
LocalOpsBot.sln
src/LocalOpsBot.Core/LocalOpsBot.Core.csproj
src/LocalOpsBot.Core/ServiceCollectionExtensions.cs
src/LocalOpsBot.Infrastructure/ServiceCollectionExtensions.cs
src/LocalOpsBot.Agent/Services/AlertDispatcher.cs
src/LocalOpsBot.Agent/Services/NotificationForwardingService.cs
tests/LocalOpsBot.Tests/LocalOpsBot.Tests.csproj
```

## 5. Phase 1 동작

```text
Monitor
  → AlertEvent
  → AlertPolicy
  → OutboundNotification
  → OutboundRouter
  → TelegramOutboundChannel
```

이 단계에서는 사용자-visible 동작이 동일해야 합니다.

## 6. DB V2

기존 V1 테이블은 rebuild하지 않고 신규 테이블을 추가합니다.

```text
device
device_capability
device_plugin_setting
device_session
device_state
command_execution_log
message_delivery
outbox_message
payload_transfer
```

## 7. Session Bridge

기존 notification pipe를 유지한 채 V2를 shadow mode로 추가합니다.

```text
sessionBridgeV2.enabled = false
```

## 8. KDE 순서

1. Pairing/Ping
2. Battery
3. Android → Windows notifications
4. Windows → Android notifications
5. Share text/URL
6. File
7. Run predefined command

KDE 공식 사용자 문서 기준 discovery는 UDP broadcast이고 TCP/UDP 포트 범위는 `1714-1764`입니다.

## 9. Done 정의

```text
Code complete
Tests complete
Operational verification complete
Rollback documented
```
