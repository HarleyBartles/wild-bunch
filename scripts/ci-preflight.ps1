<#
.SYNOPSIS
  Run the repository preflight checks for local and CI use.
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Full,
    [string]$ChangedFrom
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..')).Path

function Find-SkillScript($skill, $core) {
    $installed = Join-Path $RepoRoot ".agents/skills/$skill/scripts/$core.ps1"
    if (Test-Path $installed) { return $installed }

    $marketplaceSource = Join-Path $RepoRoot ".agents/plugins/marketplace-source/codex-marketplace/plugins"
    if (Test-Path $marketplaceSource) {
        $glob = Join-Path $marketplaceSource "*/skills/$skill/scripts/$core.ps1"
        $found = @(Get-Item $glob -ErrorAction SilentlyContinue)
        if ($found.Count -gt 0) { return $found[0].FullName }
    }
    throw "$skill $core wrapper not found"
}

# Bundled skill .ps1 wrappers use ValueFromRemainingArguments and expect the
# same --check/--changed-from flags as the underlying Python scripts.
$commonArgs = @()
if ($Check) { $commonArgs += '--check' }

$standards = Find-SkillScript 'repo-standards' 'repo-standards'
& $standards @commonArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$scaffold = Find-SkillScript 'repo-standards' 'scaffold-all'
& $scaffold @commonArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# generate-index-mesh reconciles the whole tracked mesh; scoped diff is
# handled by validate-agent-mesh and the optional ci-preflight-extra hook.
$mesh = Find-SkillScript 'generating-agent-mesh' 'generate-index-mesh'
& $mesh @commonArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$validateArgs = @()
if ($Check) { $validateArgs += '--check' }
if ($ChangedFrom) { $validateArgs += '--changed-from'; $validateArgs += $ChangedFrom }
$validate = Find-SkillScript 'generating-agent-mesh' 'validate-agent-mesh'
& $validate @validateArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$refresh = Find-SkillScript 'refreshing-installed-skills' 'refresh-installed-skills'
& $refresh @commonArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$extra = Join-Path $ScriptDir 'ci-preflight-extra.ps1'
if (Test-Path $extra) {
    $extraArgs = @{ }
    if ($Check) { $extraArgs['Check'] = $true }
    if ($ChangedFrom) { $extraArgs['ChangedFrom'] = $ChangedFrom }
    & $extra @extraArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

exit 0
