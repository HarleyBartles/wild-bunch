[CmdletBinding()]
param(
    [switch]$Check,
    [string]$ChangedFrom
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$RepoRoot = & git -C $ScriptDir rev-parse --show-toplevel
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($RepoRoot)) {
    throw 'Could not determine repository root from git rev-parse.'
}

function Test-LastExitCode {
    param([string]$Message)
    if ($LASTEXITCODE -ne 0) {
        throw $Message
    }
}

Push-Location -LiteralPath $RepoRoot
try {
    Write-Host '--- Repo-local skill validation ---'
    & py -3 "$ScriptDir\validate_local_skills_extra.py" --check "$RepoRoot\.agents\skills" wild-bunch-
    Test-LastExitCode 'repo-local skill validation failed'

    if ($Check) {
        Write-Host '--- Repo-specific preflight: -Check mode, skipping heavy build/test ---'
        exit 0
    }

    Write-Host '--- Python script tests ---'
    python -m pip install -r "$ScriptDir\requirements.txt"
    Test-LastExitCode 'python requirements installation failed'
    python -m pytest "$ScriptDir\tests" -q
    Test-LastExitCode 'python script tests failed'

    Write-Host '--- Backend preflight ---'
    dotnet restore WildBunch.sln
    Test-LastExitCode 'dotnet restore failed'
    dotnet build WildBunch.sln --no-restore --configuration Release
    Test-LastExitCode 'dotnet build failed'
    dotnet tool restore
    Test-LastExitCode 'dotnet tool restore failed'

    & "$ScriptDir/postgres-dev.ps1" test -- dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api --configuration Release
    Test-LastExitCode 'dotnet ef migrations list failed'

    & "$ScriptDir/postgres-dev.ps1" test -- dotnet test WildBunch.sln --no-build --no-restore --configuration Release
    Test-LastExitCode 'dotnet test failed'

    Write-Host '--- Frontend preflight ---'
    Push-Location src/WildBunch.Web
    try {
        npm ci
        Test-LastExitCode 'npm ci failed'
        npm run typecheck
        Test-LastExitCode 'npm run typecheck failed'
        npm run test
        Test-LastExitCode 'npm run test failed'
        npm run build
        Test-LastExitCode 'npm run build failed'
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}
