#!/usr/bin/env pwsh
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("setup", "ensure", "start", "stop", "status", "reset")]
    [string]$Command = "ensure"
)

$ErrorActionPreference = "Stop"

$PG_ROOT = "Z:\_postgres-cluster"
$PG_BIN = "$PG_ROOT\bin"
$PG_DATA = "$PG_ROOT\data"
$PG_LOGS = "$PG_ROOT\logs"
$PORT = 5435
$DB_NAME = "wildbunch_dev"

function Test-Binary([string]$Name) {
    $p = Join-Path $PG_BIN $Name
    if (-not (Test-Path $p)) { throw "PostgreSQL binary not found: $p" }
    return $p
}

function Assert-LastExitCode {
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE"
    }
}

function Test-ClusterRunning {
    $pg_ctl = Test-Binary "pg_ctl.exe"
    $result = & $pg_ctl status -D $PG_DATA 2>&1
    return ($result -match "server is running")
}

function Initialize-Cluster {
    if (Test-Path "$PG_DATA\PG_VERSION") { return }
    New-Item -ItemType Directory -Force -Path $PG_DATA, $PG_LOGS | Out-Null
    $initdb = Test-Binary "initdb.exe"
    & $initdb -D $PG_DATA -U postgres --locale=en_US.UTF-8 --encoding=UTF8
    Assert-LastExitCode
    Set-Content -Path "$PG_DATA\postgresql.conf" -Value @"
port = $PORT
listen_addresses = 'localhost'
"@
    Add-Content -Path "$PG_DATA\pg_hba.conf" -Value @"
host all all 127.0.0.1/32 trust
host all all ::1/128 trust
"@
}

function Start-Cluster {
    if (Test-ClusterRunning) { return }
    $pg_ctl = Test-Binary "pg_ctl.exe"
    & $pg_ctl -D $PG_DATA -l "$PG_LOGS\postgresql.log" start
    Assert-LastExitCode
}

function Stop-Cluster {
    if (-not (Test-ClusterRunning)) {
        $global:LASTEXITCODE = 0
        return
    }
    $pg_ctl = Test-Binary "pg_ctl.exe"
    & $pg_ctl -D $PG_DATA stop
    Assert-LastExitCode
}

function Get-ClusterStatus {
    $pg_ctl = Test-Binary "pg_ctl.exe"
    & $pg_ctl -D $PG_DATA status
}

function Ensure-Database {
    $psql = Test-Binary "psql.exe"
    $createdb = Test-Binary "createdb.exe"
    $env:PGPASSWORD = ""
    $exists = & $psql -h localhost -p $PORT -U postgres -At -c "SELECT 1 FROM pg_database WHERE datname='$DB_NAME'" 2>&1
    if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE" }
    if ($exists -ne "1") {
        & $createdb -h localhost -p $PORT -U postgres $DB_NAME
        Assert-LastExitCode
    }
}

function Reset-Database {
    $psql = Test-Binary "psql.exe"
    $createdb = Test-Binary "createdb.exe"
    $env:PGPASSWORD = ""
    & $psql -h localhost -p $PORT -U postgres -c "DROP DATABASE IF EXISTS $DB_NAME" 2>&1 | Out-Null
    Assert-LastExitCode
    & $createdb -h localhost -p $PORT -U postgres $DB_NAME
    Assert-LastExitCode
}

switch ($Command) {
    "setup"  { Initialize-Cluster }
    "ensure" { Initialize-Cluster; Start-Cluster; Ensure-Database }
    "start"  { Start-Cluster }
    "stop"   { Stop-Cluster }
    "status" { Get-ClusterStatus }
    "reset"  { Start-Cluster; Reset-Database }
    default  { throw "Unknown command: $Command" }
}
