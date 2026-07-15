# 18. Tray UI Implementation Specification

## Dashboard card order

```text
STATUS
TELEGRAM
DEVICES
NOTIFICATIONS
TRANSFERS
MONITORS
UPDATES
```

## Devices empty state

```text
DEVICES
KDE Connect integration is off.
[Enable device connection]
```

## Paired device card

```text
Galaxy S24                         Connected
Battery 74% · Charging
Last seen: now

Notifications     ON
Share             ON
Run commands      ON
Clipboard         OFF

[Settings] [Unpair]
```

## Pairing dialog

표시:

- device name/type
- shortened device ID
- fingerprint
- verification code
- expiry countdown

동작:

- Accept
- Reject
- timeout 처리
- certificate changed 경고
- background thread 결과를 WPF Dispatcher로 반영

## Device settings

```text
Notifications
  Receive phone notifications       ON
  Send Windows notifications         ON
  Show as Windows Toast              ON
  Store notification content         OFF

Share
  Receive files                      ON
  Ask before saving                  ON
  Download folder                    Downloads\Homebase

Commands
  Status                             ON
  Lock PC                            ON
  Restart approved service           OFF

Clipboard                            OFF
Media control                        OFF
```

## Transfer row

```text
photo.jpg
Galaxy S24 → This PC
12.4 MB / 24.8 MB      50%
[Cancel]
```

실행 파일은 자동 열지 않습니다.

## Accessibility

- keyboard navigation
- pairing code selectable
- color 외 텍스트 상태
- 100/125/150% DPI 확인
