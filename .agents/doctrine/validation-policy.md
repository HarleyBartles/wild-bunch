# Validation doctrine

Use the portable testing and verification skills for general test design and
completion workflow. This file records Wild Bunch lanes and exceptions.

## Canonical gate

```powershell
.\tools\postgres-dev.ps1 ensure
py -3 tools\run.py ci --check
```

The runner checks repository standards, skill projections and scripts, the
generated mesh, .NET build and tests, frontend install/typecheck/tests/build,
and diff hygiene. Use `--diagnostics` only to collect multiple independent
failures. A normal hooked commit checks the exact staged snapshot.

For persistence changes, also run:

```powershell
dotnet tool restore
dotnet ef migrations list --project src\WildBunch.Persistence --startup-project src\WildBunch.Api
```

The shared local PostgreSQL service is `localhost:5435`; leave it running.
Direct PostgreSQL-backed test commands need
`ConnectionStrings__WildBunchPostgresDb=Host=localhost;Port=5435;Database=wildbunch_dev;Username=postgres`
in the same process. A refused connection on 5435 is environment evidence;
check `postgres-dev.ps1 status|ensure` before judging it as a product failure.

## Repository test lanes

- `WildBunch.Domain.Tests` and `WildBunch.Application.Tests`: isolated domain
  and application behavior.
- `WildBunch.Api.Tests`: API-specific contracts and configuration.
- `WildBunch.GameContent.Tests`: deterministic seed codec, setup pipeline, and
  distribution guardrails.
- `WildBunch.Integration.Tests`: full HTTP and persistence flows against real
  PostgreSQL; user-facing flows live under `Acceptance/`.
- `src/WildBunch.Web`: Vitest component/route behavior, TypeScript checking,
  and production build.

Tests follow the source-layer namespace and folder. Repository fixtures and
builders live under `TestInfrastructure/`; fakes and stubs live under
`TestDoubles/`. Seeded generator tests use explicit deterministic seeds rather
than sampling until a desired outcome appears.

Tests rendering `RouterProvider` use `createAppRouter()`, never the shared
router singleton, because TanStack Router retains state between tests. Async
lazy-route assertions use an appropriate explicit wait.

## Generated mesh failures

When a routed file moves, regenerate the whole mesh with
`py -3 tools\run.py ci --apply`; do not hand-edit an index or regenerate only a
subtree. `TestResults/`, `node_modules/`, and other ignored outputs must remain
excluded from the mesh.
