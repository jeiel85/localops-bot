# 17. File-by-File Change Matrix

## Core csproj

추가:

```xml
<ItemGroup>
  <ProjectReference Include="..\LocalOpsBot.Protocol\LocalOpsBot.Protocol.csproj" />
</ItemGroup>
```

## Test csproj

추가:

```xml
<ProjectReference Include="..\..\src\LocalOpsBot.Protocol\LocalOpsBot.Protocol.csproj" />
```

## Core ServiceCollectionExtensions

추가:

```csharp
services.AddSingleton<IRemoteCommandRouter, LegacyRemoteCommandRouter>();
services.AddSingleton<IOutboundRouter, OutboundRouter>();
```

기존 Handler 등록은 모두 유지합니다.

## Infrastructure ServiceCollectionExtensions

`AddLocalOpsTelegram`에 추가:

```csharp
services.AddSingleton<TelegramNotificationRenderer>();
services.AddSingleton<IOutboundChannel, TelegramOutboundChannel>();
```

## AlertDispatcher

제거:

- `ITelegramClient`
- `IOptions<TelegramOptions>`
- chat ID 선택
- Telegram HTML formatting

추가:

- `IOutboundRouter`
- `AlertEvent → OutboundNotification`
- delivery result 저장

## NotificationForwardingService

제거:

- `ITelegramClient`
- `TelegramOptions`
- HTML formatting

추가:

- `IOutboundRouter`
- `DeliveryPolicy.LocalPreferred`
- origin metadata

## SqliteMigrator

추가:

```csharp
if (version < 2)
    await ApplyV2Async(ct);
```

V2는 transaction 안에서 table/index 생성과 version insert를 함께 처리합니다.

## Tray

TRAIN-A에서는 변경하지 않습니다.

TRAIN-B부터:

- SessionBridgeClient
- pairing UI
- device UI
- operation handlers

## 삭제 파일

TRAIN-A에서 삭제하는 파일은 없습니다.
