# Repository scripts

Use the [command bus guide](../tools/README.md) for supported build, test, and repository-validation work. This folder contains standalone repository operations that have a concrete reason to remain outside the bus: native process lifecycle management and tests for tracked hooks or standalone scripts.

## Development servers

Use `bash scripts/dev-servers.sh <ensure|start|stop|status>` on Linux or `.\scripts\dev-servers.ps1 <ensure|start|stop|status>` on Windows to manage this worktree's API and Vite processes. These are separate implementations because Bash and PowerShell use different process discovery, launch, health-check, and termination mechanisms.

The API uses port 5275 and Vite uses port 5173 when available. Each script resolves the current worktree, chooses fallback ports when another worktree owns the canonical ports, and records process state under `.local/dev-servers/`. `ensure` rebuilds before launch and reuses only healthy processes recorded for the worktree.

Use `stop` only for this worktree's servers. Stop them before a clean .NET build if locked assemblies prevent it.

## Shared PostgreSQL

The Windows shared service is managed by [`tools/postgres-dev.ps1`](../tools/postgres-dev.ps1). Run `.\tools\postgres-dev.ps1 ensure` before PostgreSQL-backed validation and `.\tools\postgres-dev.ps1 status` for a read-only service check. The cluster listens on `localhost:5435`; leave it running during normal worker cleanup. See [local PostgreSQL](../docs/local-postgresql.md) for the human setup guide.

## Test ownership

`scripts/tests/` holds behavior tests for tracked hooks and standalone scripts. `tools/tests/` holds command-bus and repository-checker behavior tests. Both are run by `py -3 tools/run.py python-tests --check`; application tests stay in their .NET projects and the web project.

## Conventions

Keep native PowerShell and Bash implementations only when platform behavior requires them; do not add thin language wrappers around portable Python implementations. All PowerShell scripts use strict mode and stop on errors. Scripts that need the repository root resolve it through Git so linked worktrees work correctly.
