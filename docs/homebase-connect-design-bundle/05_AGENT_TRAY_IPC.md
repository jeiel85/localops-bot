# 05. Agent–Tray IPC

## 1. 현재 구조의 교체 이유

현재 알림 전용 단방향 Pipe로는 다음 작업을 지원하기 어렵습니다.

- Android 알림 action 실행
- Clipboard write
- URL open
- File open
- Media control
- 사용자 확인
- Tray 상태 조회

목표는 request/response/event를 모두 지원하는 양방향 IPC입니다.

## 2. Pipe topology

초기 구현:

```text
\\.\pipe\Homebase.SessionBridge
Agent: server
Tray: client
Direction: InOut
Max instances: 8
```

멀티 사용자 세션 지원을 위해 연결 후 Windows Session ID를 등록합니다.

## 3. Frame format

```text
[4-byte unsigned little-endian body length]
[UTF-8 JSON body]
```

규칙:

- header는 `ReadExactlyAsync` 사용
- control frame 최대 1 MiB
- 0 또는 초과 길이는 즉시 연결 종료
- JSON depth 제한
- binary payload를 JSON base64로 전송하지 않음
- 파일은 임시 파일 descriptor 또는 별도 stream channel 사용

## 4. Message shape

```json
{
  "schemaVersion": 1,
  "messageId": "uuid",
  "kind": "request",
  "operation": "clipboard.set",
  "correlationId": null,
  "sentAt": "2026-07-14T12:00:00Z",
  "expiresAt": "2026-07-14T12:00:30Z",
  "body": {
    "text": "hello"
  }
}
```

`kind`:

- `hello`
- `challenge`
- `challenge_response`
- `request`
- `response`
- `event`
- `cancel`
- `heartbeat`
- `goodbye`

## 5. Handshake

```text
1. Tray connects.
2. Tray sends hello: pid, sessionId, userSid, clientVersion, nonce.
3. Agent obtains actual client identity through pipe impersonation.
4. Agent verifies claimed SID and session.
5. Agent sends random challenge.
6. Tray signs or HMACs challenge using per-install IPC secret protected by DPAPI.
7. Agent verifies response.
8. Both negotiate schema version and capabilities.
9. Connection becomes Active.
```

최소 버전에서는 SID 검증 + per-install random secret을 사용합니다.

## 6. Pipe ACL

허용:

- LocalSystem
- Built-in Administrators
- 현재 설치 사용자 SID 또는 활성 user session SID

금지:

- 전체 Authenticated Users
- Everyone
- Anonymous

멀티 사용자 지원 전에는 installer가 선택한 사용자 SID를 config에 저장합니다.

## 7. Session operations

### Tray → Agent events

```text
toast.posted
toast.updated
toast.removed
clipboard.changed
media.session.changed
tray.ready
tray.health
user.confirmation.result
```

### Agent → Tray requests

```text
toast.dismiss
toast.invokeAction
clipboard.get
clipboard.set
url.open
file.open
media.playPause
media.next
media.previous
media.setVolume
user.confirm
```

## 8. Request dispatcher

```csharp
public interface ISessionOperationHandler
{
    string Operation { get; }
    Task<SessionOperationResult> HandleAsync(
        SessionOperationRequest request,
        CancellationToken ct);
}
```

Handler는 Tray에서 DI로 등록합니다.

## 9. Timeout

| Operation | Timeout |
|---|---:|
| heartbeat | 5s |
| clipboard get/set | 3s |
| URL open | 5s |
| notification action | 10s |
| user confirmation | 60s |
| file open | 10s |
| media command | 5s |

## 10. Disconnect behavior

- pending request를 `UserSessionUnavailable`로 종료
- 자동 재연결: 1s, 2s, 5s, 10s, 30s
- heartbeat interval 15s
- 3회 누락 시 disconnect
- Agent는 연결 종료 후 user-session 기능만 degraded 처리

## 11. Migration path

1. 기존 Notification Pipe 유지
2. 새 Session Bridge를 feature flag로 추가
3. Toast event를 두 pipe로 shadow send
4. 결과 비교
5. 새 Bridge 기본화
6. 기존 Pipe 제거

## 12. Security logging

로그에 남길 것:

- connection accepted/rejected
- SID/session mismatch
- protocol version
- operation name
- duration
- result code

로그에 남기지 않을 것:

- clipboard text
- notification body
- file contents
- challenge secret
- authentication token
