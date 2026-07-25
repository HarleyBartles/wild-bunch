#!/usr/bin/env pwsh
param(
  [Parameter(Position = 0)]
  [string]$PlanFile = ''
)

$ErrorActionPreference = 'Stop'

function Get-PlanStem {
  param([string]$Path)

  $resolved = (Resolve-Path -LiteralPath $Path).Path
  $stem = [System.IO.Path]::GetFileNameWithoutExtension($resolved).Trim()
  if ([string]::IsNullOrWhiteSpace($stem)) {
    throw "plan file name must not be blank"
  }

  return ($stem -replace '[\\/:*?"<>|]', '-')
}

# Resolve the repo root. If an absolute plan file is provided, use its working
# tree so a worktree plan uses the worktree root instead of the main checkout
# root. Otherwise resolve relative to the current directory's repo.
$resolvedPlan = ''
if (-not [string]::IsNullOrWhiteSpace($PlanFile)) {
  $testPath = $PlanFile
  if (-not ([System.IO.Path]::IsPathRooted($testPath))) {
    try {
      $cwdRoot = (git rev-parse --show-toplevel).Trim()
      $testPath = Join-Path $cwdRoot $testPath
    }
    catch {
      # Current directory is not inside a git repo; leave relative and fail below
    }
  }
  if (-not (Test-Path -LiteralPath $testPath)) {
    Write-Error "no such plan file: $testPath"
    exit 2
  }
  $resolvedPlan = (Resolve-Path -LiteralPath $testPath).Path
  $planDir = Split-Path -Parent $resolvedPlan
  $root = (git -C "$planDir" rev-parse --show-toplevel).Trim()
}
else {
  $root = (git rev-parse --show-toplevel).Trim()
}

$PlanFile = $resolvedPlan

$workspaceRoot = Join-Path $root '.agents/superpowers/sdd'
New-Item -ItemType Directory -Force -Path $workspaceRoot | Out-Null
[System.IO.File]::WriteAllText((Join-Path $workspaceRoot '.gitignore'), "*`n", [System.Text.UTF8Encoding]::new($false))

$currentPlanMarker = Join-Path $workspaceRoot 'current-plan.txt'
$planStem = ''
if (-not [string]::IsNullOrWhiteSpace($PlanFile)) {
  $planStem = Get-PlanStem -Path $PlanFile
  Set-Content -LiteralPath $currentPlanMarker -Value $planStem -Encoding utf8
}
elseif (Test-Path -LiteralPath $currentPlanMarker) {
  $planStem = (Get-Content -LiteralPath $currentPlanMarker -Raw).Trim()
}

if ([string]::IsNullOrWhiteSpace($planStem)) {
  Write-Output $workspaceRoot
  exit 0
}

$planRoot = Join-Path $workspaceRoot $planStem
New-Item -ItemType Directory -Force -Path $planRoot | Out-Null
Write-Output $planRoot
