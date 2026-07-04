using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class TerrainAssignerTests
{
    private static IReadOnlyList<SeedWorldTrail> Assign(
        IReadOnlyList<TrailEdge> edges,
        Dictionary<int, (int X, int Y)> towns,
        Dictionary<int, int> clusters,
        SeedWorldVariant variant,
        IReadOnlyList<string> townIds)
        => TerrainAssigner.Assign(edges, towns, clusters, variant, townIds,
            new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode())),
            SaltSource.CreateFixed("test"),
            outlierSlot: null);

    [Fact]
    public void Assign_NormalTrails_AreIn2To8DayRange()
    {
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) }, { 1, (200, 100) }, { 2, (300, 100) }, { 3, (200, 250) }
        };
        var clusters = new Dictionary<int, int> { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 1 } };
        var edges = new List<TrailEdge>
        {
            new(0, 1, 100), new(1, 2, 100), new(1, 3, 150)
        };
        var townIds = new[] { "t0", "t1", "t2", "t3" };

        var trails = Assign(edges, towns, clusters, SeedWorldVariant.Canonical, townIds);

        // 100px = 4 days, 150px = 6 days — honest 25px/day scale, no bottom clamp.
        Assert.All(trails, t => Assert.InRange(t.RideDayDistance, 2m, 8m));
    }

    [Fact]
    public void Assign_IntraClusterEdge_GetsEasierOrEqualTerrainThanInterCluster()
    {
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) }, { 1, (130, 100) }, { 2, (400, 100) }
        };
        var clusters = new Dictionary<int, int> { { 0, 0 }, { 1, 0 }, { 2, 1 } };
        var edges = new List<TrailEdge>
        {
            new(0, 1, 30),   // Intra-cluster
            new(1, 2, 270)   // Inter-cluster (long)
        };
        var townIds = new[] { "t0", "t1", "t2" };

        var trails = Assign(edges, towns, clusters, SeedWorldVariant.Canonical, townIds);

        var intra = trails.Single(t => t.FromTownId == "t0" && t.ToTownId == "t1");
        var inter = trails.Single(t => t.FromTownId == "t1" && t.ToTownId == "t2");

        // Intra-cluster trails should generally be easier than inter-cluster long
        // trails. Terrain/risk/water are now probabilistic per-trail, so we assert
        // the structural relationship: intra risk <= inter risk.
        Assert.True(intra.Risk <= inter.Risk,
            $"Intra-cluster risk ({intra.Risk}) should be <= inter-cluster risk ({inter.Risk}).");
    }

    [Fact]
    public void Assign_InterClusterShortEdge_GetsValidTerrainAndRisk()
    {
        // 60px = 2.4 ride-days, which is <= 4 days → "short" inter-cluster
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) }, { 1, (160, 100) }
        };
        var clusters = new Dictionary<int, int> { { 0, 0 }, { 1, 1 } };
        var edges = new List<TrailEdge> { new(0, 1, 60) };
        var townIds = new[] { "t0", "t1" };

        var trails = Assign(edges, towns, clusters, SeedWorldVariant.Canonical, townIds);

        var inter = trails.Single();
        // Short inter-cluster trails get a mix of Badlands/Hills/OpenRange terrain
        // and Moderate/Low/High risk. Just assert the terrain is a valid enum value
        // and the ride-day distance is correct.
        Assert.True(Enum.IsDefined(inter.Terrain), $"Invalid terrain: {inter.Terrain}");
        Assert.True(Enum.IsDefined(inter.Risk), $"Invalid risk: {inter.Risk}");
        Assert.Equal(2m, inter.RideDayDistance);
    }

    [Fact]
    public void Assign_InterClusterShortEdge_FrontierVariant_GetsValidTerrain()
    {
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) }, { 1, (160, 100) }
        };
        var clusters = new Dictionary<int, int> { { 0, 0 }, { 1, 1 } };
        var edges = new List<TrailEdge> { new(0, 1, 60) };
        var townIds = new[] { "t0", "t1" };

        var trails = Assign(edges, towns, clusters, SeedWorldVariant.Frontier, townIds);

        var inter = trails.Single();
        // Frontier inter-cluster short gets a mix — just assert it's valid.
        Assert.True(Enum.IsDefined(inter.Terrain), $"Invalid terrain: {inter.Terrain}");
    }

    [Fact]
    public void Assign_DistancesMatchPixelGeometry()
    {
        // 100px at 25px/ride-day = 4 ride-days
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) }, { 1, (200, 100) }
        };
        var clusters = new Dictionary<int, int> { { 0, 0 }, { 1, 0 } };
        var edges = new List<TrailEdge> { new(0, 1, 100) };
        var townIds = new[] { "t0", "t1" };

        var trails = Assign(edges, towns, clusters, SeedWorldVariant.Canonical, townIds);

        Assert.Equal(4m, trails.Single().RideDayDistance);
    }
}
