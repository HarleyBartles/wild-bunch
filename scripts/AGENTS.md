# scripts/ AGENTS.md

This folder contains deterministic workflow scripts for the Wild Bunch repo.
All scripts are idempotent and safe to re-run. Inspect this folder before
running ad-hoc commands for dev server management, database setup, skill
syncing, image processing, or index mesh generation - the scripts here are
the canonical way to perform these operations.

Treat the scripts folder as a first-class discovery surface. If there is a
repo-local script for the task, use it before claiming the environment lacks
the needed tooling. That includes PostgreSQL setup/validation via
`postgres-dev.ps1` and image cutting/normalization via
`image_asset_pipeline.py`.

## Shared requirements

- `postgres-dev.ps1` requires PowerShell and the repo-local PostgreSQL 16.14
  tooling under `.local/postgresql16`.
- `image_asset_pipeline.py` requires Python 3.11+ with Pillow installed in the
  active environment.
- Optional image pipeline fallback: Node.js 20+ with `sharp`, then `jimp` only
  if `sharp` cannot be used.

## Scripts

### dev-servers.ps1
**Use when** you need to start, stop, check, or ensure the API + Vite dev
servers are running for local development or integration testing.

- `.\scripts\dev-servers.ps1 ensure` - start servers if not running, no-op if already up (default)
- `.\scripts\dev-servers.ps1 start` - start servers (fails if already running on the same ports)
- `.\scripts\dev-servers.ps1 stop` - stop servers for this worktree
- `.\scripts\dev-servers.ps1 status` - print server status without changing state

The API runs on port 5275, Vite on port 5173. The script resolves the
worktree root via `git rev-parse` so it works correctly from git worktrees.
The PostgreSQL connection string is pinned to the shared dev instance at
`localhost:5434`.

**Use before** running integration tests that need a live API, or before
playtesting the browser game locally. **Use `stop`** when you need to free
locked DLLs for a clean `dotnet build`.

### postgres-dev.ps1
**Use when** you need to set up, start, stop, reset, or validate the local
PostgreSQL dev database used by integration tests.

- `.\scripts\postgres-dev.ps1 ensure` - initialize cluster + start + wait ready + ensure database (default)
- `.\scripts\postgres-dev.ps1 setup` - same as ensure
- `.\scripts\postgres-dev.ps1 start` - start the cluster if stopped
- `.\scripts\postgres-dev.ps1 stop` - stop the cluster
- `.\scripts\postgres-dev.ps1 reset` - drop and recreate the dev database (destructive - confirm before running)
- `.\scripts\postgres-dev.ps1 status` - print cluster status
- `.\scripts\postgres-dev.ps1 validate` - run schema validation against the dev database
- `.\scripts\postgres-dev.ps1 test` - run connection test

The cluster runs on port 5434 with database `wildbunch_dev`. PostgreSQL
tooling is expected under `.local/postgresql16` (shared across worktrees).
The script resolves the persistent main checkout root so the data directory
is shared, not duplicated per worktree.

**Use before** running `dotnet test` on `WildBunch.Integration.Tests` - the
integration tests require this database to be running. Set the connection
string environment variable:
`$env:ConnectionStrings__WildBunchPostgresDb = "Host=localhost;Port=5434;Database=wildbunch_dev;Username=postgres"`

### sync-skills.ps1
**Use when** you have updated the agent-asset-marketplace submodule and need
to sync vendored skills into `.agents/skills/`.

- `.\scripts\sync-skills.ps1` - sync if provenance SHA changed (no-op if already synced)
- `.\scripts\sync-skills.ps1 -Force` - re-copy all skill directories regardless of provenance

Reads `.agents/plugins/marketplace.json` and copies skill folders from the
submodule into `.agents/skills/`. Provenance is tracked in
`.agents/skills/.provenance.json`.

**Use after** `git submodule update --remote .agents/plugins/marketplace-source`
to pull in upstream skill updates.

### image_asset_pipeline.py
**Use when** you need to cut a generated image away from a flat background,
slice a turnaround sheet into individual views, and normalize the result
onto a fixed canvas.

- `python scripts/image_asset_pipeline.py normalize --input <source.png> --out <normalized.png>`
- `python scripts/image_asset_pipeline.py slice-sheet --input <sheet.png> --out-dir <out-dir> --names front,profile,rear,front-oblique,rear-oblique`
- `python scripts/image_asset_pipeline.py promote-sprites --input-root <pipeline-root> --out-root <sprites-root>` - cut the staged pipeline tree to transparent cutouts in place, then normalize and mirror it into the matching final sprites tree, skipping `normalized/` scratch files

The primary backend is Pillow in Python 3.11+ with the package installed in
the active environment. The selection and promotion note lives in
`.agents/art/asset-pipeline/`.

### generate_index_mesh.py
**Use when** you have added, removed, or renamed files or directories and
need to regenerate the repo-wide `INDEX.md` mesh.

- `python scripts/generate_index_mesh.py` - regenerate all INDEX.md files
- `python scripts/generate_index_mesh.py --validate` - validate without writing (CI mode)

Generates `INDEX.md` files in every directory (excluding build artifacts,
node_modules, etc.) with links to child directories and files. Also checks
ADR freshness in `docs/adr/`.

**Use after** creating new source files, test files, or directories to keep
the index mesh current. The INDEX.md files are tracked in git.

## Conventions

- All PowerShell scripts use `Set-StrictMode -Version Latest` and
  `$ErrorActionPreference = 'Stop'` - they fail fast on errors.
- Scripts that need the repo root resolve it via `git rev-parse` so they
  work from git worktrees, not just the main checkout.
- The PostgreSQL tooling and data directory are shared across worktrees via
  the persistent main checkout root - do not stop the PostgreSQL cluster
  during normal worker cleanup.
- Dev servers are worktree-scoped (each worktree gets its own ports if the
  canonical ports are taken) but the PostgreSQL cluster is shared.
