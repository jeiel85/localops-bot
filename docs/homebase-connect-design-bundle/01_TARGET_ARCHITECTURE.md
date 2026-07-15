# 01. Target Architecture

## 1. 논리 구조

```text
┌────────────────────────────────────────────────────────────┐
│ External and Companion Clients                             │
│                                                            │
│  Telegram Client       KDE Connect Android      Local UI   │
└──────────┬──────────────────────┬──────────────────┬────────┘
           │                      │                  │
           ▼                      ▼                  ▼
┌────────────────────────────────────────────────────────────┐
│ Transport / Ingress Layer                                  │
│                                                            │
│ TelegramTransport   KdeConnectTransport   SessionBridge    │
└──────────┬──────────────────────┬──────────────────┬────────┘
           │                      │                  │
           └──────────────┬───────┴──────────────────┘
                          ▼
┌────────────────────────────────────────────────────────────┐
│ Application Routing                                        │
│                                                            │
│ RemoteCommandRouter   DeviceMessageRouter   OutboundRouter │
│ AuthorizationPolicy   DeliveryPlanner       LoopGuard      │
└──────────┬──────────────────────┬──────────────────┬────────┘
           │                      │                  │
           ▼                      ▼                  ▼
┌────────────────────────────────────────────────────────────┐
│ Domain and Plugins                                          │
│                                                            │
│ Existing Commands   Monitoring   Alerting   Device Plugins │
└──────────┬──────────────────────┬──────────────────┬────────┘
           │                      │                  │
           ▼                      ▼                  ▼
┌────────────────────────────────────────────────────────────┐
│ Platform and Persistence                                    │
│                                                            │
│ Windows APIs   SQLite   File/Payload Store   Tray Services │
└────────────────────────────────────────────────────────────┘
```

## 2. Target projects

```text
src/
├─ LocalOpsBot.Protocol/
│  ├─ Messaging/
│  ├─ Devices/
│  ├─ Capabilities/
│  └─ Serialization/
├─ LocalOpsBot.Core/
│  ├─ Commands/
│  ├─ Delivery/
│  ├─ Devices/
│  ├─ Plugins/
│  ├─ Security/
│  └─ Monitoring/
├─ LocalOpsBot.Data/
├─ LocalOpsBot.Infrastructure/
│  ├─ Windows/
│  ├─ Ipc/
│  ├─ Files/
│  └─ Security/
├─ LocalOpsBot.Transport.Telegram/
├─ LocalOpsBot.Transport.KdeConnect/
├─ LocalOpsBot.Agent/
└─ LocalOpsBot.Tray/
```

기존 프로젝트를 즉시 전부 이동하지 않습니다.

- GOAL-01~03에서는 Telegram 구현을 기존 `Infrastructure`에 둡니다.
- Transport abstraction이 안정화된 후 `Transport.Telegram`으로 이동합니다.
- KDE 관련 구현은 처음부터 별도 프로젝트에 둡니다.
- `LocalOpsBot.Protocol`은 Windows, Telegram, KDE 라이브러리를 참조하지 않습니다.

## 3. Dependency rules

```text
LocalOpsBot.Protocol
    └─ BCL only

LocalOpsBot.Core
    └─ Protocol

LocalOpsBot.Data
    ├─ Core
    └─ Protocol

LocalOpsBot.Infrastructure
    ├─ Core
    └─ Protocol

LocalOpsBot.Transport.Telegram
    ├─ Core
    └─ Protocol

LocalOpsBot.Transport.KdeConnect
    ├─ Core
    └─ Protocol

LocalOpsBot.Agent
    ├─ Core
    ├─ Data
    ├─ Infrastructure
    ├─ Transport.Telegram
    └─ Transport.KdeConnect

LocalOpsBot.Tray
    ├─ Core
    ├─ Protocol
    └─ Infrastructure
```

금지되는 참조:

- Core → Telegram
- Core → KDE
- Core → WPF
- Protocol → Core
- Telegram Transport → KDE Transport
- Tray → Data
- Plugin → 구체 Transport

## 4. Runtime ownership

### Agent

- Device registry
- Transport lifecycle
- Pairing and trust
- Command routing
- Delivery planning
- Monitoring
- Alert policy
- SQLite
- Payload transfer coordination
- Telegram polling
- KDE network connection
- Windows Service operations

### Tray

- Windows user notification access
- User-session clipboard
- URL and file open
- Media session
- Notification action invocation
- Interactive confirmation
- Dashboard and settings
- Tray process health report

### Data

- Durable trusted device records
- Capability cache
- Delivery attempts
- Command audit
- Payload transfer audit
- Existing metrics and alerts

## 5. Layer boundaries

### Protocol layer

Wire-neutral data only:

- `DeviceEnvelope`
- `EndpointAddress`
- `DeviceIdentity`
- `CapabilityDescriptor`
- `PayloadDescriptor`
- protocol versions

### Core layer

Business behavior:

- delivery policy
- command authorization
- device state transition
- plugin activation
- loop prevention
- risk classification

### Adapter layer

External representation:

- Telegram text and buttons
- KDE packet types
- Named Pipe frames
- Windows API calls

## 6. Error containment

각 Transport는 독립적인 Hosted Service로 실행합니다.

```text
TelegramTransport failure
    → Telegram만 degraded
    → KDE, monitoring, Tray 유지

KdeConnectTransport failure
    → LAN device 기능만 degraded
    → Telegram과 monitoring 유지

Tray disconnect
    → user-session 기능만 unavailable
    → Agent와 transports 유지

SQLite write failure
    → 전달 시도는 정책에 따라 계속
    → audit failure를 별도 로그
```

Hosted Service가 처리하지 않은 예외를 밖으로 던져 Agent 전체를 종료시키지 않도록 supervisor wrapper를 둡니다.
