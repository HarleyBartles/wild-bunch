# CI Efficiency: Draft PR Gating and Local Preflight Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce GitHub Actions CI churn by skipping CI on draft PRs, adding a local `ci-preflight.ps1` script that mirrors CI, and updating agent-facing policy docs to require the preflight before moving PRs to ready.

**Architecture:** A new PowerShell preflight script (`scripts/ci-preflight.ps1`) coordinates the existing `scripts/postgres-dev.ps1`, `scripts/generate_index_mesh.ps1`, `dotnet`, `npm`, and `python` commands. The `.github/workflows/ci.yml` trigger is narrowed to non-draft PRs plus `ready_for_review`. `.agents/docs/workflow-policy.md` and `.agents/docs/validation-policy.md` enforce draft-by-default and preflight discipline. `scripts/README.md` documents the script.

**Tech Stack:** GitHub Actions, PowerShell, .NET, Node/Vite, Python (index mesh)

## Global Constraints

- Keep changes to CI workflow, scripts, and agent-facing docs only. No product code changes.
- The preflight script must run on Windows PowerShell from the repo root or any git worktree.
- The preflight script must reuse the existing shared local PostgreSQL service (`scripts/postgres-dev.ps1`).
- The `index-mesh` CI job stays in `ci.yml` for this change; splitting it is a fast-follow.
- All file path references must be exact and verified against current source.
- The local preflight must be CI-equivalent: `Release` build/test, `npm ci`, `npm run typecheck/test/build`, and index mesh `--check`.
- No Python wrapper is needed for the preflight script; it is a task orchestration script that follows the `postgres-dev.ps1` / `dev-servers.ps1` pattern.

## Why PowerShell (not a Python script with `.ps1` wrapper)

The preflight is a task orchestration script, not a Python-anchored data processing script. It calls `dotnet`, `npm`, and existing PowerShell scripts (`postgres-dev.ps1`, `generate_index_mesh.ps1`). The repo's existing orchestration scripts (`dev-servers.ps1`, `postgres-dev.ps1`) are PowerShell. The local dev environment is Windows. A Python script would need to shell out to PowerShell to run `postgres-dev.ps1` and `generate_index_mesh.ps1`, which is less natural. Therefore the primary script is `scripts/ci-preflight.ps1`.

---

### Task 1: Create `scripts/ci-preflight.ps1`

**Files:**
- Create: `scripts/ci-preflight.ps1`

**Interfaces:**
- Consumes: `scripts/postgres-dev.ps1`, `scripts/generate_index_mesh.ps1`, `dotnet`, `npm`, `py`
- Produces: `scripts/ci-preflight.ps1` (local CI preflight command)

- [x] **Step 1: Verify the script does not exist and run a failing test**

Run:
```powershell
Test-Path .\scripts\ci-preflight.ps1
```
Expected: `False`

- [x] **Step 2: Create the PowerShell preflight script**

Create `scripts/ci-preflight.ps1` with the following content:

```powershell
[CmdletBinding()]
param(
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$SkipIndexMesh
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ScriptDir = (Resolve-Path $PSScriptRoot).Path
$RepoRoot = & git rev-parse --show-toplevel

Push-Location -LiteralPath $RepoRoot
try {
    if (-not $SkipBackend) {
        Write-Host '--- Backend preflight ---'
        dotnet restore WildBunch.sln
        dotnet build WildBunch.sln --no-restore --configuration Release
        dotnet tool restore
        & "$ScriptDir/postgres-dev.ps1" test -- dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api --configuration Release
        & "$ScriptDir/postgres-dev.ps1" test -- dotnet test WildBunch.sln --no-build --no-restore --configuration Release
    }

    if (-not $SkipFrontend) {
        Write-Host '--- Frontend preflight ---'
        Push-Location src/WildBunch.Web
        try {
            npm ci
            npm run typecheck
            npm run test
            npm run build
        }
        finally {
            Pop-Location
        }
    }

    if (-not $SkipIndexMesh) {
        Write-Host '--- Index mesh preflight ---'
        & "$ScriptDir/generate_index_mesh.ps1" -Check
    }
}
finally {
    Pop-Location
}
```

- [x] **Step 3: Run the script to verify it passes**

Ensure the shared local PostgreSQL service is available:
```powershell
.\scripts\postgres-dev.ps1 ensure
```

Run the backend preflight only:
```powershell
.\scripts\ci-preflight.ps1 -SkipFrontend -SkipIndexMesh
```
Expected: exits 0 with backend build and test output.

If the repository has known failing tests or build issues, the script will fail; do not commit until it passes. This is expected.

- [x] **Step 4: Commit**

```bash
git add scripts/ci-preflight.ps1
git commit -m "feat: add local CI preflight script"
```

---

### Task 2: Update `.github/workflows/ci.yml` to skip draft PRs

**Files:**
- Modify: `.github/workflows/ci.yml` (lines 3-13 and the `backend`, `frontend`, `index-mesh` job definitions)

**Interfaces:**
- Consumes: GitHub Actions `pull_request` event and `ready_for_review` type
- Produces: CI workflow that runs on `push` to `main` and non-draft `pull_request` events

- [x] **Step 1: Update the `on` block**

Replace the `on` block in `.github/workflows/ci.yml`:

```yaml
on:
  push:
    branches: [main]
  pull_request:
    types: [opened, synchronize, reopened, ready_for_review]
```

- [x] **Step 2: Add `if` guard to each job**

For the `backend` job, change:

```yaml
  backend:
    name: Backend (.NET build + tests)
    runs-on: ubuntu-latest
```

to:

```yaml
  backend:
    name: Backend (.NET build + tests)
    if: github.event_name != 'pull_request' || github.event.pull_request.draft == false
    runs-on: ubuntu-latest
```

For the `frontend` job, change:

```yaml
  frontend:
    name: Frontend (Vite tests + typecheck + build)
    runs-on: ubuntu-latest
```

to:

```yaml
  frontend:
    name: Frontend (Vite tests + typecheck + build)
    if: github.event_name != 'pull_request' || github.event.pull_request.draft == false
    runs-on: ubuntu-latest
```

For the `index-mesh` job, change:

```yaml
  index-mesh:
    name: Index mesh + plugin manifest
    runs-on: ubuntu-latest
```

to:

```yaml
  index-mesh:
    name: Index mesh + plugin manifest
    if: github.event_name != 'pull_request' || github.event.pull_request.draft == false
    runs-on: ubuntu-latest
```

- [x] **Step 3: Verify the YAML is valid**

If `pyyaml` is available, run:
```powershell
py -3 -c "import yaml, sys; yaml.safe_load(open('.github/workflows/ci.yml')); print('YAML OK')"
```
Expected: `YAML OK`

If `pyyaml` is not installed, visually verify the `on` and `if` blocks match the snippets above.

- [x] **Step 4: Open a draft PR smoke test (optional but recommended)**

Create a draft PR from this branch and push an empty commit. Verify that the CI workflow does not run. Mark the PR ready for review and verify CI runs. This is the authoritative test.

- [x] **Step 5: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: skip draft PRs and trigger on ready_for_review"
```

---

### Task 3: Update `.agents/docs/workflow-policy.md` to enforce draft PR discipline

**Files:**
- Modify: `.agents/docs/workflow-policy.md`

**Interfaces:**
- None

- [x] **Step 1: Update the `Branch + PR Workflow` section**

Replace the `Branch + PR Workflow` section with the following:

```markdown
## Branch + PR Workflow
- Workers branch from current `main`.
- Workers push a branch and **open or return a PR as draft** while work is in progress.
- A PR is not marked ready for review until work is complete, the branch is current with `origin/main`, and the local CI preflight (`.\scripts\ci-preflight.ps1`) passes.
- The PR is the normal publication surface.
- Direct pushes to `main` require explicit latest-turn authorization.
- `GREEN` means PR-ready with validation and evidence, not direct-main landing.
- Merge and landing verification are separate GPT or human steps after PR review and merge.
```

- [x] **Step 2: Insert the `Draft PR CI gating` section**

After the updated `Branch + PR Workflow` section, add:

```markdown
## Draft PR CI gating
- CI runs on `push` to `main` and on non-draft `pull_request` events.
- CI does not run on draft PRs.
- When a draft PR is marked ready for review, CI is triggered as the final gate.
- Use the local CI preflight (`.\scripts\ci-preflight.ps1`) to catch failures before moving a PR out of draft.
```

- [x] **Step 3: Update the `GREEN Checklist`**

Replace the `GREEN Checklist` with:

```markdown
## GREEN Checklist

Before claiming work is complete or requesting review, verify:

- [x] Work pushed to branch
- [x] PR raised as draft
- [x] Branch is current with `origin/main`
- [x] Local CI preflight passed (`.\scripts\ci-preflight.ps1`)
- [x] PR is marked ready for review
- [x] PR body fresh (matches actual implementation, not stale plan)
- [x] Linear issue fresh (updated with current status if applicable)
- [x] CI passing (all relevant checks green)
- [x] Index mesh regenerated (if file structure changed)
- [x] Plan committed with all checkboxes checked (if implementation plan exists)
```

- [x] **Step 4: Commit**

```bash
git add .agents/docs/workflow-policy.md
git commit -m "docs: add draft PR CI gating and local preflight workflow policy"
```

---

### Task 4: Update `.agents/docs/validation-policy.md` to add the CI preflight checklist

**Files:**
- Modify: `.agents/docs/validation-policy.md`

**Interfaces:**
- None

- [x] **Step 1: Insert the CI preflight section**

After the `## Validation Commands` section (line 15), add:

```markdown
## CI Preflight (run locally before marking a PR ready)

Before moving a PR out of draft, run the local CI preflight:

```powershell
.\scripts\ci-preflight.ps1
```

This mirrors the `ci.yml` workflow:

- Backend: `dotnet restore`, `dotnet build --configuration Release`, `dotnet tool restore`, `dotnet ef migrations list`, and `dotnet test --configuration Release` via the shared PostgreSQL service.
- Frontend: `npm ci`, `npm run typecheck`, `npm run test`, and `npm run build` in `src/WildBunch.Web`.
- Index mesh: `generate_index_mesh --check`.

If the script fails, fix the issue and re-run before marking the PR ready. Use `-SkipBackend`, `-SkipFrontend`, or `-SkipIndexMesh` to narrow the run when iterating.

For changes that affect persistence, `.\scripts\postgres-dev.ps1 validate` remains the focused PostgreSQL validation lane.
```

- [x] **Step 2: Commit**

```bash
git add .agents/docs/validation-policy.md
git commit -m "docs: add CI preflight checklist to validation policy"
```

---

### Task 5: Update `scripts/README.md` to document `ci-preflight.ps1`

**Files:**
- Modify: `scripts/README.md`

**Interfaces:**
- None

- [x] **Step 1: Add the new script section**

After the `### generate_index_mesh.py/.ps1` section, add:

```markdown
### ci-preflight.ps1
**Use when** you need to run the same checks CI runs before marking a PR ready for review.

- `.\scripts\ci-preflight.ps1` - run all local CI preflight checks
- `.\scripts\ci-preflight.ps1 -SkipBackend` - skip the .NET/PostgreSQL checks
- `.\scripts\ci-preflight.ps1 -SkipFrontend` - skip the Vite/TypeScript checks
- `.\scripts\ci-preflight.ps1 -SkipIndexMesh` - skip the index mesh validation

This mirrors the `ci.yml` workflow locally: `dotnet restore`, `dotnet build --configuration Release`, `dotnet ef migrations list`, `dotnet test --configuration Release` via the shared PostgreSQL service, the frontend `npm ci` / typecheck / test / build pipeline, and the `generate_index_mesh.py --check` validation.

**Use before** taking a PR out of draft. The `workflow-policy.md` requires the preflight to pass before marking a PR ready for review.
```

- [x] **Step 2: Commit**

```bash
git add scripts/README.md
git commit -m "docs: document ci-preflight.ps1 in scripts README"
```

---

### Task 6: Update `scripts/generate_index_mesh.py` to add a generated-file notice

**Files:**
- Modify: `scripts/generate_index_mesh.py` (around line 176)

**Interfaces:**
- None

- [x] **Step 1: Replace the generated notice in `render_index`**

In `scripts/generate_index_mesh.py`, replace the `render_index` header block:

```python
    lines: list[str] = ["# INDEX.md", ""]
    lines.append("This index is generated by `scripts/generate_index_mesh.py`.")
    lines.append("")
```

with:

```python
    lines: list[str] = ["# INDEX.md", ""]
    lines.append("> **Generated by `scripts/generate_index_mesh.py`.** "
              "Do not hand-edit this file. If it is stale, regenerate it with "
              "`python scripts/generate_index_mesh.py` (or `.\scripts\generate_index_mesh.ps1`) "
              "and commit the result. Do not reason about merging or manually updating INDEX.md files.")
    lines.append("")
```

- [x] **Step 2: Run the generator locally and inspect an output file**

Run:
```powershell
python scripts/generate_index_mesh.py
```

Open a generated `INDEX.md` (e.g. `scripts/INDEX.md` or `.agents/superpowers/plans/INDEX.md`) and verify the blockquote notice is present.

- [x] **Step 3: Commit**

```bash
git add scripts/generate_index_mesh.py
git commit -m "chore: add generated-file notice to index mesh output"
```

---

### Task 7: Regenerate index mesh and verify

**Files:**
- Modify: all generated `INDEX.md` files (e.g. `scripts/INDEX.md`, `.agents/superpowers/plans/INDEX.md`, etc.)

**Interfaces:**
- None

- [x] **Step 1: Regenerate the index mesh**

Run:
```powershell
python scripts/generate_index_mesh.py
```

This will update every `INDEX.md` in the repo to include the new generated-file notice.

- [x] **Step 2: Verify the check passes**

Run:
```powershell
python scripts/generate_index_mesh.py --check
```
Expected: `INDEX.md files are up to date` or exits 0.

- [x] **Step 3: Commit the generated index files**

First review the generated `INDEX.md` files:
```bash
git status --short
```

Then add all generated `INDEX.md` files and commit:
```bash
git add -A
git commit -m "chore: regenerate index mesh with generated-file notice"
```

---

## Execution Confidence

9/10. The changes are small and isolated. The `ci-preflight.ps1` script reuses existing scripts and commands. The `ci.yml` `if` guard is a standard GitHub Actions pattern. The `generate_index_mesh.py` change is a single string replacement in `render_index`. The main uncertainty is the local Windows environment (PostgreSQL service, Node, .NET, Python) being set up to run the preflight end-to-end. The implementer should verify the script passes locally before marking the PR ready.
