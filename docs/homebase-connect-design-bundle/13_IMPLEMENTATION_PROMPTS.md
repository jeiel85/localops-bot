# 13. Implementation Prompts

아래 블록은 바이브 코딩 모델에 GOAL별로 전달할 수 있는 작업 지시입니다.

## Prompt 1 — Foundation

```text
Repository: jeiel85/homebase

Implement GOAL-01 from docs/10_GOAL_ROADMAP.md.

Create LocalOpsBot.Protocol as a net9.0 class library with no external package dependencies.
Implement DeviceEnvelope, EndpointAddress, PayloadDescriptor, enums, validation, and JSON serialization.
Add the project to LocalOpsBot.sln and reference it from LocalOpsBot.Core.
Add unit tests for roundtrip, invalid IDs, expiration, hop limit, and payload metadata.

Constraints:
- Do not change runtime behavior.
- Do not move Telegram code.
- Do not introduce Windows dependencies into Protocol.
- All public contracts require XML documentation.
- Run dotnet build and dotnet test.
Return a file-by-file summary and test results.
```

## Prompt 2 — Command abstraction

```text
Implement GOAL-02.

Introduce RemoteCommand, CommandPrincipal, RemoteCommandResult, IRemoteCommandHandler, and command risk levels.
Create a compatibility adapter from the existing BotCommand.
Migrate handlers incrementally without changing Telegram user-visible output.
Ensure command handlers no longer depend on Telegram model types after migration.

Do not remove existing Telegram polling.
Do not add arbitrary shell execution.
Add authorization and routing tests.
```

## Prompt 3 — Outbound router

```text
Implement GOAL-04.

Refactor AlertDispatcher so it no longer depends on ITelegramClient or TelegramOptions.
Introduce OutboundNotification, IOutboundRouter, IDeliveryPlanner, and a Telegram channel adapter.
Preserve current mute, dedup, rate-limit, localization, and HTML output behavior.

Use golden tests to prove the output is unchanged.
A Telegram send failure must not terminate the Agent.
```

## Prompt 4 — Session Bridge

```text
Implement GOAL-06 behind sessionBridgeV2.enabled.

Create a duplex Named Pipe protocol supporting hello, challenge, request, response, event, cancel, heartbeat, and goodbye.
Use ReadExactlyAsync semantics for the 4-byte header and body.
Set a 1 MiB control frame limit.
Validate the actual pipe client's Windows identity and do not grant Authenticated Users globally.
Add timeout, correlation, reconnect, and disconnect handling.

Keep the legacy notification pipe available as fallback.
Add integration tests for partial reads, oversized frames, malformed JSON, timeout, cancellation, and unauthorized client.
```

## Prompt 5 — KDE spike

```text
Implement GOAL-07 as a prototype, not production code.

Lock exact commits for jeiel85/kdeconnect-android and the official kdeconnect-kde repository.
Inspect identity, pairing, TLS, ping, capability, and payload behavior.
Create sanitized protocol fixtures.
Build a console peer proving Android discovery, pairing, ping, disconnect, and reconnect.

Do not copy GPL source code into Homebase.
Document every protocol assumption with the source file and locked commit.
Do not merge the spike into production transport until the conformance report is complete.
```

## Prompt 6 — KDE transport

```text
Implement GOAL-08 using the approved conformance report.

Create LocalOpsBot.Transport.KdeConnect.
Keep KDE packet names inside the adapter.
Map KDE identity and capabilities into Homebase DeviceRecord and Homebase capability IDs.
Implement certificate persistence, pairing state, session registry, send queue, packet size limits, and reconnect.

Failures must be isolated to the KDE transport or individual session.
Telegram and all existing monitoring must continue working when KDE is disabled or broken.
```

## Prompt 7 — Notifications

```text
Implement GOAL-11.

Receive Android notification events through KdeConnectTransport and map them to MobileNotificationEvent.
Apply app privacy modes before persistence or display.
Support posted, updated, and removed states.
Optionally show the notification as a Windows Toast through the Tray Session Bridge.
Prevent loops from Telegram, KDE Connect, Homebase, and Phone Link.

Telegram forwarding of Android notifications must default to OFF.
Add privacy and loop tests.
```

## Prompt 8 — File transfer

```text
Implement GOAL-13.

Stream payloads to a protected Agent temp folder.
Validate file name, size, free space, and SHA-256.
Never load the full file into memory.
After validation, request the Tray to move the file into the configured user download folder.
Do not auto-open executable files.
Support progress and cancellation.
Add tests for traversal, reserved names, truncation, oversized payload, cancel, and cleanup.
```

## Prompt 9 — Run command

```text
Implement GOAL-14.

Expose only predefined IRemoteCommandHandler entries to KDE Connect and Telegram.
Add command risk levels, transport/device allowlists, replay protection, audit, and optional user confirmation through Tray.
Do not accept executable paths, PowerShell, CMD, or raw argument strings from a remote peer.

Start with status, lock PC, mute, unmute, and approved service restart handlers.
```

## Prompt 10 — Final audit

```text
Audit the completed Device Hub implementation against every completion criterion in docs/10_GOAL_ROADMAP.md and docs/11_TEST_AND_CONFORMANCE.md.

Report:
- unmet criteria
- architecture boundary violations
- security issues
- protocol assumptions
- missing tests
- installer/update risks
- backward compatibility risks
- license risks

Do not modify code in the first pass. Produce an evidence-backed audit with exact file paths and line references.
```
