#!/usr/bin/env pwsh
param(
  [Parameter(Position = 0, Mandatory = $true)]
  [string]$Base,

  [Parameter(Position = 1, Mandatory = $true)]
  [string]$Head,

  [Parameter(Position = 2, Mandatory = $true)]
  [string]$PlanFile,

  [Parameter(Position = 3)]
  [string]$OutFile = ''
)

$ErrorActionPreference = 'Stop'

# Resolve relative plan paths against the current directory's repo root, then
# determine the target repo/worktree root from the plan file's location.
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
    $workspace = & (Join-Path $scriptDir 'sdd-workspace.ps1') $resolvedPlan
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

  Set-Content -LiteralPath $OutFile -Value $content -Encoding utf8

  $commits = [int](& git rev-list --count "$Base..$Head")
  $bytes = (Get-Item -LiteralPath $OutFile).Length
  Write-Output ("wrote {0}: {1} commit(s), {2} bytes" -f $OutFile, $commits, $bytes)
}
finally {
  Pop-Location
}
