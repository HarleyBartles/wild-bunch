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
$HealthRetryDelaysSeconds = @(2, 4, 8, 16, 32)

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
        checkoutRoot = $WorktreeRoot
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

function Get-ObjectValue {
    param(
        [Parameter(Mandatory)]$InputObject,
        [Parameter(Mandatory)][string]$Name
    )

    if ($null -eq $InputObject) {
        return $null
    }

    if ($InputObject -is [System.Collections.IDictionary]) {
        return $InputObject[$Name]
    }

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -ne $property) {
        return $property.Value
    }

    return $null
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

function Invoke-DotNetBuild {
    param([Parameter(Mandatory)][string]$ProjectPath)
    & dotnet build $ProjectPath --nologo | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $ProjectPath."
    }
}

function Invoke-NpmBuild {
    param([Parameter(Mandatory)][string]$WorkingDirectory)

    Push-Location $WorkingDirectory
    try {
        & npm.cmd run build | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "npm build failed in $WorkingDirectory."
        }
    }
    finally {
        Pop-Location
    }
}

function Wait-ForHealthy {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Url,
        [Parameter(Mandatory)][scriptblock]$Probe
    )

    for ($attempt = 0; $attempt -lt $HealthRetryDelaysSeconds.Count; $attempt++) {
        try {
            if (& $Probe) {
                return
            }
        }
        catch {}

        Start-Sleep -Seconds $HealthRetryDelaysSeconds[$attempt]
    }

    try {
        if (& $Probe) {
            return
        }
    }
    catch {}

    throw "$Name server did not become healthy on $Url after retries."
}

function Test-ApiHealthy {
    param([Parameter(Mandatory)][string]$Url)
    try {
        $checkUrl = $Url -replace '://localhost:', '://127.0.0.1:'
        $response = Invoke-WebRequest -Uri "$checkUrl/health" -Method GET -TimeoutSec 5 -UseBasicParsing -ErrorAction Stop
        return $response.StatusCode -eq 200
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
        return [PSCustomObject]@{ ApiPort = $CanonicalApiPort; VitePort = $CanonicalVitePort; Reused = $false; Reason = 'canonical ports free' }
    }

    if (-not $apiCanonicalFree) {
        $apiOwnedHere = Test-PortOwnedByWorktree -Port $CanonicalApiPort
        if ($apiOwnedHere) {
            return [PSCustomObject]@{ ApiPort = $CanonicalApiPort; VitePort = $CanonicalVitePort; Reused = $true; Reason = 'canonical API port owned by this worktree' }
        }
    }

    if (-not $viteCanonicalFree) {
        $viteOwnedHere = Test-PortOwnedByWorktree -Port $CanonicalVitePort
        if ($viteOwnedHere) {
            return [PSCustomObject]@{ ApiPort = $CanonicalApiPort; VitePort = $CanonicalVitePort; Reused = $true; Reason = 'canonical Vite port owned by this worktree' }
        }
    }

    $apiPort = Find-FreePort -StartPort ($CanonicalApiPort + 1)
    $vitePort = Find-FreePort -StartPort ($CanonicalVitePort + 1)
    return [PSCustomObject]@{ ApiPort = $apiPort; VitePort = $vitePort; Reused = $false; Reason = 'canonical ports occupied by another worktree; allocated fallback ports' }
}

function Start-ApiServer {
    param([Parameter(Mandatory)][int]$Port)

    $apiUrl = "http://localhost:$Port"
    Invoke-DotNetBuild -ProjectPath $ApiProject
    $env:ConnectionStrings__WildBunchPostgresDb = $PostgresConnectionString
    $env:ASPNETCORE_ENVIRONMENT = 'Development'

    $process = Start-Process -FilePath 'dotnet' `
        -ArgumentList @('run', '--project', $ApiProject, '--urls', $apiUrl) `
        -WorkingDirectory $WorktreeRoot `
        -PassThru `
        -WindowStyle Hidden

    try {
        Wait-ForHealthy -Name 'API' -Url $apiUrl -Probe { Test-ApiHealthy -Url $apiUrl }
        return @($process.Id, $apiUrl)
    }
    catch {
        Stop-ProcessTree -ProcessId $process.Id
        throw
    }
}

function Start-ViteServer {
    param([Parameter(Mandatory)][int]$Port, [Parameter(Mandatory)][string]$ApiUrl)

    Invoke-NpmBuild -WorkingDirectory (Join-Path $WorktreeRoot $WebProject)
    $env:VITE_API_BASE_URL = $ApiUrl
    $viteUrl = "http://localhost:$Port"

    $process = Start-Process -FilePath 'npm.cmd' `
        -ArgumentList @('run', 'dev', '--', '--port', $Port, '--strictPort') `
        -WorkingDirectory (Join-Path $WorktreeRoot $WebProject) `
        -PassThru `
        -WindowStyle Hidden

    try {
        Wait-ForHealthy -Name 'Vite' -Url $viteUrl -Probe { Test-ViteHealthy -Url $viteUrl }
        return @($process.Id, $viteUrl)
    }
    catch {
        Stop-ProcessTree -ProcessId $process.Id
        throw
    }
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
        $apiPid = Get-ObjectValue -InputObject $state -Name 'apiPid'
        $vitePid = Get-ObjectValue -InputObject $state -Name 'vitePid'
        $apiUrl = Get-ObjectValue -InputObject $state -Name 'apiUrl'
        $viteUrl = Get-ObjectValue -InputObject $state -Name 'viteUrl'
        $apiAlive = Test-PidAlive -ProcessId $apiPid
        $viteAlive = Test-PidAlive -ProcessId $vitePid

        if ($apiAlive -and $viteAlive `
            -and (Test-ApiHealthy -Url $apiUrl) `
            -and (Test-ViteHealthy -Url $viteUrl)) {
            Write-State -ApiPid $apiPid -VitePid $vitePid -ApiPort (Get-ObjectValue -InputObject $state -Name 'apiPort') -VitePort (Get-ObjectValue -InputObject $state -Name 'vitePort') -ApiUrl $apiUrl -ViteUrl $viteUrl
            Write-Host "Dev servers already running for this worktree (reused)."
            Write-Host "  Checkout:  $WorktreeRoot"
            Write-Host "  Worktree:  $WorktreeRoot"
            Write-Host "  State:     $StateFile"
            Write-Host "  Branch:    $(Get-WorktreeBranch)"
            Write-Host "  API:       $apiUrl (PID $apiPid)"
            Write-Host "  Frontend:  $viteUrl (PID $vitePid)"
            Write-Host "  Reused:    yes (state file matched live processes)"
            return
        }

        if (-not $apiAlive -or -not $viteAlive) {
            Write-Host "Stale dev-server state detected; cleaning up and restarting."
            if ($apiAlive) { Stop-ProcessTree -ProcessId $apiPid }
            if ($viteAlive) { Stop-ProcessTree -ProcessId $vitePid }
            Clear-State
        }
    }

    $ports = Resolve-ServerPorts

    if ($ports.Reused) {
        $apiPort = Get-ObjectValue -InputObject $ports -Name 'ApiPort'
        $vitePort = Get-ObjectValue -InputObject $ports -Name 'VitePort'
        $reason = Get-ObjectValue -InputObject $ports -Name 'Reason'
        $apiPids = @(Get-PortListenerPids -Port $apiPort)
        $vitePids = @(Get-PortListenerPids -Port $vitePort)
        $apiPid = if ($apiPids.Count -gt 0) { $apiPids[0] } else { 0 }
        $vitePid = if ($vitePids.Count -gt 0) { $vitePids[0] } else { 0 }
        $apiUrl = "http://localhost:$apiPort"
        $viteUrl = "http://localhost:$vitePort"

        if ((Test-ApiHealthy -Url $apiUrl) -and (Test-ViteHealthy -Url $viteUrl)) {
            Write-State -ApiPid $apiPid -VitePid $vitePid -ApiPort $apiPort -VitePort $vitePort -ApiUrl $apiUrl -ViteUrl $viteUrl
            Write-Host "Dev servers already running for this worktree (reused)."
            Write-Host "  Worktree:  $WorktreeRoot"
            Write-Host "  Branch:    $(Get-WorktreeBranch)"
            Write-Host "  API:       $apiUrl (PID $apiPid)"
            Write-Host "  Frontend:  $viteUrl (PID $vitePid)"
            Write-Host "  Reused:    yes ($reason)"
            return
        }

        Write-Host "Existing dev servers for this worktree are unhealthy; restarting."
        if ($apiPid -ne 0) { Stop-ProcessTree -ProcessId $apiPid }
        if ($vitePid -ne 0 -and $vitePid -ne $apiPid) { Stop-ProcessTree -ProcessId $vitePid }
        Clear-State
        $ports = Resolve-ServerPorts
    }

    $apiPort = Get-ObjectValue -InputObject $ports -Name 'ApiPort'
    $vitePort = Get-ObjectValue -InputObject $ports -Name 'VitePort'
    $reason = Get-ObjectValue -InputObject $ports -Name 'Reason'
    Write-Host "Starting dev servers for this worktree."
    Write-Host "  Checkout:  $WorktreeRoot"
    Write-Host "  Worktree:  $WorktreeRoot"
    Write-Host "  State:     $StateFile"
    Write-Host "  Branch:    $(Get-WorktreeBranch)"
    Write-Host "  Reason:    $reason"

    $api = $null
    $vite = $null
    try {
        $apiPid, $apiUrl = Start-ApiServer -Port $apiPort
        Write-Host "  API:       $apiUrl (PID $apiPid)"

        $vitePid, $viteUrl = Start-ViteServer -Port $vitePort -ApiUrl $apiUrl
        Write-Host "  Frontend:  $viteUrl (PID $vitePid)"

        Write-State -ApiPid $apiPid -VitePid $vitePid -ApiPort $apiPort -VitePort $vitePort -ApiUrl $apiUrl -ViteUrl $viteUrl
    }
    catch {
        if ($null -ne $vitePid -and $vitePid -ne 0) {
            Stop-ProcessTree -ProcessId $vitePid
        }
        if ($null -ne $apiPid -and $apiPid -ne 0) {
            Stop-ProcessTree -ProcessId $apiPid
        }
        Clear-State
        throw
    }

    Write-Host ""
    Write-Host "Dev servers are ready."
    Write-Host "  Checkout:  $WorktreeRoot"
    Write-Host "  API:      $apiUrl"
    Write-Host "  Frontend: $viteUrl"
    if ($apiPort -ne $CanonicalApiPort -or $vitePort -ne $CanonicalVitePort) {
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

    $apiPid = Get-ObjectValue -InputObject $state -Name 'apiPid'
    $apiUrl = Get-ObjectValue -InputObject $state -Name 'apiUrl'
    if (Test-PidAlive -ProcessId $apiPid) {
        Stop-ProcessTree -ProcessId $apiPid
        $stoppedApi = $true
        Write-Host "Stopped API server (PID $apiPid) on $apiUrl."
    }

    $vitePid = Get-ObjectValue -InputObject $state -Name 'vitePid'
    $viteUrl = Get-ObjectValue -InputObject $state -Name 'viteUrl'
    if (Test-PidAlive -ProcessId $vitePid) {
        Stop-ProcessTree -ProcessId $vitePid
        $stoppedVite = $true
        Write-Host "Stopped Vite dev server (PID $vitePid) on $viteUrl."
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
    Write-Host "Checkout:  $WorktreeRoot"
    Write-Host "State:     $StateFile"
    Write-Host "Branch:    $branch"

    if ($null -eq $state) {
        Write-Host "State:     no state file found"
        Write-Host ""
        Write-Host "No dev servers recorded for this worktree."
        Write-Host "Run .\scripts\dev-servers.ps1 ensure to start them."
        return
    }

    $apiPid = Get-ObjectValue -InputObject $state -Name 'apiPid'
    $vitePid = Get-ObjectValue -InputObject $state -Name 'vitePid'
    $apiUrl = Get-ObjectValue -InputObject $state -Name 'apiUrl'
    $viteUrl = Get-ObjectValue -InputObject $state -Name 'viteUrl'
    $apiAlive = Test-PidAlive -ProcessId $apiPid
    $viteAlive = Test-PidAlive -ProcessId $vitePid
    $apiHealthy = if ($apiAlive) { Test-ApiHealthy -Url $apiUrl } else { $false }
    $viteHealthy = if ($viteAlive) { Test-ViteHealthy -Url $viteUrl } else { $false }

    Write-Host "State:     recorded at $(Get-ObjectValue -InputObject $state -Name 'startedAt')"
    Write-Host "API:       $apiUrl (PID $apiPid) - $(if ($apiHealthy) { 'healthy' } elseif ($apiAlive) { 'alive but not responding' } else { 'dead' })"
    Write-Host "Frontend:  $viteUrl (PID $vitePid) - $(if ($viteHealthy) { 'healthy' } elseif ($viteAlive) { 'alive but not responding' } else { 'dead' })"

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
