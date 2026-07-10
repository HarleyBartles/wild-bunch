#!/usr/bin/env pwsh
param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$RemainingArgs
)

$ErrorActionPreference = 'Stop'

function Write-JsonError {
  param([string]$Message)
  Write-Output ('{"error": "' + $Message.Replace('"', '\"') + '"}')
}

function Get-RequiredValue {
  param(
    [string[]]$Args,
    [int]$Index,
    [string]$Flag
  )

  $next = $Index + 1
  if ($next -ge $Args.Count) {
    throw "$Flag requires a value"
  }

  return @($next, $Args[$next])
}

function Get-OwnerPid {
  $self = Get-CimInstance Win32_Process -Filter "ProcessId=$PID" -ErrorAction SilentlyContinue
  if (-not $self) {
    return $null
  }

  $parentId = [int]$self.ParentProcessId
  if ($parentId -le 0) {
    return $null
  }

  $parent = Get-CimInstance Win32_Process -Filter "ProcessId=$parentId" -ErrorAction SilentlyContinue
  if ($parent -and [int]$parent.ParentProcessId -gt 1) {
    return [int]$parent.ParentProcessId
  }

  return $parentId
}

function Set-ProcessEnv {
  param(
    [hashtable]$Saved,
    [string]$Name,
    [string]$Value
  )

  if (-not $Saved.ContainsKey($Name)) {
    $Saved[$Name] = [Environment]::GetEnvironmentVariable($Name, 'Process')
  }

  if ([string]::IsNullOrEmpty($Value)) {
    Remove-Item -Path "Env:$Name" -ErrorAction SilentlyContinue
  }
  else {
    Set-Item -Path "Env:$Name" -Value $Value
  }
}

function Restore-ProcessEnv {
  param([hashtable]$Saved)

  foreach ($entry in $Saved.GetEnumerator()) {
    if ($null -eq $entry.Value -or $entry.Value -eq '') {
      Remove-Item -Path "Env:$($entry.Key)" -ErrorAction SilentlyContinue
    }
    else {
      Set-Item -Path "Env:$($entry.Key)" -Value $entry.Value
    }
  }
}

$SavedEnv = @{}
$ScriptDir = Split-Path -Parent $PSCommandPath
$ProjectDir = ''
$Foreground = $false
$ForceBackground = $false
$BindHost = '127.0.0.1'
$UrlHost = ''
$IdleTimeoutMinutes = ''

$i = 0
while ($i -lt $RemainingArgs.Count) {
  switch ($RemainingArgs[$i]) {
    '--project-dir' {
      $i, $ProjectDir = Get-RequiredValue -Args $RemainingArgs -Index $i -Flag '--project-dir'
    }
    '--host' {
      $i, $BindHost = Get-RequiredValue -Args $RemainingArgs -Index $i -Flag '--host'
    }
    '--url-host' {
      $i, $UrlHost = Get-RequiredValue -Args $RemainingArgs -Index $i -Flag '--url-host'
    }
    '--idle-timeout-minutes' {
      $i, $IdleTimeoutMinutes = Get-RequiredValue -Args $RemainingArgs -Index $i -Flag '--idle-timeout-minutes'
    }
    '--open' {
      Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_OPEN' -Value '1'
    }
    '--foreground' {
      $Foreground = $true
    }
    '--no-daemon' {
      $Foreground = $true
    }
    '--background' {
      $ForceBackground = $true
    }
    '--daemon' {
      $ForceBackground = $true
    }
    default {
      Write-JsonError "Unknown argument: $($RemainingArgs[$i])"
      exit 1
    }
  }

  $i++
}

if ([string]::IsNullOrWhiteSpace($UrlHost)) {
  if ($BindHost -eq '127.0.0.1' -or $BindHost -eq 'localhost') {
    $UrlHost = 'localhost'
  }
  else {
    $UrlHost = $BindHost
  }
}

if (-not [string]::IsNullOrWhiteSpace($IdleTimeoutMinutes)) {
  [int]$idleMinutes = 0
  if (-not [int]::TryParse($IdleTimeoutMinutes, [ref]$idleMinutes) -or $idleMinutes -lt 1) {
    Write-JsonError '--idle-timeout-minutes must be a positive integer'
    exit 1
  }

  Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_IDLE_TIMEOUT_MS' -Value ([string]($idleMinutes * 60 * 1000))
}

if (-not $Foreground -and -not $ForceBackground -and $env:CODEX_CI) {
  $Foreground = $true
}

$SessionId = "$PID-$([DateTimeOffset]::UtcNow.ToUnixTimeSeconds())"
if (-not [string]::IsNullOrWhiteSpace($ProjectDir)) {
  $brainstormRoot = Join-Path $ProjectDir '.superpowers/brainstorm'
  $SessionDir = Join-Path $brainstormRoot $SessionId
  New-Item -ItemType Directory -Force -Path $brainstormRoot | Out-Null
}
else {
  $SessionDir = Join-Path ([System.IO.Path]::GetTempPath()) "brainstorm-$SessionId"
}

$StateDir = Join-Path $SessionDir 'state'
$PidFile = Join-Path $StateDir 'server.pid'
$LogFile = Join-Path $StateDir 'server.log'
$ServerIdFile = Join-Path $StateDir 'server-instance-id'

New-Item -ItemType Directory -Force -Path (Join-Path $SessionDir 'content') | Out-Null
New-Item -ItemType Directory -Force -Path $StateDir | Out-Null

$ServerId = [Guid]::NewGuid().ToString('N')
Set-Content -LiteralPath $ServerIdFile -Value $ServerId -Encoding ascii

if (Test-Path -LiteralPath $PidFile) {
  try {
    $oldPid = [int]((Get-Content -LiteralPath $PidFile -Raw).Trim())
    Stop-Process -Id $oldPid -ErrorAction SilentlyContinue
  }
  catch {
    # Ignore stale pid files.
  }

  Remove-Item -LiteralPath $PidFile -Force -ErrorAction SilentlyContinue
}

try {
  $ownerPid = Get-OwnerPid
  if ($null -eq $ownerPid -or $ownerPid -eq 1) {
    $ownerPid = $PID
  }

  if ([string]::IsNullOrWhiteSpace($ProjectDir)) {
    Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_PORT_FILE' -Value ''
    Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_TOKEN_FILE' -Value ''
  }
  else {
    Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_PORT_FILE' -Value (Join-Path $ProjectDir '.superpowers/brainstorm/.last-port')
    Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_TOKEN_FILE' -Value (Join-Path $ProjectDir '.superpowers/brainstorm/.last-token')
  }

  Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_DIR' -Value $SessionDir
  Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_HOST' -Value $BindHost
  Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_URL_HOST' -Value $UrlHost
  Set-ProcessEnv -Saved $SavedEnv -Name 'BRAINSTORM_OWNER_PID' -Value ([string]$ownerPid)

  $startParams = @{
    FilePath = 'node'
    ArgumentList = @('server.cjs', "--brainstorm-server-id=$ServerId")
    WorkingDirectory = $ScriptDir
    RedirectStandardOutput = $LogFile
    RedirectStandardError = $LogFile
    PassThru = $true
  }

  if ($Foreground) {
    $startParams['NoNewWindow'] = $true
  }
  else {
    $startParams['WindowStyle'] = 'Hidden'
  }

  $Process = Start-Process @startParams
  Set-Content -LiteralPath $PidFile -Value $Process.Id -Encoding ascii

  for ($attempt = 0; $attempt -lt 50; $attempt++) {
    if (Test-Path -LiteralPath $LogFile) {
      $startedLine = Select-String -LiteralPath $LogFile -Pattern 'server-started' | Select-Object -First 1
      if ($startedLine) {
        for ($aliveAttempt = 0; $aliveAttempt -lt 20; $aliveAttempt++) {
          if (-not (Get-Process -Id $Process.Id -ErrorAction SilentlyContinue)) {
            break
          }
          Start-Sleep -Milliseconds 100
        }

        if (-not (Get-Process -Id $Process.Id -ErrorAction SilentlyContinue)) {
          $projectArg = ''
          if (-not [string]::IsNullOrWhiteSpace($ProjectDir)) {
            $projectArg = " --project-dir $ProjectDir"
          }
          Write-JsonError "Server started but was killed. Retry in a persistent terminal with: $ScriptDir/start-server$projectArg --host $BindHost --url-host $UrlHost --foreground"
          exit 1
        }

        if ($Foreground) {
          Wait-Process -Id $Process.Id
          exit $Process.ExitCode
        }

        $startedText = $startedLine.Line
        Write-Output $startedText
        exit 0
      }
    }

    Start-Sleep -Milliseconds 100
  }

  Write-JsonError 'Server failed to start within 5 seconds'
  exit 1
}
finally {
  Restore-ProcessEnv -Saved $SavedEnv
}
