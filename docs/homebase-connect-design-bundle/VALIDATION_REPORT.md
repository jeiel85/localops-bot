# Validation Report

Validation date: 2026-07-14

## Artifact validation completed

- ZIP archive generated
- all JSON files parsed
- manifest regenerated
- PowerShell apply and verify scripts included
- apply script refuses existing-file overwrite
- English-only artifact filenames used
- transactional V2 migration candidate included
- PC → Android KDE notification support reflected
- KDE official port range `1714-1764` reflected
- phase limitations documented

## Source alignment completed

The kit was aligned to the observed Homebase source:

- `LocalOpsBot.Core.csproj`
- `BotCommand`
- `ICommandHandler`
- Core and Infrastructure DI extensions
- `AlertDispatcher`
- `NotificationForwardingService`
- `SqliteMigrator`
- test project references

## Not executed in artifact runtime

- `dotnet restore`
- `dotnet build`
- `dotnet test`
- PowerShell apply script
- V2 migration against the user's real DB
- Android KDE real-device conformance
- Windows Firewall installer changes

Reason: the artifact runtime did not expose the .NET SDK and did not contain a local checkout of the user's repository.

## Required validation in the Homebase environment

```powershell
dotnet restore LocalOpsBot.sln
dotnet build LocalOpsBot.sln -c Debug
dotnet test LocalOpsBot.sln -c Debug
```

Before DB merge:

- back up real V1 DB
- fresh migration test
- V1 → V2 test
- failure injection and rollback

Before KDE transport merge:

- lock exact source commits
- capture sanitized fixtures
- pair with real Android device
- test both notification directions
