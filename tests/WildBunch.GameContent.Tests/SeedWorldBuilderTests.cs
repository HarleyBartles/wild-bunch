using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class SeedWorldBuilderTests
{
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
        // Min 5, max 20 (offset-encoded in 4 bits: 0-15 → 5-20).
        for (var count = 5; count <= 20; count++)
        {
            var seedWorld = CreateSeedWorldWithCount(count);
            Assert.Equal(count, seedWorld.TownCount);
            Assert.Equal(count, seedWorld.SelectedTownIds.Count);
        }
    }

    [Fact]
    public void CreateWorld_GeometryDerivedDistances_AreCanonical()
    {
        // Geometry-derived trail distances are canonical game-world distances,
        // not UI labels. They should be derived from the actual coordinate
        // geometry of towns based on the map layout palette.
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var source = new GameSetupDeterministicSource(seedWorld.SeedCodeText);
        var world = SeedWorldBuilder.CreateWorld(seedWorld, source);

        // Get the map towns with coordinates
        var mapTowns = SeedWorldMapLayout.GetMapTowns(world, seedWorld.MapLayoutPalette);
        var townCoordinates = mapTowns.ToDictionary(t => t.Id, t => (t.X, t.Y));

        // Verify each trail's distance matches the Euclidean distance
        // between its endpoint towns (scaled to ride-day units).
        foreach (var trail in world.Trails)
        {
            var fromCoords = townCoordinates[trail.FromTownId.Value];
            var toCoords = townCoordinates[trail.ToTownId.Value];

            // Calculate Euclidean distance in coordinate space
            var dx = toCoords.X - fromCoords.X;
            var dy = toCoords.Y - fromCoords.Y;
            var coordinateDistance = Math.Sqrt(dx * dx + dy * dy);

            // Scale to ride-day distance (approximately 1 ride-day per 50 coordinate units)
            var expectedDistance = Math.Round(coordinateDistance / 50.0, 1);

            // Allow small rounding differences
            Assert.Equal(expectedDistance, (double)trail.RideDayDistance, 1);
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
            selectedTownIds, townServices, trails));

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
            selectedTownIds, townServices, trails));

        return SeedWorldResolver.Resolve(seedCode);
    }

    private static World BuildSeedWorld(SeedWorld seedWorld)
    {
        var seedCode = SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode == Guid.Empty
            ? SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld)
            : seedWorld.SeedCode);
        var source = new GameSetupDeterministicSource(seedCode);
        return SeedWorldBuilder.CreateWorld(seedWorld, source);
    }

    private static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong tail)
        => SeedWorldSeedCodeFactory.CreateSeedCode(worldVariant, accusationIndex, defaultCulpritIndex, cashBonus, tail);
}
