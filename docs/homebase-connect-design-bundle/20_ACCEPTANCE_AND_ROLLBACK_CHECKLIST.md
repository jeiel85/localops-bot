# 20. Acceptance and Rollback Checklist

## GOAL PR checklist

```text
[ ] One GOAL only
[ ] Existing Telegram behavior unchanged
[ ] Build passes
[ ] Unit tests pass
[ ] Integration tests pass
[ ] Manual validation completed
[ ] Security reviewed
[ ] Log redaction checked
[ ] Safe config defaults
[ ] Rollback documented
[ ] Changelog updated
```

## TRAIN-A acceptance

```text
[ ] /ping
[ ] /status
[ ] /events
[ ] automatic alert
[ ] Toast forwarding
[ ] mute/dedup/rate limit
[ ] no ITelegramClient in AlertDispatcher
[ ] no ITelegramClient in NotificationForwardingService
[ ] V1 DB opens
[ ] V1 → V2 succeeds
```

## KDE notification acceptance

```text
[ ] Android → Windows post/update/remove
[ ] Windows → Android
[ ] privacy before persistence
[ ] no Phone Link loop
[ ] no Telegram Desktop loop
[ ] no Homebase/KDE self-loop
[ ] KDE offline → Telegram fallback
```

## Feature rollback

```json
{
  "deviceHub": { "enabled": false },
  "sessionBridge": { "enabled": false },
  "transports": { "kdeConnect": { "enabled": false } }
}
```

## DB rollback

V2는 신규 테이블만 추가합니다. 가장 안전한 rollback은 migration 전 DB backup 복원입니다.
