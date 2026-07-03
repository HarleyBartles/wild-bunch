using WildBunch.Domain.World;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

internal static class TerrainAssigner
{
    private const double CoordinateScale = 25.0; // 25px = 1 ride-day
    private const int MinNormalRideDays = 2;
    private const int MaxNormalRideDays = 5;
    private const int InterClusterShortDayThreshold = 4;

    public static IReadOnlyList<SeedWorldTrail> Assign(
        IReadOnlyList<TrailEdge> edges,
        Dictionary<int, (int X, int Y)> towns,
        Dictionary<int, int> clusterAssignments,
        SeedWorldVariant variant,
        IReadOnlyList<string> townIds,
        int? outlierSlot)
    {
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(towns);
        ArgumentNullException.ThrowIfNull(clusterAssignments);
        ArgumentNullException.ThrowIfNull(townIds);

        var trails = new List<SeedWorldTrail>(edges.Count);
        foreach (var edge in edges)
        {
            var fromId = townIds[edge.FromSlot];
            var toId = townIds[edge.ToSlot];
            var isOutlier = outlierSlot.HasValue && (edge.FromSlot == outlierSlot.Value || edge.ToSlot == outlierSlot.Value);
            var (terrain, water, risk, rideDays) = ClassifyEdge(
                edge, clusterAssignments, variant, isOutlier);

            var trailId = $"trail-{edge.FromSlot}-{edge.ToSlot}";
            trails.Add(new SeedWorldTrail(
                trailId, fromId, toId, risk, terrain, water, rideDays));
        }
        return trails;
    }

    private static (TrailTerrain Terrain, WaterFeature Water, TrailRisk Risk, decimal RideDays) ClassifyEdge(
        TrailEdge edge,
        Dictionary<int, int> clusterAssignments,
        SeedWorldVariant variant,
        bool isOutlier)
    {
        if (isOutlier)
        {
            return (TrailTerrain.Mountains, WaterFeature.None, TrailRisk.High, 6m);
        }

        var rawRideDays = edge.PixelDistance / CoordinateScale;
        var rideDays = Math.Clamp(
            Math.Round((decimal)rawRideDays, MidpointRounding.AwayFromZero),
            MinNormalRideDays, MaxNormalRideDays);

        var sameCluster = clusterAssignments.TryGetValue(edge.FromSlot, out var cA)
            && clusterAssignments.TryGetValue(edge.ToSlot, out var cB)
            && cA == cB && cA != -1;

        if (sameCluster)
        {
            var terrain = variant == SeedWorldVariant.Canonical ? TrailTerrain.OpenRange : TrailTerrain.Hills;
            return (terrain, WaterFeature.Creek, TrailRisk.Low, rideDays);
        }

        if (rideDays <= InterClusterShortDayThreshold)
        {
            var terrain = variant == SeedWorldVariant.Canonical ? TrailTerrain.Badlands : TrailTerrain.Hills;
            return (terrain, WaterFeature.None, TrailRisk.Moderate, rideDays);
        }

        return (TrailTerrain.Mountains, WaterFeature.None, TrailRisk.High, rideDays);
    }
}
