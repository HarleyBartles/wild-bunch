#!/usr/bin/env pwsh
param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$RemainingArgs
)

$ErrorActionPreference = 'Stop'

function Write-Json {
  param([string]$Text)
  Write-Output $Text
}

function Get-ServerId {
  param([string]$Path)

  if (-not (Test-Path -LiteralPath $Path)) {
    return $null
  }

  $id = (Get-Content -LiteralPath $Path -Raw).Trim()
  if ($id -match '^[A-Za-z0-9_-]{32,64}$') {
    return $id
  }

  return $null
}

function Get-CommandLineForPid {
  param([int]$Pid)

  $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$Pid" -ErrorAction SilentlyContinue
  if ($proc) {
    return $proc.CommandLine
  }

  return $null
}

function Test-ServerProcess {
  param(
    [int]$Pid,
    [string]$ExpectedId
  )

  if (-not (Get-Process -Id $Pid -ErrorAction SilentlyContinue)) {
    return $false
  }

  $commandLine = Get-CommandLineForPid -Pid $Pid
  if ([string]::IsNullOrWhiteSpace($commandLine)) {
    return $false
  }

  return $commandLine -like "*--brainstorm-server-id=$ExpectedId*"
}

function Mark-Stopped {
  param(
    [string]$StateDir,
    [string]$Reason
  )

  Remove-Item -LiteralPath (Join-Path $StateDir 'server-info') -Force -ErrorAction SilentlyContinue
  $payload = '{"reason":"' + $Reason.Replace('"', '\"') + '","timestamp":' + [DateTimeOffset]::UtcNow.ToUnixTimeSeconds() + '}'
  Set-Content -LiteralPath (Join-Path $StateDir 'server-stopped') -Value $payload -Encoding utf8
}

function Test-UnderTempRoot {
  param([string]$Path)

  $fullPath = [System.IO.Path]::GetFullPath($Path)
  $tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
  if (-not $tempRoot.EndsWith([System.IO.Path]::DirectorySeparatorChar)) {
    $tempRoot = $tempRoot + [System.IO.Path]::DirectorySeparatorChar
  }

  return $fullPath.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)
}

if ($RemainingArgs.Count -ne 1 -or [string]::IsNullOrWhiteSpace($RemainingArgs[0])) {
  Write-Json '{"error": "Usage: stop-server.ps1 <session_dir>"}'
  exit 1
}

$SessionDir = $RemainingArgs[0]
$StateDir = Join-Path $SessionDir 'state'
$PidFile = Join-Path $StateDir 'server.pid'
$ServerIdFile = Join-Path $StateDir 'server-instance-id'

if (Test-Path -LiteralPath $PidFile) {
  try {
    $pid = [int]((Get-Content -LiteralPath $PidFile -Raw).Trim())
  }
  catch {
    $pid = 0
  }

  $expectedId = Get-ServerId -Path $ServerIdFile
  if ($pid -le 0 -or $null -eq $expectedId -or -not (Test-ServerProcess -Pid $pid -ExpectedId $expectedId)) {
    Remove-Item -LiteralPath $PidFile, $ServerIdFile -Force -ErrorAction SilentlyContinue
    Mark-Stopped -StateDir $StateDir -Reason 'stale_pid'
    Write-Json '{"status": "stale_pid"}'
    exit 0
  }

  Stop-Process -Id $pid -ErrorAction SilentlyContinue

  for ($i = 0; $i -lt 20; $i++) {
    if (-not (Get-Process -Id $pid -ErrorAction SilentlyContinue)) {
      break
    }
    Start-Sleep -Milliseconds 100
  }

  if (Get-Process -Id $pid -ErrorAction SilentlyContinue) {
    Stop-Process -Id $pid -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 100
  }

  if (Get-Process -Id $pid -ErrorAction SilentlyContinue) {
    Write-Json '{"status": "failed", "error": "process still running"}'
    exit 1
  }

  Remove-Item -LiteralPath $PidFile, $ServerIdFile, (Join-Path $StateDir 'server.log') -Force -ErrorAction SilentlyContinue
  Mark-Stopped -StateDir $StateDir -Reason 'stop-server'

  if (Test-UnderTempRoot -Path $SessionDir) {
    Remove-Item -LiteralPath $SessionDir -Recurse -Force -ErrorAction SilentlyContinue
  }

  Write-Json '{"status": "stopped"}'
}
else {
  Write-Json '{"status": "not_running"}'
}
