# Geometry-First Map Generation - Plan 1: Core Pipeline

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the geometry-first procedural map generation pipeline — clustered town placement, Delaunay+MST trail graph generation, geometry-derived terrain/distance assignment, outlier guarantee, and the `MapGenerator` orchestrator.

**Architecture:** A linear pipeline of five new production classes plus their unit tests. Each class is built TDD-style: write tests from the ref, verify they fail, implement from the ref, verify they pass, commit. The pipeline is internally `static` classes with `InternalsVisibleTo` test access. `MapGenerator.Generate` is a drop-in replacement for the stub `SeedWorldBuilder.CreateWorld` — same signature, different behavior.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, Delaunator NuGet v1.0.11 (MIT, netstandard2.0)

## Prerequisites

- Plan 0 (Clean Slate) must be complete. The codec is v16, `SeedWorld` has `ClusterCount`/`GraphDensity`, `BuildTrails` returns empty, `SeedWorldBuilder.CreateWorld` is a stub producing placeholder coordinates.
- The `Delaunator` NuGet package must be added to `WildBunch.GameContent`. (If Plan 0 didn't add it, add it as the first step of Task 1.)

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

## Task 1: Create TrailEdge Record

**File:** `src/WildBunch.GameContent/NewGame/TrailEdge.cs`

Create an internal record used by the pipeline components. Signature: `TrailEdge(int FromSlot, int ToSlot, double PixelDistance)`. Add an `OrderedSlots` property for deduplication.

See `pipeline-production.md` Section 1.

- [ ] Create file with the `TrailEdge` record from `pipeline-production.md` Section 1
- [ ] Build: `dotnet build src/WildBunch.GameContent/WildBunch.GameContent.csproj`
- [ ] Expected: PASS
- [ ] Commit: `git commit -m "feat: add TrailEdge internal record for pipeline graph edges"`

## Task 2: Implement ClusterPlacementGenerator

**File:** `src/WildBunch.GameContent/NewGame/ClusterPlacementGenerator.cs`

Static class with a single `Place(...)` method returning `(Towns, ClusterAssignments, OutlierSlot)`.

Key behaviors:
- `DeriveClusterCenters`: seed-derived cluster centers within 800x500 bounds, minimum 150px separation
- `AssignTownsToClusters`: round-robin for Boring mode, salt-derived for non-Boring
- `PlaceTownsInClusters`: towns placed around cluster centers with deterministic/salt-derived offsets
- `PlaceOutlierTown`: when OutlierSlotType=1 and non-Boring, place at 150px from nearest neighbor

See `pipeline-production.md` Section 2.

- [ ] Write tests: create `tests/WildBunch.GameContent.Tests/ClusterPlacementGeneratorTests.cs` from `pipeline-tests.md` Section 2
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "ClusterPlacementGeneratorTests"`
- [ ] Expected: FAIL (class doesn't exist)
- [ ] Implement `ClusterPlacementGenerator` from `pipeline-production.md` Section 2
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "ClusterPlacementGeneratorTests"`
- [ ] Expected: PASS
- [ ] Commit: `git commit -m "feat: implement ClusterPlacementGenerator for clustered town placement"`

## Task 3: Implement TrailGraphGenerator

**File:** `src/WildBunch.GameContent/NewGame/TrailGraphGenerator.cs`

Static class with `Generate(...)` returning `IReadOnlyList<TrailEdge>`.

Key behaviors:
- Compute Delaunay triangulation using Delaunator NuGet package
- Compute MST (Kruskal's algorithm) from Delaunay edges
- Select extra edges based on density (Sparse=MST only for Boring, Dense=MST+shorter half for Boring, salt-selected for non-Boring)
- Apply redundant-corridor filter: remove A-C when B is within 15px of A-C line and both A-B and B-C exist
- Apply close-parallel filter: remove edges within 15° angle and 30px separation
- Repair connectivity via BFS if filters disconnect any town

See `pipeline-production.md` Section 3.

- [ ] Write tests: create `tests/WildBunch.GameContent.Tests/TrailGraphGeneratorTests.cs` from `pipeline-tests.md` Section 3
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "TrailGraphGeneratorTests"`
- [ ] Expected: FAIL (class doesn't exist)
- [ ] Implement `TrailGraphGenerator` from `pipeline-production.md` Section 3
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "TrailGraphGeneratorTests"`
- [ ] Expected: PASS
- [ ] Commit: `git commit -m "feat: implement TrailGraphGenerator with Delaunay + MST + filters"`

## Task 4: Implement TerrainAssigner

**File:** `src/WildBunch.GameContent/NewGame/TerrainAssigner.cs`

Static class with `Assign(...)` returning `IReadOnlyList<SeedWorldTrail>`.

Key behaviors:
- Intra-cluster edges: OpenRange/Creek/Low (Canonical) or Hills/Creek/Low (variant)
- Inter-cluster short (<=4 days): Badlands/None/Moderate (Canonical) or Hills/None/Moderate (variant)
- Inter-cluster long (>4 days): Mountains/None/High
- Outlier edges: Mountains/None/High, exactly 6 ride-days
- Distance derived from pixel distance at 25px/ride-day, clamped to 2-5 days for normal trails

See `pipeline-production.md` Section 4.

- [ ] Write tests: create `tests/WildBunch.GameContent.Tests/TerrainAssignerTests.cs` from `pipeline-tests.md` Section 4
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "TerrainAssignerTests"`
- [ ] Expected: FAIL (class doesn't exist)
- [ ] Implement `TerrainAssigner` from `pipeline-production.md` Section 4
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "TerrainAssignerTests"`
- [ ] Expected: PASS
- [ ] Commit: `git commit -m "feat: implement TerrainAssigner for geometry-derived terrain/distance"`

## Task 5: Implement OutlierGuarantee

**File:** `src/WildBunch.GameContent/NewGame/OutlierGuarantee.cs`

Static class with `Enforce(...)` returning `(Trails, AdjustedTowns)`.

Key behaviors:
- No-op if outlierSlot is null (returns inputs unchanged)
- If outlier has multiple incident trails, keep only the shortest
- Enforce the single incident trail is exactly 6 ride-days
- Adjust outlier coordinates if needed to ensure 150px distance

See `pipeline-production.md` Section 5.

- [ ] Write tests: create `tests/WildBunch.GameContent.Tests/OutlierGuaranteeTests.cs` from `pipeline-tests.md` Section 5
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "OutlierGuaranteeTests"`
- [ ] Expected: FAIL (class doesn't exist)
- [ ] Implement `OutlierGuarantee` from `pipeline-production.md` Section 5
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "OutlierGuaranteeTests"`
- [ ] Expected: PASS
- [ ] Commit: `git commit -m "feat: implement OutlierGuarantee for outlier trail invariant"`

## Task 6: Implement MapGenerator Orchestrator

**File:** `src/WildBunch.GameContent/NewGame/MapGenerator.cs`

Static class with `Generate(...)` returning `World`. Orchestrates the pipeline:
1. Derive town names from seed
2. Place towns via `ClusterPlacementGenerator`
3. Generate trail edges via `TrailGraphGenerator`
4. Assign terrain via `TerrainAssigner`
5. Enforce outlier via `OutlierGuarantee`
6. Assemble final `World` via `SeedWorldCatalog.CreateWorld`

See `pipeline-production.md` Section 6.

- [ ] Write tests: create `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs` from `pipeline-tests.md` Section 6
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "MapGeneratorTests"`
- [ ] Expected: FAIL (class doesn't exist)
- [ ] Implement `MapGenerator` from `pipeline-production.md` Section 6
- [ ] Run tests: `dotnet test tests/WildBunch.GameContent.Tests/ --filter "MapGeneratorTests"`
- [ ] Expected: PASS
- [ ] Commit: `git commit -m "feat: implement MapGenerator orchestrator for geometry-first pipeline"`

## Definition of Done

- [ ] TrailEdge record compiles
- [ ] ClusterPlacementGenerator: deterministic for same seed, varied by salt, respects bounds and cluster separation
- [ ] TrailGraphGenerator: connected, planar (no crossings), respects density settings, filters work
- [ ] TerrainAssigner: distances match pixel geometry, terrain varies by cluster relationship and variant
- [ ] OutlierGuarantee: exactly one 6-day incident trail, coordinate adjustment works
- [ ] MapGenerator: full pipeline integration, Boring determinism, connectivity, planarity
- [ ] All new tests pass
- [ ] All new production code compiles without errors
- [ ] Existing tests still pass (stub `CreateWorld` is still in place — `MapGenerator` is not wired yet)
