# Wild Bunch Testing Lanes

Wild Bunch keeps tests grouped by behavioral scope, not by storage provider.

## Unit

Unit tests prove one object, rule, aggregate method, domain service, handler, or
small collaborator under controlled construction.

Use unit tests when the subject owns the mechanics being exercised. These tests
do not enter through HTTP and should stay fast and surgical.

## Acceptance

Acceptance tests prove one public product contract.

Acceptance tests use an authenticated test client, an in-memory store by
default, and exactly one public API call as the behavior under test. They may
seed a known aggregate/session state directly or via named scenario seeds, but
the action under test must enter through the real public API endpoint. The test
should assert the public result and the aggregate/session state transition that
the call caused.

Current-state note: the production API does not yet enforce authentication or
authorization middleware. In this repo, "authenticated test client" means the
test host carries a fixed test identity, represented today by a test
`Authorization: Test acceptance-user` header, and is ready for future auth
plumbing, not that the production API is already security-gated.

## Integration

Integration tests prove product-flow composition across multiple API calls.

Use this lane for workflows such as start game -> preview travel -> start
journey -> advance day -> resolve encounter -> read journal. These tests may
also use in-memory storage, but they should stay focused on multi-endpoint
composition rather than a single contract.

## Provider And Storage

Provider/storage tests are exceptional, not the default confidence lane.

Use them when the behavior under test is EF mapping, migrations, SQL
translation, snapshot persistence, concurrency, or provider-specific behavior.
For Wild Bunch persistence work, the active provider lane is PostgreSQL-backed
and should use a dedicated local/test PostgreSQL database with no production or
user data when it needs to exercise `WildBunch.Persistence` against the real
provider. In the current lane, provider/storage tests also prove `jsonb`
payload columns and at least one JSONB operator query against repository-saved
state. The persistent local development app database is a separate concern; see
[Local PostgreSQL](local-postgresql.md) for that convention.
When this lane is intentional, name it clearly so it is obvious that the test is
about provider fidelity rather than ordinary gameplay behavior.

Local app launch is separate from this lane: the committed API launch profile
supplies the repo-local development connection string so F5 and `dotnet run`
work without manually exporting `ConnectionStrings__WildBunchPostgresDb` each
time.

The repo-local PostgreSQL validation lane has a dedicated entrypoint:

```powershell
.\scripts\postgres-dev.ps1 validate
```

That command provisions the local cluster if needed, exports the repo-local
connection string for child `dotnet` commands, restores tools, and runs the EF
and test checks as one repeatable lane.

For issue-specific PostgreSQL-backed acceptance or integration checks, use the
targeted script wrapper:

```powershell
.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "SaloonConfrontationAcceptanceTests"
```

The wrapper starts or reuses the local cluster, sets
`ConnectionStrings__WildBunchPostgresDb` in the same process, and then runs
`dotnet test` with the arguments you pass after `--`.

That wrapper is the supported repo-local PostgreSQL-backed test path. A direct
`dotnet test` is only valid when the caller has already exported
`ConnectionStrings__WildBunchPostgresDb` in the same shell session.

## Repo Placement

- Unit tests live in the domain/application/game-content test projects.
- Acceptance tests live under `tests/WildBunch.Integration.Tests/Acceptance`.
- Workflow integration tests remain under `tests/WildBunch.Integration.Tests`.
- Provider/storage checks remain explicitly named inside the integration test
  project.
