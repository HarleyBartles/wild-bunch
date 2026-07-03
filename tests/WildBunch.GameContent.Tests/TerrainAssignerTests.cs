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
        => TerrainAssigner.Assign(edges, towns, clusters, variant, townIds, outlierSlot: null);

    [Fact]
    public void Assign_NormalTrails_AreIn2To5DayRange()
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

        Assert.All(trails, t => Assert.InRange(t.RideDayDistance, 2m, 5m));
    }

    [Fact]
    public void Assign_IntraClusterEdge_GetsEasierTerrainThanInterCluster()
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

        // Intra-cluster (Canonical) = OpenRange/Creek/Low
        Assert.Equal(TrailTerrain.OpenRange, intra.Terrain);
        Assert.Equal(WaterFeature.Creek, intra.WaterFeature);
        Assert.Equal(TrailRisk.Low, intra.Risk);

        // Inter-cluster long (Canonical) = Mountains/None/High
        Assert.Equal(TrailTerrain.Mountains, inter.Terrain);
        Assert.Equal(WaterFeature.None, inter.WaterFeature);
        Assert.Equal(TrailRisk.High, inter.Risk);
    }

    [Fact]
    public void Assign_InterClusterShortEdge_GetsBadlandsForCanonical()
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
        Assert.Equal(TrailTerrain.Badlands, inter.Terrain);
        Assert.Equal(WaterFeature.None, inter.WaterFeature);
        Assert.Equal(TrailRisk.Moderate, inter.Risk);
    }

    [Fact]
    public void Assign_InterClusterShortEdge_GetsHillsForFrontierVariant()
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
        // Frontier inter-cluster short = Hills (variant modulation)
        Assert.Equal(TrailTerrain.Hills, inter.Terrain);
    }

    [Fact]
    public void Assign_OutlierEdge_GetsMountainsAndHighRisk()
    {
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) }, { 1, (250, 100) } // 1 is the outlier at 150px
        };
        var clusters = new Dictionary<int, int> { { 0, 0 }, { 1, -1 } };
        var edges = new List<TrailEdge> { new(0, 1, 150) };
        var townIds = new[] { "t0", "t1" };

        var trails = TerrainAssigner.Assign(edges, towns, clusters, SeedWorldVariant.Canonical, townIds, outlierSlot: 1);

        var outlier = trails.Single();
        Assert.Equal(TrailTerrain.Mountains, outlier.Terrain);
        Assert.Equal(WaterFeature.None, outlier.WaterFeature);
        Assert.Equal(TrailRisk.High, outlier.Risk);
    }

    [Fact]
    public void Assign_OutlierEdge_IsExactly6RideDays()
    {
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) }, { 1, (250, 100) } // 150px = 6 ride-days
        };
        var clusters = new Dictionary<int, int> { { 0, 0 }, { 1, -1 } };
        var edges = new List<TrailEdge> { new(0, 1, 150) };
        var townIds = new[] { "t0", "t1" };

        var trails = TerrainAssigner.Assign(edges, towns, clusters, SeedWorldVariant.Canonical, townIds, outlierSlot: 1);

        Assert.Equal(6m, trails.Single().RideDayDistance);
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
