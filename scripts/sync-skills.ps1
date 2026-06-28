<#
.SYNOPSIS
  Sync vendored repo skills from the pinned agent-asset-marketplace submodule
  into .agents/skills/ so Devin CLI (and any .agents-aware runtime) discovers
  and invokes them as repo skills.

.DESCRIPTION
  Reads .agents/plugins/marketplace.json, resolves each default-installed
  plugin's skills/ directory under .agents/plugins/marketplace-source/, and
  copies every <skill>/ folder (full fidelity: SKILL.md + references/ +
  assets/ + scripts/ + hooks/ + agents/) into .agents/skills/<skill>/.

  Provenance is recorded in .agents/skills/.provenance.json as the submodule
  HEAD SHA. When the recorded SHA matches the current submodule HEAD and the
  target tree is non-empty, the sync is a no-op (cheap re-run).

  To refresh after upstream plugin updates:
    git submodule update --remote .agents/plugins/marketplace-source
    .\scripts\sync-skills.ps1

.PARAMETER Force
  Re-copy all skill directories even when provenance matches.

.EXAMPLE
  .\scripts\sync-skills.ps1
  .\scripts\sync-skills.ps1 -Force
#>
[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$RepoRoot = (Resolve-Path (Join-Path $ScriptDir '..')).Path

$MarketplaceJsonPath = Join-Path $RepoRoot '.agents\plugins\marketplace.json'
$SubmoduleRoot = Join-Path $RepoRoot '.agents\plugins\marketplace-source'
$PluginsRoot = Join-Path $SubmoduleRoot 'codex-marketplace\plugins'
$SkillsRoot = Join-Path $RepoRoot '.agents\skills'
$ProvenancePath = Join-Path $SkillsRoot '.provenance.json'

if (-not (Test-Path $MarketplaceJsonPath)) {
    throw "marketplace.json not found at $MarketplaceJsonPath"
}
if (-not (Test-Path $SubmoduleRoot)) {
    throw "Submodule not initialized at $SubmoduleRoot. Run: git submodule update --init .agents/plugins/marketplace-source"
}
if (-not (Test-Path $PluginsRoot)) {
    throw "Plugin source root not found at $PluginsRoot. Submodule may be on an unexpected branch."
}

# Resolve submodule HEAD SHA (cheap, no network).
$SubmoduleSha = (& git -C $SubmoduleRoot rev-parse HEAD 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($SubmoduleSha)) {
    throw "Could not resolve submodule HEAD SHA at $SubmoduleRoot"
}
$SubmoduleSha = $SubmoduleSha.Trim()

# Provenance-based skip (idempotent re-run).
$existingProvenance = $null
if (Test-Path $ProvenancePath) {
    try {
        $existingProvenance = (Get-Content $ProvenancePath -Raw -Encoding UTF8 | ConvertFrom-Json)
    } catch {
        $existingProvenance = $null
    }
}

$hasSkillDirs = $false
if (Test-Path $SkillsRoot) {
    $hasSkillDirs = $null -ne (Get-ChildItem -Path $SkillsRoot -Directory -ErrorAction SilentlyContinue | Select-Object -First 1)
}

if (-not $Force -and $existingProvenance -and $existingProvenance.sha -eq $SubmoduleSha -and $hasSkillDirs) {
    Write-Host "Skills already synced at submodule SHA $SubmoduleSha. Use -Force to re-copy." -ForegroundColor DarkGray
    Write-Host "Synced skills: $($existingProvenance.syncedSkills) from $($existingProvenance.syncedPlugins) plugins." -ForegroundColor DarkGray
    return
}

# Load marketplace and resolve default-installed plugins.
$marketplace = (Get-Content $MarketplaceJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json)
$defaultPlugins = @($marketplace.plugins | Where-Object { $_.policy.installation -eq 'INSTALLED_BY_DEFAULT' })

if ($defaultPlugins.Count -eq 0) {
    throw "No INSTALLED_BY_DEFAULT plugins found in $MarketplaceJsonPath"
}

Write-Host "Syncing skills from $($defaultPlugins.Count) default-installed plugins (submodule SHA $SubmoduleSha)..." -ForegroundColor Cyan

# Ensure skills root exists.
if (-not (Test-Path $SkillsRoot)) {
    New-Item -ItemType Directory -Path $SkillsRoot | Out-Null
}

# Track synced skill names so we can prune stale vendored skills not in the
# current default-installed set (e.g. after a plugin is removed or renamed).
$syncedSkillNames = [System.Collections.Generic.HashSet[string]]::new()
$syncedPluginNames = [System.Collections.Generic.List[string]]::new()

# Copy each plugin's skill directories.
foreach ($plugin in $defaultPlugins) {
    $pluginName = $plugin.name
    $pluginSkillsDir = Join-Path (Join-Path $PluginsRoot $pluginName) 'skills'
    if (-not (Test-Path $pluginSkillsDir)) {
        Write-Warning "Plugin '$pluginName' has no skills/ directory at $pluginSkillsDir; skipping."
        continue
    }

    $skillDirs = Get-ChildItem -Path $pluginSkillsDir -Directory -ErrorAction SilentlyContinue
    if (-not $skillDirs) {
        Write-Warning "Plugin '$pluginName' skills/ is empty at $pluginSkillsDir; skipping."
        continue
    }

    $syncedPluginNames.Add($pluginName) | Out-Null
    $pluginSkillCount = 0

    foreach ($skillDir in $skillDirs) {
        $skillName = $skillDir.Name
        $destSkillDir = Join-Path $SkillsRoot $skillName

        # Collision guard: if two plugins project a skill with the same name,
        # the first one wins and a warning is emitted. This keeps the sync
        # deterministic and surfaces the conflict rather than silently
        # overwriting.
        if ($syncedSkillNames.Contains($skillName)) {
            Write-Warning "Skill '$skillName' (from plugin '$pluginName') collides with an already-synced skill of the same name; keeping the first copy."
            continue
        }

        # Full-fidelity copy: remove existing dest, then copy the whole tree.
        if (Test-Path $destSkillDir) {
            Remove-Item -Recurse -Force $destSkillDir
        }
        Copy-Item -Recurse -Force $skillDir.FullName $destSkillDir

        $syncedSkillNames.Add($skillName) | Out-Null
        $pluginSkillCount++
    }

    Write-Host "  $pluginName : $pluginSkillCount skill(s)" -ForegroundColor Gray
}

# Prune stale vendored skill directories that are no longer projected by any
# default-installed plugin. Preserve custody skills (those not in the
# marketplace's known plugin set) only if explicitly marked — but per mesh
# policy, custody skills must have a named reason in mesh-policy.md. The
# current policy says no custody skills are retained, so any vendored skill
# dir not in $syncedSkillNames is stale and removed.
$staleDirs = Get-ChildItem -Path $SkillsRoot -Directory -ErrorAction SilentlyContinue | Where-Object { -not $syncedSkillNames.Contains($_.Name) }
foreach ($stale in $staleDirs) {
    Write-Warning "Removing stale vendored skill '$($stale.Name)' (no longer projected by default-installed plugins)."
    Remove-Item -Recurse -Force $stale.FullName
}

# Write provenance.
$provenance = [ordered]@{
    sha = $SubmoduleSha
    syncedAt = (Get-Date -Format 'o')
    syncedPlugins = $syncedPluginNames.ToArray()
    syncedSkills = $syncedSkillNames.Count
    source = 'HarleyBartles/agent-asset-marketplace'
    sourcePath = '.agents/plugins/marketplace-source/codex-marketplace/plugins'
    marketplaceFile = '.agents/plugins/marketplace.json'
}
$provenanceJson = $provenance | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText($ProvenancePath, $provenanceJson, (New-Object System.Text.UTF8Encoding $false))

Write-Host ""
Write-Host "Synced $($syncedSkillNames.Count) skill(s) from $($syncedPluginNames.Count) plugin(s) into .agents/skills/." -ForegroundColor Green
Write-Host "Provenance: $SubmoduleSha -> $ProvenancePath" -ForegroundColor DarkGray
Write-Host "Next: regenerate the index mesh with 'python scripts/generate_index_mesh.py'." -ForegroundColor DarkGray
