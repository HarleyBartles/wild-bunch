#!/usr/bin/env pwsh
param(
  [Parameter(Position = 0, Mandatory = $true)]
  [string]$PlanFile,

  [Parameter(Position = 1, Mandatory = $true)]
  [string]$Base,

  [Parameter(Position = 2, Mandatory = $true)]
  [string]$Head,

  [Parameter(Position = 3)]
  [string]$OutFile = ''
)

$ErrorActionPreference = 'Stop'

# Resolve the repo/worktree root. $PlanFile can be `-` to mean "no plan; use
# the current directory's repo and the plan-less workspace from sdd-workspace".
if ($PlanFile -eq '-') {
  $resolvedPlan = $null
  $repoRoot = (git rev-parse --show-toplevel).Trim()
}
else {
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
    Write-Error "bad PLAN_FILE: $resolvedPlan"
    exit 2
  }

  $resolvedPlan = (Resolve-Path -LiteralPath $resolvedPlan).Path
  $repoRoot = (git -C (Split-Path -Parent $resolvedPlan) rev-parse --show-toplevel).Trim()
}

if (-not [string]::IsNullOrWhiteSpace($OutFile)) {
  if (-not [System.IO.Path]::IsPathRooted($OutFile)) {
    $OutFile = Join-Path $repoRoot $OutFile
  }
}

Push-Location $repoRoot
try {
  & git rev-parse --verify --quiet $Base *> $null
  if ($LASTEXITCODE -ne 0) {
    Write-Error "bad BASE: $Base"
    exit 2
  }

  & git rev-parse --verify --quiet $Head *> $null
  if ($LASTEXITCODE -ne 0) {
    Write-Error "bad HEAD: $Head"
    exit 2
  }

  $scriptDir = Split-Path -Parent $PSCommandPath
  if ([string]::IsNullOrWhiteSpace($OutFile)) {
    if ($resolvedPlan) {
      $workspace = & ([System.IO.Path]::Combine($scriptDir, 'sdd-workspace.ps1')) $resolvedPlan
    }
    else {
      $workspace = & ([System.IO.Path]::Combine($scriptDir, 'sdd-workspace.ps1'))
    }
    $baseShort = (& git rev-parse --short $Base).Trim()
    $headShort = (& git rev-parse --short $Head).Trim()
    $OutFile = Join-Path $workspace ("review-{0}..{1}.diff" -f $baseShort, $headShort)
  }

  $outParent = Split-Path -Parent $OutFile
  if (-not [string]::IsNullOrWhiteSpace($outParent)) {
    New-Item -ItemType Directory -Force -Path $outParent | Out-Null
  }

  $content = @(
    "# Review package: $Base..$Head"
    ""
    "## Commits"
  ) + @(& git log --oneline "$Base..$Head") + @(
    ""
    "## Files changed"
  ) + @(& git diff --stat "$Base..$Head") + @(
    ""
    "## Diff"
  ) + @(& git diff -U10 "$Base..$Head")

  $encoding = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllLines($OutFile, $content, $encoding)

  $commits = [int](& git rev-list --count "$Base..$Head")
  $bytes = (Get-Item -LiteralPath $OutFile).Length
  Write-Output ("wrote {0}: {1} commit(s), {2} bytes" -f $OutFile, $commits, $bytes)
}
finally {
  Pop-Location
}
