#!/usr/bin/env pwsh
param(
  [Parameter(Position = 0)]
  [string]$PlanFile = ''
)

$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
  (git rev-parse --show-toplevel).Trim()
}

function Get-PlanStem {
  param([string]$Path)

  $resolved = (Resolve-Path -LiteralPath $Path).Path
  $stem = [System.IO.Path]::GetFileNameWithoutExtension($resolved).Trim()
  if ([string]::IsNullOrWhiteSpace($stem)) {
    throw "plan file name must not be blank"
  }

  return ($stem -replace '[\\/:*?"<>|]', '-')
}

$root = Get-RepoRoot
$workspaceRoot = Join-Path $root '.agents/superpowers/sdd'
New-Item -ItemType Directory -Force -Path $workspaceRoot | Out-Null
Set-Content -LiteralPath (Join-Path $workspaceRoot '.gitignore') -Value '*' -Encoding utf8

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
