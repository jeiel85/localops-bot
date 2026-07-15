# Patch 03 — Telegram outbound DI

File:

```text
src/LocalOpsBot.Infrastructure/ServiceCollectionExtensions.cs
```

Add:

```csharp
using LocalOpsBot.Core.Delivery;
```

Inside `AddLocalOpsTelegram` after the typed HttpClient registration:

```csharp
services.AddSingleton<TelegramNotificationRenderer>();
services.AddSingleton<IOutboundChannel, TelegramOutboundChannel>();
```

Keep `AllowedChatPolicy`.
