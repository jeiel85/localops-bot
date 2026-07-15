# Patch 05 — NotificationForwardingService

Replace:

```text
src/LocalOpsBot.Agent/Services/NotificationForwardingService.cs
```

with:

```text
replacement-files/src/LocalOpsBot.Agent/Services/NotificationForwardingService.cs
```

The replacement:

- keeps the current notification bridge
- keeps masking
- keeps Blocked defense-in-depth
- removes direct Telegram dependencies
- routes with `DeliveryPolicy.LocalPreferred`
- falls back to Telegram while KDE is unavailable
