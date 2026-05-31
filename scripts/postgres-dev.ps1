[CmdletBinding()]
param(
    [ValidateSet('install-tools', 'setup', 'start', 'stop', 'reset', 'status')]
    [string]$Command = 'setup'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
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
}
