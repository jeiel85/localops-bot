# Patch 06 — Telegram command compatibility

Do not change `TelegramPollingService` in TRAIN-A.

The new `IRemoteCommandRouter` is for future KDE ingress.

Before exposing a legacy Handler to KDE, audit its use of:

```text
command.ChatId
command.UserId
```

Handlers that require Telegram identity must be migrated before KDE exposure.
