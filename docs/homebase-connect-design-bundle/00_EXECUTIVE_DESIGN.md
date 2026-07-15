# 00. Executive Design

## 1. 제품 정의 변경

기존 정의:

> 개인 Windows PC의 상태와 알림을 Telegram으로 확인하는 모니터링 봇

목표 정의:

> Homebase는 Windows PC를 중심으로 Telegram, Android, 로컬 사용자 세션을 연결하는 개인 Device Hub다.

핵심 변화는 기능 추가가 아니라 **채널과 도메인 로직의 분리**입니다.

현재 형태:

```text
Monitor → AlertDispatcher → TelegramClient
TelegramPollingService → BotCommand → CommandRouter → Telegram reply
Tray → NotificationPipe → Agent → Telegram
```

목표 형태:

```text
Domain Event → Delivery Planner → Outbound Router → Transport Adapter
Transport Ingress → Remote Command / Device Message → Router → Response Sink
Tray ↔ Session Bridge ↔ Agent
```

## 2. 성공 기준

### 기능 기준

- 기존 Telegram 명령과 자동 알림이 계속 동작합니다.
- KDE Connect Android에서 Homebase PC를 발견하고 페어링할 수 있습니다.
- Android 알림을 Homebase Tray 또는 Agent에서 확인할 수 있습니다.
- Android 배터리 상태를 `/devices` 또는 Dashboard에서 볼 수 있습니다.
- Android에서 Homebase로 URL·텍스트·파일을 보낼 수 있습니다.
- Android에서 사전 정의 명령을 실행할 수 있습니다.
- KDE 장치가 연결되지 않은 경우 중요 PC 알림은 Telegram으로 전달됩니다.
- Agent와 Tray가 양방향으로 요청과 이벤트를 주고받습니다.

### 품질 기준

- 기존 Telegram 회귀 테스트 통과
- Transport 장애가 Agent 전체를 종료시키지 않음
- 중복 메시지와 전달 루프 방지
- DB migration rollback 또는 복구 절차 제공
- 민감 알림의 저장·전송 정책 분리
- 원격 명령은 권한 등급과 명시적 allowlist 적용
- 프로토콜 fixture 기반 호환 테스트 제공

## 3. 범위

### 1차 범위

- Transport abstraction
- Device registry
- Capability negotiation
- Typed envelope
- 양방향 Agent–Tray IPC
- KDE identity / pairing / ping 기반 연결
- Battery
- Android notification receive
- Share text / URL / file
- Predefined run command
- Telegram fallback policy

### 후속 범위

- 알림 dismiss 및 reply
- Media control
- Clipboard
- Find my phone
- Screenshot
- Virtual touchpad / keyboard

### 제외

- 임의 PowerShell·CMD 실행
- SMS 발신
- 통화 제어
- 무제한 파일 시스템 탐색
- 인터넷 공개 포트
- 계정 서버
- 클라우드 릴레이
- KDE Connect 전체 기능 복제

## 4. 현실적 채널 역할

| 흐름 | 초기 주 채널 | 이유 |
|---|---|---|
| PC → 모바일 부팅/장애 알림 | KDE 우선 + Telegram fallback | LAN에서는 직접 전달하고 외부망·오프라인에 대비 |
| Android → PC 알림 | KDE Connect | Android Notification Listener 활용 |
| Android → PC 배터리 | KDE Connect | 장치 상태 동기화에 적합 |
| URL·텍스트·파일 공유 | KDE Connect | LAN 직접 전송 |
| 원격 PC 상태 조회 | 요청이 들어온 채널 | 응답 상관관계 유지 |
| PC 제어 명령 | Telegram 또는 KDE | 동일 CommandRouter 사용 |
| PC → Android 일반 알림 | KDE Connect | KDE의 Send Notifications 기능 사용 |
| 긴급 알림 | Both | 로컬 직접 전달과 외부망 전달을 동시에 확보 |

## 5. 중요한 설계 결정

### ADR-001: Telegram을 제거하지 않는다

Telegram은 Homebase의 외부 접근 및 비상 채널로 유지합니다.

### ADR-002: KDE Connect는 Adapter다

Homebase Core가 KDE packet type을 직접 알지 않게 합니다. KDE packet은 Transport Adapter에서 Homebase message 또는 command로 변환합니다.

### ADR-003: Domain event와 wire packet을 분리한다

`AlertEvent` 같은 도메인 모델을 KDE 또는 Telegram 포맷으로 오염시키지 않습니다.

### ADR-004: Agent와 Tray는 양방향 IPC를 사용한다

알림 수집뿐 아니라 Clipboard, URL open, Notification action, Media control에 필요합니다.

### ADR-005: raw shell을 금지한다

원격 명령은 `ICommandHandler` 또는 서명된 `CommandDefinition`으로만 실행합니다.

### ADR-006: stock Android app 우선

전용 Homebase Android 앱은 프로토콜과 사용성이 검증된 뒤 개발합니다.
