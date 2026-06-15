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

```powershell
.\scripts\postgres-dev.ps1 install-tools
```

Initialize or start the persistent local development cluster and database:

```powershell
.\scripts\postgres-dev.ps1 setup
```

Run the PostgreSQL validation lane with the repo-local connection string and
tooling setup already wired in:

```powershell
.\scripts\postgres-dev.ps1 validate
```

The validation command provisions the persistent cluster if needed, exports
`ConnectionStrings__WildBunchPostgresDb` for the child `dotnet` commands,
restores repo-local tools, runs `dotnet ef migrations list`, and then runs
`dotnet test WildBunch.sln`.

For a targeted PostgreSQL-backed test run, use the repo-local script wrapper.
That is the supported path from a fresh checkout:

```powershell
.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "SaloonConfrontationAcceptanceTests"
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

```powershell
.\scripts\postgres-dev.ps1 status
```

## Reset

Reset is explicit and destructive to the persistent local development database
only:

```powershell
.\scripts\postgres-dev.ps1 reset
```

Use reset only when you intend to recreate the persistent local app database.
It does not touch temporary integration-test databases.

## Local Launch Flow

1. `.\scripts\postgres-dev.ps1 install-tools`
2. `.\scripts\postgres-dev.ps1 setup`
3. Launch `WildBunch.Api` via Visual Studio/F5 or:

```powershell
dotnet run --project src/WildBunch.Api --launch-profile http
```

The app should start against `wildbunch_dev` on `localhost:5434` without
manually setting `ConnectionStrings__WildBunchPostgresDb` first.
