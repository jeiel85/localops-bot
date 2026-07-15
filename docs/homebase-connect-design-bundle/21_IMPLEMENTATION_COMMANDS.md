# 21. Exact Implementation Commands

## Protocol project

```powershell
dotnet new classlib `
  -n LocalOpsBot.Protocol `
  -f net9.0 `
  -o src/LocalOpsBot.Protocol

Remove-Item src/LocalOpsBot.Protocol/Class1.cs

dotnet sln LocalOpsBot.sln add `
  src/LocalOpsBot.Protocol/LocalOpsBot.Protocol.csproj

dotnet add src/LocalOpsBot.Core/LocalOpsBot.Core.csproj reference `
  src/LocalOpsBot.Protocol/LocalOpsBot.Protocol.csproj

dotnet add tests/LocalOpsBot.Tests/LocalOpsBot.Tests.csproj reference `
  src/LocalOpsBot.Protocol/LocalOpsBot.Protocol.csproj
```

## Build

```powershell
dotnet restore LocalOpsBot.sln
dotnet build LocalOpsBot.sln -c Debug --no-restore
dotnet test LocalOpsBot.sln -c Debug --no-build
```

## DB backup

```powershell
$Db = "$env:ProgramData\Homebase\data\localops.db"
$Backup = "$Db.pre-device-hub-$(Get-Date -Format yyyyMMdd-HHmmss).bak"
Copy-Item $Db $Backup
```

## Release evidence

```powershell
dotnet --info | Out-File artifacts/dotnet-info.txt
dotnet build LocalOpsBot.sln -c Release |
  Tee-Object artifacts/build-release.txt
dotnet test LocalOpsBot.sln -c Release |
  Tee-Object artifacts/test-release.txt
```
