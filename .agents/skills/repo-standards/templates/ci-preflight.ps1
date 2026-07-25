<#
.SYNOPSIS
  Run the repository preflight checks for local and CI use.
#>
[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Full
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..')).Path

function Find-SkillScript($skill, $core) {
    $paths = @(
        (Join-Path $RepoRoot ".agents/skills/$skill/scripts/$core.ps1"),
        (Join-Path $RepoRoot ".agents/plugins/marketplace-source/codex-marketplace/plugins/repo-worker-pack/skills/$skill/scripts/$core.ps1"),
        (Join-Path $RepoRoot ".agents/plugins/marketplace-source/codex-marketplace/plugins/superpowers-plus/skills/$skill/scripts/$core.ps1"),
        (Join-Path $RepoRoot ".agents/plugins/marketplace-source/codex-marketplace/plugins/house-skills/skills/$skill/scripts/$core.ps1")
    )
    foreach ($p in $paths) {
        if (Test-Path $p) { return $p }
    }
    throw "$skill $core wrapper not found"
}

$mesh = Find-SkillScript 'generating-index-mesh' 'generate-index-mesh'
$refresh = Find-SkillScript 'refreshing-installed-skills' 'refresh-installed-skills'
$scope = if ($Full) { @() } else { @('-ChangedFrom', 'origin/main') }

& $mesh -Check:$Check @scope
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $refresh -Check:$Check
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$doctrine = Join-Path $ScriptDir 'validate_agent_mesh.ps1'
if (Test-Path $doctrine) {
    & $doctrine -Check:$Check @scope
    exit $LASTEXITCODE
}

exit 0
