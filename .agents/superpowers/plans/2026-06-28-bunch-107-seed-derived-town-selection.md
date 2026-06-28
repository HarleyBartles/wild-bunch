# BUNCH-107: Seed-Derived Town Selection Model

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the two-canned-set `TownSetKey` model with a true seed-derived deterministic town selection over the full town catalog, where the seed determines town count, which towns are selected, and the trail graph between them with baseline terrain/water/distance stored in the `SeedWorld` template.

**Architecture:** `SeedWorldCatalog` provides the full town catalog (8 towns) and base trail definitions (9 trails) with per-variant terrain/water/distance. `SeedWorldResolver` derives town count (6-8), town selection (anchor towns always included, rest seed-selected), and trail graph from the seed. `SeedWorld` holds `SelectedTownIds` and `Trails` (list of `SeedWorldTrail` with terrain/water/distance). `SeedWorldBuilder` builds `World` from the `SeedWorld` template. `DifficultyEnvelope` may later modify terrain/distance downstream of the seed codec. `StartingTownPolicy` validates the player's chosen starting town against the generated world.

**Tech Stack:** C# / .NET 10, xUnit, PostgreSQL (EF Core).

## Global Constraints

- Starting town is NOT seed-owned. `StartingTownPolicy` remains the seam.
- Same seed + same difficulty must produce the same resolved map.
- Difficulty may later influence map pressure/layout realization downstream of the seed codec, not by hiding difficulty inside the seed.
- `SeedWorld` holds the default terrain and trail distances. Later difficulty can modify those values.
- Do not implement BUNCH-93 entropy controls or BUNCH-94 difficulty controls.
- Do not change `GameStarted` event shape, snapshot codec, EF schema, `StartGameRequest`, or frontend.
- Keep `GameSession` as the live-play aggregate root.
- The canonical seed world (all 8 towns, all 9 trails) must still work for existing tests.
- Anchor towns (pinecross, redmesa, holloway) must always be selected to guarantee trail graph connectivity.
- Town count range: 6-8 (minimum 6 for playability, maximum 8 = full catalog).

---

## File Structure

**New files:**
- `src/WildBunch.GameContent/NewGame/SeedWorldTrail.cs` — already created. Record holding trail id, from/to town IDs, risk, terrain, water feature, ride-day distance.

**Modified production files:**
- `src/WildBunch.GameContent/NewGame/SeedWorld.cs` — replace `TownSetKey` with `SelectedTownIds` + `Trails`
- `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs` — remove two-set, expose full catalog + trail definitions + selection helpers
- `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs` — seed-derived town count, selection, trail building
- `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs` — build `World` from `SeedWorld` template
- `src/WildBunch.GameContent/NewGame/StartingWorldDescriptorSeedMixer.cs` — new signature using sorted town IDs
- `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs` — update `IsCanonicalSeedWorld` check
- `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs` — use canonical world from catalog
- `src/WildBunch.GameContent/NewGame/StartingTownCatalog.cs` — already updated

**Modified test files:**
- `tests/WildBunch.GameContent.Tests/SeedWorldSeedCodeFactory.cs` — new API
- `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs` — new round-trip + selection tests
- `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs` — town selection, trail, stability tests
- `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalog.cs` — use canonical seed world
- `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalogGuardrailTests.cs` — update guardrails
- `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs` — update SeedWorld construction
- `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs` — update CreateSeedCode
- `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalog.cs` — update shape signatures
- `tests/WildBunch.Integration.Tests/GameApiTests.cs` — may need route updates

---

## Task 1: Update SeedWorld record and SeedWorldTrail

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorld.cs`
- Create: `src/WildBunch.GameContent/NewGame/SeedWorldTrail.cs` (already created)

**Interfaces:**
- Produces: `SeedWorld(Guid, SeedWorldVariant, IReadOnlyList<string>, IReadOnlyList<SeedWorldTrail>, int, int, int)` and `SeedWorldTrail(string, string, string, TrailRisk, TrailTerrain, WaterFeature, decimal)`

- [x] **Step 1: Create SeedWorldTrail record** — already done
- [x] **Step 2: Update SeedWorld record** — already done (replaced TownSetKey with SelectedTownIds + Trails)

---

## Task 2: Update SeedWorldCatalog — full catalog, remove two-set

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs`

**Interfaces:**
- Produces: `SeedWorldCatalog.AllTowns` (IReadOnlyList<SeedTownDefinition>), `SeedWorldCatalog.AllTrails` (IReadOnlyList<SeedTrailDefinition>), `SeedWorldCatalog.GetTown(string id)`, `SeedWorldCatalog.GetTrailVariant(SeedTrailDefinition, SeedWorldVariant)`, `SeedWorldCatalog.CreateWorld(SeedWorldVariant, IReadOnlyList<string> selectedTownIds, IReadOnlyList<SeedWorldTrail> trails)`

- [ ] **Step 1: Write the failing test**

```csharp
// In SeedWorldBuilderTests.cs
[Fact]
public void CatalogExposesAllEightTowns()
{
    var towns = SeedWorldCatalog.AllTowns;
    Assert.Equal(8, towns.Count);
    Assert.Contains(towns, t => t.Id == "pinecross");
    Assert.Contains(towns, t => t.Id == "openpass");
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests --filter "CatalogExposesAllEightTowns"`
Expected: FAIL — `AllTowns` does not exist

- [ ] **Step 3: Implement — rewrite SeedWorldCatalog**

Remove the two-set approach. Expose `AllTowns` and `AllTrails` as public readonly lists. Add `CreateWorld(SeedWorldVariant, IReadOnlyList<string>, IReadOnlyList<SeedWorldTrail>)` that builds a `World` from selected town IDs and seed-world trails. Keep `CreateCanonicalWorld()` for the map layout.

- [ ] **Step 4: Run test to verify it passes**
- [ ] **Step 5: Commit**

---

## Task 3: Update SeedWorldResolver — seed-derived town selection

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs`
- Modify: `src/WildBunch.GameContent/NewGame/StartingWorldDescriptorSeedMixer.cs`

**Interfaces:**
- Produces: `SeedWorldResolver.Resolve(Guid)` now derives `SelectedTownIds` and `Trails` from the seed. `StartingWorldDescriptorSeedMixer.CreateSeedWorldSignature(SeedWorld)` uses sorted town IDs instead of town set key.

- [ ] **Step 1: Write the failing test**

```csharp
// In SeedWorldResolverTests.cs
[Fact]
public void ResolverDerivesTownCountFromSeed()
{
    var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
    Assert.InRange(seedWorld.SelectedTownIds.Count, 6, 8);
}

[Fact]
public void ResolverAlwaysIncludesAnchorTowns()
{
    for (var i = 0; i < 32; i++)
    {
        var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
        Assert.Contains("pinecross", seedWorld.SelectedTownIds);
        Assert.Contains("redmesa", seedWorld.SelectedTownIds);
        Assert.Contains("holloway", seedWorld.SelectedTownIds);
    }
}

[Fact]
public void DifferentSeedsCanProduceDifferentTownCounts()
{
    var counts = new HashSet<int>();
    for (var i = 0; i < 128; i++)
    {
        var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
        counts.Add(seedWorld.SelectedTownIds.Count);
    }
    Assert.True(counts.Count >= 2, $"Expected at least 2 different town counts, got {counts.Count}");
}
```

- [ ] **Step 2: Run tests to verify they fail**
- [ ] **Step 3: Implement — rewrite SeedWorldResolver.Resolve**

Derive town count (6-8) from seed. Always include anchor towns (pinecross, redmesa, holloway). Select remaining towns deterministically from the seed using a Fisher-Yates-like shuffle. Build trails from catalog where both endpoints are selected, with terrain/water/distance from the catalog indexed by world variant.

- [ ] **Step 4: Update StartingWorldDescriptorSeedMixer.CreateSeedWorldSignature**

Replace `seedWorld.TownSetKey` with `string.Join(",", seedWorld.SelectedTownIds.OrderBy(id => id))`.

- [ ] **Step 5: Run tests to verify they pass**
- [ ] **Step 6: Commit**

---

## Task 4: Update SeedWorldBuilder — build World from SeedWorld template

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`
- Modify: `src/WildBunch.GameContent/NewGame/GameSetupResolver.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// In SeedWorldBuilderTests.cs
[Fact]
public void BuilderCreatesWorldFromSeedWorldTemplate()
{
    var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
    var source = new GameSetupDeterministicSource(seedWorld.SeedCodeText);
    var world = SeedWorldBuilder.CreateWorld(seedWorld, source);
    Assert.Equal(seedWorld.SelectedTownIds.Count, world.Towns.Count);
    Assert.Equal(seedWorld.Trails.Count, world.Trails.Count);
}

[Fact]
public void SameSeedProducesSameWorld()
{
    var seed = Guid.NewGuid();
    var seedWorld = SeedWorldResolver.Resolve(seed);
    var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seed));
    var world1 = SeedWorldBuilder.CreateWorld(seedWorld, source);
    var world2 = SeedWorldBuilder.CreateWorld(seedWorld, source);
    Assert.Equal(world1.Towns.Count, world2.Towns.Count);
    Assert.Equal(world1.Trails.Count, world2.Trails.Count);
}
```

- [ ] **Step 2: Run tests to verify they fail**
- [ ] **Step 3: Implement — rewrite SeedWorldBuilder.CreateWorld**

Build `World` from `SeedWorld.SelectedTownIds` and `SeedWorld.Trails`. Look up town definitions from `SeedWorldCatalog` by ID. Convert `SeedWorldTrail` records to `Trail` domain objects. The `DifficultyEnvelope` seam is left open: `CreateWorld` accepts an optional `DifficultyEnvelope?` parameter (null for now) that may later modify terrain/distance.

- [ ] **Step 4: Update GameSetupResolver.IsCanonicalSeedWorld**

Check that `SelectedTownIds.Count == 8` and all anchor towns are present, instead of checking `TownSetKey`.

- [ ] **Step 5: Run tests to verify they pass**
- [ ] **Step 6: Commit**

---

## Task 5: Update test helper and resolver tests

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldSeedCodeFactory.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldResolverTests.cs`

- [ ] **Step 1: Update SeedWorldSeedCodeFactory**

Replace the `townSetKey` parameter with a simpler API: `CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong salt)` that finds a seed producing a world with the given variant and case fields. The town selection is seed-derived, so the factory can't control it directly — it just finds a seed that matches the non-town fields.

- [ ] **Step 2: Update SeedWorldResolverTests**

Remove tests referencing `TownSetKey`. Add tests for:
- Round-trip through UUID
- Different seeds produce different town counts
- Different seeds produce different selected towns
- Same seed is stable
- Anchor towns always present
- Canonical seed world has all 8 towns

- [ ] **Step 3: Run tests to verify they pass**
- [ ] **Step 4: Commit**

---

## Task 6: Update SeedWorldBuilderTests — town selection and trail proofs

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs`

- [ ] **Step 1: Write tests proving different seeds produce different worlds**

```csharp
[Fact]
public void DifferentSeedsCanProduceDifferentTownSelections()
{
    var selections = new HashSet<string>();
    for (var i = 0; i < 128; i++)
    {
        var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
        selections.Add(string.Join(",", seedWorld.SelectedTownIds.OrderBy(id => id)));
    }
    Assert.True(selections.Count >= 2, $"Expected at least 2 different town selections, got {selections.Count}");
}

[Fact]
public void DifferentSeedsCanProduceDifferentTrailSignatures()
{
    var signatures = new HashSet<string>();
    for (var i = 0; i < 128; i++)
    {
        var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
        var sig = string.Join(",", seedWorld.Trails.Select(t => t.Id).OrderBy(id => id));
        signatures.Add(sig);
    }
    Assert.True(signatures.Count >= 2, $"Expected at least 2 different trail signatures, got {signatures.Count}");
}

[Fact]
public void SelectedStartingTownMustBeInGeneratedWorld()
{
    var seedWorld = SeedWorldResolver.Resolve(Guid.NewGuid());
    var source = new GameSetupDeterministicSource(seedWorld.SeedCodeText);
    var world = SeedWorldBuilder.CreateWorld(seedWorld, source);
    var nonSelectedTown = SeedWorldCatalog.AllTowns.First(t => !seedWorld.SelectedTownIds.Contains(t.Id));
    Assert.Throws<ArgumentException>(() =>
        StartingTownPolicy.ResolveStartingTown(world, new TownId(nonSelectedTown.Id)));
}
```

- [ ] **Step 2: Run tests to verify they pass**
- [ ] **Step 3: Commit**

---

## Task 7: Update TravelTestSeedCatalog and guardrail tests

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalog.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalogGuardrailTests.cs`

- [ ] **Step 1: Update TravelTestSeedCatalog**

Remove all `WorldTownSetAlternate` / `WorldTownSetDefault` references. All entries use `SeedWorldResolver.CreateCanonicalSeedWorld()` (which selects all 8 towns). Remove entries that depended on the alternate town set having different towns — they now all use the canonical world. Update `SeedWorldEntry` to use the new `SeedWorld` shape.

- [ ] **Step 2: Update TravelTestSeedCatalogGuardrailTests**

Remove tests referencing `TownSetKey` or `WorldTownSetAlternate`. Add guardrail test proving canonical seed world has all 8 towns and all 9 trails.

- [ ] **Step 3: Run tests to verify they pass**
- [ ] **Step 4: Commit**

---

## Task 8: Update GameSetupResolverTests and SeededNewGameFactoryTests

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`

- [ ] **Step 1: Update GameSetupResolverTests**

Replace all `SeedWorld` construction using `TownSetKey` with `SeedWorldResolver.Resolve(seedCode)` or `SeedWorldResolver.CreateCanonicalSeedWorld()`. Update `CashBonusIsCappedByEntropyPolicy` to construct a SeedWorld with `SelectedTownIds` and `Trails` from the resolver.

- [ ] **Step 2: Update SeededNewGameFactoryTests**

Update `CreateSeedCode` helper to use the new API (no `townSetKey` parameter). Update tests that referenced `TownSetKey`.

- [ ] **Step 3: Run tests to verify they pass**
- [ ] **Step 4: Commit**

---

## Task 9: Update integration tests

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalog.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalogTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/BoringScenarioBuilderTests.cs`

- [ ] **Step 1: Update ScenarioSeedCatalog**

Update shape signatures to remove `TownSetKey` references. The canonical seed world still produces all 8 towns and all 9 trails, so route signatures should stay the same for canonical-world scenarios.

- [ ] **Step 2: Update GameApiTests**

The canonical seed world still starts in pinecross with the same connected towns, so most tests should pass without changes. Check for any references to `TownSetKey` or alternate town sets.

- [ ] **Step 3: Run integration tests to verify they pass**
- [ ] **Step 4: Commit**

---

## Task 10: Update docs, plan, PR body

**Files:**
- Modify: `AGENTS.md` (root)
- Modify: `src/WildBunch.GameContent/AGENTS.md`
- Modify: `.agents/superpowers/plans/2026-06-28-bunch-107-seed-codec-adventure-template-setup.md`
- Modify: `.agents/superpowers/output/pr_body_bunch107_impl.txt`

- [ ] **Step 1: Update AGENTS.md files**

Remove references to `TownSetKey`, `WorldTownSetDefault`, `WorldTownSetAlternate`. Document the seed-derived town selection model: seed determines town count (6-8), selects towns from catalog (anchor towns always included), builds trail graph with terrain/water/distance from catalog.

- [ ] **Step 2: Update plan file**

Mark the two-set approach as replaced. Update the SeedWorld seam definition to show `SelectedTownIds` and `Trails`.

- [ ] **Step 3: Update PR body**

Document the seed-derived town selection model. Remove references to "two canned sets" or "transitional seam proof."

- [ ] **Step 4: Commit**

---

## Task 11: Full validation

- [ ] **Step 1: Run `dotnet build`**
- [ ] **Step 2: Run `.\scripts\postgres-dev.ps1 test -- dotnet test --no-build`**
- [ ] **Step 3: Run `python scripts/generate_index_mesh.py --check`**
- [ ] **Step 4: Run `dotnet ef migrations list`**
- [ ] **Step 5: Commit and force-push**
