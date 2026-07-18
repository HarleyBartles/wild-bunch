#!/usr/bin/env pwsh
param(
  [Parameter(Position = 0, Mandatory = $true)]
  [string]$PlanFile,

  [Parameter(Position = 1, Mandatory = $true)]
  [string]$TaskNumber,

  [Parameter(Position = 2)]
  [string]$OutFile = ''
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $PlanFile)) {
  Write-Error "no such plan file: $PlanFile"
  exit 2
}

$scriptDir = Split-Path -Parent $PSCommandPath
if ([string]::IsNullOrWhiteSpace($OutFile)) {
  $workspace = & (Join-Path $scriptDir 'sdd-workspace.ps1') $PlanFile
  $OutFile = Join-Path $workspace "task-$TaskNumber-brief.md"
}

$selectedLines = New-Object 'System.Collections.Generic.List[string]'
$inFence = $false
$inTask = $false
foreach ($line in Get-Content -LiteralPath $PlanFile) {
  if ($line -match '^```') {
    $inFence = -not $inFence
  }
  elseif (-not $inFence -and $line -match '^#+[ \t]+Task[ \t]+([0-9]+)') {
    $inTask = ([string]$Matches[1]) -eq ([string]$TaskNumber)
  }

  if ($inTask) {
    $selectedLines.Add($line)
  }
}

if ($selectedLines.Count -eq 0) {
  Write-Error "task $TaskNumber not found in $PlanFile (no heading matching 'Task $TaskNumber')"
  exit 3
}

$outParent = Split-Path -Parent $OutFile
if (-not [string]::IsNullOrWhiteSpace($outParent)) {
  New-Item -ItemType Directory -Force -Path $outParent | Out-Null
}

Set-Content -LiteralPath $OutFile -Value $selectedLines -Encoding utf8
Write-Output ("wrote {0}: {1} lines" -f $OutFile, $selectedLines.Count)
