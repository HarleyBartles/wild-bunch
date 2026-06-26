[CmdletBinding()]
param(
    [ValidateSet('install-tools', 'ensure', 'setup', 'start', 'stop', 'reset', 'status', 'validate', 'test')]
    [string]$Command = 'setup'
    ,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$RemainingArguments = @()
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Resolve-PersistentRepoRoot {
    $scriptDir = (Resolve-Path $PSScriptRoot).Path
    $fallbackRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path

    $gitPath = (Get-Command git -ErrorAction SilentlyContinue)
    if ($null -eq $gitPath) {
        return $fallbackRoot
    }

    $commonDir = $null
    try {
        $commonDir = (& git rev-parse --git-common-dir 2>$null)
        if ($LASTEXITCODE -ne 0) { $commonDir = $null }
    }
    catch {
        $commonDir = $null
    }

    if ([string]::IsNullOrWhiteSpace($commonDir)) {
        return $fallbackRoot
    }

    try {
        $commonDirFull = (Resolve-Path $commonDir -ErrorAction Stop).Path
        $persistentRoot = (Resolve-Path (Join-Path $commonDirFull '..') -ErrorAction Stop).Path
        return $persistentRoot
    }
    catch {
        return $fallbackRoot
    }
}

$RepoRoot = Resolve-PersistentRepoRoot
$PostgreSqlVersion = '16.14'
$PostgreSqlDownloadPage = 'https://www.postgresql.org/download/windows/'
$LocalRoot = Join-Path $RepoRoot '.local\postgresql16'
$PostgresDevRoot = Join-Path $RepoRoot '.local\postgres-dev'
$BinDir = Join-Path $LocalRoot 'bin'
$DataDir = Join-Path $PostgresDevRoot 'data\wildbunch-dev'
$LogDir = Join-Path $PostgresDevRoot 'logs'
$LogFile = Join-Path $LogDir 'wildbunch-dev.log'
$Port = 5434
$DatabaseName = 'wildbunch_dev'
$HostName = 'localhost'
$ValidationConnectionString = "Host=$HostName;Port=$Port;Database=$DatabaseName;Username=postgres"

function Get-BinaryPath {
    param([Parameter(Mandatory)][string]$Name)
    $path = Join-Path $BinDir $Name
    if (-not (Test-Path $path)) {
        throw "Missing PostgreSQL binary: $path"
    }

    return $path
}

function Get-ToolingVersion {
    if (-not (Test-Path (Join-Path $BinDir 'postgres.exe'))) {
        return $null
    }

    $versionOutput = & (Get-BinaryPath 'postgres.exe') --version
    if ($LASTEXITCODE -ne 0 -or $null -eq $versionOutput) {
        return $null
    }

    if ($versionOutput -match '(\d+\.\d+)') {
        return $Matches[1]
    }

    return $null
}

function Write-ToolingInstructions {
    $toolingPath = Join-Path $RepoRoot '.local\postgresql16'
    Write-Host "PostgreSQL tooling is expected at $toolingPath and pinned to version $PostgreSqlVersion."
    Write-Host "This is the persistent main checkout's tooling root, shared across worktrees."
    Write-Host "If the binaries are missing, download the Windows installer from $PostgreSqlDownloadPage, install PostgreSQL $PostgreSqlVersion into $toolingPath, and rerun this command."
}

function Assert-PostgreSqlTooling {
    $toolingVersion = Get-ToolingVersion
    if ($null -eq $toolingVersion) {
        Write-ToolingInstructions
        throw "Missing PostgreSQL tooling under .local/postgresql16."
    }

    if ($toolingVersion -ne $PostgreSqlVersion) {
        Write-ToolingInstructions
        throw "Expected PostgreSQL tooling version $PostgreSqlVersion but found $toolingVersion."
    }
}

function Invoke-PostgresBinary {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    & (Get-BinaryPath $Name) @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

function Invoke-DotNetCommand {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
    }
}

function Invoke-WithValidationConnectionString {
    param([Parameter(Mandatory)][scriptblock]$Action)

    $previousConnectionString = $env:ConnectionStrings__WildBunchPostgresDb
    try {
        $env:ConnectionStrings__WildBunchPostgresDb = $ValidationConnectionString
        & $Action
    }
    finally {
        if ($null -eq $previousConnectionString) {
            Remove-Item Env:\ConnectionStrings__WildBunchPostgresDb -ErrorAction SilentlyContinue
        }
        else {
            $env:ConnectionStrings__WildBunchPostgresDb = $previousConnectionString
        }
    }
}

function Initialize-PostgresValidationLane {
    Initialize-Cluster
    Start-Cluster
    Wait-ForReady
    Ensure-Database
}

function Ensure-Directory {
    param([Parameter(Mandatory)][string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path | Out-Null
    }
}

function Set-PostgresSetting {
    param(
        [Parameter(Mandatory)][string]$ConfigPath,
        [Parameter(Mandatory)][string]$SettingName,
        [Parameter(Mandatory)][string]$SettingValue
    )

    $content = Get-Content -LiteralPath $ConfigPath
    $pattern = "^\s*#?\s*$([regex]::Escape($SettingName))\s*="
    $newLine = "$SettingName = $SettingValue"

    if ($content -match $pattern) {
        $updated = foreach ($line in $content) {
            if ($line -match $pattern) {
                $newLine
            }
            else {
                $line
            }
        }

        [System.IO.File]::WriteAllLines((Resolve-Path $ConfigPath).Path, $updated)
        return
    }

    Add-Content -LiteralPath $ConfigPath -Value $newLine
}

function Test-ClusterRunning {
    if (-not (Test-Path (Join-Path $DataDir 'PG_VERSION'))) {
        return $false
    }

    & (Get-BinaryPath 'pg_isready.exe') -h $HostName -p $Port -U postgres | Out-Null
    return $LASTEXITCODE -eq 0
}

function Initialize-Cluster {
    Assert-PostgreSqlTooling

    if (-not (Test-Path (Join-Path $DataDir 'PG_VERSION'))) {
        Ensure-Directory (Split-Path $DataDir -Parent)
        Invoke-PostgresBinary 'initdb.exe' @('-D', $DataDir, '--auth=trust', '--encoding=UTF8', '-U', 'postgres')
    }

    $configPath = Join-Path $DataDir 'postgresql.conf'
    Set-PostgresSetting -ConfigPath $configPath -SettingName 'port' -SettingValue $Port
    Set-PostgresSetting -ConfigPath $configPath -SettingName 'listen_addresses' -SettingValue "'$HostName'"
}

function Start-Cluster {
    if (Test-ClusterRunning) {
        return
    }

    Ensure-Directory $LogDir
    Invoke-PostgresBinary 'pg_ctl.exe' @('-D', $DataDir, '-l', $LogFile, '-w', '-o', "-p $Port -h $HostName", 'start')
}

function Stop-Cluster {
    if (Test-ClusterRunning) {
        Invoke-PostgresBinary 'pg_ctl.exe' @('-D', $DataDir, '-m', 'fast', 'stop')
    }
}

function Wait-ForReady {
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        & (Get-BinaryPath 'pg_isready.exe') -h $HostName -p $Port -U postgres | Out-Null
        if ($LASTEXITCODE -eq 0) {
            return
        }

        Start-Sleep -Seconds 1
    }

    throw "PostgreSQL did not become ready on ${HostName}:$Port."
}

function Ensure-Database {
    $query = "SELECT 1 FROM pg_database WHERE datname = '$DatabaseName';"
    $result = & (Get-BinaryPath 'psql.exe') -h $HostName -p $Port -U postgres -d postgres -tAc $query
    if ($LASTEXITCODE -ne 0) {
        throw "psql failed while checking for database '$DatabaseName'."
    }

    $resultText = if ($null -eq $result) { '' } else { $result.ToString().Trim() }
    if ($resultText -ne '1') {
        Invoke-PostgresBinary 'createdb.exe' @('-h', $HostName, '-p', $Port, '-U', 'postgres', $DatabaseName)
    }
}

function Reset-Cluster {
    Stop-Cluster
    if (Test-Path $DataDir) {
        Remove-Item -LiteralPath $DataDir -Recurse -Force
    }

    if (Test-Path $LogFile) {
        Remove-Item -LiteralPath $LogFile -Force
    }
}

function Invoke-ValidationLane {
    Initialize-PostgresValidationLane

    Invoke-WithValidationConnectionString {
        Invoke-DotNetCommand @('tool', 'restore')
        Invoke-DotNetCommand @('ef', 'migrations', 'list', '--project', 'src/WildBunch.Persistence', '--startup-project', 'src/WildBunch.Api')
        Invoke-DotNetCommand @('test', 'WildBunch.sln')
    }

    Write-Host "PostgreSQL validation lane completed."
    Write-Host "Connection string: $ValidationConnectionString"
    Write-Host "Direct PostgreSQL-backed dotnet test runs must either use this lane or export ConnectionStrings__WildBunchPostgresDb themselves."
    Write-Host "Use '.\scripts\postgres-dev.ps1 status' to check the shared service. Do not stop it during normal worker cleanup; it is reused by other workers and worktrees."
}

function Invoke-TargetedTestLane {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $testArguments = $Arguments
    if ($testArguments.Count -gt 0 -and $testArguments[0] -eq '--') {
        $testArguments = @($testArguments[1..($testArguments.Count - 1)])
    }

    if ($testArguments.Count -eq 0) {
        throw "Usage: .\scripts\postgres-dev.ps1 test -- <dotnet command or dotnet test arguments>"
    }

    # If the caller passed a full 'dotnet <subcommand>' (e.g. 'dotnet ef migrations list'),
    # run it as-is. If the caller passed bare test arguments (e.g. '--no-build' or
    # 'tests/WildBunch.Integration.Tests'), prepend 'dotnet test'.
    # This means all of the following work:
    #   .\scripts\postgres-dev.ps1 test -- --no-build
    #   .\scripts\postgres-dev.ps1 test -- dotnet test --no-build
    #   .\scripts\postgres-dev.ps1 test -- dotnet ef migrations list --project src/...
    #   .\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests --no-build
    $dotnetArguments = if ($testArguments[0] -eq 'dotnet') {
        @($testArguments[1..($testArguments.Count - 1)])
    } else {
        @('test') + $testArguments
    }

    Initialize-PostgresValidationLane

    Invoke-WithValidationConnectionString {
        Invoke-DotNetCommand $dotnetArguments
    }

    Write-Host "PostgreSQL targeted test lane completed."
    Write-Host "Connection string: $ValidationConnectionString"
    Write-Host "Direct PostgreSQL-backed dotnet runs must either use this lane or export ConnectionStrings__WildBunchPostgresDb themselves."
}

switch ($Command) {
    'install-tools' {
        $toolingVersion = Get-ToolingVersion
        if ($null -eq $toolingVersion) {
            Write-ToolingInstructions
            exit 1
        }

        if ($toolingVersion -ne $PostgreSqlVersion) {
            Write-ToolingInstructions
            exit 1
        }

        Write-Host "PostgreSQL tooling is already pinned at version $toolingVersion in $LocalRoot."
    }
    'setup' {
        Initialize-Cluster
        Start-Cluster
        Wait-ForReady
        Ensure-Database
        Write-Host "Persistent local development database ready."
        Write-Host "Connection string: Host=$HostName;Port=$Port;Database=$DatabaseName;Username=postgres"
    }
    'start' {
        Initialize-Cluster
        Start-Cluster
        Wait-ForReady
        Ensure-Database
        Write-Host "Persistent local development database started."
    }
    'ensure' {
        Initialize-Cluster
        Start-Cluster
        Wait-ForReady
        Ensure-Database
        Write-Host "Shared local PostgreSQL service is ready on ${HostName}:$Port."
        Write-Host "Service owned by persistent checkout: $RepoRoot"
        Write-Host "Reuse this service from any worktree; do not stop it during normal worker cleanup."
    }
    'stop' {
        Stop-Cluster
        Write-Host "Persistent local development database stopped."
    }
    'reset' {
        Reset-Cluster
        Initialize-Cluster
        Start-Cluster
        Wait-ForReady
        Ensure-Database
        Write-Host "Persistent local development database reset."
    }
    'status' {
        if (-not (Test-Path (Join-Path $DataDir 'PG_VERSION'))) {
            Write-Host "Cluster not initialized at $DataDir."
            exit 0
        }

        if (Test-ClusterRunning) {
            Write-Host "Cluster is running on ${HostName}:$Port."

            $query = "SELECT 1 FROM pg_database WHERE datname = '$DatabaseName';"
            $result = & (Get-BinaryPath 'psql.exe') -h $HostName -p $Port -U postgres -d postgres -tAc $query
            $resultText = if ($null -eq $result) { '' } else { $result.ToString().Trim() }
            if ($LASTEXITCODE -eq 0 -and $resultText -eq '1') {
                Write-Host "Persistent app database '$DatabaseName' exists."
            }
            else {
                Write-Host "Persistent app database '$DatabaseName' is missing."
            }
        }
        else {
            Write-Host "Cluster exists but is not running on ${HostName}:$Port."
            Write-Host "Persistent app database '$DatabaseName' status is unavailable until the cluster is started."
        }
    }
    'validate' {
        Invoke-ValidationLane
    }
    'test' {
        Invoke-TargetedTestLane -Arguments $RemainingArguments
    }
}
