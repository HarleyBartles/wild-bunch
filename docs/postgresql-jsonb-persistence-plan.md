# PostgreSQL JSONB Persistence Plan

This page turns issue #31 into a staged persistence migration plan that matches the current Wild Bunch topology instead of replacing it.

## Current Source-Backed Decision

- PostgreSQL is the provider target.
- SQLite is not the end-state architecture goal; it may remain a transition lane while the cutover is being proven, but it is not the preferred persistence shape.
- `Persistence:Provider` still selects `Sqlite` or `PostgreSql` in the current code, but the end-state target is PostgreSQL-native persistence rather than SQLite compatibility.
- `GameSession` remains the command-side aggregate root.
- `IGameSessionRepository` remains the command persistence boundary.
- Read repositories stay query-only.
- Runtime session state remains composed and aggregate-owned rather than normalized into many gameplay tables.

This matches the current source shape:

- `WildBunch.Persistence.DependencyInjection` wires `UseSqlite(...)` directly today.
- `WildBunch.Persistence.DependencyInjection` now routes through a small provider-selection seam in `WildBunch.Persistence`.
- `WildBunch.Api` applies migrations on startup through the persistence service provider.
- The current composed session store uses `GameSessions`, `GameSessionComponents`, `GameSessionLogEntries`, and `GameSessionTravelDiaryDays`.
- Component payloads are already serialized as JSON strings, so PostgreSQL `jsonb` is a natural later fit for cohesive runtime components.

## Current Persistence Topology

The current topology is already close to a PostgreSQL-friendly hybrid store:

- `GameSessions` stores the session envelope: id, timestamps, status, travel difficulty, and schema version.
- `GameSessionComponents` stores composed session state by component name and payload.
- `GameSessionLogEntries` stores ordered append-only log rows.
- `GameSessionTravelDiaryDays` stores ordered diary rows with serialized payloads.
- `EfGameSessionRepository` owns save/load of the aggregate and coordinates component/log/diary persistence.
- `EfGameSessionReadRepository` and `EfGameJournalReadRepository` are read-only projections over the same store.

That shape means the first PostgreSQL version should preserve the same repository and unit-of-work boundaries, then swap provider-specific column types and indexes underneath them.

## Recommended Migration Strategy

1. Treat PostgreSQL as the end-state provider target.
2. Keep the composed session model intact.
3. Prefer native PostgreSQL identity and storage shapes, including `uuid` IDs and FKs where the provider supports them cleanly.
4. Keep append-only log and diary surfaces as ordered relational rows.
5. Use PostgreSQL-native storage for cohesive runtime state where it helps evolution and indexing.
6. Retire SQLite compatibility once the PostgreSQL lane is fully proven and the cutover is accepted.

The migration should be evolutionary, not a domain redesign.

## Target Shape

The intended PostgreSQL shape should stay close to the current composed model:

- `game_sessions`
  - id (`uuid`)
  - created_at_utc (`timestamp with time zone`)
  - updated_at_utc (`timestamp with time zone`)
  - status
  - travel_difficulty
  - schema_version
- `game_session_components`
  - session_id (`uuid`)
  - component_name
  - component_version
  - payload `jsonb`
  - updated_at_utc (`timestamp with time zone`)
- `game_session_log_entries`
  - session_id (`uuid`)
  - sequence
  - kind
  - message
  - day
  - turn
- `game_session_travel_diary_days`
  - session_id (`uuid`)
  - sequence
  - payload `jsonb` or provider-owned JSON equivalent
  - recorded_at_utc (`timestamp with time zone`)

The key design choice is to use relational columns for identity, ordering, filtering, and append streams while letting `jsonb` hold evolving cohesive runtime state.

## Provider Boundary Plan

The provider seam should stay below the application layer.

- `GameSession` remains authoritative for gameplay mutation.
- `IGameSessionRepository` continues to represent the command write boundary.
- Read repositories remain projection-only and do not become mutation gateways.
- Persistence-specific provider selection belongs in `WildBunch.Persistence`, not in domain objects.

If a provider-selection seam is added later, it should be small and configuration-driven. The current codebase does not need speculative multi-provider plumbing to justify the plan.

## Staged Implementation Path

### Stage 1: Provider Readiness

- Add PostgreSQL provider support in persistence configuration.
- Keep the current transition lane green while the PostgreSQL cutover is proven.
- Introduce provider-specific EF configuration only where the provider truly differs.
- Preserve or reset migrations as needed to reach the PostgreSQL-native shape cleanly.
- Use `ConnectionStrings:WildBunchPostgresDb` for the opt-in PostgreSQL lane when a dedicated test or host database is available.

### Stage 2: JSONB Adoption

- Map component payloads to PostgreSQL `jsonb`.
- Keep envelope columns relational.
- Keep log and diary append rows ordered and queryable.
- Verify that hidden state still does not leak into public DTOs or read models.

### Stage 3: Validation and Read Surfaces

- Add PostgreSQL integration tests that exercise round-trip aggregate persistence.
- Confirm read repositories still project query-only models.
- Confirm log ordering, diary ordering, and aggregate rehydration remain stable.
- Keep the current transition lane green until the PostgreSQL cutover is complete.

### Stage 4: Operational Decision

- Decide later whether the SQLite lane is kept temporarily for transition validation or retired once PostgreSQL is the sole provider.
- Treat PostgreSQL as the target provider.
- Do not make PostgreSQL a hard runtime requirement before the transition lane is proven.

## Validation Strategy

Validate each layer separately so false-green results are harder to miss.

- `dotnet tool restore`
- `dotnet build WildBunch.sln`
- `dotnet test WildBunch.sln`
- `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
- PostgreSQL lane smoke tests can be run with `ConnectionStrings__WildBunchPostgresDb` set to a dedicated disposable PostgreSQL test database connection string, for example:

```powershell
$env:ConnectionStrings__WildBunchPostgresDb = "Host=...;Database=...;Username=...;Password=..."
dotnet test WildBunch.sln --filter PostgreSqlPersistenceTests
```

- The current live lane covers aggregate save/load round-trip, composed component rows, ordered log rows, and ordered travel diary rows.
- The current provider-native goal prefers `uuid` IDs and FKs on PostgreSQL instead of SQLite-compatible text GUID compromises.

When PostgreSQL provider work starts, add provider-specific integration coverage for:

- aggregate save/load round-trip
- composed component persistence
- log ordering and pagination
- diary ordering and rehydration
- read-only query behavior
- hidden-state boundary checks

## Hidden-State Guardrails

Do not let the PostgreSQL provider become a shortcut around the aggregate.

- Hidden culprit truth stays internal.
- Randomness internals, salts, rolls, bribe thresholds, and generator internals stay out of public DTOs and read responses.
- Component rows are persistence artifacts, not public mutation APIs.
- Query models should remain projections, not back doors into command state.

## False-Green Checks

- PostgreSQL package references alone are not progress.
- A `jsonb` column without a working provider path is not progress.
- Normalizing runtime state into many gameplay tables is not acceptable.
- Letting command handlers mutate component rows directly around `GameSession` is not acceptable.
- Letting read repositories mutate gameplay state is not acceptable.
- Treating `ConnectionStrings__WildBunchPostgresDb` as required for normal local builds is not acceptable.
- Claiming live PostgreSQL coverage without a real database and a non-skipped lane is not acceptable.
- Treating text GUID columns as the preferred PostgreSQL design is not acceptable once native `uuid` mapping is proven.
- Treating this planning slice as completion of #31 is not acceptable.

## What This Issue Is Now

Issue #31 is now an actionable provider-planning slice:

- it recommends PostgreSQL as the provider target,
- it treats SQLite as a transition lane rather than the end-state design,
- it preserves the `GameSession` aggregate and repository boundary,
- and it gives the later implementation a safe target shape without forcing a risky rewrite today.
