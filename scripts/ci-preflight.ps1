[CmdletBinding()]
param(
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$SkipIndexMesh
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$RepoRoot = & git rev-parse --show-toplevel

Push-Location -LiteralPath $RepoRoot
try {
    if (-not $SkipBackend) {
        Write-Host '--- Backend preflight ---'
        dotnet restore WildBunch.sln
        dotnet build WildBunch.sln --no-restore --configuration Release
        dotnet tool restore
        & "$ScriptDir/postgres-dev.ps1" test -- dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api --configuration Release
        & "$ScriptDir/postgres-dev.ps1" test -- dotnet test WildBunch.sln --no-build --no-restore --configuration Release
    }

    if (-not $SkipFrontend) {
        Write-Host '--- Frontend preflight ---'
        Push-Location src/WildBunch.Web
        try {
            npm ci
            npm run typecheck
            npm run test
            npm run build
        }
        finally {
            Pop-Location
        }
    }

    if (-not $SkipIndexMesh) {
        Write-Host '--- Index mesh preflight ---'
        & "$ScriptDir/generate_index_mesh.ps1" -Check
    }
}
finally {
    Pop-Location
}
