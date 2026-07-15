# Start Here — Implementation-Ready Guide

이 문서는 기존 설계 묶음을 실제 Homebase 저장소에 적용하는 시작점입니다.

## 1. 구현 전략

전체 기능을 한 번에 구현하지 않습니다.

```text
TRAIN-A: Foundation
  GOAL-00 ~ GOAL-05
  Protocol, command compatibility, outbound routing, DB V2

TRAIN-B: Local Session
  GOAL-06
  Agent ↔ Tray 양방향 IPC

TRAIN-C: KDE Compatibility
  GOAL-07 ~ GOAL-10
  conformance spike, discovery, pairing, battery, device UI

TRAIN-D: User Features
  GOAL-11 ~ GOAL-18
  notifications, share, file, commands, media, clipboard, release
```

TRAIN-A가 끝날 때까지 기존 Telegram UX가 바뀌면 안 됩니다.

## 2. 첫 작업 브랜치

```powershell
git checkout main
git pull
git checkout -b feature/device-hub-foundation

dotnet restore LocalOpsBot.sln
dotnet build LocalOpsBot.sln -c Debug
dotnet test LocalOpsBot.sln -c Debug
```

현재 Homebase가 .NET 9 target이므로 .NET 9 SDK가 필요합니다.

## 3. 구현 패키지 적용

```powershell
Set-ExecutionPolicy -Scope Process Bypass

.\path\to\implementation-kit\phase-1\apply-phase1.ps1 `
  -RepoRoot (Get-Location).Path `
  -Apply
```

그 다음:

```powershell
.\path\to\implementation-kit\phase-1\verify-phase1.ps1 `
  -RepoRoot (Get-Location).Path
```

기존 파일은 자동 덮어쓰지 않습니다. `patch-guides`를 숫자 순서대로 적용합니다.

## 4. 첫 PR 완료 조건

```text
feat: add transport-neutral protocol and outbound routing foundation
```

- `LocalOpsBot.Protocol` 프로젝트 포함
- Core → Protocol reference
- 기존 Telegram 명령 출력 동일
- AlertPolicy 동작 동일
- `AlertDispatcher`에서 `ITelegramClient` 직접 의존 제거
- 신규 테스트 통과
- fresh config에서 Telegram 동작

## 5. KDE 구현 전 필수 절차

`22_KDE_PROTOCOL_CONFORMANCE_RUNBOOK.md`를 먼저 수행합니다.

- Android commit SHA
- desktop commit SHA
- discovery/identity/pairing fixtures
- notification/share/run-command fixtures

확인하지 않은 packet field를 추측해서 구현하지 않습니다.

## 6. 최종 알림 흐름

```text
Windows Toast
   ├─ KDE device online → Android direct
   └─ KDE unavailable → Telegram fallback

Critical system alert
   ├─ KDE direct
   └─ Telegram simultaneous

Android notification
   └─ Homebase Tray / Windows Toast
```
