# 10. GOAL Roadmap

각 GOAL은 독립 PR과 검증 가능한 완료 기준을 가집니다.

---

## GOAL-00: Baseline and Regression Lock

### 목적

현재 Homebase 동작을 변경하기 전에 기준선을 고정합니다.

### 작업

- 현재 release version 기록
- `/ping`, `/status`, `/alerts`, boot alert 수동 테스트
- Telegram integration test harness
- current SQLite backup/restore test
- current notification forwarding test
- architecture decision log 시작

### 완료 기준

- 기존 테스트 전부 통과
- Telegram golden output 확보
- fresh install smoke test 문서화
- rollback installer 확보

---

## GOAL-01: Protocol Project and Core Models

### 작업

- `LocalOpsBot.Protocol` 생성
- `DeviceEnvelope`
- `EndpointAddress`
- `PayloadDescriptor`
- validation
- JSON serialization options
- JSON Schema 추가

### 수정 파일

```text
LocalOpsBot.sln
src/LocalOpsBot.Protocol/*
tests/.../Protocol/*
```

### 완료 기준

- Protocol은 BCL 외 의존성 없음
- roundtrip serialization test
- invalid envelope test
- schema validation fixture

---

## GOAL-02: Remote Command Abstraction

### 작업

- `RemoteCommand`
- `CommandPrincipal`
- `RemoteCommandResult`
- `IRemoteCommandHandler`
- compatibility adapter
- 기존 Handler 단계적 변환

### 완료 기준

- Telegram 명령 결과 변화 없음
- Handler가 Telegram model을 참조하지 않음
- origin endpoint로 응답
- risk level unit test

---

## GOAL-03: Telegram Transport Adapter

### 작업

- Telegram polling을 ingress adapter로 변경
- Telegram renderer
- Telegram endpoint mapping
- authorization principal mapping
- 기존 `ITelegramClient` 유지

### 완료 기준

- 모든 기존 Telegram 명령 통과
- unauthorized behavior 동일
- offset persistence 동일
- token 401 handling 동일
- Core에 Telegram namespace 참조 없음

---

## GOAL-04: Outbound Router and Alert Migration

### 작업

- `OutboundNotification`
- `IOutboundRouter`
- `IDeliveryPlanner`
- `ITransportRegistry`
- Telegram notification channel
- `AlertDispatcher`에서 Telegram 제거

### 완료 기준

- 기존 alert formatting 결과 동일
- mute/dedup/rate limit 유지
- delivery log 생성
- Telegram failure가 Agent를 종료하지 않음

---

## GOAL-05: Device Registry and SQLite V2

### 작업

- V2 migration
- device repositories
- device service
- state transition validator
- Dashboard read model

### 완료 기준

- V1 DB upgrade 성공
- upgrade 전후 기존 데이터 보존
- migration failure rollback
- device CRUD/revoke tests

---

## GOAL-06: Bidirectional Session Bridge

### 작업

- duplex pipe
- handshake
- SID validation
- request/response/event multiplexing
- heartbeat
- reconnect
- feature flag shadow mode

### 완료 기준

- Toast shadow event 일치
- partial header read test
- malformed frame isolation
- unauthorized SID rejected
- pending request disconnect handling
- legacy pipe fallback

---

## GOAL-07: KDE Protocol Conformance Spike

### 목적

production code 전에 실제 Android 앱과 wire contract를 잠급니다.

### 작업

- upstream/fork commit lock
- packet capture or instrumentation
- fixture generator
- identity/pair/ping fixtures
- minimal console peer prototype
- protocol assumptions 문서화

### 완료 기준

- Android가 prototype을 발견
- pair/unpair 성공
- ping 왕복
- reconnect
- fixture가 개인정보를 포함하지 않음
- production 구현 여부 검토 통과

---

## GOAL-08: KDE Transport Foundation

### 작업

- discovery
- identity codec
- certificate store
- TLS session
- pairing coordinator
- device session registry
- send queue
- capability mapper

### 완료 기준

- Agent hosted service로 동작
- Telegram과 동시에 실행
- malformed packet은 해당 session만 종료
- certificate mismatch 거부
- paired device persistence
- restart 후 reconnect

---

## GOAL-09: Devices and Pairing UI

### Dashboard 추가

- discovered devices
- trusted devices
- connected state
- verification code
- accept/reject
- unpair/revoke
- capability list
- plugin toggle

### 완료 기준

- UAC 없이 user-level pairing UI
- Agent에 pair decision 전달
- expired request 처리
- certificate change 경고
- accessibility keyboard navigation

---

## GOAL-10: Battery and Device Status

### 작업

- BatteryPlugin
- device latest state
- `/devices`
- `/device`
- Dashboard device card
- optional low battery alert

### 완료 기준

- real Android battery update
- update dedup
- stale state 표시
- reconnect 후 refresh

---

## GOAL-11: Android Notification Receive

### 작업

- KDE notification mapper
- privacy filter
- notification metadata/content store
- Windows Toast display option
- remove/update handling
- feedback loop guard

### 완료 기준

- allowed app notification 표시
- blocked app content 저장 안 함
- update가 duplicate로 보이지 않음
- remove 반영
- Telegram/KDE/Homebase loop 없음

---

## GOAL-12: Share Text and URL

### 작업

- share packet mapping
- clipboard set through Tray
- URL open confirmation policy
- audit

### 완료 기준

- Android → PC text
- Android → PC URL
- PC → Android text/URL
- unsafe scheme block
- clipboard content logging 없음

---

## GOAL-13: File Transfer

### 작업

- streaming payload
- temp store
- size/hash validation
- file name sanitize
- user destination via Tray
- progress/cancel

### 완료 기준

- 0-byte, small, large file
- cancel
- insufficient disk
- path traversal attack rejected
- executable not auto-opened
- temp cleanup

---

## GOAL-14: Predefined Run Command

### 작업

- command definitions
- KDE command list mapping
- handler invocation
- risk/authorization
- confirmation

### 완료 기준

- read-only command
- lock PC
- approved service action
- unauthorized device rejected
- replayed command not re-executed
- no raw shell path

---

## GOAL-15: Delivery Policy and Fallback

### 작업

- reachable device awareness
- priority policy
- fallback delay
- acknowledgement
- outbox
- dead letter

### 완료 기준

- critical Both
- warning fallback
- duplicate prevention
- offline target behavior
- expiry
- retry classification

---

## GOAL-16: Media Control

### 완료 기준

- active Windows media session
- play/pause/next/previous
- Tray unavailable behavior
- per-device permission

---

## GOAL-17: Clipboard

### 완료 기준

- opt-in
- manual send
- length limit
- password manager privacy
- no persistence
- loop prevention

---

## GOAL-18: Release Hardening

### 작업

- installer firewall rule
- certificate generation
- config migration
- diagnostics
- uninstall cleanup
- recovery guide

### 완료 기준

- fresh Windows 10/11 install
- upgrade from current release
- uninstall
- feature disable
- firewall cleanup
- no Defender-unfriendly driver 추가
