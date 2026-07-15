# Homebase Connect — Implementation-Ready Design Bundle

Homebase를 Telegram 중심 모니터링 봇에서 Windows 중심 Device Hub로 확장하기 위한 설계 및 구현 준비 패키지입니다.

## Start here

1. `START_HERE_IMPLEMENTATION.md`
2. `16_IMPLEMENTATION_READY_MASTER_SPEC.md`
3. `10_GOAL_ROADMAP.md`
4. `implementation-kit/phase-1/README.md`

## 구현 준비 자산

- copy-ready .NET 9 Protocol project
- transport-neutral Core types
- Telegram outbound adapter
- replacement `AlertDispatcher`
- replacement `NotificationForwardingService`
- xUnit test files
- safe PowerShell apply/verify scripts
- transactional V2 `SqliteMigrator` replacement candidate
- file-by-file patch guides
- UI implementation spec
- internal message catalog
- KDE protocol conformance runbook
- acceptance and rollback checklist

## 알림 목표

KDE Connect는 양방향 알림 기능을 제공합니다.

```text
Windows notification
  → KDE direct when connected
  → Telegram fallback when unavailable

Critical alert
  → KDE and Telegram

Android notification
  → Homebase / Windows
```

## Target repositories

- Homebase: `jeiel85/homebase`
- Android reference: `jeiel85/kdeconnect-android`
- Homebase branch observed: `main`
- Android branch observed: `master`
- update date: 2026-07-14

## Validation limitation

이 아티팩트 실행 환경에는 .NET SDK가 없어 포함된 C# 파일을 실제 컴파일하지 못했습니다.

실제 Homebase 환경에서 반드시 실행하십시오.

```powershell
dotnet restore LocalOpsBot.sln
dotnet build LocalOpsBot.sln -c Debug
dotnet test LocalOpsBot.sln -c Debug
```

KDE packet field는 exact commit과 fixture로 잠근 뒤 구현해야 합니다.
