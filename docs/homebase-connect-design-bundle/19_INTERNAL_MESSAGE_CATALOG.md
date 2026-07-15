# 19. Internal Message Catalog

Homebase Core는 KDE packet name을 직접 사용하지 않습니다.

## Device lifecycle

```text
homebase.device.discovered
homebase.device.pairing.requested
homebase.device.pairing.accepted
homebase.device.pairing.rejected
homebase.device.connected
homebase.device.disconnected
homebase.device.revoked
homebase.device.capabilities.changed
```

## Battery

```text
homebase.device.battery.changed
```

```json
{
  "chargePercent": 74,
  "isCharging": true,
  "thresholdEvent": false
}
```

## Notification

```text
homebase.notification.posted
homebase.notification.updated
homebase.notification.removed
homebase.notification.action.invoke
homebase.notification.reply
```

## Share

```text
homebase.share.text
homebase.share.url
homebase.share.file.offer
homebase.share.file.progress
homebase.share.file.completed
homebase.share.file.cancel
```

허용 URL scheme 기본값:

- `http`
- `https`

차단:

- `file`
- `javascript`
- `data`
- `shell`

## Commands

```text
homebase.command.catalog.request
homebase.command.catalog.response
homebase.command.invoke
homebase.command.result
```

Remote packet에는 executable path나 shell script를 넣지 않습니다.

## Mapping gate

정확한 KDE packet mapping은 다음 두 조건을 충족해야 합니다.

- locked commit
- fixture test

fixture 없는 mapping은 미완료입니다.
