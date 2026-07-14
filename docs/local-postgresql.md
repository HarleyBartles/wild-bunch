# Local PostgreSQL

Wild Bunch runs against PostgreSQL locally. The repo keeps the persistent
development database convention explicit so the app does not depend on an
unspecified machine-global cluster or data directory.

## Convention

- Tooling version: PostgreSQL `16.14`
- Tooling root: `.local/postgresql16`
- Persistent cluster data: `.local/postgres-dev/data/wildbunch-dev`
- Persistent logs: `.local/postgres-dev/logs/wildbunch-dev.log`
- Persistent local app database: `wildbunch_dev`
- Host and port: `localhost:5434`
- Connection-string name: `ConnectionStrings:WildBunchPostgresDb`
- Service ownership: the persistent main checkout (the repo root that owns the
  shared `.git` directory) owns the running cluster, tooling, and data dir.
  Worktrees reuse `localhost:5434` and do not provision their own cluster.

The persistent local development database is the database Wild Bunch uses when
Harley runs the app locally. It should survive app restarts until the explicit
reset command is run.

`WildBunch.Api` applies migrations on startup, so the persistent database stays
in step with the current schema when the app launches.

Temporary integration tests use dedicated local/test PostgreSQL databases with
no production or user data. Those test-created databases are separate from the
persistent local app database and are dropped only by the harness that created
them.

## Setup

Check or document the pinned tooling first:

```bash
bash scripts/postgres-dev.sh install-tools
```

Ensure the shared local PostgreSQL service is running (starts it if down, reuses
it if already healthy):

```bash
bash scripts/postgres-dev.sh ensure
```

`ensure` is the normal worker entry point before PostgreSQL-dependent tests or
local app launch. It is idempotent: if the shared service on `localhost:5434` is
already healthy, it returns immediately without restarting. The service is owned
by the persistent main checkout, so workers in any worktree reuse the same
running cluster and the same `wildbunch_dev` app database.

If you need the older full-provision verb (same effect when the cluster is down),
`setup` remains available:

```bash
bash scripts/postgres-dev.sh setup
```

Run the PostgreSQL validation lane with the repo-local connection string and
tooling setup already wired in:

```bash
bash scripts/postgres-dev.sh validate
```

The validation command provisions the persistent cluster if needed, exports
`ConnectionStrings__WildBunchPostgresDb` for the child `dotnet` commands,
restores repo-local tools, runs `dotnet ef migrations list`, and then runs
`dotnet test WildBunch.sln`.

For a targeted PostgreSQL-backed test run, use the repo-local script wrapper.
That is the supported path from a fresh checkout:

```bash
bash scripts/postgres-dev.sh test -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "SaloonConfrontationAcceptanceTests"
```

That route provisions or starts the persistent cluster, exports
`ConnectionStrings__WildBunchPostgresDb` in the same process, and runs
`dotnet test` with the arguments you pass after `--`.

If you deliberately bypass the script, you must set
`ConnectionStrings__WildBunchPostgresDb` yourself in the same shell before
running `dotnet test`. A plain `dotnet test` without that connection string is
not a supported PostgreSQL-backed test path in this repo.

Then launch Wild Bunch Api through Visual Studio/F5 or `dotnet run`.
The committed launch profile supplies the repo-local connection string for the
`http` and `https` profiles, so you do not need to set it manually for the
normal local launch path.

If you want to confirm the connection string shape explicitly, it is:

```powershell
Host=localhost;Port=5434;Database=wildbunch_dev;Username=postgres
```

## Status

Check whether the local cluster is running and the app database exists:

```bash
bash scripts/postgres-dev.sh status
```

## Shared Service and Worker Cleanup

The local PostgreSQL service is a shared, long-lived developer service owned by
the persistent main checkout. It is not per-run setup/teardown.

- Run `bash scripts/postgres-dev.sh ensure` before PostgreSQL-dependent tests or
  local app launch. It reuses a healthy service and only starts one when down.
- Normal worker cleanup must **not** stop the shared service. A later worker or
  worktree expects to reuse it.
- `stop` and `reset` are manual/destructive. Use them only when you explicitly
  intend to shut down or recreate the shared service, or when Harley asks.
- Worktrees reuse `localhost:5434` and the main checkout's tooling and data dir.
  A worktree does not provision its own cluster. If `ensure` reports missing
  tooling, install PostgreSQL tooling into the main checkout's
  `.local/postgresql16` (see `install-tools`), not the worktree's.

## Reset

Reset is explicit and destructive to the persistent local development database
only:

```bash
bash scripts/postgres-dev.sh reset
```

Use reset only when you intend to recreate the persistent local app database.
It does not touch temporary integration-test databases.

Reset is manual and not part of normal worker cleanup. Do not run `reset` from a
worker lane unless you explicitly intend to recreate the shared service.

## Local Launch Flow

1. `bash scripts/postgres-dev.sh install-tools`
2. `bash scripts/postgres-dev.sh ensure`
3. Launch `WildBunch.Api` via Visual Studio/F5 or:

```powershell
dotnet run --project src/WildBunch.Api --launch-profile http
```

The app should start against `wildbunch_dev` on `localhost:5434` without
manually setting `ConnectionStrings__WildBunchPostgresDb` first.
