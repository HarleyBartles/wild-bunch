[CmdletBinding()]
param(
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$SkipIndexMesh
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Assert-LastExitCode {
    param([string]$Message)
    if ($LASTEXITCODE -ne 0) {
        throw $Message
    }
}

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$RepoRoot = & git rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($RepoRoot)) {
    throw 'Could not determine repository root from git rev-parse.'
}

Push-Location -LiteralPath $RepoRoot
try {
    if (-not $SkipBackend) {
        Write-Host '--- Backend preflight ---'
        dotnet restore WildBunch.sln
        Assert-LastExitCode 'dotnet restore failed'
        dotnet build WildBunch.sln --no-restore --configuration Release
        Assert-LastExitCode 'dotnet build failed'
        dotnet tool restore
        Assert-LastExitCode 'dotnet tool restore failed'
        & "$ScriptDir/postgres-dev.ps1" test -- dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api --configuration Release
        Assert-LastExitCode 'dotnet ef migrations list failed'
        & "$ScriptDir/postgres-dev.ps1" test -- dotnet test WildBunch.sln --no-build --no-restore --configuration Release
        Assert-LastExitCode 'dotnet test failed'
    }

    if (-not $SkipFrontend) {
        Write-Host '--- Frontend preflight ---'
        Push-Location src/WildBunch.Web
        try {
            npm ci
            Assert-LastExitCode 'npm ci failed'
            npm run typecheck
            Assert-LastExitCode 'npm run typecheck failed'
            npm run test
            Assert-LastExitCode 'npm run test failed'
            npm run build
            Assert-LastExitCode 'npm run build failed'
        }
        finally {
            Pop-Location
        }
    }

    if (-not $SkipIndexMesh) {
        Write-Host '--- Index mesh preflight ---'
        & "$ScriptDir/generate_index_mesh.ps1" -Check
        Assert-LastExitCode 'generate_index_mesh -Check failed'

        Write-Host '--- Validating marketplace.json ---'
        python -c "import json; m=json.load(open('.agents/plugins/marketplace.json')); assert m['name']=='wild-bunch'; assert len(m['plugins'])==7; assert all(p['policy']['installation']=='INSTALLED_BY_DEFAULT' for p in m['plugins']); assert {p['name'] for p in m['plugins']}=={'repo-worker-pack','superpowers-plus','wild-bunch-project-pack','game-studio','dotnet-kit','architecture-pack','frontend-pack'}; print('OK marketplace.json')"
        Assert-LastExitCode 'marketplace.json validation failed'
    }
}
finally {
    Pop-Location
}
