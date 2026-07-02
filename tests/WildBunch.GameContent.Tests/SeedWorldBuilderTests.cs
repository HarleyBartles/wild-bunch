using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldBuilderTests
{
    [Fact]
    public void OutlierSlot_ActivatesBasedOnEntropy()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var seedWorldWithOutlier = seedWorld with { OutlierSlotType = 1 }; // Simple outlier
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorldWithOutlier.SeedCode));

        // Boring should not activate outlier
        var boringWorld = SeedWorldBuilder.CreateWorld(seedWorldWithOutlier, source, GameEntropy.Boring);
        Assert.Equal(seedWorld.TownCount, boringWorld.Towns.Count);
        Assert.DoesNotContain(boringWorld.Towns, t => t.IsOutlier);

        // Wild should activate outlier
        var wildWorld = SeedWorldBuilder.CreateWorld(seedWorldWithOutlier, source, GameEntropy.Wild);
        Assert.Equal(seedWorld.TownCount + 1, wildWorld.Towns.Count);
        var outlier = wildWorld.Towns.First(t => t.IsOutlier);
        Assert.NotNull(outlier);

        // Verify outlier trail is exactly 6 days
        var outlierTrails = wildWorld.Trails.Where(t => t.FromTownId.Value == outlier.Id.Value || t.ToTownId.Value == outlier.Id.Value).ToList();
        Assert.Single(outlierTrails);
        var outlierTrail = outlierTrails[0];
        Assert.Equal(6m, outlierTrail.RideDayDistance);

        // Verify all town IDs are unique
        var townIds = wildWorld.Towns.Select(t => t.Id.Value).ToList();
        Assert.True(townIds.Count == townIds.Distinct().Count(), "All town IDs must be unique");

        // Verify all trails point to real towns
        var townIdSet = wildWorld.Towns.Select(t => t.Id.Value).ToHashSet();
        foreach (var trail in wildWorld.Trails)
        {
            Assert.True(townIdSet.Contains(trail.FromTownId.Value), $"Trail {trail.Id} FromTownId {trail.FromTownId.Value} does not exist in towns");
            Assert.True(townIdSet.Contains(trail.ToTownId.Value), $"Trail {trail.Id} ToTownId {trail.ToTownId.Value} does not exist in towns");
        }
    }

    [Fact]
    public void CreateCanonicalWorldProducesEightTownsFromNamePool()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();

        Assert.Equal(8, world.Towns.Count);
        Assert.Equal(14, world.Trails.Count);

        // Every town name must come from the name pool.
        var poolIds = SeedWorldCatalog.NamePool.Select(n => n.Id).ToHashSet();
        foreach (var town in world.Towns)
        {
            Assert.Contains(town.Id.Value, poolIds);
        }
    }

    [Fact]
    public void CreateCanonicalWorldAppliesUniformProsperousPalette()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();

        foreach (var town in world.Towns)
        {
            Assert.Equal(TownProsperity.Prosperous, town.Prosperity);
        }
    }

    [Fact]
    public void CreateCanonicalWorldAppliesHubTelegraphServicesPalette()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();
        var townsByIndex = world.Towns.OrderBy(t => t.Id.Value, StringComparer.OrdinalIgnoreCase).ToArray();

        // HubTelegraph: only slot 0 has telegraph. But slot assignment is by
        // position in the derived name list, not by sorted order. We verify
        // that exactly one town has telegraph.
        var telegraphTowns = world.Towns.Where(t => t.Services.HasFlag(TownServices.Telegraph)).ToArray();
        Assert.Single(telegraphTowns);
    }

    [Fact]
    public void FrontierVariantProducesDifferentTerrainThanCanonical()
    {
        var canonicalWorld = BuildSeedWorld(SeedWorldResolver.Resolve(CreateSeedCode(0, 1, 3, 0, tail: 0)));
        var frontierWorld = BuildSeedWorld(SeedWorldResolver.Resolve(CreateSeedCode(1, 1, 3, 0, tail: 0)));

        // Frontier variant should produce at least one different terrain/water combo.
        var canonicalSignature = string.Join("|", canonicalWorld.Trails
            .OrderBy(t => t.Id.Value)
            .Select(t => $"{t.Terrain}/{t.WaterFeature}/{t.RideDayDistance}"));
        var frontierSignature = string.Join("|", frontierWorld.Trails
            .OrderBy(t => t.Id.Value)
            .Select(t => $"{t.Terrain}/{t.WaterFeature}/{t.RideDayDistance}"));

        Assert.NotEqual(canonicalSignature, frontierSignature);
    }

    [Fact]
    public void DifferentSeedsCanProduceDifferentTownNames()
    {
        // Different encoded fields produce different name shuffles, so different
        // town name selections. We use two different cash bonus values to get
        // different derivation seeds.
        var seedA = CreateSeedCode(0, 1, 3, 0, tail: 0);
        var seedB = CreateSeedCode(0, 1, 3, 3, tail: 0);

        var namesA = string.Join(",", SeedWorldResolver.Resolve(seedA).SelectedTownIds.OrderBy(id => id));
        var namesB = string.Join(",", SeedWorldResolver.Resolve(seedB).SelectedTownIds.OrderBy(id => id));

        Assert.NotEqual(namesA, namesB);
    }

    [Fact]
    public void SameSeedProducesSameWorld()
    {
        var seed = SeedWorldResolver.CreateCanonicalSeedCode();
        var seedWorld = SeedWorldResolver.Resolve(seed);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seed));
        var world1 = SeedWorldBuilder.CreateWorld(seedWorld, source);
        var world2 = SeedWorldBuilder.CreateWorld(seedWorld, source);
        Assert.Equal(world1.Towns.Count, world2.Towns.Count);
        Assert.Equal(world1.Trails.Count, world2.Trails.Count);
    }

    [Fact]
    public void SelectedStartingTownMustBeInGeneratedWorld()
    {
        // Starting town is NOT seed-owned but must be in the generated world.
        var seed = SeedWorldResolver.CreateCanonicalSeedCode();
        var seedWorld = SeedWorldResolver.Resolve(seed);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));
        var world = SeedWorldBuilder.CreateWorld(seedWorld, source);

        // Pick a name from the pool that is NOT in the world.
        var poolIds = SeedWorldCatalog.NamePool.Select(n => n.Id).ToHashSet();
        var worldIds = world.Towns.Select(t => t.Id.Value).ToHashSet();
        var nonSelectedId = poolIds.First(id => !worldIds.Contains(id));

        Assert.Throws<ArgumentException>(() =>
            StartingTownPolicy.ResolveStartingTown(world, new TownId(nonSelectedId)));
    }

    [Fact]
    public void BuilderCreatesWorldFromSeedWorldTemplate()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var source = new GameSetupDeterministicSource(seedWorld.SeedCodeText);
        var world = SeedWorldBuilder.CreateWorld(seedWorld, source);
        Assert.Equal(seedWorld.TownCount, world.Towns.Count);
        Assert.Equal(seedWorld.Trails.Count, world.Trails.Count);
    }

    [Fact]
    public void DeriveTownNamesWithAllZeroFields_ProducesNonTrivialShuffle()
    {
        // Boundary: when all encoded fields are 0, the xorshift seed would be 0.
        // xorshift32 has 0 as a fixed point (produces all zeros), so the shuffle
        // would be a no-op. The guard (seed=0 → seed=1) ensures a non-trivial
        // shuffle. Verify the first town is NOT just the first pool entry.
        var townNames = SeedWorldCatalog.DeriveTownNames(
            SeedWorldVariant.Canonical,
            townCount: 5,
            accusationIndex: 0,
            defaultCulpritIndex: 0,
            cashBonus: 0,
            ProsperityPalette.UniformProsperous,
            ServicesPalette.HubTelegraph,
            MapLayoutPalette.HubAndSpoke);

        Assert.Equal(5, townNames.Count);
        // The shuffle must not be a no-op — at least one town must differ from
        // the natural pool order.
        var poolIds = SeedWorldCatalog.NamePool.Select(n => n.Id).ToArray();
        var anyDiffer = townNames.Any(t => t.Id != poolIds[Array.IndexOf(townNames.ToArray(), t)]);
        Assert.True(anyDiffer || townNames[0].Id != poolIds[0],
            "DeriveTownNames must produce a non-trivial shuffle even when all encoded fields are 0.");
    }

    [Fact]
    public void StartingTownPolicyDefaultsToFirstTown()
    {
        // Starting town is NOT seed-owned. The safe default from StartingTownPolicy
        // is the first town in the world (slot 0), which is always present.
        var canonicalWorld = SeedWorldBuilder.CreateCanonicalWorld();
        var defaultTown = StartingTownPolicy.ResolveStartingTown(canonicalWorld, null);
        Assert.Contains(canonicalWorld.Towns, t => t.Id.Equals(defaultTown));
    }

    [Fact]
    public void StartingTownPolicyAcceptsAnyValidTownChoice()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();
        var chosenTown = world.Towns.First();

        var resolved = StartingTownPolicy.ResolveStartingTown(world, chosenTown.Id);
        Assert.Equal(chosenTown.Id, resolved);
    }

    [Fact]
    public void StartingTownPolicyRejectsInvalidTownChoice()
    {
        var world = SeedWorldBuilder.CreateCanonicalWorld();

        Assert.Throws<ArgumentException>(() =>
            StartingTownPolicy.ResolveStartingTown(world, new TownId("nonexistent-town")));
    }

    [Fact]
    public void ProsperityPaletteAppliesToAllTowns()
    {
        // Build a world with BoomtownHub palette: slot 0 = Boomtown, rest = Prosperous.
        var seedWorld = CreateSeedWorldWithPalettes(ProsperityPalette.BoomtownHub, ServicesPalette.NoTelegraph);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));
        var world = SeedWorldBuilder.CreateWorld(seedWorld, source);

        var towns = world.Towns.ToArray();
        Assert.Equal(TownProsperity.Boomtown, towns[0].Prosperity);
        for (var i = 1; i < towns.Length; i++)
        {
            Assert.Equal(TownProsperity.Prosperous, towns[i].Prosperity);
        }
    }

    [Fact]
    public void ServicesPaletteAppliesToAllTowns()
    {
        // Build a world with AllTelegraph palette: every town has telegraph.
        var seedWorld = CreateSeedWorldWithPalettes(ProsperityPalette.UniformProsperous, ServicesPalette.AllTelegraph);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));
        var world = SeedWorldBuilder.CreateWorld(seedWorld, source);

        foreach (var town in world.Towns)
        {
            Assert.True(town.Services.HasFlag(TownServices.Telegraph));
        }
    }

    [Fact]
    public void TownCountRespectsMinAndMax()
    {
        // Min 5, max 10 (offset-encoded in 4 bits: 0-15 → 5-20, wrapped to 5-10 via modulo).
        for (var count = 5; count <= 10; count++)
        {
            var seedWorld = CreateSeedWorldWithCount(count);
            Assert.Equal(count, seedWorld.TownCount);
            Assert.Equal(count, seedWorld.SelectedTownIds.Count);
        }
    }

    [Fact]
    public void CreateWorld_GeometryDerivedDistances_AreCanonical()
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));
        var world = SeedWorldBuilder.CreateWorld(seedWorld, source, WildBunch.Domain.Travel.GameEntropy.Boring);

        // All trails should have geometry-derived distances (2-6 days)
        Assert.All(world.Trails, trail =>
        {
            Assert.InRange(trail.RideDayDistance, 2m, 6m);
        });

        // Distances should be deterministic for the same seed
        var world2 = SeedWorldBuilder.CreateWorld(seedWorld, source, WildBunch.Domain.Travel.GameEntropy.Boring);
        foreach (var trail in world.Trails)
        {
            var matchingTrail = world2.Trails.First(t => t.Id == trail.Id);
            Assert.Equal(trail.RideDayDistance, matchingTrail.RideDayDistance);
        }
    }

    private static SeedWorld CreateSeedWorldWithPalettes(ProsperityPalette prosperity, ServicesPalette services)
    {
        var variant = SeedWorldVariant.Canonical;
        var townCount = 8;
        var accusationIndex = 1;
        var defaultCulpritIndex = 3;
        var cashBonus = 0;
        var mapLayout = MapLayoutPalette.HubAndSpoke;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperity, services, mapLayout);
        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(services, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = SeedWorldCatalog.BuildTrails(variant, townNames, mapLayout);

        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(new SeedWorld(
            Guid.Empty, variant, townCount, services, prosperity, mapLayout,
            accusationIndex, defaultCulpritIndex, cashBonus,
            selectedTownIds, townServices, trails, OutlierSlotType: 0));

        return SeedWorldResolver.Resolve(seedCode);
    }

    private static SeedWorld CreateSeedWorldWithCount(int townCount)
    {
        var variant = SeedWorldVariant.Canonical;
        var accusationIndex = 1;
        var defaultCulpritIndex = 3;
        var cashBonus = 0;
        var prosperity = ProsperityPalette.UniformProsperous;
        var services = ServicesPalette.HubTelegraph;
        var mapLayout = MapLayoutPalette.HubAndSpoke;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperity, services, mapLayout);
        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(services, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = SeedWorldCatalog.BuildTrails(variant, townNames, mapLayout);

        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(new SeedWorld(
            Guid.Empty, variant, townCount, services, prosperity, mapLayout,
            accusationIndex, defaultCulpritIndex, cashBonus,
            selectedTownIds, townServices, trails, OutlierSlotType: 0));

        return SeedWorldResolver.Resolve(seedCode);
    }

    [Fact]
    public void CreateWorld_WildEntropy_TrimOutlierTrails_MaintainsConnectivity()
    {
        // Create a seed world with many towns to have potential outliers
        var seedWorld = CreateSeedWorldWithCount(10);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));

        // Create worlds with different entropy levels
        var boringWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Boring);
        var wildWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Wild);

        // Wild entropy should NOT remove any towns — all towns remain present
        Assert.Equal(boringWorld.Towns.Count, wildWorld.Towns.Count);

        // Verify connectivity is maintained (all towns should be reachable)
        var townIds = wildWorld.Towns.Select(t => t.Id).ToHashSet();
        var adjacency = BuildAdjacencyList(wildWorld.Trails);

        // Check that every town is reachable from every other town
        foreach (var startTown in wildWorld.Towns)
        {
            var reachable = GetReachableTowns(startTown.Id, adjacency);
            Assert.Equal(townIds, reachable);
        }

        // Verify that trails are trimmed appropriately (fewer trails than boring
        // if outlier trail trimming removed trails, but all towns remain)
        Assert.True(wildWorld.Trails.Count > 0, "At least some trails should remain after trimming");
    }

    [Fact]
    public void CreateWorld_NonBoringEntropy_TrimsOutlierTrails_NotTowns()
    {
        // Create a seed world with many towns
        var seedWorld = CreateSeedWorldWithCount(10);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));

        // Test with different entropy levels
        var boringWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Boring);
        var classicWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Classic);
        var adventurousWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Adventurous);

        // No towns should be removed — all entropy modes keep the same town count
        Assert.Equal(10, boringWorld.Towns.Count);
        Assert.Equal(10, classicWorld.Towns.Count);
        Assert.Equal(10, adventurousWorld.Towns.Count);

        // Trails may be trimmed (fewer than boring) but towns are never removed
        Assert.True(boringWorld.Trails.Count >= classicWorld.Trails.Count);
        Assert.True(boringWorld.Trails.Count >= adventurousWorld.Trails.Count);
    }

    [Fact]
    public void CreateWorld_EntropicModes_TrimOutlierTrails_MaintainsConnectivity()
    {
        // Create a seed world with many towns to have potential outliers
        var seedWorld = CreateSeedWorldWithCount(10);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));

        // Create worlds with different entropy levels
        var boringWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Boring);
        var classicWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Classic);
        var adventurousWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Adventurous);
        var wildWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Wild);

        // Entropic modes should NOT remove any towns — all towns remain present
        Assert.Equal(boringWorld.Towns.Count, classicWorld.Towns.Count);
        Assert.Equal(boringWorld.Towns.Count, adventurousWorld.Towns.Count);
        Assert.Equal(boringWorld.Towns.Count, wildWorld.Towns.Count);

        // Verify connectivity is maintained for all entropic modes
        foreach (var world in new[] { classicWorld, adventurousWorld, wildWorld })
        {
            var townIds = world.Towns.Select(t => t.Id).ToHashSet();
            var adjacency = BuildAdjacencyList(world.Trails);

            // Check that every town is reachable from every other town
            foreach (var startTown in world.Towns)
            {
                var reachable = GetReachableTowns(startTown.Id, adjacency);
                Assert.True(townIds.SetEquals(reachable),
                    $"Connectivity should be maintained for {world.Towns.Count} towns");
            }

            // Verify that trails are trimmed appropriately
            Assert.True(world.Trails.Count > 0, "At least some trails should remain after trimming");
        }
    }

    [Fact]
    public void CreateWorld_BoringMode_DoesNotTrimOutlierTowns()
    {
        // Create a seed world with many towns
        var seedWorld = CreateSeedWorldWithCount(10);
        var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));

        // Test with Boring entropy
        var boringWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Boring);

        // Boring entropy should not trim towns - all towns should be present
        Assert.Equal(10, boringWorld.Towns.Count);

        // Trails should be the full graph for Boring entropy
        var expectedTrailCount = SeedWorldCatalog.BuildTrails(
            seedWorld.WorldVariant,
            SeedWorldCatalog.DeriveTownNames(
                seedWorld.WorldVariant,
                seedWorld.TownCount,
                seedWorld.AccusationIndex,
                seedWorld.DefaultCulpritIndex,
                seedWorld.CashBonus,
                seedWorld.ProsperityPalette,
                seedWorld.ServicesPalette,
                seedWorld.MapLayoutPalette),
            seedWorld.MapLayoutPalette).Count;

        Assert.Equal(expectedTrailCount, boringWorld.Trails.Count);
    }

    private static Dictionary<TownId, HashSet<TownId>> BuildAdjacencyList(IReadOnlyList<Trail> trails)
    {
        var adjacency = new Dictionary<TownId, HashSet<TownId>>();
        foreach (var trail in trails)
        {
            if (!adjacency.ContainsKey(trail.FromTownId))
                adjacency[trail.FromTownId] = new HashSet<TownId>();
            if (!adjacency.ContainsKey(trail.ToTownId))
                adjacency[trail.ToTownId] = new HashSet<TownId>();

            adjacency[trail.FromTownId].Add(trail.ToTownId);
            adjacency[trail.ToTownId].Add(trail.FromTownId);
        }
        return adjacency;
    }

    private static HashSet<TownId> GetReachableTowns(TownId start, Dictionary<TownId, HashSet<TownId>> adjacency)
    {
        var visited = new HashSet<TownId>();
        var queue = new Queue<TownId>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current))
                continue;

            visited.Add(current);
            if (adjacency.ContainsKey(current))
            {
                foreach (var neighbor in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                        queue.Enqueue(neighbor);
                }
            }
        }

        return visited;
    }

    private static World BuildSeedWorld(SeedWorld seedWorld, GameEntropy entropy = GameEntropy.Boring)
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode == Guid.Empty
            ? SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld)
            : seedWorld.SeedCode);
        var source = new GameSetupDeterministicSource(seedCode);
        return SeedWorldBuilder.CreateWorld(seedWorld, source, entropy);
    }

    private static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, accusationIndex, defaultCulpritIndex, cashBonus, tail);


}
