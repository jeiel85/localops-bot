# 09. Configuration and Security

## 1. Config sections

```text
transports.telegram
transports.kdeConnect
delivery
devices
pairing
sessionBridge
plugins
payloads
security
retention
```

기존 `telegram` section은 migration 기간에 계속 읽습니다.

우선순위:

```text
transports.telegram
→ 없으면 legacy telegram
```

## 2. KDE options

```json
{
  "enabled": false,
  "deviceName": "Homebase PC",
  "listenAddresses": ["0.0.0.0"],
  "pairingMode": "Interactive",
  "pairingTimeoutSeconds": 30,
  "certificatePath": "%ProgramData%/Homebase/security/kde-device.pfx",
  "maxConcurrentSessions": 4,
  "maxControlPacketBytes": 1048576,
  "maxPayloadBytes": 1073741824
}
```

포트와 상세 wire option은 source-lock한 KDE protocol 기준으로 확정합니다. 사용자가 임의로 바꾸는 고급 설정은 기본 UI에서 숨깁니다.

## 3. Delivery options

```json
{
  "defaultPolicy": "TelegramFallback",
  "fallbackDelaySeconds": 15,
  "criticalPolicy": "Both",
  "warningPolicy": "TelegramFallback",
  "infoPolicy": "LocalPreferred",
  "maxHops": 4,
  "replayWindowMinutes": 10
}
```

## 4. Pairing options

```json
{
  "allowNewDevices": true,
  "requireTrayConfirmation": true,
  "allowTelegramConfirmation": false,
  "rememberTrustedDevices": true,
  "rejectCertificateChanges": true
}
```

## 5. Secret storage

- Telegram token: environment variable 또는 DPAPI protected storage
- KDE private key: PFX + machine ACL, private key export 금지
- IPC secret: DPAPI LocalMachine 또는 user-scoped secret
- config file에 raw private key 금지
- logs에 token, certificate private material 금지

## 6. Remote command security

- Handler allowlist
- risk level
- transport allowlist
- device allowlist
- principal trust
- rate limit
- replay protection
- confirmation
- audit

절대 구현 금지:

```text
/run powershell ...
/exec ...
raw command string from KDE packet
arbitrary executable + arbitrary args
```

## 7. File transfer security

- max total and per-file size
- extension policy
- Windows reserved name validation
- path traversal validation
- SHA-256
- temp folder ACL
- executable quarantine
- user confirmation option
- auto-open OFF
- free disk threshold

## 8. Notification privacy

App privacy modes:

```text
Allow
HideContent
BlockImages
MetadataOnly
Block
```

기본 block:

- password manager
- authenticator
- Windows Security
- banking apps
- Homebase-generated notification
- KDE Connect-generated notification
- Telegram feedback source

## 9. Logging classification

```text
Public
Operational
Sensitive
Secret
```

- Public/Operational: normal logs
- Sensitive: hash or redact
- Secret: never log

## 10. Firewall and network

KDE compatibility 기능이 활성화될 때만 Windows Firewall rule을 추가합니다.

- Private network profile only by default
- Public profile disabled
- inbound rule scoped to executable
- uninstall removes rule
- disable feature disables or removes rule
- no internet port forwarding
- no UPnP

## 11. Threat model

주요 위협:

- 같은 LAN의 위조 device
- certificate change/MITM
- replayed command
- malicious file name
- notification data leakage
- IPC impersonation
- command privilege escalation
- feedback loop and alert storm
- DB tampering
- local low-privilege user connecting to SYSTEM pipe

각 위협에 대응 테스트를 `11_TEST_AND_CONFORMANCE.md`에 정의합니다.
