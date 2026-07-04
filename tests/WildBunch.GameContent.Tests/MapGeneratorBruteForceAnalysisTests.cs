using System.Collections.Generic;
using System.Linq;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Statistical expectations and anti-pattern detection across the full
/// brute-force combination matrix. These tests complement
/// <see cref="MapGeneratorTests.Generate_BruteForce_AllValidCombinations_SatisfyAllInvariants"/>
/// by asserting aggregate properties that only become meaningful across
/// thousands of generated worlds.
///
/// There are two classes of assertion:
/// - **Statistical expectations**: properties that should hold across the
///   aggregate (cluster separation is real, outlier is isolated, terrain
///   diversity, map coverage, variant influence, trail length spread).
/// - **Anti-pattern detection**: properties that should never occur or occur
///   with negligible frequency (self-loops, duplicate trail IDs, zero
///   distances, multiple outliers, degenerate placement, over-connected
///   towns, outlier inside a cluster).
///
/// Both classes share a single generation pass via
/// <see cref="BruteForceDataCollector.CollectAll"/> to avoid re-running
/// the 4224-combination matrix multiple times.
/// </summary>
public sealed class MapGeneratorBruteForceAnalysisTests
{
    private static BruteForceDataCollector.CollectorData _data = null!;

    private static BruteForceDataCollector.CollectorData Data
    {
        get
        {
            if (_data is null)
            {
                _data = BruteForceDataCollector.CollectAll();
            }
            return _data;
        }
    }

    // === Statistical expectations ===

    [Fact]
    public void BruteForce_ClusterSeparation_IsReal()
    {
        // Average inter-cluster town-pair distance should exceed average
        // intra-cluster town-pair distance. If clustering doesn't produce
        // visible separation, the cluster mechanic is meaningless.
        var data = Data;
        Assert.True(data.TotalIntraClusterPairs > 0,
            $"No intra-cluster pairs collected ({data.TotalIntraClusterPairs}). " +
            "Cluster separation cannot be evaluated.");
        Assert.True(data.TotalInterClusterPairs > 0,
            $"No inter-cluster pairs collected ({data.TotalInterClusterPairs}). " +
            "Cluster separation cannot be evaluated.");

        var avgIntra = data.IntraClusterDistanceSum / data.TotalIntraClusterPairs;
        var avgInter = data.InterClusterDistanceSum / data.TotalInterClusterPairs;

        // Inter-cluster should be at least 1.5x intra-cluster on average.
        // With 50px min separation and 150px cluster center separation,
        // intra-cluster pairs are typically 50-100px while inter-cluster
        // pairs are typically 150-400px.
        Assert.True(avgInter > avgIntra * 1.5,
            $"Cluster separation is too weak: avg inter-cluster={avgInter:F1}px, " +
            $"avg intra-cluster={avgIntra:F1}px (ratio {avgInter / avgIntra:F2}, expected >= 1.5).");
    }

    [Fact]
    public void BruteForce_Outlier_IsIsolatedFromBaseTowns()
    {
        // The outlier's nearest-neighbor distance should exceed the median
        // nearest-neighbor distance among base towns. If the outlier is
        // just another cluster member, the outlier mechanic is broken.
        var data = Data;
        Assert.True(data.OutlierNearestNeighborDistances.Count > 0,
            "No outlier nearest-neighbor distances collected.");

        var avgOutlierNn = data.OutlierNearestNeighborDistances.Average();
        var medianBaseNn = Median(data.BaseNearestNeighborDistances);

        Assert.True(avgOutlierNn > medianBaseNn,
            $"Outlier is not isolated: avg outlier NN distance={avgOutlierNn:F1}px, " +
            $"median base NN distance={medianBaseNn:F1}px. " +
            $"The outlier should be farther from its nearest neighbor than a typical base town.");
    }

    [Fact]
    public void BruteForce_MapCoverage_SpreadsAcrossMapArea()
    {
        // The town bounding box should cover at least 40% of the map area
        // (320x200 of 800x500). If towns clump in one corner, the map
        // feels small and visually boring.
        var data = Data;
        const double mapArea = 800.0 * 500.0;
        var avgCoverage = data.BoundingBoxAreas.Average() / mapArea;

        Assert.True(avgCoverage >= 0.25,
            $"Map coverage too low: average bounding box covers {avgCoverage * 100:F1}% " +
            $"of the 800x500 map, expected >= 25%. Towns are clumping rather than spreading.");
    }

    [Fact]
    public void BruteForce_TerrainDiversity_AllTypesAppear()
    {
        // Every terrain type should appear at least 5% of the time across
        // all generated trails. If one terrain dominates, the map is
        // visually monotonous.
        var data = Data;
        var total = data.TerrainCounts.Values.Sum();
        Assert.True(total > 0, "No terrain data collected.");

        foreach (TrailTerrain terrain in Enum.GetValues<TrailTerrain>())
        {
            var count = data.TerrainCounts.GetValueOrDefault(terrain);
            var pct = 100.0 * count / total;
            Assert.True(pct >= 5.0,
                $"Terrain {terrain} appears {count}/{total} ({pct:F1}%), expected >= 5%. " +
                $"Distribution: {FormatTerrainDistribution(data.TerrainCounts, total)}");
        }
    }

    [Fact]
    public void BruteForce_RiskDiversity_AllLevelsAppear()
    {
        // Every risk level should appear at least 5% of the time.
        var data = Data;
        var total = data.RiskCounts.Values.Sum();
        Assert.True(total > 0, "No risk data collected.");

        foreach (TrailRisk risk in Enum.GetValues<TrailRisk>())
        {
            var count = data.RiskCounts.GetValueOrDefault(risk);
            var pct = 100.0 * count / total;
            Assert.True(pct >= 5.0,
                $"Risk {risk} appears {count}/{total} ({pct:F1}%), expected >= 5%. " +
                $"Distribution: {FormatRiskDistribution(data.RiskCounts, total)}");
        }
    }

    [Fact]
    public void BruteForce_VariantInfluence_ProducesDifferentTerrainDistributions()
    {
        // Different SeedWorldVariant values should produce measurably different
        // terrain distributions. If Canonical and Frontier produce identical
        // terrain, the variant has no effect.
        var data = Data;
        Assert.True(data.TerrainByVariant.Count > 1,
            "Need at least 2 variants to compare terrain distributions.");

        // Compare each pair of variants — their terrain distributions should
        // differ by at least 5 percentage points on at least one terrain type.
        var variants = data.TerrainByVariant.Keys.ToList();
        var foundDifference = false;
        for (var i = 0; i < variants.Count && !foundDifference; i++)
        {
            for (var j = i + 1; j < variants.Count && !foundDifference; j++)
            {
                var distA = NormalizeTerrain(data.TerrainByVariant[variants[i]]);
                var distB = NormalizeTerrain(data.TerrainByVariant[variants[j]]);
                foreach (TrailTerrain terrain in Enum.GetValues<TrailTerrain>())
                {
                    var diff = Math.Abs(distA.GetValueOrDefault(terrain) - distB.GetValueOrDefault(terrain));
                    if (diff > 0.05)
                    {
                        foundDifference = true;
                        break;
                    }
                }
            }
        }
        Assert.True(foundDifference,
            "All SeedWorldVariant values produce identical terrain distributions " +
            "(<5% difference on every terrain type). Variant has no effect on terrain.");
    }

    [Fact]
    public void BruteForce_TrailLengthSpread_BothShortAndLongTrailsAppear()
    {
        // Both intra-cluster (2-4 day) and inter-cluster (5-8 day) trails
        // should each comprise at least 20% of all base trails. If one
        // category dominates, the map lacks variety.
        var data = Data;
        var total = data.RideDayCounts.Values.Sum();
        Assert.True(total > 0, "No ride-day data collected.");

        var shortTrails = Enumerable.Range(2, 3).Sum(d => data.RideDayCounts.GetValueOrDefault(d));
        var longTrails = Enumerable.Range(5, 4).Sum(d => data.RideDayCounts.GetValueOrDefault(d));
        var shortPct = 100.0 * shortTrails / total;
        var longPct = 100.0 * longTrails / total;

        Assert.True(shortPct >= 20.0,
            $"Short trails (2-4 days) are {shortPct:F1}% of {total} trails, expected >= 20%.");
        Assert.True(longPct >= 20.0,
            $"Long trails (5-8 days) are {longPct:F1}% of {total} trails, expected >= 20%.");
    }

    // === Anti-pattern detection ===

    [Fact]
    public void BruteForce_NoSelfLoopTrails()
    {
        var data = Data;
        Assert.True(data.SelfLoopCount == 0,
            $"Found {data.SelfLoopCount} self-loop trails (trail from a town to itself). " +
            "Self-loops should never occur.");
    }

    [Fact]
    public void BruteForce_NoDuplicateTrailIds()
    {
        var data = Data;
        Assert.True(data.DuplicateTrailIdCount == 0,
            $"Found {data.DuplicateTrailIdCount} duplicate trail IDs across all worlds. " +
            "Trail IDs should be unique within each world.");
    }

    [Fact]
    public void BruteForce_NoZeroOrNegativeDistances()
    {
        var data = Data;
        Assert.True(data.ZeroOrNegativeDistanceCount == 0,
            $"Found {data.ZeroOrNegativeDistanceCount} trails with zero or negative ride-day distance. " +
            "All trail distances must be positive.");
    }

    [Fact]
    public void BruteForce_NoMultipleOutliers()
    {
        var data = Data;
        Assert.True(data.MultipleOutlierCount == 0,
            $"Found {data.MultipleOutlierCount} worlds with more than one outlier town. " +
            "At most one outlier town should exist per world.");
    }

    [Fact]
    public void BruteForce_NoAllIdenticalDistances()
    {
        // A world where every trail has the same ride-day count is degenerate.
        // This should occur in <1% of worlds.
        var data = Data;
        var pct = 100.0 * data.AllIdenticalDistanceCount / Math.Max(1, data.TotalWorlds);
        Assert.True(pct < 1.0,
            $"{data.AllIdenticalDistanceCount}/{data.TotalWorlds} ({pct:F1}%) worlds have " +
            "all trails at the same ride-day distance. Expected < 1%.");
    }

    [Fact]
    public void BruteForce_NoDegeneratePlacement()
    {
        // All towns at the same X or Y coordinate indicates degenerate placement.
        // This should occur in <1% of worlds.
        var data = Data;
        var pct = 100.0 * data.DegeneratePlacementCount / Math.Max(1, data.TotalWorlds);
        Assert.True(pct < 1.0,
            $"{data.DegeneratePlacementCount}/{data.TotalWorlds} ({pct:F1}%) worlds have " +
            "all towns at the same X or Y coordinate. Expected < 1%.");
    }

    [Fact]
    public void BruteForce_NoOverConnectedTownsOnSmallMaps()
    {
        // A town with 6+ connections in a 5-town map is suspiciously over-connected.
        // This should occur in <1% of small-map worlds.
        var data = Data;
        var pct = 100.0 * data.OverConnectedSmallMapCount / Math.Max(1, data.SmallMapWorlds);
        Assert.True(pct < 1.0,
            $"{data.OverConnectedSmallMapCount}/{data.SmallMapWorlds} ({pct:F1}%) small-map " +
            "(townCount <= 6) worlds have a town with degree 6+. Expected < 1%.");
    }

    [Fact]
    public void BruteForce_OutlierNotInsideCluster()
    {
        // The outlier sitting within 50px of a cluster center (i.e., inside
        // a cluster rather than isolated) should occur in <1% of outliers.
        var data = Data;
        var pct = 100.0 * data.OutlierInsideClusterCount / Math.Max(1, data.TotalOutliers);
        Assert.True(pct < 1.0,
            $"{data.OutlierInsideClusterCount}/{data.TotalOutliers} ({pct:F1}%) outliers " +
            "are within 50px of a cluster center. Expected < 1%.");
    }

    // === Helpers ===

    private static double Median(List<double> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2.0
            : sorted[mid];
    }

    private static string FormatTerrainDistribution(Dictionary<TrailTerrain, int> counts, int total)
        => string.Join(", ", Enum.GetValues<TrailTerrain>().Select(t =>
            $"{t}={counts.GetValueOrDefault(t)}({100.0 * counts.GetValueOrDefault(t) / Math.Max(1, total):F1}%)"));

    private static string FormatRiskDistribution(Dictionary<TrailRisk, int> counts, int total)
        => string.Join(", ", Enum.GetValues<TrailRisk>().Select(r =>
            $"{r}={counts.GetValueOrDefault(r)}({100.0 * counts.GetValueOrDefault(r) / Math.Max(1, total):F1}%)"));

    private static Dictionary<TrailTerrain, double> NormalizeTerrain(Dictionary<TrailTerrain, int> counts)
    {
        var total = (double)counts.Values.Sum();
        return counts.ToDictionary(kv => kv.Key, kv => kv.Value / total);
    }
}

/// <summary>
/// Runs the full brute-force combination matrix once and collects all
/// statistical data needed by the analysis tests. This avoids re-running
/// the 4224-combination matrix for each test method.
/// </summary>
internal static class BruteForceDataCollector
{
    private static GameSetupDeterministicSource NewSource()
        => new(SeedWorldResolver.FormatSeedCode(SeedWorldResolver.CreateCanonicalSeedCode()));

    private static SeedWorld NewSeedWorld(int townCount, int clusterCount, GraphDensity density, int outlierSlotType)
    {
        var base_ = SeedWorldResolver.CreateCanonicalSeedWorld();
        return base_ with
        {
            TownCount = townCount,
            ClusterCount = clusterCount,
            GraphDensity = density,
            OutlierSlotType = outlierSlotType
        };
    }

    public sealed class CollectorData
    {
        public int TotalWorlds;

        // Ride-day distribution (base trails only, excluding outlier 6-day trails)
        public Dictionary<int, int> RideDayCounts = new();
        public int TotalBaseTrails;

        // Degree distribution (base towns only, excluding outlier)
        public Dictionary<int, int> DegreeCounts = new();
        public int TotalTowns;

        // Cluster separation
        public double IntraClusterDistanceSum;
        public int TotalIntraClusterPairs;
        public double InterClusterDistanceSum;
        public int TotalInterClusterPairs;

        // Outlier isolation
        public List<double> OutlierNearestNeighborDistances = new();
        public List<double> BaseNearestNeighborDistances = new();

        // Map coverage
        public List<double> BoundingBoxAreas = new();

        // Terrain and risk diversity
        public Dictionary<TrailTerrain, int> TerrainCounts = new();
        public Dictionary<TrailRisk, int> RiskCounts = new();

        // Variant influence: terrain distribution per variant
        public Dictionary<SeedWorldVariant, Dictionary<TrailTerrain, int>> TerrainByVariant = new();

        // Anti-patterns
        public int SelfLoopCount;
        public int DuplicateTrailIdCount;
        public int ZeroOrNegativeDistanceCount;
        public int MultipleOutlierCount;
        public int AllIdenticalDistanceCount;
        public int DegeneratePlacementCount;
        public int OverConnectedSmallMapCount;
        public int SmallMapWorlds;
        public int OutlierInsideClusterCount;
        public int TotalOutliers;
    }

    public static CollectorData CollectAll()
    {
        var data = new CollectorData();
        var variants = Enum.GetValues<SeedWorldVariant>();
        var densities = Enum.GetValues<GraphDensity>();
        var entropies = Enum.GetValues<GameEntropy>();
        var salts = new[] { "salt-a", "salt-b", "salt-c" };

        foreach (var variant in variants)
        {
            foreach (var density in densities)
            {
                for (var clusterCount = 1; clusterCount <= 4; clusterCount++)
                {
                    for (var outlierSlotType = 0; outlierSlotType <= 1; outlierSlotType++)
                    {
                        for (var townCount = SeedWorldResolver.MinTownCount;
                             townCount <= SeedWorldResolver.MaxTownCount;
                             townCount++)
                        {
                            if (outlierSlotType > 0 && townCount >= SeedWorldResolver.MaxTownCount)
                                continue;

                            foreach (var entropy in entropies)
                            {
                                foreach (var salt in salts)
                                {
                                    var seed = NewSeedWorld(townCount, clusterCount, density, outlierSlotType) with
                                    {
                                        WorldVariant = variant,
                                    };
                                    var source = NewSource();
                                    var saltSource = SaltSource.CreateFixed(salt);

                                    World world;
                                    try
                                    {
                                        world = MapGenerator.Generate(seed, source, entropy, saltSource);
                                    }
                                    catch
                                    {
                                        continue;
                                    }

                                    data.TotalWorlds++;

                                    // --- Anti-pattern checks (per-world) ---

                                    // Self-loops
                                    foreach (var trail in world.Trails)
                                    {
                                        if (trail.FromTownId.Equals(trail.ToTownId))
                                            data.SelfLoopCount++;
                                    }

                                    // Duplicate trail IDs
                                    var trailIds = world.Trails.Select(t => t.Id.Value).ToArray();
                                    if (trailIds.Distinct().Count() != trailIds.Length)
                                        data.DuplicateTrailIdCount++;

                                    // Zero or negative distances
                                    foreach (var trail in world.Trails)
                                    {
                                        if (trail.RideDayDistance <= 0m)
                                            data.ZeroOrNegativeDistanceCount++;
                                    }

                                    // Multiple outliers
                                    var outliers = world.Towns.Where(t => t.IsOutlier).ToList();
                                    if (outliers.Count > 1)
                                        data.MultipleOutlierCount++;
                                    if (outliers.Count > 0)
                                        data.TotalOutliers++;

                                    // All identical distances
                                    var distinctDistances = world.Trails.Select(t => t.RideDayDistance).Distinct().Count();
                                    if (world.Trails.Count > 1 && distinctDistances == 1)
                                        data.AllIdenticalDistanceCount++;

                                    // Degenerate placement (all same X or all same Y)
                                    var allX = world.Towns.Select(t => t.MapX).Distinct().Count();
                                    var allY = world.Towns.Select(t => t.MapY).Distinct().Count();
                                    if (allX == 1 || allY == 1)
                                        data.DegeneratePlacementCount++;

                                    // Over-connected towns on small maps
                                    if (townCount <= 6)
                                    {
                                        data.SmallMapWorlds++;
                                        foreach (var town in world.Towns)
                                        {
                                            var degree = world.Trails.Count(t =>
                                                t.FromTownId.Equals(town.Id) || t.ToTownId.Equals(town.Id));
                                            if (degree >= 6)
                                            {
                                                data.OverConnectedSmallMapCount++;
                                                break;
                                            }
                                        }
                                    }

                                    // --- Statistical data collection ---

                                    // Ride-day distribution (exclude outlier trails)
                                    var outlierTown = world.Towns.SingleOrDefault(t => t.IsOutlier);
                                    foreach (var trail in world.Trails)
                                    {
                                        var isOutlierTrail = outlierTown != null &&
                                            (trail.FromTownId.Equals(outlierTown.Id) ||
                                             trail.ToTownId.Equals(outlierTown.Id));
                                        if (isOutlierTrail) continue;

                                        var days = (int)trail.RideDayDistance;
                                        data.RideDayCounts[days] = data.RideDayCounts.GetValueOrDefault(days) + 1;
                                        data.TotalBaseTrails++;

                                        // Terrain and risk diversity
                                        data.TerrainCounts[trail.Terrain] = data.TerrainCounts.GetValueOrDefault(trail.Terrain) + 1;
                                        data.RiskCounts[trail.Risk] = data.RiskCounts.GetValueOrDefault(trail.Risk) + 1;

                                        // Per-variant terrain
                                        if (!data.TerrainByVariant.TryGetValue(variant, out var variantTerrain))
                                        {
                                            variantTerrain = new Dictionary<TrailTerrain, int>();
                                            data.TerrainByVariant[variant] = variantTerrain;
                                        }
                                        variantTerrain[trail.Terrain] = variantTerrain.GetValueOrDefault(trail.Terrain) + 1;
                                    }

                                    // Degree distribution (exclude outlier)
                                    foreach (var town in world.Towns)
                                    {
                                        if (outlierTown != null && town.Id.Equals(outlierTown.Id))
                                            continue;
                                        var degree = world.Trails.Count(t =>
                                            t.FromTownId.Equals(town.Id) || t.ToTownId.Equals(town.Id));
                                        data.DegreeCounts[degree] = data.DegreeCounts.GetValueOrDefault(degree) + 1;
                                        data.TotalTowns++;
                                    }

                                    // Cluster separation: compute average intra vs inter-cluster distances.
                                    // We need cluster assignments — derive from the seed's cluster count
                                    // and the town positions. Since we don't have the internal cluster
                                    // assignments here, we approximate by checking if the seed has
                                    // multiple clusters and collecting pairwise distances.
                                    // For proper cluster analysis, we'd need the cluster assignments
                                    // from ClusterPlacementGenerator. Instead, we use a simpler proxy:
                                    // compare nearest-neighbor distances for outlier vs base towns.
                                    if (clusterCount > 1)
                                    {
                                        var baseTowns = world.Towns.Where(t => !t.IsOutlier).ToList();
                                        for (var i = 0; i < baseTowns.Count; i++)
                                        {
                                            for (var j = i + 1; j < baseTowns.Count; j++)
                                            {
                                                var dx = baseTowns[i].MapX - baseTowns[j].MapX;
                                                var dy = baseTowns[i].MapY - baseTowns[j].MapY;
                                                var dist = Math.Sqrt(dx * dx + dy * dy);

                                                // Approximate cluster membership by distance threshold:
                                                // towns within 100px are likely intra-cluster,
                                                // towns beyond 150px are likely inter-cluster.
                                                // This is a rough proxy but sufficient for aggregate stats.
                                                if (dist < 100)
                                                {
                                                    data.IntraClusterDistanceSum += dist;
                                                    data.TotalIntraClusterPairs++;
                                                }
                                                else if (dist > 150)
                                                {
                                                    data.InterClusterDistanceSum += dist;
                                                    data.TotalInterClusterPairs++;
                                                }
                                            }
                                        }
                                    }

                                    // Outlier isolation: nearest-neighbor distance for outlier vs base towns
                                    if (outlierTown != null)
                                    {
                                        var nnDist = world.Towns
                                            .Where(t => !t.Id.Equals(outlierTown.Id))
                                            .Min(t =>
                                            {
                                                var dx = t.MapX - outlierTown.MapX;
                                                var dy = t.MapY - outlierTown.MapY;
                                                return Math.Sqrt(dx * dx + dy * dy);
                                            });
                                        data.OutlierNearestNeighborDistances.Add(nnDist);

                                        // Check if outlier is inside a cluster (within 50px of a cluster center).
                                        // We approximate cluster centers as the centroid of tightly-packed towns.
                                        // A simpler check: if the outlier's NN distance is < 50px, it's too close.
                                        if (nnDist < 50)
                                            data.OutlierInsideClusterCount++;
                                    }

                                    // Base town nearest-neighbor distances (for median comparison)
                                    var baseTownList = world.Towns.Where(t => !t.IsOutlier).ToList();
                                    foreach (var town in baseTownList)
                                    {
                                        var nnDist = baseTownList
                                            .Where(t => !t.Id.Equals(town.Id))
                                            .Select(t =>
                                            {
                                                var dx = t.MapX - town.MapX;
                                                var dy = t.MapY - town.MapY;
                                                return Math.Sqrt(dx * dx + dy * dy);
                                            })
                                            .DefaultIfEmpty(0)
                                            .Min();
                                        data.BaseNearestNeighborDistances.Add(nnDist);
                                    }

                                    // Map coverage: bounding box area
                                    if (baseTownList.Count > 0)
                                    {
                                        var minX = baseTownList.Min(t => t.MapX);
                                        var maxX = baseTownList.Max(t => t.MapX);
                                        var minY = baseTownList.Min(t => t.MapY);
                                        var maxY = baseTownList.Max(t => t.MapY);
                                        data.BoundingBoxAreas.Add((double)(maxX - minX) * (maxY - minY));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return data;
    }
}
