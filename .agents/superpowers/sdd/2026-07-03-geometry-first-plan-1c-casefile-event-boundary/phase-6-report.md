# Phase 6: Persistence Serializer Update — Report

## What I Implemented

Added `CaseFileGenerated` to the `ResolveEventType` switch in the persistence
serializer so the event can be deserialized when replaying an event stream.

**File modified:** `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`

The new case was placed immediately after `WorldGenerated` (line 38), following
the same `nameof(...) => typeof(...)` pattern used for every other domain event
in the switch:

```csharp
nameof(WorldGenerated) => typeof(WorldGenerated),
nameof(CaseFileGenerated) => typeof(CaseFileGenerated),
nameof(StartingTownSelected) => typeof(StartingTownSelected),
```

No custom JSON converter is required for `CaseFileGenerated` / `CaseFileSnapshot`
because the snapshot is composed entirely of primitives, strings, enums (serialized
as strings via `nameof`/`Enum.Parse` in the snapshot's `ToDomain`), and arrays —
all handled by `System.Text.Json` with the existing `EventOptions`
(`JsonSerializerDefaults.Web` + `OutlawGangIdJsonConverter`).

## What I Tested

### Build
- `dotnet build src/WildBunch.Persistence/WildBunch.Persistence.csproj`
  - Result: **Build succeeded. 0 Error(s).** (2 pre-existing warnings in
    `WildBunch.Domain` unrelated to this change.)

### Tests
- `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`
  - Result: **Passed! — Failed: 0, Passed: 516, Skipped: 0, Total: 516**
- `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`
  - Result: **Passed! — Failed: 0, Passed: 204, Skipped: 0, Total: 204**
- `dotnet test tests/WildBunch.Integration.Tests --filter "FullyQualifiedName~DevDifficultyForcedSerializerTests"`
  (serializer round-trip lane that does not require PostgreSQL)
  - Result: **Passed! — Failed: 0, Passed: 2, Skipped: 0, Total: 2**

### Integration test suite (full)
- `dotnet test tests/WildBunch.Integration.Tests`
  - Result: Failed: 141, Passed: 27, Skipped: 2, Total: 170
  - **All 141 failures are pre-existing environment issues**, not caused by this
    change. Every failure throws
    `System.InvalidOperationException : Set ConnectionStrings__WildBunchPostgresDb
    to run the PostgreSQL test lane.` (the PostgreSQL test lane is not configured
    in this environment) with one unrelated
    `ScenarioSeedCatalogTests.CachedScenarioSeedDriftFailuresNameTheFixtureAndShape`
    string-assertion failure. None of these touch `ResolveEventType` or
    `CaseFileGenerated`. The serializer-specific lane above confirms the
    serializer itself works.

The plan's Phase 6 verification criteria only require "Build passes", which is
satisfied. Phase 5 already added the `CaseFileGenerated` event round-trip tests
in `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs`, which are
included in the 516 passing Domain.Tests.

## TDD Evidence

TDD was not required for this phase. The plan's Phase 6 verification is
"Build passes" only; the round-trip behavior tests were added in Phase 5.

## Files Changed

- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`
  (+1 line: added `nameof(CaseFileGenerated) => typeof(CaseFileGenerated),` to
  `ResolveEventType`)
- `.agents/superpowers/sdd/2026-07-03-geometry-first-plan-1c-casefile-event-boundary/phase-6-report.md`
  (this report)

## Self-Review Findings

- **Completeness:** The single required change (add `CaseFileGenerated` to
  `ResolveEventType`) is done and matches the plan's code snippet exactly.
- **Quality:** Placement next to `WorldGenerated` is consistent with the brief's
  guidance ("Follow the same pattern used for `WorldGenerated`") and keeps the
  start-flow events grouped.
- **Discipline:** No extra converters, no speculative changes, no test additions
  beyond what the plan specifies for this phase.
- **Testing:** Build passes; Domain (516) and Application (204) tests pass;
  serializer lane passes. Integration failures are pre-existing PostgreSQL
  environment issues.

## Issues or Concerns

- The working tree contained a pre-existing, unrelated modification to
  `phase-4-report.md` (a commit-SHA edit) and several untracked session brief
  files from earlier phases. I left those untouched and committed only my
  serializer change plus this report.
- Integration tests cannot be fully validated in this environment because the
  PostgreSQL test lane requires `ConnectionStrings__WildBunchPostgresDb`. This
  is an environment limitation, not a code defect.
