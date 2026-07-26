# Scripts

This folder contains deterministic workflow scripts for the Wild Bunch repo.
All scripts are idempotent and safe to re-run. Inspect this folder before
running ad-hoc commands for dev server management, database setup, or image
processing.

Generic repo-maintenance mechanics (skill sync, index mesh generation,
repo-standards checks) are handled by the bundled marketplace skills under
`.agents/skills/`. These scripts are the operational and repo-specific
extensions that remain in `scripts/`.

## Shared requirements

- `postgres-dev.ps1` requires PowerShell and the repo-local PostgreSQL 16.14
  tooling under `.local/postgresql16`.
- `postgres-dev.sh` is the Linux/bash entrypoint and requires native
  PostgreSQL 16.14 command-line tools on `PATH` or a `POSTGRES_BIN_DIR`
  override.
- `image_asset_pipeline.py` is a wrapper around the asset-local implementation
  under `src/WildBunch.Assets/scripts/` and requires Python 3.11+ with Pillow
  installed in the active environment.

## Scripts

### ci-preflight.sh / ci-preflight.ps1
**Use when** you need to run the same checks CI runs before marking a PR ready for review.

- `bash scripts/ci-preflight.sh` - run all local CI preflight checks
- `bash scripts/ci-preflight.sh --check` - run non-destructive pre-commit checks
- `.\scripts\ci-preflight.ps1` - PowerShell entrypoint (`-Check` for pre-commit)

This is the bundled `repo-standards` `ci-preflight` template. It runs
repo-standards checks, scaffold checks, index mesh generation/validation,
agent mesh validation, skill refresh validation, and then the repo-specific
`ci-preflight-extra` hook for backend and frontend build/test lanes.

**Use before** taking a PR out of draft. The `workflow-policy.md` requires the preflight to pass before marking a PR ready for review.

### ci-preflight-extra.sh / ci-preflight-extra.ps1
Repo-specific extension script called by `ci-preflight` after the generic
marketplace skill checks. It runs `dotnet restore/build/ef/test` and the
`src/WildBunch.Web` `npm ci/typecheck/test/build` pipeline. In `--check` mode
it is a no-op because those lanes are too heavy for the pre-commit hook.

### dev-servers.sh / dev-servers.ps1
**Use when** you need to start, stop, check, or ensure the API + Vite dev
servers are running for local development or integration testing.

- `bash scripts/dev-servers.sh ensure` - start servers if not running, no-op if already up (default)
- `.\scripts\dev-servers.ps1 ensure` - start servers if not running, no-op if already up (default)
- `bash scripts/dev-servers.sh start` - start servers (fails if already running on the same ports)
- `bash scripts/dev-servers.sh stop` - stop servers for this worktree
- `bash scripts/dev-servers.sh status` - print server status without changing state
- `.\scripts\dev-servers.ps1 start` - start servers (fails if already running on the same ports)
- `.\scripts\dev-servers.ps1 stop` - stop servers for this worktree
- `.\scripts\dev-servers.ps1 status` - print server status without changing state

The API runs on port 5275, Vite on port 5173. The script resolves the
worktree root via `git rev-parse` so it works correctly from git worktrees.
The PostgreSQL connection string is pinned to the shared dev instance at
`localhost:5434`.
Startup probes use the dedicated API health endpoint and exponential backoff so
the script retries quickly at first, then backs off to avoid flakey waits.
Each `ensure` run rebuilds the API and web bundle before launch, and the script
will recycle stale or unhealthy recorded state instead of trusting it blindly.

**Use before** running integration tests that need a live API, or before
playtesting the browser game locally. **Use `stop`** when you need to free
locked DLLs for a clean `dotnet build`.

### postgres-dev.sh / postgres-dev.ps1
**Use when** you need to set up, start, stop, reset, or validate the local
PostgreSQL dev database used by integration tests.

- `bash scripts/postgres-dev.sh ensure` - initialize cluster + start + wait ready + ensure database (default)
- `bash scripts/postgres-dev.sh setup` - same as ensure
- `bash scripts/postgres-dev.sh start` - start the cluster if stopped
- `bash scripts/postgres-dev.sh stop` - stop the cluster
- `bash scripts/postgres-dev.sh reset` - drop and recreate the dev database (destructive - confirm before running)
- `bash scripts/postgres-dev.sh status` - print cluster status
- `bash scripts/postgres-dev.sh validate` - run schema validation against the dev database
- `bash scripts/postgres-dev.sh test` - run connection test
- `.\scripts\postgres-dev.ps1 ensure` - PowerShell entrypoint with the same commands

The cluster runs on port 5434 with database `wildbunch_dev`. PostgreSQL
tooling is expected under `.local/postgresql16` for the PowerShell path and as
native Linux binaries on `PATH` for the bash path (or via `POSTGRES_BIN_DIR`).
The script resolves the persistent main checkout root so the data directory is
shared, not duplicated per worktree.

**Use before** running `dotnet test` on `WildBunch.Integration.Tests` - the
integration tests require this database to be running. Set the connection
string environment variable:
`$env:ConnectionStrings__WildBunchPostgresDb = "Host=localhost;Port=5434;Database=wildbunch_dev;Username=postgres"`

### image_asset_pipeline.sh / image_asset_pipeline.ps1
**Use when** you need to cut a generated image away from a flat background,
slice a turnaround sheet into individual views, normalize a building sprite
onto a fixed canvas, scale tile art into the staging canvas, or move tile art
through the tile-safe staging/promotion path.

- `bash scripts/image_asset_pipeline.sh cut-background --input <source.png> --out <cut.png>` - cut one image away from a flat background without resizing it; add `--remove-islands` for a second pass that clears enclosed chroma islands
- `bash scripts/image_asset_pipeline.sh cut-background-tree --input-root <source-root> --out-root <out-root>` - cut every PNG in a tree away from a flat background without resizing it; add `--remove-islands` for the second island pass
- `bash scripts/image_asset_pipeline.sh normalize --input <source.png> --out <normalized.png>`
- `bash scripts/image_asset_pipeline.sh slice-sheet --input <sheet.png> --out-dir <out-dir> --names front,profile,rear,front-oblique,rear-oblique`
- `bash scripts/image_asset_pipeline.sh stage-tiles --input-root <source-root> --out-root <staging-root>` - cut tile art to transparent cutouts while preserving the full tile canvas; add `--remove-islands` for the second island pass
- `bash scripts/image_asset_pipeline.sh promote-tiles --input-root <staging-root> --out-root <sprites-root>` - copy staged tile PNGs into the matching sprites tree without resizing
- `bash scripts/image_asset_pipeline.sh promote-sprites --input-root <pipeline-root> --out-root <sprites-root>` - cut the staged building tree to transparent cutouts in place, then normalize it into the matching final sprites tree, skipping `normalized/` scratch files; add `--remove-islands` for the second island pass
- `.\scripts\image_asset_pipeline.ps1` - PowerShell wrapper that passes all arguments through to the Python script

Both wrappers are thin convenience layers that call the Python script.

The primary backend is Pillow in Python 3.11+ with the package installed in
the active environment. The selection and promotion note lives in
`.agents/docs/asset-pipeline/selection-cut-normalization.md`.

## Extension hooks

The following scripts are not invoked directly; they are extension points for
the bundled marketplace skills.

- `scripts/generate_index_mesh_extra.py` (and `.sh`/`.ps1` wrappers) is called
  by `generating-agent-mesh` after it generates the `INDEX.md` mesh. It appends
  the ADR freshness table to `docs/adr/INDEX.md`.
- `scripts/validate_local_skills_extra.py` (and `.sh`/`.ps1` wrappers) is
  called by `refreshing-installed-skills` while syncing skills. It validates
  the `wild-bunch-*` repo-local skill directories.

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
