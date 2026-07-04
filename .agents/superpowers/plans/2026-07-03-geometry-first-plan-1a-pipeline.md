# Geometry-First Map Generation - Plan 1a: Core Pipeline

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the geometry-first procedural map generation pipeline — clustered town placement, Delaunay+MST trail graph generation, geometry-derived terrain/distance assignment, outlier guarantee, and the `MapGenerator` orchestrator.

**Architecture:** A linear pipeline of five new production classes plus their unit tests. Each class is built TDD-style: write tests from the ref, verify they fail, implement from the ref, verify they pass, commit. The pipeline is internally `internal static` classes with `InternalsVisibleTo` test access. `MapGenerator.Generate` is a drop-in replacement for the stub `SeedWorldBuilder.CreateWorld` — same signature, different behavior. No domain, persistence, or event-sourcing changes in this plan.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, Delaunator NuGet v1.0.11 (MIT, netstandard2.0)

## Prerequisites

- Plan 0 (Clean Slate) must be complete. The codec is v16, `SeedWorld` has `ClusterCount`/`GraphDensity`, `BuildTrails` returns empty, `SeedWorldBuilder.CreateWorld` is a hub-and-spoke stub.

## Global Constraints

- Map bounds: 800x500 (Padding=50, so usable 50-750 x, 50-450 y)
- Coordinate scale: 25px = 1 ride-day
- Normal trail distances: 2-5 days (clamped from geometry)
- Outlier trail distance: exactly 6 days (enforced by 150px placement from nearest neighbor)
- Minimum cluster center separation: 150px
- Redundant-corridor filter: remove edge A-C when town B is within 15px of the A-C line segment AND both A-B and B-C are accepted edges
- Close-parallel filter: remove edges within 15° angle and 30px separation of an already-accepted edge
- Connectivity: MST guarantees all towns reachable; filters re-check via BFS and repair if needed
- Delaunay triangulation guarantees planarity (no crossings) by construction
- Boring mode: deterministic from seed only (no salt influence on cluster centers)
- Non-Boring modes: cluster centers are seed-derived, town offsets use runtime salt
- `InternalsVisibleTo("WildBunch.GameContent.Tests")` is already set — internal methods on new classes are testable directly

## Reference Files

- `docs/superpowers/refs/pipeline-production.md` — production code for all pipeline classes (Sections 1-6)
- `docs/superpowers/refs/pipeline-tests.md` — unit and integration tests (Sections 2-6)

## New Files

Production:
- `src/WildBunch.GameContent/NewGame/TrailEdge.cs`
- `src/WildBunch.GameContent/NewGame/ClusterPlacementGenerator.cs`
- `src/WildBunch.GameContent/NewGame/TrailGraphGenerator.cs`
- `src/WildBunch.GameContent/NewGame/TerrainAssigner.cs`
- `src/WildBunch.GameContent/NewGame/OutlierGuarantee.cs`
- `src/WildBunch.GameContent/NewGame/MapGenerator.cs`

Tests:
- `tests/WildBunch.GameContent.Tests/ClusterPlacementGeneratorTests.cs`
- `tests/WildBunch.GameContent.Tests/TrailGraphGeneratorTests.cs`
- `tests/WildBunch.GameContent.Tests/TerrainAssignerTests.cs`
- `tests/WildBunch.GameContent.Tests/OutlierGuaranteeTests.cs`
- `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs`

---

## Task 1: Add Delaunator NuGet Package + Create TrailEdge Record

**Files:**
- Modify: `src/WildBunch.GameContent/WildBunch.GameContent.csproj` — add Delaunator package reference
- Create: `src/WildBunch.GameContent/NewGame/TrailEdge.cs`
- Test: build verification only (TrailEdge is a simple record, no behavior to test)

**Interfaces:**
- Produces: `TrailEdge(int FromSlot, int ToSlot, double PixelDistance)` with `OrderedSlots` property. Used by Tasks 2-6.

- [ ] **Step 1: Add Delaunator NuGet package**

Run: `dotnet add src/WildBunch.GameContent/WildBunch.GameContent.csproj package Delaunator --version 1.0.11`
Expected: package added successfully.

- [ ] **Step 2: Create TrailEdge record**

Create `src/WildBunch.GameContent/NewGame/TrailEdge.cs` from `pipeline-production.md` Section 1.

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/WildBunch.GameContent/WildBunch.GameContent.csproj`
Expected: PASS

- [ ] **Step 4: Commit**

`git add -A; git commit -m "feat: add Delaunator NuGet package and TrailEdge internal record"`

## Task 2: Implement ClusterPlacementGenerator

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/ClusterPlacementGenerator.cs`
- Create: `tests/WildBunch.GameContent.Tests/ClusterPlacementGeneratorTests.cs`

**Interfaces:**
- Consumes: `SeedWorld` (TownCount, ClusterCount, WorldVariant, OutlierSlotType), `GameEntropy`, `SaltSource`, `GameSetupDeterministicSource`
- Produces: `ClusterPlacementGenerator.Place(SeedWorld, GameSetupDeterministicSource, GameEntropy, SaltSource?)` returning `(Dictionary<int, (int X, int Y)> Towns, Dictionary<int, int> ClusterAssignments, int? OutlierSlot)`. Used by Task 6.

- [ ] **Step 1: Write the failing tests**

Create `tests/WildBunch.GameContent.Tests/ClusterPlacementGeneratorTests.cs` from `pipeline-tests.md` Section 2.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "ClusterPlacementGeneratorTests"`
Expected: FAIL (class doesn't exist)

- [ ] **Step 3: Implement ClusterPlacementGenerator**

Create `src/WildBunch.GameContent/NewGame/ClusterPlacementGenerator.cs` from `pipeline-production.md` Section 2.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "ClusterPlacementGeneratorTests"`
Expected: PASS

- [ ] **Step 5: Commit**

`git add -A; git commit -m "feat: implement ClusterPlacementGenerator for clustered town placement"`

## Task 3: Implement TrailGraphGenerator

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/TrailGraphGenerator.cs`
- Create: `tests/WildBunch.GameContent.Tests/TrailGraphGeneratorTests.cs`

**Interfaces:**
- Consumes: `TrailEdge` from Task 1, `SeedWorld` (GraphDensity, TownCount), `GameEntropy`, `SaltSource`, `GameSetupDeterministicSource`, `ClusterPlacementGenerator.Place(...)` output from Task 2
- Produces: `TrailGraphGenerator.Generate(SeedWorld, Dictionary<int, (int, int)>, Dictionary<int, int>, GameSetupDeterministicSource, GameEntropy, SaltSource?)` returning `IReadOnlyList<TrailEdge>`. Uses `Delaunator` NuGet via `using DelaunatorSharp;`. The package's `IPoint` interface requires `int Index`, `double X`, `double Y`.

- [ ] **Step 1: Write the failing tests**

Create `tests/WildBunch.GameContent.Tests/TrailGraphGeneratorTests.cs` from `pipeline-tests.md` Section 3.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "TrailGraphGeneratorTests"`
Expected: FAIL (class doesn't exist)

- [ ] **Step 3: Implement TrailGraphGenerator**

Create `src/WildBunch.GameContent/NewGame/TrailGraphGenerator.cs` from `pipeline-production.md` Section 3.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "TrailGraphGeneratorTests"`
Expected: PASS

- [ ] **Step 5: Commit**

`git add -A; git commit -m "feat: implement TrailGraphGenerator with Delaunay + MST + filters"`

## Task 4: Implement TerrainAssigner

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/TerrainAssigner.cs`
- Create: `tests/WildBunch.GameContent.Tests/TerrainAssignerTests.cs`

**Interfaces:**
- Consumes: `TrailEdge` from Task 1, `SeedWorld` (WorldVariant), `SeedWorldTrail` (existing), domain enums `TrailTerrain`, `WaterFeature`, `TrailRisk` from `WildBunch.Domain.World`
- Produces: `TerrainAssigner.Assign(IReadOnlyList<TrailEdge>, Dictionary<int, (int, int)>, Dictionary<int, int>, SeedWorldVariant, IReadOnlyList<string>, int?)` returning `IReadOnlyList<SeedWorldTrail>`. Used by Task 6.

- [ ] **Step 1: Write the failing tests**

Create `tests/WildBunch.GameContent.Tests/TerrainAssignerTests.cs` from `pipeline-tests.md` Section 4.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "TerrainAssignerTests"`
Expected: FAIL (class doesn't exist)

- [ ] **Step 3: Implement TerrainAssigner**

Create `src/WildBunch.GameContent/NewGame/TerrainAssigner.cs` from `pipeline-production.md` Section 4.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "TerrainAssignerTests"`
Expected: PASS

- [ ] **Step 5: Commit**

`git add -A; git commit -m "feat: implement TerrainAssigner for geometry-derived terrain/distance"`

## Task 5: Implement OutlierGuarantee

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/OutlierGuarantee.cs`
- Create: `tests/WildBunch.GameContent.Tests/OutlierGuaranteeTests.cs`

**Interfaces:**
- Consumes: `TrailEdge` from Task 1, `SeedWorldTrail` (existing), `Dictionary<int, (int, int)>` from `ClusterPlacementGenerator.Place(...)` output (Task 2), `IReadOnlyList<TrailEdge>` from `TrailGraphGenerator.Generate(...)` output (Task 3), `IReadOnlyList<SeedWorldTrail>` from `TerrainAssigner.Assign(...)` output (Task 4)
- Produces: `OutlierGuarantee.Enforce(IReadOnlyList<SeedWorldTrail>, Dictionary<int, (int, int)>, int?, IReadOnlyList<string>)` returning `(IReadOnlyList<SeedWorldTrail> Trails, Dictionary<int, (int X, int Y)> Towns)`. Used by Task 6.

- [ ] **Step 1: Write the failing tests**

Create `tests/WildBunch.GameContent.Tests/OutlierGuaranteeTests.cs` from `pipeline-tests.md` Section 5.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "OutlierGuaranteeTests"`
Expected: FAIL (class doesn't exist)

- [ ] **Step 3: Implement OutlierGuarantee**

Create `src/WildBunch.GameContent/NewGame/OutlierGuarantee.cs` from `pipeline-production.md` Section 5.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "OutlierGuaranteeTests"`
Expected: PASS

- [ ] **Step 5: Commit**

`git add -A; git commit -m "feat: implement OutlierGuarantee for outlier trail invariant"`

## Task 6: Implement MapGenerator Orchestrator

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/MapGenerator.cs`
- Create: `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs`

**Interfaces:**
- Consumes: `ClusterPlacementGenerator` (Task 2), `TrailGraphGenerator` (Task 3), `TerrainAssigner` (Task 4), `OutlierGuarantee` (Task 5), `SeedWorldCatalog.DeriveTownNames` (existing), `SeedWorldCatalog.CreateWorld` (existing), `SeedWorld` (existing), `GameEntropy`, `SaltSource`, `GameSetupDeterministicSource` (existing)
- Produces: `MapGenerator.Generate(SeedWorld, GameSetupDeterministicSource, GameEntropy, SaltSource?)` returning `World`. Signature mirrors `SeedWorldBuilder.CreateWorld` so it is a drop-in replacement. Not wired into `GameSetupResolver` yet — that's Plan 2.

- [ ] **Step 1: Write the failing tests**

Create `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs` from `pipeline-tests.md` Section 6.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "MapGeneratorTests"`
Expected: FAIL (class doesn't exist)

- [ ] **Step 3: Implement MapGenerator**

Create `src/WildBunch.GameContent/NewGame/MapGenerator.cs` from `pipeline-production.md` Section 6.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "MapGeneratorTests"`
Expected: PASS

- [ ] **Step 5: Run the full GameContent test suite to verify no regressions**

Run: `dotnet test tests/WildBunch.GameContent.Tests/`
Expected: PASS — all existing tests still pass (stub `CreateWorld` is still in place, `MapGenerator` is not wired yet)

- [ ] **Step 6: Commit**

`git add -A; git commit -m "feat: implement MapGenerator orchestrator for geometry-first pipeline"`

## Definition of Done

- [ ] Delaunator NuGet package added to WildBunch.GameContent
- [ ] TrailEdge record compiles
- [ ] ClusterPlacementGenerator: deterministic for same seed, varied by salt, respects bounds and cluster separation
- [ ] TrailGraphGenerator: connected, planar (no crossings), respects density settings, filters work
- [ ] TerrainAssigner: distances match pixel geometry, terrain varies by cluster relationship and variant
- [ ] OutlierGuarantee: exactly one 6-day incident trail, coordinate adjustment works
- [ ] MapGenerator: full pipeline integration, Boring determinism, connectivity, planarity
- [ ] All new tests pass
- [ ] All new production code compiles without errors
- [ ] Existing tests still pass (stub `CreateWorld` is still in place)
