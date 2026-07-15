# 12. Release and Operations

## 1. Feature flags

```text
deviceHub.enabled
sessionBridgeV2.enabled
kdeConnect.enabled
plugins.mobileNotifications.enabled
plugins.share.enabled
plugins.runCommand.enabled
delivery.outbox.enabled
```

모든 새 기능 기본값 OFF로 시작하고, 안정화된 GOAL에서 기본값을 변경합니다.

## 2. Versioning

- Homebase app version: semantic version
- envelope schema version: integer
- IPC schema version: integer
- KDE compatibility version: integer
- DB schema version: integer

서로 독립적으로 관리합니다.

## 3. Diagnostics

`/diagnostics`와 Dashboard에 추가:

- Telegram transport state
- KDE transport state
- active sessions
- paired devices
- last discovery
- certificate fingerprint
- Session Bridge state
- outbox pending/dead count
- payload temp usage
- active plugins
- DB schema version

민감 정보는 표시하지 않습니다.

## 4. Logs

```text
agent-.log
tray-.log
transport-telegram-.log
transport-kde-.log
ipc-.log
migration-.log
```

실제 구현에서는 Serilog sink를 파일별로 과도하게 분리하기보다 SourceContext 기반 filter를 사용할 수 있습니다.

## 5. Installer

KDE 기능 선택 시:

- certificate 생성
- security folder ACL
- Private profile firewall rule
- config section
- optional feature 안내

Uninstall:

- service
- tray startup
- firewall rule
- temp payload
- IPC secret
- certificate 삭제 여부 질문
- user data 보존 옵션

## 6. Upgrade

현재 버전 → Device Hub 버전:

1. service stop
2. DB backup
3. binary replace
4. config migration
5. DB migration
6. service start
7. health check
8. 실패 시 binary rollback + DB restore 안내

## 7. Recovery

문서화할 장애:

- Android가 PC를 찾지 못함
- pairing code 불일치
- certificate changed
- Windows Firewall
- VPN adapter interference
- Tray disconnected
- Telegram only works
- DB migration failed
- payload stuck
- duplicate notification loop

## 8. Observability

로컬 전용 원칙을 유지합니다.

- cloud telemetry 없음
- analytics 없음
- crash upload 없음
- local diagnostics export만 제공
- export 전 secret redaction

## 9. Release phases

### Alpha

- developer machine only
- feature flags
- verbose logs
- manual pairing

### Beta

- installer support
- Dashboard
- upgrade test
- limited plugin set

### Stable

- Telegram regression complete
- real-device soak
- security review
- recovery docs
- config migration
