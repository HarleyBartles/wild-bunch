# scripts/ AGENTS.md

This folder contains deterministic workflow scripts for the Wild Bunch repo.
All scripts are idempotent and safe to re-run. Inspect this folder before
running ad-hoc commands for dev server management, database setup, skill
syncing, image processing, or index mesh generation - the scripts here are
the canonical way to perform these operations.

Treat the scripts folder as a first-class discovery surface. If there is a
repo-local script for the task, use it before claiming the environment lacks
the needed tooling. That includes PostgreSQL setup/validation via
`postgres-dev.ps1` and image cutting/normalization via the compatibility
wrapper for `src/WildBunch.Assets/scripts/image_asset_pipeline.py`.

## Shared requirements

- `postgres-dev.ps1` requires PowerShell and the repo-local PostgreSQL 16.14
  tooling under `.local/postgresql16`.
- `image_asset_pipeline.py` is a wrapper around the asset-local implementation
  under `src/WildBunch.Assets/scripts/` and requires Python 3.11+ with Pillow
  installed in the active environment.

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

### install_agent_skills.py/.ps1
**Use when** you have updated the agent-asset-marketplace submodule and need
to sync vendored skills into `.agents/skills/`.

- `python scripts/install_agent_skills.py` - sync if provenance SHA changed (no-op if already synced)
- `python scripts/install_agent_skills.py --force` - re-copy all skill directories regardless of provenance
- `python scripts/install_agent_skills.py --check` - check mode: report what would change without making changes (CI validation)
- `.\scripts\install_agent_skills.ps1` - PowerShell wrapper (same flags: -Check, -Force)

The Python implementation provides content comparison to reduce file churn and supports --check mode for CI validation. The PowerShell wrapper is a thin convenience layer that calls the Python script.

Reads `.agents/plugins/marketplace.json` and copies skill folders from the
submodule into `.agents/skills/`. Provenance is tracked in
`.agents/skills/.provenance.json`.

**Use after** `git submodule update --remote .agents/plugins/marketplace-source`
to pull in upstream skill updates.

### image_asset_pipeline.py
**Use when** you need to cut a generated image away from a flat background,
slice a turnaround sheet into individual views, normalize a building sprite
onto a fixed canvas, scale tile art into the staging canvas, or move tile art
through the tile-safe staging/promotion path.

- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py cut-background --input <source.png> --out <cut.png>` - cut one image away from a flat background without resizing it; add `--remove-islands` for a second pass that clears enclosed chroma islands
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py cut-background-tree --input-root <source-root> --out-root <out-root>` - cut every PNG in a tree away from a flat background without resizing it; add `--remove-islands` for the second island pass
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py normalize --input <source.png> --out <normalized.png>`
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py slice-sheet --input <sheet.png> --out-dir <out-dir> --names front,profile,rear,front-oblique,rear-oblique`
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py tiles --input-root <source-root> --staging-root <staging-root> --production-root <production-root>` - run the tile pipeline sequentially from source to staging to production
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py tiles --input-root <source-root> --staging-root <staging-root> --production-root <production-root> --stage-only` - stop after the staging step
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py tiles --staging-root <staging-root> --production-root <production-root> --promote-only` - promote existing staged tiles into production without restaging
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py stage-tiles --input-root <source-root> --out-root <staging-root>` - phase-only helper that scales full-size tile masters down to the staging canvas without trimming or recentering
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py promote-tiles --input-root <staging-root> --out-root <production-root>` - phase-only helper that copies staged tile PNGs into the matching production tree without resizing
- `python src/WildBunch.Assets/scripts/image_asset_pipeline.py promote-sprites --input-root <pipeline-root> --out-root <production-root>` - cut the staged building tree to transparent cutouts in place, then normalize it into the matching final production tree, skipping `normalized/` scratch files; add `--remove-islands` for the second island pass

The primary backend is Pillow in Python 3.11+ with the package installed in
the active environment. The selection and promotion note lives in
`.agents/docs/asset-pipeline/selection-cut-normalization.md`.

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
