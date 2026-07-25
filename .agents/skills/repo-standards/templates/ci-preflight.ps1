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

$standards = Find-SkillScript 'repo-standards' 'repo-standards'
$scaffold = Find-SkillScript 'repo-standards' 'scaffold-all'
$mesh = Find-SkillScript 'generating-agent-mesh' 'generate-index-mesh'
$validate = Find-SkillScript 'generating-agent-mesh' 'validate-agent-mesh'
$refresh = Find-SkillScript 'refreshing-installed-skills' 'refresh-installed-skills'

& $standards -Check:$Check
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $scaffold -Check:$Check
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# generate-index-mesh reconciles the whole tracked mesh; scoped diff is
# handled by validate-agent-mesh and the optional ci-preflight-extra hook.
& $mesh -Check:$Check
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$validateArgs = @()
if ($Check) { $validateArgs += '--check' }
if ($ChangedFrom) { $validateArgs += '--changed-from'; $validateArgs += $ChangedFrom }
& $validate @validateArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $refresh -Check:$Check
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$extra = Join-Path $ScriptDir 'ci-preflight-extra.ps1'
if (Test-Path $extra) {
    & $extra -Check:$Check -ChangedFrom:$ChangedFrom
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

exit 0
