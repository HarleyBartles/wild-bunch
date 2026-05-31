# PostgreSQL JSONB Persistence Plan

This page turns issue #31 into a staged persistence migration plan that matches the current Wild Bunch topology instead of replacing it.

## Current Source-Backed Decision

- SQLite remains the local and development default.
- PostgreSQL should be added as an additional provider path first, not used to replace SQLite immediately.
- `Persistence:Provider` now selects `Sqlite` or `PostgreSql`, while the default remains SQLite.
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

1. Keep SQLite as the default local/dev provider.
2. Add PostgreSQL as a parallel provider path behind the same persistence boundaries.
3. Keep the composed session model intact.
4. Move cohesive component payloads to PostgreSQL `jsonb` where it materially helps evolution and indexing.
5. Keep append-only log and diary surfaces as ordered relational rows.
6. Add PostgreSQL-specific integration coverage before any attempt to retire SQLite.

The migration should be evolutionary, not a domain redesign.

## Target Shape

The intended PostgreSQL shape should stay close to the current composed model:

- `game_sessions`
  - id
  - created_at_utc
  - updated_at_utc
  - status
  - travel_difficulty
  - schema_version
- `game_session_components`
  - session_id
  - component_name
  - component_version
  - payload `jsonb`
  - updated_at_utc
- `game_session_log_entries`
  - session_id
  - sequence
  - kind
  - message
  - day
  - turn
- `game_session_travel_diary_days`
  - session_id
  - sequence
  - payload `jsonb` or provider-owned JSON equivalent
  - recorded_at_utc

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
- Keep the SQLite path intact.
- Introduce provider-specific EF configuration only where the provider truly differs.
- Preserve the current migration lineage and schema versioning.
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
- Confirm SQLite continues to pass the same behavioral tests.

### Stage 4: Operational Decision

- Decide later whether SQLite remains the default forever or becomes a dev-only fallback.
- Retire SQLite only if Harley explicitly chooses that path.
- Do not make PostgreSQL a hard runtime requirement before that decision.

## Validation Strategy

Validate each layer separately so false-green results are harder to miss.

- `dotnet tool restore`
- `dotnet build WildBunch.sln`
- `dotnet test WildBunch.sln`
- `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
- PostgreSQL lane smoke tests can be run by setting `ConnectionStrings__WildBunchPostgresDb` to a dedicated test database connection string.

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
- A provider switch that breaks SQLite development is not acceptable.
- Normalizing runtime state into many gameplay tables is not acceptable.
- Letting command handlers mutate component rows directly around `GameSession` is not acceptable.
- Letting read repositories mutate gameplay state is not acceptable.
- Treating `ConnectionStrings__WildBunchPostgresDb` as required for normal local builds is not acceptable.
- Treating this planning slice as completion of #31 is not acceptable.

## What This Issue Is Now

Issue #31 is now an actionable provider-planning slice:

- it recommends PostgreSQL as the additional provider path,
- it keeps SQLite as the current local/dev baseline,
- it preserves the `GameSession` aggregate and repository boundary,
- and it gives the later implementation a safe target shape without forcing a risky rewrite today.
