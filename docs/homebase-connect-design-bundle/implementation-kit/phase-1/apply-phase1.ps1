[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot,

    [switch]$Apply
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path $RepoRoot).Path
$KitRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceRoot = Join-Path $KitRoot "new-files"

if (-not (Test-Path (Join-Path $RepoRoot "LocalOpsBot.sln"))) {
    throw "LocalOpsBot.sln was not found in RepoRoot: $RepoRoot"
}

$files = Get-ChildItem $SourceRoot -Recurse -File

Write-Host "Phase 1 planned files:"
foreach ($file in $files) {
    $relative = $file.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    Write-Host "  + $relative"
}

if (-not $Apply) {
    Write-Host ""
    Write-Host "Dry run only. Re-run with -Apply."
    exit 0
}

foreach ($file in $files) {
    $relative = $file.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    $destination = Join-Path $RepoRoot $relative
    if (Test-Path $destination) {
        throw "Refusing to overwrite existing file: $destination"
    }
}

foreach ($file in $files) {
    $relative = $file.FullName.Substring($SourceRoot.Length).TrimStart('\', '/')
    $destination = Join-Path $RepoRoot $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item $file.FullName $destination
}

Push-Location $RepoRoot
try {
    dotnet sln LocalOpsBot.sln add `
        src/LocalOpsBot.Protocol/LocalOpsBot.Protocol.csproj

    dotnet add src/LocalOpsBot.Core/LocalOpsBot.Core.csproj reference `
        src/LocalOpsBot.Protocol/LocalOpsBot.Protocol.csproj

    dotnet add tests/LocalOpsBot.Tests/LocalOpsBot.Tests.csproj reference `
        src/LocalOpsBot.Protocol/LocalOpsBot.Protocol.csproj
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "New files copied."
Write-Host "Apply the patch-guides before building."
