# 11. Test and Conformance Plan

## 1. Test layers

```text
Unit
Contract
Integration
Protocol fixture
Real device
Installer
Security
Soak
```

## 2. Unit tests

### Envelope

- valid roundtrip
- missing type
- expired
- max hop
- body size
- payload metadata mismatch

### Router

- unknown message type
- plugin selection
- disabled plugin
- handler exception isolation
- origin response

### Delivery

- LocalPreferred
- TelegramFallback
- Both
- quiet hours
- critical bypass
- dedup
- retry
- expiry

### Authorization

- Telegram allowed chat
- unpaired device
- revoked device
- risk level
- confirmation
- replay

## 3. IPC tests

- header split into 1+3 bytes
- body split into multiple reads
- oversized frame
- negative/zero equivalent length
- malformed JSON
- unsupported schema
- duplicate request id
- timeout
- cancel
- disconnect during response
- SID mismatch
- heartbeat loss
- concurrent requests

## 4. SQLite tests

- V1 → V2
- already V2
- transaction rollback
- interrupted migration
- backup restore
- concurrent outbox claim
- foreign key enforcement
- retention cleanup

## 5. KDE fixture tests

- identity decode/encode
- pair request/accept/reject
- unknown property tolerance
- unknown packet type
- capability mismatch
- invalid certificate
- changed certificate
- control packet size
- payload metadata
- notification update/remove
- share URL/text/file
- command request

## 6. Real device matrix

| Device | Android | Network | Required |
|---|---|---|---:|
| Galaxy S24 | current user OS | home Wi-Fi | Yes |
| Android emulator | supported API | host LAN/NAT | Recommended |
| second Android device | optional | home Wi-Fi | Optional |

테스트 네트워크:

- Private Wi-Fi
- Public profile
- Windows Firewall on
- VPN/WARP on and off
- sleep/resume
- Wi-Fi reconnect
- PC reboot
- Android battery optimization enabled

## 7. Security tests

- unpaired packet injection
- replay command
- certificate substitution
- malicious filename
- path traversal
- reserved Windows name
- huge declared payload
- truncated payload
- unauthorized pipe client
- sensitive log scan
- feedback loop
- alert flood
- malformed notification action

## 8. Soak test

24시간:

- Telegram polling
- KDE reconnect
- notification volume
- no handle leak
- no socket leak
- memory trend
- DB growth
- temp cleanup
- Tray reconnect

## 9. Performance targets

- command routing p95 under 100ms excluding collector/network
- local ping response under 500ms on normal LAN
- notification processing under 1s
- Tray IPC request p95 under 200ms
- 100 concurrent queued outbound messages without loss
- 1 GiB file streaming without loading full file into memory

## 10. Release gate

- all unit/contract tests
- upgrade test
- real device pair
- notification receive
- share file
- command authorization
- restart/reconnect
- uninstall cleanup
- no secret in logs
