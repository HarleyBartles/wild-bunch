using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class OutlierGuaranteeTests
{
    [Fact]
    public void Enforce_NoOutlierSlot_ReturnsInputUnchanged()
    {
        var towns = new Dictionary<int, (int X, int Y)> { { 0, (100, 100) }, { 1, (200, 100) } };
        var trails = new List<SeedWorldTrail>
        {
            new("trail-0-1", "t0", "t1", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m)
        };

        var (resultTrails, resultTowns) = OutlierGuarantee.Enforce(trails, towns, outlierSlot: null, townIds: new[] { "t0", "t1" });

        Assert.Same(trails, resultTrails);
        Assert.Same(towns, resultTowns);
    }

    [Fact]
    public void Enforce_OutlierWithSingleIncidentTrail_KeepsTrailAt6Days()
    {
        // Outlier town at slot 1, 150px from slot 0 → already 6 ride-days.
        var towns = new Dictionary<int, (int X, int Y)> { { 0, (100, 100) }, { 1, (250, 100) } };
        var trails = new List<SeedWorldTrail>
        {
            new("trail-0-1", "t0", "t1", TrailRisk.High, TrailTerrain.Mountains, WaterFeature.None, 6m)
        };

        var (resultTrails, resultTowns) = OutlierGuarantee.Enforce(trails, towns, outlierSlot: 1, townIds: new[] { "t0", "t1" });

        Assert.Single(resultTrails);
        Assert.Equal(6m, resultTrails[0].RideDayDistance);
    }

    [Fact]
    public void Enforce_OutlierWithMultipleIncidentTrails_KeepsOnlyShortestAndEnforces6Days()
    {
        // Outlier at slot 2 with two incident trails.
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) }, { 1, (200, 100) }, { 2, (250, 100) }
        };
        var trails = new List<SeedWorldTrail>
        {
            new("trail-0-1", "t0", "t1", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
            new("trail-0-2", "t0", "t2", TrailRisk.High, TrailTerrain.Mountains, WaterFeature.None, 6m),
            new("trail-1-2", "t1", "t2", TrailRisk.High, TrailTerrain.Mountains, WaterFeature.None, 2m) // shorter
        };

        var (resultTrails, resultTowns) = OutlierGuarantee.Enforce(trails, towns, outlierSlot: 2, townIds: new[] { "t0", "t1", "t2" });

        // Only the shortest incident trail should remain on the outlier.
        var outlierIncident = resultTrails.Where(t => t.FromTownId == "t2" || t.ToTownId == "t2").ToList();
        Assert.Single(outlierIncident);
        Assert.Equal(6m, outlierIncident[0].RideDayDistance);

        // The non-outlier trail must survive.
        Assert.Contains(resultTrails, t => t.Id == "trail-0-1");
    }

    [Fact]
    public void Enforce_OutlierTrailNotExactly6Days_AdjustsCoordinatesTo150px()
    {
        // Outlier at slot 1, 100px (4 ride-days) from slot 0 — needs adjustment to 150px.
        var towns = new Dictionary<int, (int X, int Y)> { { 0, (400, 250) }, { 1, (500, 250) } };
        var trails = new List<SeedWorldTrail>
        {
            new("trail-0-1", "t0", "t1", TrailRisk.High, TrailTerrain.Mountains, WaterFeature.None, 4m)
        };

        var (resultTrails, resultTowns) = OutlierGuarantee.Enforce(trails, towns, outlierSlot: 1, townIds: new[] { "t0", "t1" });

        Assert.Single(resultTrails);
        Assert.Equal(6m, resultTrails[0].RideDayDistance);

        // Verify the outlier's coordinates were moved to exactly 150px from its connected neighbor.
        var outlier = resultTowns[1];
        var neighbor = resultTowns[0];
        var dx = outlier.X - neighbor.X;
        var dy = outlier.Y - neighbor.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        Assert.Equal(150.0, distance, 1); // within 1px tolerance from integer rounding
    }
}
