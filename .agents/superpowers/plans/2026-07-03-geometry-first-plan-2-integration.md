# Geometry-First Map Generation - Plan 2: Wire & Integration

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire `MapGenerator.Generate` into the game-setup pipeline, delete the stub `SeedWorldBuilder.CreateWorld`, and rewrite the geometry/trail tests that were stripped in Plan 0 to assert against the real pipeline's output.

**Architecture:** Three tasks. Task 1 swaps the orchestrator call and deletes the stub. Task 2 rewrites the tests that were deleted or gutted in Plan 0 — now they assert real pipeline behavior (MST edge counts, 2-5 day distances, outlier at 6 days, planarity, connectivity, salt variance). Task 3 is final verification.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3

## Prerequisites

- Plan 0 (Clean Slate) must be complete.
- Plans 1a-1e must be complete -- `MapGenerator.Generate` exists, `StartNew` is deleted, canonical start flow is in place, `CaseFileSnapshot` carries all 14 fields.
- Plan 1f (Clean Handoff) must be complete -- ADRs and decomposition audit are fresh, tracked-items doc exists.

## Tasks

### Task 1: Wire MapGenerator Into GameSetupResolver

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`

**Changes:**
- In `GameSetupResolver.cs`: find the `SeedWorldBuilder.CreateWorld(seedWorld, source, entropy.GameEntropy, mysteryTruth.SaltSource)` call and replace it with `MapGenerator.Generate(seedWorld, source, entropy.GameEntropy, mysteryTruth.SaltSource)`
- In `SeedWorldBuilder.cs`: delete the stub `CreateWorld` method. Keep: `CreateCanonicalWorld`, `NonNegativeModulo`, `ComputeStableHash` overloads, `IsCanonicalSeedWorld` (with `ClusterCount`/`GraphDensity` comparisons)

- [ ] Implement changes
- [ ] Build: `dotnet build src/WildBunch.GameContent/WildBunch.GameContent.csproj`
- [ ] Expected: PASS
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "GameSetupResolverTests|SeededNewGameFactoryTests|SeedWorldBuilderTests"`
- [ ] Expected: Some tests may fail — `SeedWorldBuilderTests` tests that called `CreateWorld` were deleted in Plan 0, but `GameSetupResolverTests` may reference the stub. If `GameSetupResolverTests` calls `SeedWorldBuilder.CreateWorld` directly, update it to call `MapGenerator.Generate` or `SeedWorldBuilder.CreateCanonicalWorld()` as appropriate. Fix any remaining compile errors inline.
- [ ] Commit: `git commit -m "feat: wire MapGenerator into GameSetupResolver, delete stub CreateWorld"`

### Task 2: Rewrite Geometry and Trail Tests for Real Pipeline

> **Note (verified at Plan 1e head):** As of Plan 1e head, no tests assert `Assert.Empty` on trails (search `rg -n "Assert\.Empty\(.*[Tt]rails" tests/` returns zero matches). The `Assert.Empty` → `Assert.NotEmpty` changes listed below for `GetStartingTownMapHandlerTests.cs`, `GetWorldMapHandlerTests.cs`, and `StartingTownMapEndpointTests.cs` may not be needed. Verify at execution time and skip any that do not apply. The three named test files all exist; `GeometryPipelineTests.cs` is a Create target (does not yet exist).

**Files:**
- Create: `tests/WildBunch.GameContent.Tests/GeometryPipelineTests.cs` — replaces the deleted `GeometryCanonicalDistanceTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs` — add back tests that assert real pipeline behavior via `MapGenerator.Generate`
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs` — add trail-count assertion that works with the real pipeline (MST of 8 towns = 7 edges)
- Modify: `tests/WildBunch.Application.Tests/GetStartingTownMapHandlerTests.cs` — replace `Assert.Empty` trail assertions with `Assert.NotEmpty` and connectivity checks
- Modify: `tests/WildBunch.Application.Tests/GetWorldMapHandlerTests.cs` — replace `Assert.Empty` trail assertion with `Assert.NotEmpty`
- Modify: `tests/WildBunch.Integration.Tests/StartingTownMapEndpointTests.cs` — replace `Assert.Empty` trail assertion with `Assert.NotEmpty`

**New test file: `GeometryPipelineTests.cs`**

This replaces the deleted `GeometryCanonicalDistanceTests.cs`. Tests go through `SeededNewGameFactory` → `GameSetupResolver` → `MapGenerator.Generate` (the real pipeline). Key tests:

```csharp
// All trails have ride-day distances in 2-6 day range
Assert.All(session.World.Trails, trail => Assert.InRange(trail.RideDayDistance, 2m, 6m));

// All towns have non-zero coordinates (clustered placement, not placeholder)
Assert.All(session.World.Towns, town => { Assert.True(town.MapX > 0); Assert.True(town.MapY > 0); });

// Boring mode: same seed produces identical coordinates
// Non-Boring mode: different salts produce different coordinates
// Trail graph is connected (BFS from first town reaches all)
// Trail graph is planar (no crossing edges)
// Outlier slot: non-Boring produces single 6-day incident trail
```

Write these tests following the patterns from `pipeline-tests.md` Section 6 (MapGeneratorTests), but going through the full `SeededNewGameFactory` path instead of calling `MapGenerator.Generate` directly.

- [ ] Create `GeometryPipelineTests.cs` with integration-level tests
- [ ] Update `SeedWorldBuilderTests.cs`: add `CreateCanonicalWorld_HasConnectedTrails` test (canonical world now has real trails from `CreateCanonicalWorld` → `CreateWorld` with placeholder coordinates, but trails come from `MapGenerator` only when called through `GameSetupResolver`). Note: `CreateCanonicalWorld` calls `SeedWorldCatalog.CreateCanonicalWorld` which calls `BuildTrails` (stub → empty). So `CreateCanonicalWorld` still produces 0 trails. Tests asserting trails on `CreateCanonicalWorld` should assert `Assert.Empty` or be skipped. Tests asserting trails should go through `MapGenerator.Generate` or `SeededNewGameFactory`.
- [ ] Update `SeedWorldResolverTests.cs`: the `CanonicalSeedWorldHasEightTowns` test was deleted in Plan 0. Add it back but assert `Assert.Empty(seedWorld.Trails)` (the SeedWorld's Trails field is empty because `BuildTrails` is a stub — trails are generated at game setup time, not at seed resolution time).
- [ ] Update `GetStartingTownMapHandlerTests.cs`: replace `Assert.Empty(byId)` and `Assert.Empty(result.Trails)` with `Assert.NotEmpty` (the handler reads from the session's world, which now has real trails from `MapGenerator`)
- [ ] Update `GetWorldMapHandlerTests.cs`: replace `Assert.Empty(result.Trails)` with `Assert.NotEmpty`
- [ ] Update `StartingTownMapEndpointTests.cs`: replace `Assert.Empty(map.Trails)` with `Assert.NotEmpty`
- [ ] Run full test suite: `dotnet test`
- [ ] Expected: PASS — all tests pass with the real pipeline
- [ ] Commit: `git commit -m "test: rewrite geometry and trail tests for real pipeline"`

### Task 3: Final Verification and Cleanup

> **Note (verified at Plan 1e head):** `MapLayoutPalette` enum is already deleted in Plan 0. Zero references remain in `src/` or `tests/` code — the only remaining mentions are historical codec-version doc comments in `SeedWorldResolver.cs` (lines 55-75) describing the v8-v16 codec evolution. The `MapLayoutPalette` cleanup step below is a confirmation only.
>
> `SeedWorldBuilder.ComputeStableHash` still has 3 private overloads (lines 62, 74, 86) in `SeedWorldBuilder.cs` with no call sites anywhere in `src/` or `tests/` (they are unused private methods). The cleanup step below remains valid: delete the unused overloads.

- [ ] Verify no remaining `MapLayoutPalette` references: search `src/` and `tests/` — expect zero matches (confirmation only; enum already deleted in Plan 0)
- [ ] Check whether `SeedWorldBuilder.ComputeStableHash` overloads are still called: search `src/` and `tests/` for `SeedWorldBuilder.ComputeStableHash`. As of Plan 1e head, 3 private overloads exist in `SeedWorldBuilder.cs` with no call sites. Delete the unused overloads.
- [ ] Build the full solution: `dotnet build`
- [ ] Expected: zero warnings related to `MapLayoutPalette`, unused code, or deleted members
- [ ] Run the full test suite: `dotnet test`
- [ ] Expected: all tests pass across all test projects
- [ ] Commit (if any cleanup changes were made): `git commit -m "chore: final cleanup of unused helpers"`
- [ ] Verify Definition of Done against the spec (`docs/superpowers/specs/2026-07-03-geometry-first-procedural-map-generation-design.md`):
  - [ ] `MapLayoutPalette` enum deleted, `ClusterCount` + `GraphDensity` added to `SeedWorld`
  - [ ] Codec v16 encodes/decodes the new fields, round-trip tests pass
  - [ ] `ClusterPlacementGenerator` produces seed-derived, entropy-varied town coordinates
  - [ ] `TrailGraphGenerator` produces planar, connected trail graphs from settled coordinates using Delaunay + MST
  - [ ] Redundant-corridor and close-parallel filters work
  - [ ] `TerrainAssigner` derives terrain/water/risk from edge geometry + variant
  - [ ] Distances derived from final edge geometry (no catalog lookup, no coordinate adjustment)
  - [ ] Normal trails are 2-5 days; outlier trails are exactly 6 days with one incident edge
  - [ ] Boring mode is deterministic for the same seed
  - [ ] Entropic modes vary with salt while preserving seed-owned structure
  - [ ] `SeedWorldCatalog.BuildTrails`, `SeedWorldMapLayout` layout methods, and `SeedWorldBuilder` distance/adjustment methods deleted
  - [ ] All existing tests updated or replaced; no pre-existing failures carried forward
  - [ ] CI passes

## Definition of Done

- [ ] Prerequisites confirmed: Plans 0-1f complete, `MapGenerator.Generate` exists, `SeedWorldBuilder.CreateWorld` stub exists
- [ ] `GameSetupResolver` calls `MapGenerator.Generate` instead of `SeedWorldBuilder.CreateWorld`
- [ ] `SeedWorldBuilder.CreateWorld` stub is deleted; kept members remain
- [ ] Geometry/trail tests assert real pipeline behavior (not placeholder)
- [ ] `MapLayoutPalette` references confirmed absent (already deleted in Plan 0)
- [ ] Full solution builds with zero related warnings
- [ ] Full test suite passes (Domain + Application + GameContent + Integration if Docker available)
