# BUNCH-76 Shared Local PostgreSQL Dev Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the local PostgreSQL dev dependency behave like a shared, long-lived developer service owned by the persistent main checkout and reused across workers/worktrees, with an explicit `ensure` entry point and a no-stop cleanup contract.

**Architecture:** The cluster is a Windows `pg_ctl`-managed postgres process on `localhost:5434` whose tooling (`pg_ctl`/`psql`/`pg_isready`) and data dir live under the persistent main checkout's `.local/`. The script resolves the main checkout root from any worktree via `git rev-parse --git-common-dir` (parent of the shared `.git`), with a fallback to `$PSScriptRoot/..` when not in a worktree or git unavailable. Worktrees borrow the main checkout's binaries + data dir + running cluster; they never provision their own. Tests already create per-run GUID-named databases (`wildbunch_{guid}`) on the shared cluster and drop them on dispose, so concurrent workers are isolated without any test-infra change. CI keeps its own `postgres:16` service container and is untouched.

**Tech Stack:** PowerShell 5.1+ (`scripts/postgres-dev.ps1`), PostgreSQL 16.14, EF Core/Npgsql (read-only dependency — no code change), Markdown docs.

**Spike gate (Phase 1, completed in planning):** Route is boring and obvious — one clear script shape, no test-infra redesign, no product/runtime persistence change, test isolation already safe via GUID DBs, CI already isolated via service container. Proceeding to implementation.

---

## File Structure

- Modify: `scripts/postgres-dev.ps1` — add persistent-main-checkout root resolution; add `ensure` command; update `install-tools` instructions to point at the main checkout path.
- Modify: `docs/local-postgresql.md` — document `ensure`, shared-service posture, no-stop cleanup rule, worktree reuse contract.
- Modify: `docs/testing-lanes.md` — reference `ensure` as the pre-PG-test step; restate no-stop cleanup rule.
- Modify: `AGENTS.md` — Validation section references `ensure`; Worker Environment/cleanup section states workers must not stop the shared local PostgreSQL service.
- Modify: `docs/adr/ADR-0004-postgresql-local-development-and-validation-lane.md` — dated addendum recording the shared, persistent-main-checkout-owned provisioning convention change.
- Unchanged: `.github/workflows/ci.yml` (CI keeps its own service container), `src/**`, `tests/**`.

No new files. No `src/` or `tests/` code changes.

---

## Task 1: Persistent main checkout root resolution in `postgres-dev.ps1`

**Files:**
- Modify: `scripts/postgres-dev.ps1:13` (the `$RepoRoot` assignment)

**Why:** Today `$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path` resolves to the directory the script lives in. When a worker runs the script from a worktree, that resolves to the worktree root, so `$BinDir`/`$DataDir`/`$PostgresDevRoot` all point at the worktree's `.local/` — which has no tooling and no cluster. We need them to point at the persistent main checkout's `.local/` so worktrees borrow the shared binaries + data dir + running cluster.

- [ ] **Step 1: Replace the `$RepoRoot` assignment with persistent-root resolution**

Replace this line in `scripts/postgres-dev.ps1` (line 13):

```powershell
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
```

with:

```powershell
function Resolve-PersistentRepoRoot {
    $scriptDir = (Resolve-Path $PSScriptRoot).Path
    $fallbackRoot = (Resolve-Path (Join-Path $scriptDir '..')).Path

    $gitPath = (Get-Command git -ErrorAction SilentlyContinue)
    if ($null -eq $gitPath) {
        return $fallbackRoot
    }

    $commonDir = $null
    try {
        $commonDir = (& git rev-parse --git-common-dir 2>$null)
        if ($LASTEXITCODE -ne 0) { $commonDir = $null }
    }
    catch {
        $commonDir = $null
    }

    if ([string]::IsNullOrWhiteSpace($commonDir)) {
        return $fallbackRoot
    }

    $commonDirFull = (Resolve-Path $commonDir).Path
    $persistentRoot = (Resolve-Path (Join-Path $commonDirFull '..')).Path
    return $persistentRoot
}

$RepoRoot = Resolve-PersistentRepoRoot
```

**Rationale:** In a worktree, `git rev-parse --git-common-dir` returns the main checkout's shared `.git` dir (verified: `C:/WORK/Devin/wild-bunch/.git`), so its parent is the main checkout root. In the main checkout itself, `--git-common-dir` returns `.git` (relative), whose resolved parent is the same main checkout root — so the function is idempotent. The fallback covers: git not on PATH, not a git repo, or any git error — in all those cases we use the script's own parent, preserving current behavior.

- [ ] **Step 2: Verify the script still loads and resolves the root correctly from the main checkout**

Run from the main checkout:

```powershell
.\scripts\postgres-dev.ps1 status
```

Expected: same behavior as before this change — prints `Cluster is running on localhost:5434.` (if running) or `Cluster exists but is not running...` / `Cluster not initialized...`. No parse error, no strict-mode error. The `$RepoRoot` resolution must not throw.

- [ ] **Step 3: Verify root resolution from a worktree points at the main checkout**

Create a temporary probe worktree and run status from it:

```powershell
git worktree add .worktrees/bunch-76-probe -b probe/bunch-76 -q
```

Then run from the worktree (note: the script lives in the worktree's `scripts/` because worktrees share the working tree):

```powershell
.\.worktrees\bunch-76-probe\scripts\postgres-dev.ps1 status
```

Expected: the script resolves `$RepoRoot` to `C:/WORK/Devin/wild-bunch` (the main checkout, where `.local/postgresql16/bin` exists), NOT to the worktree path. The status output should reflect the main checkout's cluster state (running/not running) — proving the worktree is borrowing the main checkout's tooling + data dir. If it instead throws `Missing PostgreSQL binary: ...\.worktrees\bunch-76-probe\.local\postgresql16\bin\...`, the resolution failed.

Clean up the probe:

```powershell
git worktree remove .worktrees/bunch-76-probe -f
git branch -D probe/bunch-76 -q
```

- [ ] **Step 4: Commit**

```powershell
git add scripts/postgres-dev.ps1
git commit -m "BUNCH-76: resolve shared PostgreSQL service root to persistent main checkout"
```

---

## Task 2: Add `ensure` command to `postgres-dev.ps1`

**Files:**
- Modify: `scripts/postgres-dev.ps1:3` (the `ValidateSet`)
- Modify: `scripts/postgres-dev.ps1` (the `switch` block, after the `'start'` case around line 295)

**Why:** Workers need a single documented entry point that means "make the shared service ready and reuse it if it's already healthy." `setup`/`start` already early-return when the cluster is running (`Start-Cluster` at line 176-183), but the contract is implicit. `ensure` makes it explicit and is the name the docs/AGENTS.md will reference.

- [ ] **Step 1: Add `ensure` to the `ValidateSet`**

Change line 3 from:

```powershell
    [ValidateSet('install-tools', 'setup', 'start', 'stop', 'reset', 'status', 'validate', 'test')]
```

to:

```powershell
    [ValidateSet('install-tools', 'ensure', 'setup', 'start', 'stop', 'reset', 'status', 'validate', 'test')]
```

- [ ] **Step 2: Add the `ensure` case to the switch block**

Insert this case immediately after the `'start'` case (after line 295, before the `'stop'` case):

```powershell
    'ensure' {
        Initialize-Cluster
        Start-Cluster
        Wait-ForReady
        Ensure-Database
        Write-Host "Shared local PostgreSQL service is ready on ${HostName}:$Port."
        Write-Host "Service owned by persistent checkout: $RepoRoot"
        Write-Host "Reuse this service from any worktree; do not stop it during normal worker cleanup."
    }
```

**Rationale:** `Initialize-Cluster` is idempotent (only initdb's if `PG_VERSION` missing, only sets config ports/listen if needed). `Start-Cluster` early-returns if `Test-ClusterRunning` is true. `Wait-ForReady` returns immediately if already ready. `Ensure-Database` only creates `wildbunch_dev` if missing. So the whole block is a no-op when the service is already healthy, and a full provision-then-start when it's not. The host messages make the shared-service contract visible at the call site.

- [ ] **Step 3: Verify `ensure` is a no-op when the cluster is already running**

First make sure the cluster is up (run `setup` if needed):

```powershell
.\scripts\postgres-dev.ps1 setup
```

Then run `ensure` twice:

```powershell
.\scripts\postgres-dev.ps1 ensure
.\scripts\postgres-dev.ps1 ensure
```

Expected: both runs print `Shared local PostgreSQL service is ready on localhost:5434.` and `Service owned by persistent checkout: C:/WORK/Devin/wild-bunch`. Neither run should take more than a second or two (no initdb, no full start). No errors.

- [ ] **Step 4: Verify `ensure` starts the cluster when it's down**

Stop the cluster, then ensure brings it back:

```powershell
.\scripts\postgres-dev.ps1 stop
.\scripts\postgres-dev.ps1 ensure
```

Expected: `stop` prints `Persistent local development database stopped.`. `ensure` takes a few seconds (start-up), then prints the ready messages. Confirm with:

```powershell
.\scripts\postgres-dev.ps1 status
```

Expected: `Cluster is running on localhost:5434.` and `Persistent app database 'wildbunch_dev' exists.`

- [ ] **Step 5: Commit**

```powershell
git add scripts/postgres-dev.ps1
git commit -m "BUNCH-76: add ensure command for shared PostgreSQL service reuse"
```

---

## Task 3: Update `install-tools` instructions to point at the main checkout

**Files:**
- Modify: `scripts/postgres-dev.ps1:54-58` (the `Write-ToolingInstructions` function)

**Why:** A worktree worker who runs `install-tools` and sees "PostgreSQL tooling is expected at `.local/postgresql16`" needs to know that means the **main checkout's** `.local/`, not the worktree's. With Task 1's root resolution, `$LocalRoot` already points at the main checkout, so the message just needs to make that explicit.

- [ ] **Step 1: Update `Write-ToolingInstructions` to print the resolved main-checkout path**

Replace the function body at lines 54-58:

```powershell
function Write-ToolingInstructions {
    $toolingPath = Join-Path $RepoRoot '.local\postgresql16'
    Write-Host "PostgreSQL tooling is expected at $toolingPath and pinned to version $PostgreSqlVersion."
    Write-Host "If the binaries are missing, download the Windows installer from $PostgreSqlDownloadPage, install PostgreSQL $PostgreSqlVersion into $toolingPath, and rerun this command."
}
```

with:

```powershell
function Write-ToolingInstructions {
    $toolingPath = Join-Path $RepoRoot '.local\postgresql16'
    Write-Host "PostgreSQL tooling is expected at $toolingPath and pinned to version $PostgreSqlVersion."
    Write-Host "This is the persistent main checkout's tooling root, shared across worktrees."
    Write-Host "If the binaries are missing, download the Windows installer from $PostgreSqlDownloadPage, install PostgreSQL $PostgreSqlVersion into $toolingPath, and rerun this command."
}
```

**Rationale:** `$RepoRoot` is now the persistent main checkout root (from Task 1), so `$toolingPath` already resolves correctly. The added line makes the shared-tooling contract explicit so a worktree worker knows to install into the main checkout, not their worktree.

- [ ] **Step 2: Verify the updated message renders**

Run:

```powershell
.\scripts\postgres-dev.ps1 install-tools
```

Expected (assuming tooling is present and pinned at 16.14): `PostgreSQL tooling is already pinned at version 16.14 in <main-checkout>\.local\postgresql16.` — no change to that success path. To see the instructions message, temporarily rename `.local/postgresql16` to `.local/postgresql16-bak`, run `install-tools`, confirm the new message prints with `This is the persistent main checkout's tooling root, shared across worktrees.`, then rename it back. (If you do this temp-rename verification, ensure you restore the directory before committing.)

- [ ] **Step 3: Commit**

```powershell
git add scripts/postgres-dev.ps1
git commit -m "BUNCH-76: clarify install-tools points at shared main-checkout tooling root"
```

---

## Task 4: Update `docs/local-postgresql.md`

**Files:**
- Modify: `docs/local-postgresql.md` (Convention section, Setup section, add Shared Service section, Reset section)

**Why:** This is the primary doc workers read for the PostgreSQL convention. It must document `ensure`, the shared-service posture, the worktree reuse contract, and the no-stop cleanup rule.

- [ ] **Step 1: Update the Convention section to state shared ownership**

In `docs/local-postgresql.md`, after the existing `Connection-string name` bullet (line 15) and before the paragraph starting "The persistent local development database is the database Wild Bunch uses" (line 17), insert:

```markdown
- Service ownership: the persistent main checkout (the repo root that owns the
  shared `.git` directory) owns the running cluster, tooling, and data dir.
  Worktrees reuse `localhost:5434` and do not provision their own cluster.
```

- [ ] **Step 2: Add `ensure` as the primary Setup entry point**

In the Setup section, replace the existing "Initialize or start the persistent local development cluster and database:" block (lines 37-41) with:

```markdown
Ensure the shared local PostgreSQL service is running (starts it if down, reuses
it if already healthy):

```powershell
.\scripts\postgres-dev.ps1 ensure
```

`ensure` is the normal worker entry point before PostgreSQL-dependent tests or
local app launch. It is idempotent: if the shared service on `localhost:5434` is
already healthy, it returns immediately without restarting. The service is owned
by the persistent main checkout, so workers in any worktree reuse the same
running cluster and the same `wildbunch_dev` app database.

If you need the older full-provision verb (same effect when the cluster is down),
`setup` remains available:

```powershell
.\scripts\postgres-dev.ps1 setup
```
```

- [ ] **Step 3: Add a Shared Service and Worker Cleanup section**

Insert a new section after the Status section (after line 88) and before the Reset section:

```markdown
## Shared Service and Worker Cleanup

The local PostgreSQL service is a shared, long-lived developer service owned by
the persistent main checkout. It is not per-run setup/teardown.

- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests or
  local app launch. It reuses a healthy service and only starts one when down.
- Normal worker cleanup must **not** stop the shared service. A later worker or
  worktree expects to reuse it.
- `stop` and `reset` are manual/destructive. Use them only when you explicitly
  intend to shut down or recreate the shared service, or when Harley asks.
- Worktrees reuse `localhost:5434` and the main checkout's tooling and data dir.
  A worktree does not provision its own cluster. If `ensure` reports missing
  tooling, install PostgreSQL tooling into the main checkout's
  `.local/postgresql16` (see `install-tools`), not the worktree's.
```

- [ ] **Step 4: Update the Reset section to reinforce manual-only**

In the Reset section (lines 90-100), after the existing "Reset is explicit and destructive" sentence, add:

```markdown
Reset is manual and not part of normal worker cleanup. Do not run `reset` from a
worker lane unless you explicitly intend to recreate the shared service.
```

- [ ] **Step 5: Update the Local Launch Flow to use `ensure`**

In the Local Launch Flow section (lines 102-113), replace step 2:

```markdown
2. `.\scripts\postgres-dev.ps1 ensure`
```

- [ ] **Step 6: Verify the doc renders coherently**

Read the full `docs/local-postgresql.md` end-to-end and confirm: `ensure` is the documented primary entry point; the shared-service/no-stop rule is clear; worktree reuse is stated; `stop`/`reset` are marked manual/destructive; no internal contradictions with the Convention section.

- [ ] **Step 7: Commit**

```powershell
git add docs/local-postgresql.md
git commit -m "BUNCH-76: document shared PostgreSQL service posture and ensure command"
```

---

## Task 5: Update `docs/testing-lanes.md`

**Files:**
- Modify: `docs/testing-lanes.md:60-83` (the Provider And Storage section's PG lane instructions)

**Why:** The PG-backed test instructions currently reference `validate`/`test` but not `ensure`, and don't state the no-stop cleanup rule. Workers need to know `ensure` is the pre-step and that they must not stop the shared service.

- [ ] **Step 1: Add `ensure` as the pre-step and the no-stop rule**

In `docs/testing-lanes.md`, replace the paragraph starting "The repo-local PostgreSQL validation lane has a dedicated entrypoint:" (lines 60-64) and its code block with:

```markdown
Before any PostgreSQL-backed test lane, ensure the shared local service is up:

```powershell
.\scripts\postgres-dev.ps1 ensure
```

`ensure` reuses a healthy service and only starts one when down. The service is
shared across workers and worktrees; do not stop it during normal worker cleanup.

The repo-local PostgreSQL validation lane then reuses that shared service and
exports the repo-local connection string for the child `dotnet` commands:

```powershell
.\scripts\postgres-dev.ps1 validate
```
```

- [ ] **Step 2: Update the targeted-test-wrapper paragraph to reference `ensure`**

Replace the paragraph starting "For issue-specific PostgreSQL-backed acceptance or integration checks" (lines 70-75) opening sentence with:

```markdown
For issue-specific PostgreSQL-backed acceptance or integration checks, ensure the
shared service first (`.\scripts\postgres-dev.ps1 ensure`), then use the targeted
script wrapper:
```

Keep the existing code block and the rest of that paragraph unchanged.

- [ ] **Step 3: Add the no-stop rule to the closing paragraph**

Replace the closing paragraph (lines 81-83, starting "That wrapper is the supported repo-local PostgreSQL-backed test path.") with:

```markdown
That wrapper is the supported repo-local PostgreSQL-backed test path. A direct
`dotnet test` is only valid when the caller has already exported
`ConnectionStrings__WildBunchPostgresDb` in the same shell session.

Normal worker cleanup must not stop the shared local PostgreSQL service. `stop`
and `reset` are manual/destructive and only for explicit service lifecycle
ownership. See [Local PostgreSQL](local-postgresql.md) for the full shared-service
convention.
```

- [ ] **Step 4: Verify the doc renders coherently**

Read the full Provider And Storage section end-to-end. Confirm: `ensure` is the documented pre-step; `validate`/`test` reuse the shared service; the no-stop cleanup rule is stated; the cross-link to `local-postgresql.md` works.

- [ ] **Step 5: Commit**

```powershell
git add docs/testing-lanes.md
git commit -m "BUNCH-76: reference ensure and no-stop cleanup rule in testing lanes"
```

---

## Task 6: Update `AGENTS.md`

**Files:**
- Modify: `AGENTS.md` (Validation section, Worker Environment section)

**Why:** AGENTS.md is the authoritative worker doctrine. The Validation section must reference `ensure` as the pre-PG-test step, and the Worker Environment section must explicitly state the no-stop cleanup rule so workers don't treat the shared service as per-run teardown.

- [ ] **Step 1: Update the Validation section to reference `ensure`**

In `AGENTS.md`, find the Validation section bullet that starts with "Run `.\scripts\postgres-dev.ps1 validate`". Insert a new bullet immediately before it:

```markdown
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests or validation to reuse the shared local service (idempotent: no-op when already healthy).
```

- [ ] **Step 2: Add the no-stop cleanup rule to the Worker Environment section**

In `AGENTS.md`, in the Worker Environment section, find the bullet starting "When you start worker-owned API servers". Insert a new bullet immediately before it:

```markdown
- The local PostgreSQL dev service (`localhost:5434`) is a shared, long-lived developer service owned by the persistent main checkout. Do not stop it during normal worker cleanup. `.\scripts\postgres-dev.ps1 stop` and `reset` are manual/destructive and only for explicit service lifecycle ownership or when Harley asks. Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent tests; it reuses a healthy service and only starts one when down.
```

- [ ] **Step 3: Verify AGENTS.md is internally consistent**

Read the Validation and Worker Environment sections. Confirm: `ensure` is the documented pre-PG-test step; the no-stop rule is explicit; `stop`/`reset` are marked manual/destructive; nothing contradicts the `docs/local-postgresql.md` posture.

- [ ] **Step 4: Commit**

```powershell
git add AGENTS.md
git commit -m "BUNCH-76: add ensure and no-stop cleanup rule to worker doctrine"
```

---

## Task 7: ADR-0004 dated addendum

**Files:**
- Modify: `docs/adr/ADR-0004-postgresql-local-development-and-validation-lane.md` (Dated Status History, Implementation Status)

**Why:** ADR-0004's own Review Triggers say "When the local database port, database name, or provisioning convention changes." The provisioning convention is changing from per-checkout to shared-persistent-main-checkout-owned. The ADR must record this.

- [ ] **Step 1: Add a dated status history entry**

In `docs/adr/ADR-0004-postgresql-local-development-and-validation-lane.md`, in the Dated Status History section (after line 10), add:

```markdown
- 2026-06-22 - live: the local PostgreSQL service is now a shared, long-lived
  developer service owned by the persistent main checkout and reused across
  workers/worktrees. `scripts/postgres-dev.ps1 ensure` is the documented
  idempotent entry point; normal worker cleanup must not stop the shared
  service. See BUNCH-76.
```

- [ ] **Step 2: Update the Implementation Status section**

Replace the Implementation Status section (lines 92-95) with:

```markdown
## Implementation Status or Plan

Live. The local PostgreSQL docs and testing-lane docs describe the convention and
the validation posture. As of BUNCH-76 (2026-06-22), the service is shared and
owned by the persistent main checkout: `scripts/postgres-dev.ps1 ensure`
idempotently reuses a healthy service or starts one when down, worktrees borrow
the main checkout's tooling and data dir via `git rev-parse --git-common-dir`
resolution, and normal worker cleanup must not stop the shared service.
```

- [ ] **Step 3: Commit**

```powershell
git add docs/adr/ADR-0004-postgresql-local-development-and-validation-lane.md
git commit -m "BUNCH-76: record shared persistent-checkout provisioning convention in ADR-0004"
```

---

## Task 8: Full validation and cross-worktree proof

**Files:** none (verification only)

**Why:** GREEN requires validation evidence and, per AGENTS.md, post-cleanup proof when validation touched `C:/WORK/**`. This task runs the full validation lane and proves the cross-worktree reuse contract, then records cleanup posture.

- [ ] **Step 1: Confirm the shared service is up via `ensure`**

```powershell
.\scripts\postgres-dev.ps1 ensure
```

Expected: `Shared local PostgreSQL service is ready on localhost:5434.` + `Service owned by persistent checkout: C:/WORK/Devin/wild-bunch`.

- [ ] **Step 2: Run the full validation lane**

```powershell
.\scripts\postgres-dev.ps1 validate
```

Expected: `dotnet tool restore` succeeds, `dotnet ef migrations list` succeeds (lists current migrations), `dotnet test WildBunch.sln` passes. The PG-backed tests create GUID-named databases on the shared cluster and drop them on dispose — no shared-service corruption.

- [ ] **Step 3: Cross-worktree reuse proof**

Create a temporary worktree and run `ensure` + `status` from it:

```powershell
git worktree add .worktrees/bunch-76-proof -b probe/bunch-76-proof -q
.\.worktrees\bunch-76-proof\scripts\postgres-dev.ps1 ensure
.\.worktrees\bunch-76-proof\scripts\postgres-dev.ps1 status
```

Expected: `ensure` prints `Service owned by persistent checkout: C:/WORK/Devin/wild-bunch` (NOT the worktree path). `status` reports the cluster running on `localhost:5434`. Confirm the worktree did NOT create its own `.local/`:

```powershell
Test-Path .\.worktrees\bunch-76-proof\.local
```

Expected: `False` (the worktree borrows the main checkout's `.local/`, never provisions its own).

Clean up the proof worktree:

```powershell
git worktree remove .worktrees\bunch-76-proof -f
git branch -D probe/bunch-76-proof -q
```

- [ ] **Step 4: Confirm CI workflow is unchanged**

```powershell
git diff main -- .github/workflows/ci.yml
```

Expected: no diff (CI keeps its own `postgres:16` service container; this slice does not touch CI).

- [ ] **Step 5: Confirm clean worktree**

```powershell
git status
```

Expected: clean working tree (all changes committed). No stray `.worktrees/` entries (they're gitignored).

- [ ] **Step 6: Cleanup proof**

The shared PostgreSQL service on `localhost:5434` is intentionally left running — this is the new contract (shared, long-lived, not stopped during normal cleanup). State this explicitly in the return. No worker-owned API servers, Vite dev servers, browsers, or watch processes were started in this slice. No ports beyond 5434 (the shared PG service) were used. The probe worktrees were removed.

- [ ] **Step 7: Final commit (if any doc fixes surfaced during validation)**

If validation surfaced any doc fix, commit it. Otherwise no commit — all work is already committed in Tasks 1-7.

---

## Self-Review

**Spec coverage (BUNCH-76 acceptance criteria):**
- "documented command workers can run to ensure local PostgreSQL is up without restarting it when already healthy" → Task 2 (`ensure` command) + Task 4 (docs).
- "shared local PostgreSQL service is documented as long-lived across workers and worktrees" → Task 4 (Shared Service section) + Task 7 (ADR addendum).
- "normal worker cleanup instructions explicitly say not to stop shared PostgreSQL" → Task 4 (Shared Service section) + Task 5 (testing-lanes) + Task 6 (AGENTS.md).
- "manual stop/reset commands remain available and clearly marked manual/destructive" → Task 4 (Reset section update) + Task 6 (AGENTS.md).
- "backend/integration test instructions use the new/shared-service posture" → Task 5 (testing-lanes).
- "CI remains isolated and still uses its own PostgreSQL service container" → Task 8 Step 4 (confirms unchanged) + plan states CI untouched throughout.
- "validation includes the relevant backend test commands and any script status/ensure checks" → Task 8.

**Spike questions answered (in plan preamble + ADR):** what starts/stops PG (script), which tests need PG (integration/provider-storage), connection string/port/db (`localhost:5434`/`wildbunch_dev`), multi-checkout reuse (yes via shared main-checkout cluster + GUID test DBs), test isolation (GUID DBs, no per-lane redesign needed), worker command (`ensure`), manual-only stop/reset (preserved).

**Placeholder scan:** No TBD/TODO/"add appropriate" placeholders. Every step has exact code or exact commands with expected output.

**Type/name consistency:** `ensure` used consistently across script, docs, AGENTS.md, ADR. `$RepoRoot` / `Resolve-PersistentRepoRoot` consistent. `localhost:5434` / `wildbunch_dev` / `ConnectionStrings__WildBunchPostgresDb` consistent with existing source. ADR-0004 path matches existing file.

**Non-goals respected:** No runtime persistence architecture change, no game-session table normalization, no CI host-service dependency, no Docker Compose, no stopping existing dev service in normal cleanup, no Devin Playbooks/`!` labels.
