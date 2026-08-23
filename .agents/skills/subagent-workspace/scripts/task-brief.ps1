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

# Resolve relative plan paths against the current directory's repo root, then
# fall back to the repo/worktree root of the plan file itself.
$resolvedPlan = $PlanFile
if (-not ([System.IO.Path]::IsPathRooted($resolvedPlan))) {
  try {
    $cwdRoot = (git rev-parse --show-toplevel).Trim()
    $resolvedPlan = Join-Path $cwdRoot $resolvedPlan
  }
  catch {
    # Current directory is not inside a git repo; leave relative and fail below
  }
}

if (-not (Test-Path -LiteralPath $resolvedPlan)) {
  Write-Error "no such plan file: $resolvedPlan"
  exit 2
}

$resolvedPlan = (Resolve-Path -LiteralPath $resolvedPlan).Path

$scriptDir = Split-Path -Parent $PSCommandPath
if ([string]::IsNullOrWhiteSpace($OutFile)) {
  $workspace = & ([System.IO.Path]::Combine($scriptDir, 'sdd-workspace.ps1')) $resolvedPlan
  $OutFile = Join-Path $workspace "task-$TaskNumber-brief.md"
}

$selectedLines = New-Object 'System.Collections.Generic.List[string]'
$inFence = $false
$inTask = $false
foreach ($line in Get-Content -LiteralPath $resolvedPlan) {
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
  Write-Error "task $TaskNumber not found in $resolvedPlan (no heading matching 'Task $TaskNumber')"
  exit 3
}

$outParent = Split-Path -Parent $OutFile
if (-not [string]::IsNullOrWhiteSpace($outParent)) {
  New-Item -ItemType Directory -Force -Path $outParent | Out-Null
}

$encoding = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($OutFile, $selectedLines, $encoding)
Write-Output ("wrote {0}: {1} lines" -f $OutFile, $selectedLines.Count)
