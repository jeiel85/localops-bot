# 08. Data and Migrations

## 1. Migration engine 개선

현재 schema version 방식은 유지하되 다음을 추가합니다.

- migration별 transaction
- migration checksum
- migration name
- started/completed logging
- 실패 시 rollback
- migration 전 DB backup
- downgrade는 자동 수행하지 않음

새 테이블:

```sql
schema_migration_history(
    version,
    name,
    checksum,
    started_at,
    completed_at,
    success,
    error
)
```

## 2. Device tables

### device

```text
device_id PK
display_name
device_type
transport_hint
protocol_version
certificate_fingerprint
trust_state
is_enabled
first_seen_at
last_seen_at
last_connected_at
metadata_json
```

### device_capability

```text
device_id FK
direction
capability
capability_version
updated_at
PK(device_id, direction, capability)
```

### device_plugin_setting

```text
device_id FK
plugin_id
enabled
settings_json
updated_at
PK(device_id, plugin_id)
```

### device_session

```text
session_id PK
device_id FK
transport_id
state
local_endpoint
remote_endpoint
connected_at
disconnected_at
close_reason
metadata_json
```

## 3. Delivery tables

### message_delivery

```text
id PK
message_id
trace_id
message_type
transport_id
target_endpoint
target_device_id
attempt
status
failure_kind
error
queued_at
started_at
sent_at
acknowledged_at
expires_at
```

Unique:

```text
(message_id, transport_id, target_endpoint, attempt)
```

### outbox_message

```text
message_id PK
envelope_json
priority
status
available_at
attempt_count
last_error
created_at
completed_at
```

## 4. Payload transfer

```text
transfer_id PK
message_id
device_id
direction
file_name
mime_type
size_bytes
sha256
temp_path
final_path
status
bytes_transferred
created_at
completed_at
error
```

Retention:

- successful transfer audit: 30 days
- failed transfer metadata: 14 days
- temp file: immediate cleanup or 24h recovery window
- no file contents in DB

## 5. Command log migration

기존 `command_log.chat_id NOT NULL`은 non-Telegram command에 적합하지 않습니다.

권장 방법:

- V2에서 `command_execution_log` 새 테이블 생성
- 기존 command log는 읽기 전용 legacy history로 유지
- 새 코드부터 새 테이블 사용
- 향후 V4에서 consolidation

```text
request_id
transport_id
endpoint_id
principal_id
source_device_id
command
args_json
raw_input
risk_level
status
error_code
error
received_at
started_at
completed_at
```

이 방식은 기존 테이블 rebuild보다 안전합니다.

## 6. Notification data policy

알림은 metadata와 content를 분리합니다.

```text
notification_metadata
notification_content
```

- metadata: app, id, time, state
- content: title/body, optional encryption or no-store
- Sensitive: content 저장 안 함
- Blocked: metadata도 최소화
- Normal: retention policy 적용

## 7. Device latest state

빠른 Dashboard 조회용:

```text
device_state(
  device_id,
  state_type,
  state_json,
  observed_at,
  expires_at,
  PK(device_id, state_type)
)
```

예:

- battery
- reachability
- active plugins
- last notification
- current media

## 8. Repository contracts

```csharp
IDeviceRepository
IDeviceCapabilityRepository
IDeviceSessionRepository
IDevicePluginSettingRepository
IDeliveryLogRepository
IOutboxRepository
IPayloadTransferRepository
ICommandExecutionRepository
IDeviceStateRepository
```

## 9. Transaction boundaries

- pairing success: device trust + fingerprint + capabilities 한 transaction
- outbox delivery: claim row atomic
- file completed: transfer status + final path 한 transaction
- command execution: start insert 후 completion update
- notification content 저장 실패가 notification forwarding을 막지 않음

## 10. SQL draft

`sql/migration_v2_device_hub.sql` 파일을 제공합니다.

실제 `SqliteMigrator`에서는 SQL을 그대로 한 번에 실행하기보다 migration method 단위로 transaction을 열고 version insert까지 같은 transaction에 포함합니다.
