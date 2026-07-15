# Patch 07 — Test project reference

Expected addition:

```xml
<ProjectReference Include="..\..\src\LocalOpsBot.Protocol\LocalOpsBot.Protocol.csproj" />
```

Then:

```powershell
dotnet test tests/LocalOpsBot.Tests/LocalOpsBot.Tests.csproj
```
