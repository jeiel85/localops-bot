# 06. KDE Connect Compatibility

## 1. 목적

Homebase가 KDE Connect Android의 필요한 기능과 통신하도록 별도 Transport Adapter를 구현합니다.

이 Adapter는 다음 원칙을 따릅니다.

- KDE 소스 코드를 복사하지 않음
- 공식/포크 코드의 외부 동작을 기준으로 독립 구현
- 패킷 fixture와 실제 Android 기기 테스트로 호환성 검증
- Homebase Core에 KDE packet name을 노출하지 않음
- 호환 범위를 명시하고 전체 프로토콜 구현을 목표로 하지 않음

## 2. 현실적인 1차 호환 범위

### 반드시 구현

- device discovery
- identity exchange
- TLS session
- pairing / unpair
- capability exchange
- ping
- payload framing
- reconnect

### 1차 기능

- Android battery → Homebase
- Android notifications → Homebase
- Android share text/URL/file → Homebase
- Homebase share text/URL/file → Android
- Android run predefined command → Homebase

### 후속 기능

- notification dismiss/reply/action
- MPRIS/media
- clipboard
- find phone
- mousepad

## 3. PC ↔ Android 알림 방향

KDE Connect는 양방향 알림 기능을 제공합니다.

```text
Android notification → Homebase
    Receive Notifications 호환 구현

Windows notification → Android
    Send Notifications 호환 구현

KDE 장치 오프라인 또는 외부망
    Telegram fallback
```

따라서 Homebase의 목표 동작은 다음과 같습니다.

- 같은 LAN에서 Android 장치가 연결되어 있으면 Windows Toast를 KDE 경로로 전달
- KDE 전송 성공 시 일반 알림은 Telegram으로 중복 발송하지 않음
- KDE 장치가 없거나 전송 실패 시 Telegram으로 fallback
- Critical 알림은 설정에 따라 KDE와 Telegram 양쪽으로 발송
- Android에서 들어온 알림을 다시 Android로 돌려보내지 않도록 trace 기반 loop guard 적용
- Phone Link, KDE Connect, Telegram Desktop, Homebase 자체 알림은 기본 loop-source 필터 적용

정확한 notification packet 필드와 action/reply 지원 범위는 GOAL-07에서 잠근 Android/desktop commit과 fixture를 기준으로 확정합니다.

## 4. Adapter components

```text
LocalOpsBot.Transport.KdeConnect/
├─ KdeConnectTransport.cs
├─ Discovery/
│  ├─ IKdeDiscoveryService.cs
│  ├─ KdeLanDiscoveryService.cs
│  └─ DiscoveryOptions.cs
├─ Protocol/
│  ├─ KdePacket.cs
│  ├─ KdePacketCodec.cs
│  ├─ KdePacketTypes.cs
│  ├─ KdeIdentityMapper.cs
│  └─ KdeCapabilityMapper.cs
├─ Security/
│  ├─ KdeCertificateStore.cs
│  ├─ KdeTlsSessionFactory.cs
│  └─ KdePairingCoordinator.cs
├─ Sessions/
│  ├─ KdeDeviceSession.cs
│  ├─ KdeSessionRegistry.cs
│  └─ KdeSendQueue.cs
├─ Payloads/
│  ├─ KdePayloadReceiver.cs
│  ├─ KdePayloadSender.cs
│  └─ KdePayloadValidation.cs
└─ Mapping/
   ├─ KdeInboundMapper.cs
   └─ KdeOutboundMapper.cs
```

## 5. Protocol source-lock procedure

정확한 wire behavior는 구현 시작 시 특정 upstream commit으로 잠급니다.

`docs/protocol-lock.json` 예:

```json
{
  "androidRepository": "jeiel85/kdeconnect-android",
  "androidRef": "<commit-sha>",
  "desktopRepository": "KDE/kdeconnect-kde",
  "desktopRef": "<commit-sha>",
  "homebaseCompatibilityVersion": 1
}
```

다음 fixture를 실제 Android 앱과 packet capture/test harness로 생성합니다.

```text
identity-vCurrent.jsonl
pair-request-vCurrent.jsonl
pair-accept-vCurrent.jsonl
ping.jsonl
battery.jsonl
notification-posted.jsonl
notification-removed.jsonl
share-text.jsonl
share-url.jsonl
share-file-metadata.jsonl
runcommand-list.jsonl
runcommand-request.jsonl
```

민감한 certificate/private key/personal notification content는 fixture에 포함하지 않습니다.

## 6. Session state machine

```text
Stopped
  → Discovering
  → TcpConnected
  → TlsNegotiating
  → IdentityReceived
  → Unpaired | PairingPending | Trusted
  → Active
  → Closing
  → Disconnected
```

각 transition은 timeout과 reason을 기록합니다.

## 7. Pairing

필수 속성:

- local certificate
- remote certificate
- fingerprint
- verification code
- pairing expiry
- explicit user accept/reject
- trusted device persistence
- certificate change detection

정책:

- 기존 device ID인데 certificate가 변경되면 자동 trust 금지
- verification code는 Tray UI와 Telegram 관리자 채널 중 하나에 표시 가능
- pairing accept는 기본적으로 Tray에서 수행
- headless pairing은 명시적 opt-in
- pairing timeout 후 request 폐기

## 8. Capability mapping

KDE capability를 Homebase capability로 mapping합니다.

```text
kde packet capability
    → KdeCapabilityMapper
    → homebase capability id
```

예시 Homebase capability:

```text
device.ping.receive
device.battery.publish
notification.mobile.publish
share.text.send
share.text.receive
share.file.send
share.file.receive
command.predefined.invoke
media.control
clipboard.exchange
```

Plugin은 Homebase capability만 참조합니다.

## 9. Payload safety

파일 수신:

1. metadata validation
2. file name sanitize
3. configured max size
4. free disk check
5. temp file write
6. streaming SHA-256
7. size/hash validation
8. atomic move
9. audit
10. optional user notification

금지:

- 상대 경로
- 절대 경로
- reserved Windows name
- ADS syntax
- 자동 실행
- 수신 즉시 열기 기본값
- executable auto-open

## 10. Reconnection

- 동일 장치 중복 session 방지
- send queue는 session별
- payload 중단 시 기본적으로 재개하지 않고 실패 처리
- control packet은 reconnect 후 idempotent한 것만 retry
- paired device는 certificate와 device id 모두 검증

## 11. Compatibility test gate

KDE 기능 GOAL은 다음을 모두 통과해야 완료입니다.

- Android real device test
- Android emulator test 가능 시 수행
- unknown packet ignored safely
- malformed packet closes only session
- certificate mismatch rejected
- pair timeout
- reconnect
- payload cancel
- 0-byte file
- filename attack cases
- capability mismatch
