using WildBunch.Domain.World;
using WildBunch.Domain.Travel;
using WildBunch.Domain.Game;

namespace WildBunch.GameContent.NewGame;

internal static class TerrainAssigner
{
    // Universal scale: 25px on the map = 1 ride-day, for every trail type.
    // This keeps the map visually honest — the line length directly tells the
    // player how long the ride will be. No hidden rescaling per cluster type.
    private const double CoordinateScale = 25.0; // 25px = 1 ride-day

    // The minimum town separation (50px) guarantees no trail rounds below 2 days,
    // so there is no bottom clamp. The top clamp caps only genuinely extreme
    // inter-cluster distances on high-cluster-count maps.
    private const int MaxNormalRideDays = 8;

    // Inter-cluster trails at or below this many days are considered "short"
    // crossings (moderate risk) rather than long mountain crossings (high risk).
    private const int InterClusterShortDayThreshold = 4;

    public static IReadOnlyList<SeedWorldTrail> Assign(
        IReadOnlyList<TrailEdge> edges,
        Dictionary<int, (int X, int Y)> towns,
        Dictionary<int, int> clusterAssignments,
        SeedWorldVariant variant,
        IReadOnlyList<string> townIds,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        int? outlierSlot = null)
    {
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(towns);
        ArgumentNullException.ThrowIfNull(clusterAssignments);
        ArgumentNullException.ThrowIfNull(townIds);

        var salt = saltSource?.Salt ?? "default";
        var trails = new List<SeedWorldTrail>(edges.Count);
        foreach (var edge in edges)
        {
            var fromId = townIds[edge.FromSlot];
            var toId = townIds[edge.ToSlot];
            var (terrain, water, risk, rideDays) = ClassifyEdge(
                edge, clusterAssignments, variant, source, salt);

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
        GameSetupDeterministicSource source,
        string salt)
    {
        // Convert pixel distance to ride-days using the universal 25px/day scale.
        // Round to the nearest integer day. No bottom clamp — the 50px minimum
        // town separation guarantees ≥2 days naturally. Top clamp at 8 days caps
        // only extreme inter-cluster distances.
        var rawRideDays = edge.PixelDistance / CoordinateScale;
        var rideDays = Math.Min(
            Math.Round((decimal)rawRideDays, MidpointRounding.AwayFromZero),
            MaxNormalRideDays);

        var sameCluster = clusterAssignments.TryGetValue(edge.FromSlot, out var cA)
            && clusterAssignments.TryGetValue(edge.ToSlot, out var cB)
            && cA == cB && cA != -1;

        // Per-trail deterministic roll for terrain/risk variety. This ensures
        // that trails within the same category (e.g. intra-cluster) don't all
        // get the exact same terrain, making the map visually diverse while
        // remaining deterministic for the same seed+salt.
        var roll = source.Roll($"terrain-{edge.FromSlot}-{edge.ToSlot}-{salt}");

        if (sameCluster)
        {
            // Intra-cluster trails: mostly low-risk, but with some variety.
            // 60% base terrain (variant-dependent), 25% Hills, 15% Badlands.
            var terrain = (roll % 100UL) switch
            {
                < 60 => variant == SeedWorldVariant.Canonical ? TrailTerrain.OpenRange : TrailTerrain.Hills,
                < 85 => TrailTerrain.Hills,
                _ => TrailTerrain.Badlands
            };
            // 85% Low risk, 15% Moderate (some trails are rougher than others)
            var risk = (roll % 100UL) < 85 ? TrailRisk.Low : TrailRisk.Moderate;
            // 70% Creek (well-watered interior), 20% Spring, 10% None
            var water = (roll / 100UL % 10UL) switch
            {
                < 7 => WaterFeature.Creek,
                < 9 => WaterFeature.Spring,
                _ => WaterFeature.None
            };
            return (terrain, water, risk, rideDays);
        }

        if (rideDays <= InterClusterShortDayThreshold)
        {
            // Short inter-cluster crossings: moderate risk with terrain variety.
            // 40% Badlands, 35% Hills, 25% OpenRange (frontier short crossings
            // are more open, canonical ones are harsher).
            var terrain = (roll % 100UL) switch
            {
                < 40 => TrailTerrain.Badlands,
                < 75 => TrailTerrain.Hills,
                _ => TrailTerrain.OpenRange
            };
            // 70% Moderate, 20% Low, 10% High
            var risk = (roll % 100UL) switch
            {
                < 70 => TrailRisk.Moderate,
                < 90 => TrailRisk.Low,
                _ => TrailRisk.High
            };
            // 60% None (drier crossing), 25% Spring, 15% Creek
            var water = (roll / 100UL % 10UL) switch
            {
                < 6 => WaterFeature.None,
                < 85 => WaterFeature.Spring,
                _ => WaterFeature.Creek
            };
            return (terrain, water, risk, rideDays);
        }

        // Long inter-cluster crossings: high risk mountain passes.
        // 70% Mountains, 20% Hills, 10% Badlands (some long crossings skirt
        // badlands rather than going over mountains).
        var longTerrain = (roll % 100UL) switch
        {
            < 70 => TrailTerrain.Mountains,
            < 90 => TrailTerrain.Hills,
            _ => TrailTerrain.Badlands
        };
        // 80% High, 15% Moderate, 5% Low
        var longRisk = (roll % 100UL) switch
        {
            < 80 => TrailRisk.High,
            < 95 => TrailRisk.Moderate,
            _ => TrailRisk.Low
        };
        // 85% None (dry mountain pass), 10% Spring, 5% River
        var longWater = (roll / 100UL % 10UL) switch
        {
            < 85 => WaterFeature.None,
            < 95 => WaterFeature.Spring,
            _ => WaterFeature.River
        };
        return (longTerrain, longWater, longRisk, rideDays);
    }
}
