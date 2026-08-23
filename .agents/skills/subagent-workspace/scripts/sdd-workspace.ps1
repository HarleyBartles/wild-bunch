param(
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

# Resolve repo root and branch. If an absolute plan file is provided, use its
# working tree so a worktree plan uses the worktree root instead of the main
# checkout root. Otherwise resolve relative to the current directory's repo.
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

# Use the main checkout's sibling for scratch, even from a linked worktree.
$commonDir = (git -C "$root" rev-parse --git-common-dir).Trim()
$mainCheckout = Split-Path -Parent $commonDir
$scratchParent = Split-Path -Parent $mainCheckout
$repoName = Split-Path -Leaf $mainCheckout
$branch = (git -C "$root" rev-parse --abbrev-ref HEAD).Trim()
$branch = $branch -replace '[\\/:*?"<>|]', '-'
$workspaceRoot = Join-Path (Join-Path (Join-Path $scratchParent "_agent-scratch") $repoName) $branch

# One-time migration: if the repo-scoped path has not been created yet but a
# legacy flat `_agent-scratch/<branch>` exists, copy its contents into the
# scoped path so the per-project separation takes effect immediately.
$legacyWorkspaceRoot = Join-Path (Join-Path $scratchParent "_agent-scratch") $branch
if ((Test-Path -LiteralPath $legacyWorkspaceRoot) -and -not (Test-Path -LiteralPath $workspaceRoot)) {
  New-Item -ItemType Directory -Force -Path $workspaceRoot | Out-Null
  Copy-Item -Path "$legacyWorkspaceRoot\*" -Destination $workspaceRoot -Recurse -Force -ErrorAction SilentlyContinue
}

New-Item -ItemType Directory -Force -Path $workspaceRoot | Out-Null

$currentPlanMarker = Join-Path $workspaceRoot 'current-plan.txt'
$planStem = ''
if (-not [string]::IsNullOrWhiteSpace($PlanFile)) {
  $planStem = Get-PlanStem -Path $PlanFile
  $encoding = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($currentPlanMarker, $planStem, $encoding)
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
