# 07. Feature Plugins

## 1. Plugin catalog

| Plugin | Direction | First release |
|---|---|---:|
| DevicePingPlugin | Both | Yes |
| BatteryPlugin | Android → PC | Yes |
| MobileNotificationPlugin | Android → PC | Yes |
| SharePlugin | Both | Yes |
| RunCommandPlugin | Android/Telegram → PC | Yes |
| SystemStatusPlugin | Client → PC → Client | Yes |
| NotificationActionPlugin | PC → Android action | Later |
| MediaControlPlugin | Android → PC | Later |
| ClipboardPlugin | Both | Later |
| FindDevicePlugin | PC → Android | Later |
| MouseInputPlugin | Android → PC | Last |

## 2. DevicePingPlugin

Message types:

```text
homebase.device.ping.request
homebase.device.ping.response
```

기능:

- reachability
- latency
- display message optional
- no side effect by default

## 3. BatteryPlugin

Internal event:

```json
{
  "messageType": "homebase.device.battery.changed",
  "body": {
    "chargePercent": 74,
    "isCharging": true,
    "thresholdEvent": null
  }
}
```

저장:

- device latest state
- optional history
- low battery alert policy

명령:

```text
/devices
/device <id>
/battery <device>
```

## 4. MobileNotificationPlugin

수신 모델:

```csharp
public sealed record MobileNotificationEvent(
    string DeviceId,
    string NotificationId,
    string AppId,
    string AppName,
    string? Title,
    string? Body,
    bool IsClearable,
    bool IsUpdate,
    bool IsRemoved,
    DateTimeOffset PostedAt,
    IReadOnlyList<MobileNotificationAction> Actions);
```

초기 동작:

- Tray Dashboard에 표시
- Windows Toast로 표시할지 설정 가능
- SQLite metadata 저장
- 민감 앱 blocklist
- body 저장 정책 분리
- Telegram 재포워딩 기본 OFF

루프 방지:

- Telegram, Homebase, KDE Connect 앱 알림 기본 제외
- 동일 notification id update 처리
- remove event 반영
- app별 privacy mode

## 5. SharePlugin

지원:

- text
- URL
- single file
- multiple files는 후속 또는 batch metadata로 지원

기본 수신 위치:

```text
%UserProfile%\Downloads\Homebase\
```

Agent가 LocalSystem이므로 실제 최종 파일 이동은 Tray에 요청합니다.

흐름:

```text
KDE payload → Agent temp store
  → hash/size validation
  → Tray user confirmation optional
  → Tray moves to user folder
  → result response
```

## 6. RunCommandPlugin

### Command source

- Telegram command
- KDE predefined command request
- Dashboard button

모두 동일 `RemoteCommandRouter`를 사용합니다.

### Definition

```json
{
  "id": "lock_pc",
  "displayName": "Lock PC",
  "handler": "LockPcCommandHandler",
  "riskLevel": "UserSessionMutation",
  "allowedTransports": ["telegram", "kdeconnect"],
  "allowedDeviceIds": [],
  "requiresConfirmation": false,
  "enabled": true
}
```

실행 파일과 arguments를 config로 직접 허용하는 generic runner는 1차 범위에서 제외합니다.

필요한 명령은 Handler로 구현합니다.

예:

- lock PC
- mute/unmute Homebase
- start approved service
- restart approved developer monitor
- open approved URL
- show PC status

## 7. SystemStatusPlugin

기존 `/status`, `/disk`, `/process`, `/services` Handler를 재사용합니다.

반환은 structured data를 우선합니다.

```json
{
  "cpuPercent": 12.3,
  "memoryPercent": 68.1,
  "uptimeSeconds": 12345,
  "disks": []
}
```

Telegram renderer는 사람이 읽는 text로 변환합니다.

Device UI가 생기면 JSON data를 직접 사용합니다.

## 8. MediaControlPlugin

Tray operation:

```text
media.listSessions
media.getCurrent
media.playPause
media.next
media.previous
media.seek
media.setVolume
```

위험도:

- `UserSessionMutation`

Tray가 없으면 `UserSessionUnavailable`.

## 9. ClipboardPlugin

기본 OFF.

정책:

- 최대 text 길이
- binary/image clipboard 제외
- password manager foreground 시 자동 sync 금지
- user initiated send 우선
- background clipboard restrictions 고려
- clipboard content DB 저장 금지
- logs에 content 금지

## 10. Plugin settings

장치별 설정:

```text
Galaxy S24
├─ mobileNotifications.enabled = true
├─ mobileNotifications.showWindowsToast = true
├─ share.enabled = true
├─ runCommand.enabled = true
├─ clipboard.enabled = false
└─ media.enabled = false
```

global disable과 device disable을 모두 지원합니다.
