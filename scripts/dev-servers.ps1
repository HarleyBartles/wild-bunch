[CmdletBinding()]
param(
    [ValidateSet('ensure', 'start', 'stop', 'status')]
    [string]$Command = 'ensure'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$CanonicalApiPort = 5275
$CanonicalVitePort = 5173
$ApiProject = 'src/WildBunch.Api'
$WebProject = 'src/WildBunch.Web'
$PostgresConnectionString = 'Host=localhost;Port=5434;Database=wildbunch_dev;Username=postgres'

function Resolve-WorktreeRoot {
    $scriptDir = (Resolve-Path $PSScriptRoot).Path
    $fallbackRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path

    $gitPath = (Get-Command git -ErrorAction SilentlyContinue)
    if ($null -eq $gitPath) {
        return $fallbackRoot
    }

    try {
        $topLevel = (& git rev-parse --show-toplevel 2>$null)
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($topLevel)) {
            return (Resolve-Path $topLevel).Path
        }
    }
    catch {}

    return $fallbackRoot
}

function Get-WorktreeBranch {
    try {
        $branch = (& git rev-parse --abbrev-ref HEAD 2>$null)
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($branch)) {
            return $branch
        }
    }
    catch {}
    return 'unknown'
}

$WorktreeRoot = Resolve-WorktreeRoot
$StateDir = Join-Path $WorktreeRoot '.local\dev-servers'
$StateFile = Join-Path $StateDir 'state.json'

function Ensure-StateDir {
    if (-not (Test-Path $StateDir)) {
        New-Item -ItemType Directory -Path $StateDir -Force | Out-Null
    }
}

function Read-State {
    if (-not (Test-Path $StateFile)) {
        return $null
    }
    try {
        return Get-Content $StateFile -Raw | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Write-State {
    param(
        [Parameter(Mandatory)][int]$ApiPid,
        [Parameter(Mandatory)][int]$VitePid,
        [Parameter(Mandatory)][int]$ApiPort,
        [Parameter(Mandatory)][int]$VitePort,
        [Parameter(Mandatory)][string]$ApiUrl,
        [Parameter(Mandatory)][string]$ViteUrl
    )
    Ensure-StateDir
    $state = [PSCustomObject]@{
        worktreeRoot = $WorktreeRoot
        branch       = Get-WorktreeBranch
        apiPid       = $ApiPid
        vitePid      = $VitePid
        apiPort      = $ApiPort
        vitePort     = $VitePort
        apiUrl       = $ApiUrl
        viteUrl      = $ViteUrl
        startedAt    = (Get-Date).ToString('o')
    }
    $state | ConvertTo-Json | Set-Content -Path $StateFile -Encoding utf8
}

function Clear-State {
    if (Test-Path $StateFile) {
        Remove-Item -LiteralPath $StateFile -Force
    }
}

function Test-PidAlive {
    param([Parameter(Mandatory)][int]$ProcessId)
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    return $null -ne $process
}

function Get-PortListenerPids {
    param([Parameter(Mandatory)][int]$Port)
    $conns = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($null -eq $conns) { return @() }
    return @($conns | ForEach-Object { $_.OwningProcess } | Sort-Object -Unique)
}

function Get-ProcessCommandLine {
    param([Parameter(Mandatory)][int]$ProcessId)
    try {
        $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$ProcessId" -ErrorAction SilentlyContinue
        if ($null -ne $proc) {
            return $proc.CommandLine
        }
    }
    catch {}
    return ''
}

function Test-PortOwnedByWorktree {
    param([Parameter(Mandatory)][int]$Port)
    $pids = @(Get-PortListenerPids -Port $Port)
    foreach ($procPid in $pids) {
        $cmdLine = Get-ProcessCommandLine -ProcessId $procPid
        if ($cmdLine -and ($cmdLine -like "*$WorktreeRoot*")) {
            return $true
        }
    }
    return $false
}

function Find-FreePort {
    param([Parameter(Mandatory)][int]$StartPort)
    $port = $StartPort
    while ($port -lt 65535) {
        $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        if ($null -eq $listeners) {
            return $port
        }
        $port++
    }
    throw "No free port found starting from $StartPort"
}

function Test-ApiHealthy {
    param([Parameter(Mandatory)][string]$Url)
    try {
        $checkUrl = $Url -replace '://localhost:', '://127.0.0.1:'
        $response = Invoke-RestMethod -Uri "$checkUrl/api/games/starting-towns" -Method GET -TimeoutSec 5 -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Test-ViteHealthy {
    param([Parameter(Mandatory)][string]$Url)
    try {
        $checkUrl = $Url -replace '://localhost:', '://127.0.0.1:'
        $response = Invoke-WebRequest -Uri $checkUrl -Method GET -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
        return $response.StatusCode -eq 200
    }
    catch {
        return $false
    }
}

function Resolve-ServerPorts {
    $state = Read-State

    $apiPort = $CanonicalApiPort
    $vitePort = $CanonicalVitePort

    $apiCanonicalFree = $null -eq (Get-NetTCPConnection -LocalPort $CanonicalApiPort -State Listen -ErrorAction SilentlyContinue)
    $viteCanonicalFree = $null -eq (Get-NetTCPConnection -LocalPort $CanonicalVitePort -State Listen -ErrorAction SilentlyContinue)

    if ($apiCanonicalFree -and $viteCanonicalFree) {
        return @{ ApiPort = $CanonicalApiPort; VitePort = $CanonicalVitePort; Reused = $false; Reason = 'canonical ports free' }
    }

    if (-not $apiCanonicalFree) {
        $apiOwnedHere = Test-PortOwnedByWorktree -Port $CanonicalApiPort
        if ($apiOwnedHere) {
            return @{ ApiPort = $CanonicalApiPort; VitePort = $CanonicalVitePort; Reused = $true; Reason = 'canonical API port owned by this worktree' }
        }
    }

    if (-not $viteCanonicalFree) {
        $viteOwnedHere = Test-PortOwnedByWorktree -Port $CanonicalVitePort
        if ($viteOwnedHere) {
            return @{ ApiPort = $CanonicalApiPort; VitePort = $CanonicalVitePort; Reused = $true; Reason = 'canonical Vite port owned by this worktree' }
        }
    }

    $apiPort = Find-FreePort -StartPort ($CanonicalApiPort + 1)
    $vitePort = Find-FreePort -StartPort ($CanonicalVitePort + 1)
    return @{ ApiPort = $apiPort; VitePort = $vitePort; Reused = $false; Reason = 'canonical ports occupied by another worktree; allocated fallback ports' }
}

function Start-ApiServer {
    param([Parameter(Mandatory)][int]$Port)

    $apiUrl = "http://localhost:$Port"
    $env:ConnectionStrings__WildBunchPostgresDb = $PostgresConnectionString
    $env:ASPNETCORE_ENVIRONMENT = 'Development'

    $process = Start-Process -FilePath 'dotnet' `
        -ArgumentList @('run', '--project', $ApiProject, '--no-build', '--urls', $apiUrl) `
        -WorkingDirectory $WorktreeRoot `
        -PassThru `
        -WindowStyle Hidden

    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        if (Test-ApiHealthy -Url $apiUrl) {
            return @{ Pid = $process.Id; Url = $apiUrl }
        }
        Start-Sleep -Seconds 1
    }

    throw "API server did not become healthy on $apiUrl within 60 seconds."
}

function Start-ViteServer {
    param([Parameter(Mandatory)][int]$Port, [Parameter(Mandatory)][string]$ApiUrl)

    $env:VITE_API_BASE_URL = $ApiUrl
    $viteUrl = "http://localhost:$Port"

    $process = Start-Process -FilePath 'cmd' `
        -ArgumentList @('/c', 'npm', 'run', 'dev', '--', '--port', $Port, '--strictPort') `
        -WorkingDirectory (Join-Path $WorktreeRoot $WebProject) `
        -PassThru `
        -WindowStyle Hidden

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if (Test-ViteHealthy -Url $viteUrl) {
            return @{ Pid = $process.Id; Url = $viteUrl }
        }
        Start-Sleep -Seconds 1
    }

    throw "Vite dev server did not become healthy on $viteUrl within 30 seconds."
}

function Stop-ProcessTree {
    param([Parameter(Mandatory)][int]$ProcessId)
    if (-not (Test-PidAlive -ProcessId $ProcessId)) { return }

    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$ProcessId" -ErrorAction SilentlyContinue
    foreach ($child in $children) {
        Stop-ProcessTree -ProcessId $child.ProcessId
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Invoke-Ensure {
    $state = Read-State

    if ($null -ne $state) {
        $apiAlive = Test-PidAlive -ProcessId $state.apiPid
        $viteAlive = Test-PidAlive -ProcessId $state.vitePid

        if ($apiAlive -and $viteAlive `
            -and (Test-ApiHealthy -Url $state.apiUrl) `
            -and (Test-ViteHealthy -Url $state.viteUrl)) {
            Write-Host "Dev servers already running for this worktree (reused)."
            Write-Host "  Worktree:  $WorktreeRoot"
            Write-Host "  Branch:    $(Get-WorktreeBranch)"
            Write-Host "  API:       $($state.apiUrl) (PID $($state.apiPid))"
            Write-Host "  Frontend:  $($state.viteUrl) (PID $($state.vitePid))"
            Write-Host "  Reused:    yes (state file matched live processes)"
            return
        }

        if (-not $apiAlive -or -not $viteAlive) {
            Write-Host "Stale dev-server state detected; cleaning up and restarting."
            if ($apiAlive) { Stop-ProcessTree -ProcessId $state.apiPid }
            if ($viteAlive) { Stop-ProcessTree -ProcessId $state.vitePid }
            Clear-State
        }
    }

    $ports = Resolve-ServerPorts

    if ($ports.Reused) {
        $apiPids = @(Get-PortListenerPids -Port $ports.ApiPort)
        $vitePids = @(Get-PortListenerPids -Port $ports.VitePort)
        $apiPid = if ($apiPids.Count -gt 0) { $apiPids[0] } else { 0 }
        $vitePid = if ($vitePids.Count -gt 0) { $vitePids[0] } else { 0 }
        $apiUrl = "http://localhost:$($ports.ApiPort)"
        $viteUrl = "http://localhost:$($ports.VitePort)"

        Write-State -ApiPid $apiPid -VitePid $vitePid -ApiPort $ports.ApiPort -VitePort $ports.VitePort -ApiUrl $apiUrl -ViteUrl $viteUrl
        Write-Host "Dev servers already running for this worktree (reused)."
        Write-Host "  Worktree:  $WorktreeRoot"
        Write-Host "  Branch:    $(Get-WorktreeBranch)"
        Write-Host "  API:       $apiUrl (PID $apiPid)"
        Write-Host "  Frontend:  $viteUrl (PID $vitePid)"
        Write-Host "  Reused:    yes ($($ports.Reason))"
        return
    }

    Write-Host "Starting dev servers for this worktree."
    Write-Host "  Worktree:  $WorktreeRoot"
    Write-Host "  Branch:    $(Get-WorktreeBranch)"
    Write-Host "  Reason:    $($ports.Reason)"

    $api = Start-ApiServer -Port $ports.ApiPort
    Write-Host "  API:       $($api.Url) (PID $($api.Pid))"

    $vite = Start-ViteServer -Port $ports.VitePort -ApiUrl $api.Url
    Write-Host "  Frontend:  $($vite.Url) (PID $($vite.Pid))"

    Write-State -ApiPid $api.Pid -VitePid $vite.Pid -ApiPort $ports.ApiPort -VitePort $ports.VitePort -ApiUrl $api.Url -ViteUrl $vite.Url

    Write-Host ""
    Write-Host "Dev servers are ready."
    Write-Host "  API:      $($api.Url)"
    Write-Host "  Frontend: $($vite.Url)"
    if ($ports.ApiPort -ne $CanonicalApiPort -or $ports.VitePort -ne $CanonicalVitePort) {
        Write-Host "  NOTE: Non-canonical ports were used because canonical ports were occupied by another worktree."
        Write-Host "  Report these actual URLs in browser-proof returns."
    }
    Write-Host ""
    Write-Host "To stop: .\scripts\dev-servers.ps1 stop"
    Write-Host "To check: .\scripts\dev-servers.ps1 status"
}

function Invoke-Stop {
    $state = Read-State
    if ($null -eq $state) {
        Write-Host "No dev-server state file found for this worktree."
        Write-Host "Nothing to stop."
        return
    }

    $stoppedApi = $false
    $stoppedVite = $false

    if (Test-PidAlive -ProcessId $state.apiPid) {
        Stop-ProcessTree -ProcessId $state.apiPid
        $stoppedApi = $true
        Write-Host "Stopped API server (PID $($state.apiPid)) on $($state.apiUrl)."
    }

    if (Test-PidAlive -ProcessId $state.vitePid) {
        Stop-ProcessTree -ProcessId $state.vitePid
        $stoppedVite = $true
        Write-Host "Stopped Vite dev server (PID $($state.vitePid)) on $($state.viteUrl)."
    }

    if (-not $stoppedApi -and -not $stoppedVite) {
        Write-Host "Dev-server PIDs in state file are no longer alive."
    }

    Clear-State
    Write-Host "Dev-server state cleared for this worktree."
}

function Invoke-Status {
    $state = Read-State
    $branch = Get-WorktreeBranch

    Write-Host "Worktree:  $WorktreeRoot"
    Write-Host "Branch:    $branch"

    if ($null -eq $state) {
        Write-Host "State:     no state file found"
        Write-Host ""
        Write-Host "No dev servers recorded for this worktree."
        Write-Host "Run .\scripts\dev-servers.ps1 ensure to start them."
        return
    }

    $apiAlive = Test-PidAlive -ProcessId $state.apiPid
    $viteAlive = Test-PidAlive -ProcessId $state.vitePid
    $apiHealthy = if ($apiAlive) { Test-ApiHealthy -Url $state.apiUrl } else { $false }
    $viteHealthy = if ($viteAlive) { Test-ViteHealthy -Url $state.viteUrl } else { $false }

    Write-Host "State:     recorded at $($state.startedAt)"
    Write-Host "API:       $($state.apiUrl) (PID $($state.apiPid)) - $(if ($apiHealthy) { 'healthy' } elseif ($apiAlive) { 'alive but not responding' } else { 'dead' })"
    Write-Host "Frontend:  $($state.viteUrl) (PID $($state.vitePid)) - $(if ($viteHealthy) { 'healthy' } elseif ($viteAlive) { 'alive but not responding' } else { 'dead' })"

    if (-not $apiAlive -or -not $viteAlive) {
        Write-Host ""
        Write-Host "Stale state detected. Run .\scripts\dev-servers.ps1 ensure to clean up and restart."
    }
}

switch ($Command) {
    'ensure' { Invoke-Ensure }
    'start'  { Invoke-Ensure }
    'stop'   { Invoke-Stop }
    'status' { Invoke-Status }
}
