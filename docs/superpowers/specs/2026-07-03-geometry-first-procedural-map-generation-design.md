# Geometry-First Procedural Map Generation Design

**Linear issue:** [BUNCH-134](https://linear.app/harleys-workspace/issue/BUNCH-134/geometry-first-procedural-map-generation-clustered-placement)
**Related issue:** BUNCH-127 (parallel evolutionary approach, superseded by this issue on merge)
**Date:** 2026-07-03

## Goals

1. **Interesting and variable maps** — clustered placement produces regions, corridors, and natural outlier positions
2. **Seed-based replayable layouts** — seed owns cluster count and graph density; same seed = same regional structure
3. **Salted variation at game setup** — entropy/salt varies town positions within clusters and edge selection within the graph
4. **No crossings, no close parallels, no redundant corridors** — Delaunay triangulation guarantees planarity; filters handle the rest
5. **Guaranteed connectivity** — Minimum Spanning Tree connects all towns by construction
6. **Distance labels match geometry** — distances derived from final edge lengths, not from a catalog
7. **Outlier behavior** — hybrid: emergent from sparse cluster edges + optional seed-owned guarantee

## Problem

The current map generation system uses two separate authored mechanisms:

1. **Town placement** via named layout palettes (`MapLayoutPalette` enum: Tree, Star) with hand-coded coordinate formulas per slot
2. **Trail topology** via `SeedWorldCatalog.BuildTrails` with hand-coded `SlotTrailDefinition` edge lists per layout

This produces several problems (identified in BUNCH-127):
- Ride-day labels can diverge from perceived line length because distances come from a catalog, not from geometry
- Generated maps can produce crossing, closely parallel, or visually redundant trails (no algorithmic guarantee against these)
- Layouts do too much by implying both town placement and trail topology
- The two-pass coordinate adjustment (`AdjustCoordinatesToMatchRideDays`) is a workaround for the fundamental problem of assigning distances from a catalog rather than deriving them from geometry

BUNCH-127 took an evolutionary approach — adding entropy-controlled town placement to the existing Tree and Star layouts. This issue takes a ground-up approach, replacing both placement and topology with a procedural pipeline.

## Design

### Architecture: Single-Phase Pipeline

One `MapGenerator` class orchestrates the pipeline, replacing `SeedWorldBuilder.CreateWorld` + `SeedWorldCatalog.BuildTrails` + `SeedWorldMapLayout`. `MapGenerator` delegates to internal component classes (`ClusterPlacementGenerator`, `TrailGraphGenerator`, `TerrainAssigner`) but is the single entry point called by `GameSetupResolver`. The pipeline is linear with no feedback loops:

```
SeedWorld + GameEntropy + SaltSource + GameSetupDeterministicSource
  → ClusterPlacementGenerator   (places towns using cluster count + spread)
  → Delaunator                   (triangulates settled coordinates)
  → TrailGraphSelector           (MST + seeded edge addition + redundant-corridor filter + parallel filter)
  → TerrainAssigner              (derives terrain/water/risk from edge geometry + variant)
  → OutlierGuarantee             (if OutlierSlotType set, ensure outlier has exactly one incident 6-day trail)
  → World                        (towns + trails with final coordinates and distances)
```

Distances are derived from the final accepted edge geometry by construction. No coordinate adjustment pass is needed — the labels always match the visual line lengths.

### Codec Changes (v16)

The 3 bits currently used for `MapLayoutPalette` (bits 24-26) are repurposed:

| Bits | Field | Encoding | Range |
|------|-------|----------|-------|
| 24-25 | `ClusterCount` | 0-3 → 1-4 clusters | 1-4 |
| 26 | `GraphDensity` | 0 = sparse, 1 = dense | 2 values |

All other seed fields remain unchanged (variant, town count, palettes, accusation/culprit indices, cash bonus, outlier slot type). Codec version bumps from v15 to v16 (+1 on `main`'s current `resolver-v15`). Round-trip tests must be updated.

`MapLayoutPalette` enum is deleted. `SeedWorld.MapLayoutPalette` field is replaced with `ClusterCount` (int) and `GraphDensity` (enum: Sparse = 0, Dense = 1).

### Clustered Town Placement

**Class:** `ClusterPlacementGenerator`
**Input:** `SeedWorld` (town count, cluster count, world variant), `GameEntropy`, `SaltSource`, `GameSetupDeterministicSource`
**Output:** `Dictionary<int, (int X, int Y)>` — slot index to pixel coordinates, plus `Dictionary<int, int>` — slot to cluster assignment

#### Algorithm

1. **Determine cluster centers**: Pick `ClusterCount` seed-derived points within the map bounds (800x500 area, matching the current coordinate space). Use `GameSetupDeterministicSource.Roll("cluster-center-{i}")` to derive x/y for each cluster center. Ensure minimum separation between centers (at least 150px). If a center is too close to an existing center, re-derive using `Roll("cluster-center-{i}-retry-{n}")` for up to 10 retries. If all retries fail (extremely unlikely with 1-4 clusters in an 800x500 area), use the last attempted position clamped to the minimum separation distance from the nearest existing center.

2. **Distribute towns across clusters**: Assign each town slot to a cluster. For Boring mode, distribute evenly (round-robin). For non-Boring, use salt-derived assignment with a bias toward even distribution. If town count < cluster count, some clusters get 0 towns — the cluster center exists but produces no towns.

3. **Place towns within clusters**: For each town, offset from its cluster center using a seed+salt-derived angle and distance. The spread distance is entropy-controlled:
   - Boring: fixed spread (60px from center, deterministic angles)
   - Classic: 40-80px spread, salt-derived angles
   - Adventurous: 40-120px spread, salt-derived angles
   - Wild: 20-160px spread, salt-derived angles, occasional far outlier

4. **Clamp to map bounds**: Ensure all towns are within the playable area (0-800 x, 0-500 y, with padding).

5. **Outlier guarantee** (if `OutlierSlotType == 1` and entropy != Boring): After placing all base towns, add one extra town at 150px (6 ride-days) from its nearest neighbor at a salt-derived angle. This town will naturally get only one incident trail from the Delaunay/MST step due to its isolation.

#### Entropy behavior

- **Boring**: Cluster centers are deterministic from seed only (no salt influence). Town placement within clusters is deterministic. Same seed = same map, every time.
- **Classic/Adventurous/Wild**: Cluster centers are seed-derived but town offsets use runtime salt. Same seed + different salt = same cluster structure but different specific town positions.

#### Key invariant

The cluster *structure* (how many clusters, which towns belong to which cluster) is seed-owned. The specific *positions* within that structure are entropy-owned. A seed always produces the same regional character, but entropy varies the details.

### Trail Graph Generation (Delaunay + MST)

**Class:** `TrailGraphGenerator`
**Input:** `Dictionary<int, (int X, int Y)>` (settled coordinates), `Dictionary<int, int>` (cluster assignments), `SeedWorld` (graph density, town count), `GameEntropy`, `SaltSource`, `GameSetupDeterministicSource`
**Output:** `IReadOnlyList<TrailEdge>` — accepted trail edges with from-slot, to-slot, pixel distance

#### Algorithm

1. **Delaunay triangulation**: Feed the settled coordinates to the Delaunator NuGet package. This produces a set of triangular edges that are **guaranteed planar** (no crossings by mathematical construction). This is the candidate edge pool.

2. **Minimum Spanning Tree**: Compute MST from the Delaunay edges using Kruskal's algorithm (union-find). Edge weight = pixel distance. MST guarantees connectivity — every town is reachable.

3. **Add back extra Delaunay edges** for variation, controlled by `GraphDensity` and entropy:
   - **Sparse + Boring**: MST only. No extra edges. Minimal frontier feel.
   - **Sparse + non-Boring**: MST + 1-2 salt-selected Delaunay edges.
   - **Dense + Boring**: MST + all Delaunay edges shorter than the median Delaunay edge length. This deterministically adds the shorter half of candidate edges, producing a well-connected map without salt variation.
   - **Dense + non-Boring**: MST + several salt-selected Delaunay edges.

   Edge selection uses `ComputeStableHash(seedCode, edgeIndex, entropy, salt)` to pick which Delaunay edges to add back, iterating through non-MST edges in a salt-shuffled order.

4. **Redundant-corridor filter**: After selecting edges, filter out any edge A-C where there exists a town B such that B lies on or near the line segment A-C (within 15px perpendicular distance) AND both A-B and B-C are already in the accepted edge set. This eliminates the "A-C overlapping A-B when B lies between A and C" problem.

5. **Close-parallel filter**: After selecting edges, filter out any edge that runs closely parallel to an already-accepted edge (within 15° angle and 30px separation). This addresses the "closely parallel trails" problem.

6. **Connectivity re-check**: After filtering, verify the graph is still connected via BFS. If filtering disconnected a town, add back the shortest Delaunay edge that reconnects it.

#### Why this works

- **No crossings**: Delaunay triangulation is planar by construction. We only ever select edges from the Delaunay set, so crossings are mathematically impossible.
- **Guaranteed connectivity**: MST connects all towns. We only *add* edges, never remove MST edges. The filter step checks connectivity and repairs if needed.
- **No redundant corridors**: The redundant-corridor filter explicitly checks for the A-B-C overlap pattern.
- **No close parallels**: The parallel filter checks angle and distance between accepted edges.
- **Deterministic**: All selection is driven by `ComputeStableHash` over seed+entropy+salt. Same inputs = same graph.
- **Entropy variation**: Non-Boring modes add more edges and use salt to pick which ones, producing different graphs for the same seed across playthroughs.

### Outlier Guarantee

After trail graph generation and terrain assignment, if `OutlierSlotType == 1` and entropy != Boring, the pipeline verifies that the outlier town (placed far from all clusters in step 5 of `ClusterPlacementGenerator`) has exactly one incident trail and that trail is exactly 6 ride-days.

If the geometry produced more than one incident trail on the outlier town (possible if another town happens to be near the outlier), the pipeline removes the extra trails, keeping only the shortest one. If the single incident trail is not exactly 6 days (possible if coordinate clamping shifted the outlier), the pipeline adjusts the outlier's coordinates to enforce the 150px distance from its connected neighbor.

This guarantee is a post-processing step, not a separate placement mechanism. The outlier town is placed during `ClusterPlacementGenerator` and its trail emerges from `TrailGraphGenerator`; the guarantee only corrects edge cases where the geometry didn't produce the expected outlier shape.

### Terrain/Water/Risk Assignment

**Class:** `TerrainAssigner`
**Input:** `IReadOnlyList<TrailEdge>` (accepted edges with pixel distances), `Dictionary<int, (int X, int Y)>` (coordinates), `Dictionary<int, int>` (cluster assignments), `SeedWorld` (world variant)
**Output:** `IReadOnlyList<SeedWorldTrail>` (complete trails with terrain, water, risk, ride-day distance)

#### Edge role classification

Each accepted edge is classified by its geometric relationship to clusters:

| Edge role | How identified | Terrain (Canonical) | Terrain (Frontier/Rail/Outback) | Water | Risk |
|-----------|---------------|---------------------|---------------------------------|-------|------|
| Intra-cluster | Both endpoints in same cluster | OpenRange | Hills | Creek | Low |
| Inter-cluster (short) | Different clusters, ≤ 4 days | Badlands | Hills | None | Moderate |
| Inter-cluster (long) | Different clusters, > 4 days | Mountains | Mountains | None | High |
| Outlier | One endpoint is the outlier town | Mountains | Mountains | None | High |

The world variant modulates the terrain palette (Canonical = gentler, Frontier/Rail/Outback = harsher). This preserves the variant-driven character without authored per-edge definitions.

### Distance Labeling

Distances are derived from the final accepted edge geometry — no catalog lookup, no clamping pass:

1. **Compute pixel distance**: `sqrt(dx² + dy²)` for each accepted edge
2. **Convert to ride-days**: `pixelDistance / 25.0` (same scale as current: 25px = 1 ride-day)
3. **Round**: Round to nearest whole day
4. **Clamp normal trails**: 2-5 days. If the raw distance is < 2 days, clamp to 2 (shouldn't happen with clustered placement's minimum separation, but clamp as a safety net).
5. **Outlier trails**: The outlier town's single incident trail is exactly 6 days (enforced by placement at 150px from its nearest neighbor).

No coordinate adjustment pass is needed. With geometry-first generation, distances are computed from the actual coordinates. The label always matches the visual line length by construction.

### Dependency

Add `Delaunator` NuGet package (v1.0.11, MIT license, netstandard2.0) to `WildBunch.GameContent`. This is a port of Mapbox's Delaunator JavaScript library, 494 GitHub stars, no dependencies. It provides fast Delaunay triangulation of 2D points with half-edge data structures.

## What's Replaced

| Current | New |
|---------|-----|
| `SeedWorldMapLayout.cs` (layout coordinate formulas) | `ClusterPlacementGenerator` |
| `SeedWorldCatalog.BuildTrails` + `GenerateTreeTrails`/`GenerateStarTrails` | `TrailGraphGenerator` |
| `SeedWorldBuilder.DeriveTownCoordinates` | Part of `ClusterPlacementGenerator` |
| `SeedWorldBuilder.DeriveDistancesAndAdjustCoordinates` | Distances derived from final edge geometry |
| `SeedWorldBuilder.ApplyLayoutSpecificTrailRemoval` | Edge selection in `TrailGraphGenerator` |
| `SeedWorldBuilder.AdjustCoordinatesToMatchRideDays` | Deleted — unnecessary with geometry-first |
| `SeedWorldBuilder.ActivateOutlierSlot` | Part of `ClusterPlacementGenerator` (outlier guarantee) |
| Authored terrain/water per edge (`SlotTrailDefinition`) | `TerrainAssigner` |
| `MapLayoutPalette` enum | `ClusterCount` + `GraphDensity` in `SeedWorld` |

## What's Kept

- `SeedWorld` record (with field changes: `MapLayoutPalette` → `ClusterCount` + `GraphDensity`)
- `SeedWorldResolver` (with codec v16 changes)
- `GameEntropy` / `SaltSource` / `GameSetupDeterministicSource` — unchanged
- `World` / `Town` / `Trail` domain entities — unchanged
- `OutlierSlotType` mechanism — kept as hybrid (emergent + optional guarantee)
- `EntropyPolicy` / `MysteryTruthResolver` — unchanged
- `GameSetupResolver` pipeline — unchanged, calls new `MapGenerator` instead of `SeedWorldBuilder.CreateWorld`
- `SeedWorldCatalog.DeriveTownNames` and name pool — unchanged
- `SeedWorldCatalog.CreateWorld` — unchanged (final World assembly)

## Testing Strategy

Tests follow the existing `InternalsVisibleTo` pattern. Internal methods on `MapGenerator` and its components are testable directly.

### Unit Tests

**Clustered placement tests** (`ClusterPlacementGeneratorTests.cs`):
- Boring determinism: same seed = same coordinates, different salt = same coordinates
- Non-Boring variation: same seed + different salt = different coordinates
- Cluster count: 1-4 clusters produce correct number of cluster centers
- Town count: 5-10 towns all placed within map bounds
- Minimum separation: no two towns closer than a threshold
- Outlier placement: when OutlierSlotType=1 and non-Boring, outlier town is far from all clusters
- Cluster assignment: towns are distributed across clusters

**Trail graph tests** (`TrailGraphGeneratorTests.cs`):
- Delaunay triangulation produces planar edges (no crossings)
- MST connects all towns (BFS connectivity)
- Sparse density: fewer edges than dense density for same seed
- Redundant-corridor filter: A-C edge removed when A-B and B-C exist and B is near the A-C line
- Close-parallel filter: parallel edges removed
- Connectivity preserved after filtering
- Boring determinism: same seed = same graph
- Non-Boring variation: same seed + different salt = different graph

**Terrain/distance tests** (`TerrainAssignerTests.cs`):
- Intra-cluster edges get easier terrain than inter-cluster
- Outlier edge gets Mountains/None/High
- Distances derived from geometry match visual line length
- Normal trails in 2-5 day range
- Outlier trail is exactly 6 days
- Variant modulates terrain palette

**Codec round-trip tests** (update `SeedWorldResolverCodecTests.cs`):
- v16 codec round-trips ClusterCount and GraphDensity correctly
- Old v15 fields preserved (variant, town count, palettes, etc.)
- Representative seed codes produce expected ClusterCount/GraphDensity values

### Integration Tests

- `SeededNewGameFactory` produces a valid `World` with the new pipeline
- Same seed + Boring = deterministic world across runs
- Same seed + non-Boring + same salt = deterministic world
- Same seed + non-Boring + different salt = different world
- All towns reachable via trails (connectivity)
- No crossing trails (planarity)
- Outlier town has exactly one incident trail

## Definition of Done

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

## Relationship to BUNCH-127

This issue is a parallel ground-up replacement. It branches from `main`, not from BUNCH-127's branch. When this issue merges, it supersedes BUNCH-127's approach. If BUNCH-127 merges first, this issue rebases and replaces its trail generation. The DOD items from BUNCH-127 (geometry-first, no crossings, no parallels, no redundant corridors, connectivity, distance labels from final edges, 2-5 day normal, 6-day outlier) are all satisfied by this design.
