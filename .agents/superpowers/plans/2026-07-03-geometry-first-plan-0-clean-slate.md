# Geometry-First Map Generation - Plan 0: Clean Slate

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Strip the entire authored-topology map system from a fresh `origin/main` branch, bump the codec to v16 with `ClusterCount`/`GraphDensity`, and bridge to a stub that produces a minimal linear trail chain so unrelated travel tests stay green. The result is a clean, compiling, fully-tested baseline that Plans 1-2 build on.

**Architecture:** One atomic commit. Delete the `MapLayoutPalette` enum, all layout coordinate methods, all trail generation methods, and all trail removal/distance-adjustment helpers. Replace `SeedWorldBuilder.CreateWorld` with a stub that produces a linear trail chain (town 0→1→2→...→N-1) with fixed terrain/distance so travel tests that search for routes keep finding them. Update the codec, the `SeedWorld` record, and every call site. Delete tests that assert old-pipeline behavior or hardcode specific town names from the authored topology.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, existing seed codec system

## Starting state

This plan starts from `origin/main` (commit `2d7f30a`). The branch compiles and all tests pass. The codec is `resolver-v15`. The `SeedWorld` record has a `MapLayoutPalette` field. The `MapLayoutPalette` enum is defined in `SeedWorldCatalog.cs`.

## Stub design

The stub `SeedWorldBuilder.CreateWorld` produces:
- Towns at (0, 0) — coordinates don't matter for Plan 0
- A linear trail chain: town 0→1→2→...→N-1
- First trail (0→1): `Low/OpenRange/Creek/4m` — matches what `TravelEntropyVarianceTests` and `TravelTestSeedCatalogGuardrailTests` search for
- Remaining trails: `Low/OpenRange/None/3m`
- No outlier town, no entropy variance

This keeps travel tests that go through `SeededNewGameFactory` green (they find trails, they find `Low/OpenRange/Creek` routes) without reproducing the old authored topology.

## What gets deleted

**Production code:**
- `MapLayoutPalette` enum (defined in `SeedWorldCatalog.cs`)
- `SeedTrailVariant` record, `SlotTrailDefinition` record (in `SeedWorldCatalog.cs`)
- `SeedWorldCatalog.BuildTrails` method and all `GenerateXxxTrails` private methods (`GenerateTrailsForLayout`, `GenerateHubAndSpokeTrails`, `GenerateTreeTrails`, `GenerateStarTrails`, `GenerateDoubleLineTrails`)
- `SeedWorldBuilder.CreateWorld` method and all private helpers: `DeriveTownCoordinates`, `DeriveDistancesAndAdjustCoordinates`, `ApplyLayoutSpecificTrailRemoval`, `ApplyHubAndSpokeTrailRemoval`, `ApplyDoubleLineTrailRemoval`, `ApplyTreeTrailRemoval`, `ApplyStarTrailRemoval`, `SelectRandomTrails`, `ApplySimpleTrailRemoval`, `AdjustCoordinatesToMatchRideDays`, `ActivateOutlierSlot`, `SelectOutlierConnectionTarget`, `VerifyConnectivity`
- `SeedWorldMapLayout`: `DeriveRotation`, `RotateCoordinates`, `ComputeStableHash` (private), `GetCoordinatesForSlot`, `GetHubAndSpokeCoordinates`, `GetTreeCoordinates`, `GetStarCoordinates`, `GetDoubleLineCoordinates`, `CenterX`/`CenterY`/`RingRadius` constants

**Test files deleted entirely:**
- `tests/WildBunch.GameContent.Tests/MapLayoutScaleTests.cs` — entire file built around `MapLayoutPalette[]` iteration
- `tests/WildBunch.GameContent.Tests/GeometryCanonicalDistanceTests.cs` — entire file tests old-pipeline geometry (coordinates, distances, salt variance from rotation)

**Test methods deleted (from `SeedWorldBuilderTests.cs`):**
Every test that calls `SeedWorldBuilder.CreateWorld` or asserts trail counts/coordinates/terrain from the old pipeline. The kept tests are listed in "What stays" below. Specifically delete:
- `OutlierSlot_ActivatesBasedOnEntropy`, `OutlierSlot_NeverThrowsAndProducesUniqueTownIdsAcrossMultipleSalts`
- `CreateCanonicalWorldProducesEightTownsFromNamePool` (asserts 14 trails)
- `FrontierVariantProducesDifferentTerrainThanCanonical`
- `SameSeedProducesSameWorld`, `DifferentSeedsCanProduceDifferentWorlds`
- `CanonicalWorldHasAllTownsFromNamePool`, `CanonicalWorldHasConnectedGraph`, `CanonicalWorldHasExpectedTrailCountForVariant`, `CanonicalWorldHasNoDuplicateTrails`, `CanonicalWorldAllTrailsHaveValidEndpoints`, `CanonicalWorldAllTrailsHavePositiveDistance`
- `FrontierWorldHasConnectedGraph`, `FrontierWorldHasExpectedTrailCountForVariant`, `FrontierWorldHasNoDuplicateTrails`, `FrontierWorldAllTrailsHaveValidEndpoints`, `FrontierWorldAllTrailsHavePositiveDistance`
- `OutlierSlot_GeometryDoesNotBreakWithAdditionalTown`, `OutlierSlot_SameSeedDifferentEntropyProducesConsistentBaseGeometry`, `OutlierSlot_DifferentSeedsCanProduceDifferentGeometry`, `OutlierSlot_SameSeedSameEntropyProducesIdenticalGeometry`, `OutlierSlot_TrailDistancesStayInRange`, `OutlierSlot_TrailEndpointsHaveValidCoordinates`
- `CreateWorld_GeometryDerivedDistances_AreCanonical`, `CreateWorld_WildEntropy_TrimOutlierTrails_MaintainsConnectivity`, `CreateWorld_NonBoringEntropy_TrimsOutlierTrails_NotTowns`, `CreateWorld_AllEntropyModes_PreserveTownCount`, `CreateWorld_BoringEntropy_DoesNotTrimTowns`
- `SelectedStartingTownMustBeInGeneratedWorld`, `BuilderCreatesWorldFromSeedWorldTemplate`
- `ProsperityPaletteAppliesToAllTowns`, `ServicesPaletteAppliesToAllTowns` (call `CreateWorld`)
- `DeriveTownNamesWithAllZeroFields_ProducesNonTrivialShuffle` (calls `DeriveTownNames` with 8 args)
- `CreateSeedWorldWithPalettes` helper, `CreateSeedWorldWithCount` helper, `BuildSeedWorld` helper, `BuildAdjacencyList` helper, `GetReachableTowns` helper

**Test methods deleted (from `SeedWorldResolverTests.cs`):**
- `CanonicalSeedWorldHasEightTowns` — asserts `Assert.Equal(14, seedWorld.Trails.Count)`
- `DifferentTownCountsProduceDifferentTrailCounts` — asserts old trail count ordering
- `CreateSeedWorldWithCount` helper — constructs `SeedWorld` with `MapLayoutPalette`, calls `DeriveTownNames` with 8 args, `BuildTrails` with 3 args

**Test methods deleted (from `SeedWorldResolverCodecTests.cs`):**
- `MapLayoutPalette_ModuloWrapping_ClampsToDefinedRange` — tests deleted enum

**Test methods deleted (from `TravelTestSeedCatalogGuardrailTests.cs`):**
- `CanonicalWorld_HasLowOpenRangeCreekRoute` — asserts specific route from specific town in old topology

**Test methods deleted (from `GameSetupResolverTests.cs`):**
- `ResolvesCanonicalSeedWorldToCanonicalGameSetup` — calls `SeedWorldBuilder.CreateWorld` directly to get expected starting town. Replace with `SeedWorldBuilder.CreateCanonicalWorld()` or derive expected town from the stub.

**Integration test scenarios deleted (from `ScenarioSeedCatalog.cs`):**
- Any `AssertCreatedSessionContract` that asserts specific town name connectivity (e.g., "quartzsite", "emberfall", "hardpan"). These tests hardcode authored-topology town connections that the stub chain doesn't reproduce. They get rewritten in Plan 2 when the real pipeline produces clustered coordinates and trail graphs.
- `StartingTownMapEndpointTests.cs` — asserts `Assert.Equal(14, map.Trails.Count)`. Delete the trail-count assertion (keep the test if it has other assertions, or delete the test if it only checks trail count).

## What gets created

**New files:**
- `src/WildBunch.GameContent/NewGame/GraphDensity.cs` — enum: `Sparse = 0, Dense = 1`

## What gets changed

**`SeedWorld.cs`:** Replace `MapLayoutPalette MapLayoutPalette` field with `int ClusterCount, GraphDensity GraphDensity`. Update XML doc comment.

**`SeedWorldResolver.cs`:**
- Version constant: `"resolver-v15"` → `"resolver-v16"`
- Add v16 entry to version history XML doc
- `Resolve()`: decode bits 24-25 as `ClusterCount` (0-3 + 1 → 1-4), bit 26 as `GraphDensity` (0/1 → Sparse/Dense). Remove MapLayoutPalette decode and modulo wrapping.
- `DeriveTownNames` call: remove `mapLayoutPalette` argument
- `BuildTrails` call: replace with `Array.Empty<SeedWorldTrail>()`
- `SeedWorld` constructor: replace `mapLayoutPalette` with `clusterCount, graphDensity`
- `CreateRepresentativeSeedCode()`: encode `(ClusterCount - 1)` to bits 24-25, `GraphDensity` to bit 26
- `Validate()`: replace MapLayoutPalette validation with `ClusterCount is < 1 or > 4` and `Enum.IsDefined(typeof(GraphDensity), ...)`
- `CreateCanonicalSeedWorldShape()`: use `clusterCount = 1, graphDensity = GraphDensity.Sparse`

**`SeedWorldCatalog.cs`:**
- Delete `MapLayoutPalette` enum, `SeedTrailVariant` record, `SlotTrailDefinition` record
- `DeriveTownNames()`: remove `MapLayoutPalette` parameter (8th param)
- `BuildTrails()`: replace with stub returning `Array.Empty<SeedWorldTrail>()` (2 params: variant, townNames)
- Delete `GenerateTrailsForLayout`, `GenerateHubAndSpokeTrails`, `GenerateTreeTrails`, `GenerateStarTrails`, `GenerateDoubleLineTrails`
- `CreateCanonicalWorld()`: update `DeriveTownNames` and `BuildTrails` calls
- `CreateWorld()`: replace `GetCoordinatesForSlot` fallback with `(0, 0)` fallback; delete rotation block

**`SeedWorldBuilder.cs`:**
- Replace `CreateWorld()` with stub that produces a linear trail chain:
  ```csharp
  public static World CreateWorld(
      SeedWorld seedWorld,
      GameSetupDeterministicSource source,
      GameEntropy entropy = GameEntropy.Boring,
      SaltSource? saltSource = null)
  {
      ArgumentNullException.ThrowIfNull(seedWorld);
      ArgumentNullException.ThrowIfNull(source);

      var townNames = SeedWorldCatalog.DeriveTownNames(
          seedWorld.WorldVariant, seedWorld.TownCount,
          seedWorld.AccusationIndex, seedWorld.DefaultCulpritIndex,
          seedWorld.CashBonus, seedWorld.ProsperityPalette, seedWorld.ServicesPalette);

      // Minimal linear trail chain: 0→1→2→...→N-1.
      // First edge is Low/OpenRange/Creek/4m (matches travel test route searches).
      // Remaining edges are Low/OpenRange/None/3m.
      var trails = new List<SeedWorldTrail>();
      for (var i = 0; i < townNames.Count - 1; i++)
      {
          var terrain = i == 0 ? TrailTerrain.OpenRange : TrailTerrain.OpenRange;
          var water = i == 0 ? WaterFeature.Creek : WaterFeature.None;
          var distance = i == 0 ? 4m : 3m;
          trails.Add(new SeedWorldTrail(
              $"trail-{i}-{i + 1}",
              townNames[i].Id,
              townNames[i + 1].Id,
              TrailRisk.Low,
              terrain,
              water,
              distance));
      }

      return SeedWorldCatalog.CreateWorld(
          seedWorld.WorldVariant, townNames, seedWorld.ServicesPalette,
          seedWorld.ProsperityPalette, trails,
          townCoordinates: null, outlierSlot: null,
          entropy, saltSource, seedWorld.SeedCode);
  }
  ```
- Delete all private helpers listed in "What gets deleted"
- `IsCanonicalSeedWorld()`: replace `MapLayoutPalette` comparison with `ClusterCount` and `GraphDensity` comparisons
- Keep: `CreateCanonicalWorld`, `ComputeStableHash` overloads, `NonNegativeModulo`

**`SeedWorldMapLayout.cs`:**
- Delete `DeriveRotation`, `RotateCoordinates`, `ComputeStableHash` (private), `GetCoordinatesForSlot`, `GetHubAndSpokeCoordinates`, `GetTreeCoordinates`, `GetStarCoordinates`, `GetDoubleLineCoordinates`, `CenterX`/`CenterY`/`RingRadius` constants
- `GetMapTowns(World world, MapLayoutPalette layout)` → `GetMapTowns(World world)` (drop layout param)
- `GetMapTowns()` → call `GetMapTowns(world)` without layout arg
- Keep: `SeedMapTown` record, `SeedMapTrailEdge` record, `GetMapTrails()`, `GetMapTrails(World)`

**`GetStartingTownMapHandler.cs`:**
- Delete the `MapLayoutPalette` derivation block (the `var layout = MapLayoutPalette.HubAndSpoke` block and the `if (session.SeedCode ...)` block that resolves `seedWorld.MapLayoutPalette`)
- Replace `GetMapTowns(session.World, layout)` with `GetMapTowns(session.World)`

**`GameSetupResolver.cs`:** No change needed — it calls `SeedWorldBuilder.CreateWorld` which is now the stub

**Test helpers updated:**
- `SeedWorldSeedCodeFactory.cs`: replace `mapLayoutPalette` variable with `clusterCount = 1, graphDensity = GraphDensity.Sparse`; remove `mapLayoutPalette` from `DeriveTownNames` call (drop to 7 args) and `BuildTrails` call (drop to 2 args); replace `mapLayoutPalette` in `SeedWorld` constructor with `clusterCount, graphDensity`
- `TravelTestSeedCatalog.cs`: same pattern in `CreateFullTownSeedWorld`
- `SeedWorldResolverTests.cs`: fix `SeedWorldValidationRejectsImpossibleManualEdits` — replace `var invalidLayout = valid with { MapLayoutPalette = (MapLayoutPalette)99 }` with `var invalidClusterCount = valid with { ClusterCount = 99 }` and `var invalidGraphDensity = valid with { GraphDensity = (GraphDensity)99 }`; update the assertion to check both
- `SeedWorldResolverTests.cs`: fix `DifferentUuidBitPositionsChangeDifferentSeedWorldFields` — remove the MapLayoutPalette assertion section (the last block that changes bits 24-26 and asserts MapLayoutPalette)
- `SeedWorldResolverCodecTests.cs` `RoundTrip_PreservesAllFields`: replace `Assert.Equal(original.MapLayoutPalette, resolved.MapLayoutPalette)` with `Assert.Equal(original.ClusterCount, resolved.ClusterCount)` and `Assert.Equal(original.GraphDensity, resolved.GraphDensity)`
- `SeedWorldResolverCodecTests.cs`: add `ResolverContractVersion_IsV16`, `RoundTrip_PreservesClusterCountAndGraphDensity`, `ClusterCount_AllValues_RoundTrip`, `GraphDensity_BothValues_RoundTrip`, `ClusterCount_BitEncoding_RoundTrip`, `GraphDensity_BitEncoding_RoundTrip` tests

**Trail-count assertions changed:**
- `GetStartingTownMapHandlerTests.cs`: replace `Assert.Equal(14, byId.Count)` and `Assert.Equal(14, result.Trails.Count)` with `Assert.NotEmpty`
- `GetWorldMapHandlerTests.cs`: replace `Assert.Equal(14, result.Trails.Count)` with `Assert.NotEmpty`

## What stays (no changes needed)

**`SeedWorldBuilderTests.cs` tests kept:**
- `NonNegativeModulo_SafeForIntMinValue`, `NonNegativeModulo_SafeForNegativeValues`, `NonNegativeModulo_SafeForPositiveValues`
- `CreateCanonicalWorldAppliesUniformProsperousPalette`, `CreateCanonicalWorldAppliesHubTelegraphServicesPalette`
- `DifferentSeedsCanProduceDifferentTownNames`
- `StartingTownPolicyDefaultsToFirstTown`, `StartingTownPolicyAcceptsAnyValidTownChoice`, `StartingTownPolicyRejectsInvalidTownChoice`
- `TownCountRespectsMinAndMax` — needs `CreateSeedWorldWithCount` helper updated to use ClusterCount/GraphDensity

**Tests that keep passing with the stub chain:**
- `SeededNewGameFactoryTests.CreatesRicherSeedWorldAndCase` — asserts `Trails.Count > 0` ✓ (chain has N-1 trails)
- `TravelEntropyVarianceTests` — `FindRouteFromCurrentTown(Low, OpenRange, Creek)` ✓ (first edge matches)
- `TravelTestSeedCatalogGuardrailTests` round-trip tests — only assert variant + town count ✓
- `TravelTestSeedCatalogGuardrailTests.AllGangMembersAreCulpritEligible` — no trail dependency ✓
- `TravelEntropyVarianceTests` salt mode tests — no trail dependency ✓

## Tasks

This is one atomic task — all changes must land together for the build to compile and tests to pass.

- [x] Create `GraphDensity.cs` enum
- [x] Update `SeedWorld.cs` record: replace `MapLayoutPalette` with `ClusterCount` + `GraphDensity`
- [x] Update `SeedWorldResolver.cs`: version, decode, encode, validate, canonical shape, DeriveTownNames call, BuildTrails call, constructor call
- [x] Update `SeedWorldCatalog.cs`: delete enum + records + trail methods, update DeriveTownNames/BuildTrails/CreateCanonicalWorld/CreateWorld
- [x] Update `SeedWorldBuilder.cs`: stub CreateWorld with linear trail chain, delete helpers, update IsCanonicalSeedWorld
- [x] Update `SeedWorldMapLayout.cs`: delete layout methods, update GetMapTowns overloads
- [x] Update `GetStartingTownMapHandler.cs`: remove MapLayoutPalette block
- [x] Delete `MapLayoutScaleTests.cs`
- [x] Delete `GeometryCanonicalDistanceTests.cs`
- [x] Update `SeedWorldBuilderTests.cs`: delete old-pipeline tests, delete dead helpers
- [x] Update `SeedWorldResolverTests.cs`: delete trail-count assertions, delete helper, fix validation test, fix bit-position test
- [x] Update `SeedWorldResolverCodecTests.cs`: delete MapLayoutPalette test, update RoundTrip test, add v16 tests
- [x] Update `SeedWorldSeedCodeFactory.cs`: replace MapLayoutPalette with ClusterCount/GraphDensity
- [x] Update `TravelTestSeedCatalog.cs`: replace MapLayoutPalette with ClusterCount/GraphDensity
- [x] Update `GetStartingTownMapHandlerTests.cs`: replace 14-trail assertions with NotEmpty
- [x] Update `GetWorldMapHandlerTests.cs`: replace 14-trail assertion with NotEmpty
- [x] Update `StartingTownMapEndpointTests.cs`: delete 14-trail assertion
- [x] Update `GameSetupResolverTests.cs`: replace SeedWorldBuilder.CreateWorld call
- [x] Update `TravelTestSeedCatalogGuardrailTests.cs`: delete CanonicalWorld_HasLowOpenRangeCreekRoute
- [x] Update integration test scenarios in `ScenarioSeedCatalog.cs`: delete assertions that hardcode specific town name connectivity
- [x] Build: `dotnet build`
- [x] Expected: PASS — zero errors
- [x] Run tests: `dotnet test`
- [x] Expected: PASS — all remaining tests pass (some tests deleted, some assertions loosened)
- [x] Commit: `git add -A; git commit -m "feat: strip authored-topology map system, bump codec to v16 with ClusterCount + GraphDensity, bridge to linear trail stub"`

## Definition of Done

- [x] Codec version is `resolver-v16`
- [x] `SeedWorld` has `ClusterCount` (int) and `GraphDensity` (enum) instead of `MapLayoutPalette`
- [x] `MapLayoutPalette` enum is deleted; zero references remain in `src/` or `tests/`
- [x] `BuildTrails` returns empty; `CreateWorld` stub produces a linear trail chain
- [x] `SeedWorldMapLayout` has no layout coordinate methods or rotation methods
- [x] Full solution builds with zero errors
- [x] All remaining tests pass (travel tests that go through SeededNewGameFactory still find routes)
- [x] No test hardcodes specific town names from the old authored topology
