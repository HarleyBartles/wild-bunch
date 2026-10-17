# Local PostgreSQL

Wild Bunch uses the shared Windows PostgreSQL cluster at
`Z:\_postgres-cluster`.

## Canonical local service

- Manager: `tools/postgres-dev.ps1`
- Host and port: `localhost:5435`
- App database: `wildbunch_dev`
- Connection string:
  `Host=localhost;Port=5435;Database=wildbunch_dev;Username=postgres`
- Ownership: the service is shared across Wild Bunch worktrees and other local
  repositories; normal worker cleanup must not stop it.

## Service commands

```powershell
.\tools\postgres-dev.ps1 status
.\tools\postgres-dev.ps1 ensure
```

`ensure` initializes or starts the shared cluster when needed and creates
`wildbunch_dev` if it is absent. It is safe to rerun. `stop` and `reset` change
shared state and require explicit lifecycle intent; they are not normal worker
cleanup commands.

## Validation

The canonical runner exports the connection string for its child .NET tests:

```powershell
.\tools\postgres-dev.ps1 ensure
py -3 tools\run.py ci --check
```

For a targeted direct test, set the same connection string in that PowerShell
process before invoking `dotnet test`:

```powershell
.\tools\postgres-dev.ps1 ensure
$env:ConnectionStrings__WildBunchPostgresDb = 'Host=localhost;Port=5435;Database=wildbunch_dev;Username=postgres'
dotnet test tests\WildBunch.Integration.Tests --filter SaloonConfrontationAcceptanceTests
```

The test harness creates and drops temporary test databases. It must not drop
the persistent `wildbunch_dev` app database.

## Local launch

```powershell
.\tools\postgres-dev.ps1 ensure
dotnet run --project src\WildBunch.Api --launch-profile http
```

Leave the shared PostgreSQL service running when the worktree task ends.
