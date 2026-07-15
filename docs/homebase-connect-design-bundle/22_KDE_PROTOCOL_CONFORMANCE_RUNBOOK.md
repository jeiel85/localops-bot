# 22. KDE Protocol Conformance Runbook

## 1. Source lock

```json
{
  "androidRepository": "jeiel85/kdeconnect-android",
  "androidCommit": "<exact SHA>",
  "desktopRepository": "https://invent.kde.org/network/kdeconnect-kde",
  "desktopCommit": "<exact SHA>",
  "observedAt": "ISO-8601",
  "portRange": "1714-1764/tcp+udp"
}
```

## 2. Verified official behavior

KDE UserBase 기준:

- desktop와 phone component
- UDP broadcast discovery
- TCP/UDP dynamic ports `1714-1764`
- phone → desktop notifications
- desktop → phone notifications
- predefined run commands
- files, URLs, plain text sharing

## 3. Fixture directory

```text
tests/Fixtures/KdeConnect/<android-commit>/
├─ metadata.json
├─ discovery/
├─ identity/
├─ pairing/
├─ ping/
├─ battery/
├─ notifications/
├─ share/
└─ runcommand/
```

## 4. Required scenarios

### Pairing

- both directions
- accept/reject
- timeout
- unpair
- changed certificate

### Notification

- Android post/update/remove
- Windows post/update/remove
- icon absent/present
- blocked app
- sensitive content
- action/reply

### Share

- text
- URL
- zero-byte file
- 1 KiB
- 100 MiB
- cancel
- duplicate filename

### Run command

- catalog
- invocation
- success
- disabled
- duplicate request ID

## 5. Fixture record

```json
{
  "sequence": 1,
  "direction": "android-to-homebase",
  "transport": "lan",
  "sessionState": "paired",
  "packetType": "<observed exact type>",
  "body": {},
  "payload": {
    "present": false,
    "size": 0,
    "fixtureFile": null
  }
}
```

## 6. Production gate

- supported packet마다 fixture
- required/optional fields 문서화
- malformed fixture tests
- real-device roundtrip
- personal data 제거
