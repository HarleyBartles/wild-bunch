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
        python -m pip install -r "$ScriptDir/requirements.txt"
        Assert-LastExitCode 'python script requirements installation failed'

        & "$ScriptDir/generate_index_mesh.ps1" -Check
        Assert-LastExitCode 'generate_index_mesh -Check failed'

        Write-Host '--- Validating repo-local skills ---'
        python "$ScriptDir/validate_repo_local_skills.py"
        Assert-LastExitCode 'repo-local skill validation failed'

        Write-Host '--- Validating marketplace plugin sync ---'
        python "$ScriptDir/validate_marketplace_plugin_sync.py"
        Assert-LastExitCode 'marketplace plugin sync validation failed'

        Write-Host '--- Validating marketplace skill projection ---'
        python "$ScriptDir/install_agent_skills.py" --check
        Assert-LastExitCode 'marketplace skill projection validation failed'
    }
}
finally {
    Pop-Location
}
