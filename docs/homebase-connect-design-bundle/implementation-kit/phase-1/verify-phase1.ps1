[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepoRoot
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path $RepoRoot).Path

Push-Location $RepoRoot
try {
    dotnet restore LocalOpsBot.sln
    dotnet build LocalOpsBot.sln -c Debug --no-restore
    dotnet test LocalOpsBot.sln -c Debug --no-build
}
finally {
    Pop-Location
}
