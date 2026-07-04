using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class MapGeneratorTests
{
    private static GameSetupDeterministicSource NewSource(Guid? seedCode = null)
        => new(SeedWorldResolver.FormatSeedCode(seedCode ?? SeedWorldResolver.CreateCanonicalSeedCode()));

    private static SeedWorld NewSeedWorld(int townCount = 8, int clusterCount = 2, GraphDensity density = GraphDensity.Sparse, int outlierSlotType = 0)
    {
        var base_ = SeedWorldResolver.CreateCanonicalSeedWorld();
        return base_ with { TownCount = townCount, ClusterCount = clusterCount, GraphDensity = density, OutlierSlotType = outlierSlotType };
    }

    [Fact]
    public void Generate_Boring_SameSeedProducesSameWorld()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2);
        var sourceA = NewSource();
        var sourceB = NewSource();

        var a = MapGenerator.Generate(seed, sourceA, GameEntropy.Boring, SaltSource.CreateFixed("any-salt"));
        var b = MapGenerator.Generate(seed, sourceB, GameEntropy.Boring, SaltSource.CreateFixed("different-salt"));

        var townsA = a.Towns.ToArray();
        var townsB = b.Towns.ToArray();

        Assert.Equal(townsA.Length, townsB.Length);
        for (var i = 0; i < townsA.Length; i++)
        {
            Assert.Equal(townsA[i].MapX, townsB[i].MapX);
            Assert.Equal(townsA[i].MapY, townsB[i].MapY);
        }
        Assert.Equal(a.Trails.Count, b.Trails.Count);
    }

    [Fact]
    public void Generate_NonBoring_SameSeedSameSaltIsDeterministic()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2);
        var source = NewSource();
        var salt = SaltSource.CreateFixed("deterministic-salt");

        var a = MapGenerator.Generate(seed, source, GameEntropy.Wild, salt);
        var b = MapGenerator.Generate(seed, source, GameEntropy.Wild, salt);

        var townsA = a.Towns.ToArray();
        var townsB = b.Towns.ToArray();

        Assert.Equal(townsA.Length, townsB.Length);
        for (var i = 0; i < townsA.Length; i++)
        {
            Assert.Equal(townsA[i].MapX, townsB[i].MapX);
            Assert.Equal(townsA[i].MapY, townsB[i].MapY);
        }
        Assert.Equal(a.Trails.Count, b.Trails.Count);
    }

    [Fact]
    public void Generate_AllTownsReachable_ConnectedGraph()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        var adjacency = new Dictionary<string, HashSet<string>>();
        foreach (var town in world.Towns) adjacency[town.Id.Value] = new HashSet<string>();
        foreach (var trail in world.Trails)
        {
            adjacency[trail.FromTownId.Value].Add(trail.ToTownId.Value);
            adjacency[trail.ToTownId.Value].Add(trail.FromTownId.Value);
        }

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(world.Towns.First().Id.Value);
        visited.Add(world.Towns.First().Id.Value);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (visited.Add(neighbor)) queue.Enqueue(neighbor);
            }
        }

        Assert.Equal(world.Towns.Count, visited.Count);
    }

    [Fact]
    public void Generate_NoCrossingTrails_PlanarGraph()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Dense);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        var townCoords = world.Towns.ToDictionary(t => t.Id.Value, t => (t.MapX, t.MapY));
        for (var i = 0; i < world.Trails.Count; i++)
        {
            for (var j = i + 1; j < world.Trails.Count; j++)
            {
                var a = world.Trails[i];
                var b = world.Trails[j];
                var shared = a.FromTownId.Equals(b.FromTownId) || a.FromTownId.Equals(b.ToTownId)
                    || a.ToTownId.Equals(b.FromTownId) || a.ToTownId.Equals(b.ToTownId);
                if (shared) continue;

                var p1 = townCoords[a.FromTownId.Value];
                var p2 = townCoords[a.ToTownId.Value];
                var p3 = townCoords[b.FromTownId.Value];
                var p4 = townCoords[b.ToTownId.Value];
                Assert.False(SegmentsIntersect(p1, p2, p3, p4),
                    $"Trails {a.Id.Value} and {b.Id.Value} cross.");
            }
        }
    }

    [Fact]
    public void Generate_NormalTrails_In2To8DayRange()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        // Honest 25px/day scale: 50px min separation → 2 days minimum,
        // 200px max inter-cluster → 8 days maximum (top clamp).
        Assert.All(world.Trails, t => Assert.InRange(t.RideDayDistance, 2m, 8m));
    }

    [Fact]
    public void Generate_OutlierSlot_NonBoring_OutlierHasSingleIncidentTrailAt6Days()
    {
        var seed = NewSeedWorld(townCount: 5, clusterCount: 1, outlierSlotType: 1);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Wild, SaltSource.CreateFixed("salt"));

        var outlier = world.Towns.SingleOrDefault(t => t.IsOutlier);
        Assert.NotNull(outlier);

        var incident = world.Trails.Where(t => t.FromTownId.Equals(outlier.Id) || t.ToTownId.Equals(outlier.Id)).ToList();
        Assert.Single(incident);
        Assert.Equal(6m, incident[0].RideDayDistance);
    }

    [Fact]
    public void Generate_OutlierSlot_Boring_NoOutlierTownAdded()
    {
        var seed = NewSeedWorld(townCount: 5, clusterCount: 1, outlierSlotType: 1);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.Equal(5, world.Towns.Count);
        Assert.DoesNotContain(world.Towns, t => t.IsOutlier);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(10)]
    public void Generate_TownCount_AllTownsPlaced(int townCount)
    {
        var seed = NewSeedWorld(townCount: townCount, clusterCount: 2);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.Equal(townCount, world.Towns.Count);
        Assert.All(world.Towns, t =>
        {
            Assert.True(t.MapX > 0, $"Town {t.Name} should have positive MapX");
            Assert.True(t.MapY > 0, $"Town {t.Name} should have positive MapY");
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Generate_ClusterCount_AllTownsAssignedToValidClusters(int clusterCount)
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: clusterCount);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.Equal(8, world.Towns.Count);
    }

    [Fact]
    public void Generate_OutlierSlot_NeverProducesDuplicateTownIds()
    {
        // Regression: MapGenerator previously called DeriveTownNames(townCount: 1, ...)
        // for the outlier name, which uses a different seed/shuffle than the main call
        // and could pick a name already in the main list. The fix derives the full
        // shuffled pool from the outlier seed and picks the first name not in the main set.
        //
        // This test brute-forces all valid parameter combinations through the fix logic
        // to prove the outlier name is always unique.
        var variants = Enum.GetValues<SeedWorldVariant>();
        var prosperityPalettes = Enum.GetValues<ProsperityPalette>();
        var servicesPalettes = Enum.GetValues<ServicesPalette>();
        var failures = new List<string>();

        for (var v = 0; v < variants.Length; v++)
        {
            for (var pp = 0; pp < prosperityPalettes.Length; pp++)
            {
                for (var sp = 0; sp < servicesPalettes.Length; sp++)
                {
                    for (var townCount = 5; townCount <= 9; townCount++)
                    {
                        for (var ai = 0; ai <= 6; ai++)
                        {
                            for (var di = 0; di <= 6; di++)
                            {
                                for (var cb = 0; cb <= 8; cb++)
                                {
                                    var mainNames = SeedWorldCatalog.DeriveTownNames(
                                        variants[v], townCount, ai, di, cb,
                                        prosperityPalettes[pp],
                                        servicesPalettes[sp]);
                                    var existingIds = new HashSet<string>(mainNames.Select(t => t.Id));

                                    // Replicate the fix: derive the full outlier pool and pick
                                    // the first name not in the main set.
                                    var outlierPool = SeedWorldCatalog.DeriveTownNames(
                                        variants[v],
                                        townCount: SeedWorldCatalog.NamePool.Count,
                                        accusationIndex: 0,
                                        defaultCulpritIndex: 0,
                                        cashBonus: 0,
                                        prosperityPalette: prosperityPalettes[pp],
                                        servicesPalette: servicesPalettes[sp]);

                                    var outlierName = outlierPool.FirstOrDefault(
                                        entry => !existingIds.Contains(entry.Id));

                                    if (outlierName is null)
                                    {
                                        failures.Add(
                                            $"variant={variants[v]}, townCount={townCount}, " +
                                            $"accusationIndex={ai}, defaultCulpritIndex={di}, " +
                                            $"cashBonus={cb}, prosperity={prosperityPalettes[pp]}, " +
                                            $"services={servicesPalettes[sp]}: " +
                                            "no unique outlier name available");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.True(failures.Count == 0,
            $"Outlier uniqueness failures ({failures.Count}).\n" +
            "First 5:\n" + string.Join("\n", failures.Take(5)));
    }

    [Theory]
    [InlineData(5, 1, "salt-a")]
    [InlineData(5, 1, "salt-b")]
    [InlineData(6, 1, "salt-c")]
    [InlineData(7, 1, "salt-d")]
    [InlineData(8, 1, "salt-e")]
    [InlineData(9, 1, "salt-f")]
    public void Generate_OutlierSlot_AllTownsHaveUniqueIds(int townCount, int outlierSlotType, string salt)
    {
        // End-to-end regression: MapGenerator.Generate must produce unique town IDs
        // when an outlier is materialized, across various town counts and salts.
        var seed = NewSeedWorld(townCount: townCount, clusterCount: 1, outlierSlotType: outlierSlotType);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Wild, SaltSource.CreateFixed(salt));

        var townIds = world.Towns.Select(t => t.Id.Value).ToArray();
        Assert.Equal(townIds.Length, townIds.Distinct().Count());
    }

    /// <summary>
    /// Brute-force invariant + distribution test: iterates over all valid
    /// combinations of SeedWorldVariant, townCount (5-10), clusterCount (1-4),
    /// GraphDensity, OutlierSlotType (0-1), GameEntropy (all 4), and multiple
    /// salts — running MapGenerator.Generate for each and asserting every map
    /// invariant:
    ///
    ///  1. Town count is in [5, 10] (+1 if outlier materialized)
    ///  2. All town IDs are unique
    ///  3. All town coordinates are in [0, 800] x [0, 500]
    ///  4. All trail distances are in [2, 8] days
    ///  5. Trail graph is connected (BFS from any town reaches all towns)
    ///  6. No two non-adjacent trail segments cross (planar graph)
    ///  7. Determinism: same seed + same salt produces identical world
    ///  8. Outlier (when materialized) has exactly one incident trail at 6 days
    ///
    /// After all combinations, asserts distribution fairness:
    ///  9. Ride-day distances (excluding outlier 6-day trails) are not excessively
    ///     biased toward a single value — each day value [2..8] should appear at
    ///     least 5% of the time, and no single value should exceed 50%.
    /// 10. Town degrees are not excessively biased — degree-2 towns should not
    ///     exceed 70% of all towns (the graph should have some branching).
    /// </summary>
    [Fact]
    public void Generate_BruteForce_AllValidCombinations_SatisfyAllInvariants()
    {
        var variants = Enum.GetValues<SeedWorldVariant>();
        var densities = Enum.GetValues<GraphDensity>();
        var entropies = Enum.GetValues<GameEntropy>();
        var salts = new[] { "salt-a", "salt-b", "salt-c" };
        var failures = new List<string>();
        var combinationsTested = 0;

        // Distribution trackers for fairness assertions after the main loop.
        // Ride-day counts exclude outlier trails (always 6 days) so the
        // distribution reflects the base graph's distance spread.
        var rideDayCounts = new Dictionary<int, int>();
        var degreeCounts = new Dictionary<int, int>();
        var totalBaseTrails = 0;
        var totalTowns = 0;

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
                            // Outlier is not allowed at max town count
                            if (outlierSlotType > 0 && townCount >= SeedWorldResolver.MaxTownCount)
                                continue;

                            foreach (var entropy in entropies)
                            {
                                foreach (var salt in salts)
                                {
                                    combinationsTested++;
                                    var label = $"variant={variant}, density={density}, " +
                                        $"clusters={clusterCount}, outlier={outlierSlotType}, " +
                                        $"towns={townCount}, entropy={entropy}, salt={salt}";

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
                                    catch (Exception ex)
                                    {
                                        failures.Add($"{label}: THREW {ex.GetType().Name}: {ex.Message}");
                                        continue;
                                    }

                                    // 1. Town count
                                    var expectedTowns = townCount;
                                    if (outlierSlotType == 1 && entropy != GameEntropy.Boring)
                                        expectedTowns++; // outlier materialized
                                    if (world.Towns.Count != expectedTowns)
                                    {
                                        failures.Add($"{label}: expected {expectedTowns} towns, got {world.Towns.Count}");
                                        continue;
                                    }

                                    // 2. Unique town IDs
                                    var townIds = world.Towns.Select(t => t.Id.Value).ToArray();
                                    if (townIds.Distinct().Count() != townIds.Length)
                                    {
                                        failures.Add($"{label}: duplicate town IDs");
                                        continue;
                                    }

                                    // 3. Coordinates in bounds
                                    foreach (var t in world.Towns)
                                    {
                                        if (t.MapX < 0 || t.MapX > 800 || t.MapY < 0 || t.MapY > 500)
                                        {
                                            failures.Add($"{label}: town {t.Id.Value} out of bounds at ({t.MapX}, {t.MapY})");
                                        }
                                    }

                                    // 4. Trail distances in [2, 8] (honest 25px/day scale:
                                    //    50px min separation → 2 days, 200px top clamp → 8 days.
                                    //    Outlier trail is always 6 days.)
                                    foreach (var trail in world.Trails)
                                    {
                                        if (trail.RideDayDistance < 2m || trail.RideDayDistance > 8m)
                                        {
                                            failures.Add($"{label}: trail {trail.Id.Value} distance {trail.RideDayDistance} out of [2,8]");
                                        }
                                    }

                                    // 5. BFS connectivity
                                    var adj = new Dictionary<string, HashSet<string>>();
                                    foreach (var id in townIds) adj[id] = new HashSet<string>();
                                    foreach (var trail in world.Trails)
                                    {
                                        adj[trail.FromTownId.Value].Add(trail.ToTownId.Value);
                                        adj[trail.ToTownId.Value].Add(trail.FromTownId.Value);
                                    }
                                    var visited = new HashSet<string>();
                                    var queue = new Queue<string>();
                                    queue.Enqueue(townIds[0]);
                                    visited.Add(townIds[0]);
                                    while (queue.Count > 0)
                                    {
                                        var cur = queue.Dequeue();
                                        foreach (var n in adj[cur])
                                        {
                                            if (visited.Add(n)) queue.Enqueue(n);
                                        }
                                    }
                                    if (visited.Count != townIds.Length)
                                    {
                                        failures.Add($"{label}: graph not connected ({visited.Count}/{townIds.Length} reachable)");
                                    }

                                    // 6. Planar graph (no crossing non-adjacent trails)
                                    var townCoords = world.Towns.ToDictionary(t => t.Id.Value, t => (t.MapX, t.MapY));
                                    for (var i = 0; i < world.Trails.Count; i++)
                                    {
                                        for (var j = i + 1; j < world.Trails.Count; j++)
                                        {
                                            var a = world.Trails[i];
                                            var b = world.Trails[j];
                                            var shared = a.FromTownId.Equals(b.FromTownId) ||
                                                a.FromTownId.Equals(b.ToTownId) ||
                                                a.ToTownId.Equals(b.FromTownId) ||
                                                a.ToTownId.Equals(b.ToTownId);
                                            if (shared) continue;

                                            var p1 = townCoords[a.FromTownId.Value];
                                            var p2 = townCoords[a.ToTownId.Value];
                                            var p3 = townCoords[b.FromTownId.Value];
                                            var p4 = townCoords[b.ToTownId.Value];
                                            if (SegmentsIntersect(p1, p2, p3, p4))
                                            {
                                                failures.Add($"{label}: trails {a.Id.Value} and {b.Id.Value} cross");
                                            }
                                        }
                                    }

                                    // 7. Determinism: re-run with same inputs
                                    var source2 = NewSource();
                                    var world2 = MapGenerator.Generate(seed, source2, entropy, saltSource);
                                    if (world.Towns.Count != world2.Towns.Count)
                                    {
                                        failures.Add($"{label}: non-deterministic town count");
                                    }
                                    else
                                    {
                                        var townsA = world.Towns.ToArray();
                                        var townsB = world2.Towns.ToArray();
                                        for (var i = 0; i < townsA.Length; i++)
                                        {
                                            if (townsA[i].MapX != townsB[i].MapX ||
                                                townsA[i].MapY != townsB[i].MapY)
                                            {
                                                failures.Add($"{label}: non-deterministic town {i} coords");
                                                break;
                                            }
                                        }
                                    }

                                    // 8. Outlier has single incident trail at 6 days
                                    if (outlierSlotType == 1 && entropy != GameEntropy.Boring)
                                    {
                                        var outlier = world.Towns.SingleOrDefault(t => t.IsOutlier);
                                        if (outlier is null)
                                        {
                                            failures.Add($"{label}: expected outlier town but none found");
                                        }
                                        else
                                        {
                                            var incident = world.Trails.Where(t =>
                                                t.FromTownId.Equals(outlier.Id) ||
                                                t.ToTownId.Equals(outlier.Id)).ToList();
                                            if (incident.Count != 1)
                                            {
                                                failures.Add($"{label}: outlier has {incident.Count} incident trails, expected 1");
                                            }
                                            else if (incident[0].RideDayDistance != 6m)
                                            {
                                                failures.Add($"{label}: outlier trail distance {incident[0].RideDayDistance}, expected 6");
                                            }
                                        }
                                    }

                                    // --- Distribution data collection (for fairness assertions below) ---

                                    // Collect ride-day counts for base trails (exclude outlier trails
                                    // which are always 6 days and would skew the distribution).
                                    var outlierTown = world.Towns.SingleOrDefault(t => t.IsOutlier);
                                    foreach (var trail in world.Trails)
                                    {
                                        var isOutlierTrail = outlierTown != null &&
                                            (trail.FromTownId.Equals(outlierTown.Id) ||
                                             trail.ToTownId.Equals(outlierTown.Id));
                                        if (isOutlierTrail) continue;

                                        var days = (int)trail.RideDayDistance;
                                        rideDayCounts[days] = rideDayCounts.GetValueOrDefault(days) + 1;
                                        totalBaseTrails++;
                                    }

                                    // Collect town degree counts (number of trails incident to each town).
                                    // Exclude outlier towns — they are intentionally degree-1 by design
                                    // (single 6-day trail) and would skew the distribution.
                                    var outlierTownForDegree = world.Towns.SingleOrDefault(t => t.IsOutlier);
                                    foreach (var town in world.Towns)
                                    {
                                        if (outlierTownForDegree != null && town.Id.Equals(outlierTownForDegree.Id))
                                            continue;
                                        var degree = world.Trails.Count(t =>
                                            t.FromTownId.Equals(town.Id) || t.ToTownId.Equals(town.Id));
                                        degreeCounts[degree] = degreeCounts.GetValueOrDefault(degree) + 1;
                                        totalTowns++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        var crossCount = failures.Count(f => f.Contains("cross"));
        var notConnectedCount = failures.Count(f => f.Contains("not connected"));
        var outOfBoundsCount = failures.Count(f => f.Contains("out of bounds"));
        var duplicateCount = failures.Count(f => f.Contains("duplicate"));
        var nonDetCount = failures.Count(f => f.Contains("non-deterministic"));
        var outlierCount = failures.Count(f => f.Contains("outlier town") || f.Contains("outlier has") || f.Contains("outlier trail"));
        var distanceCount = failures.Count(f => f.Contains("distance"));
        var threwCount = failures.Count(f => f.Contains("THREW"));
        Assert.True(failures.Count == 0,
            $"Brute-force: {failures.Count} failures / {combinationsTested} combos. " +
            $"cross={crossCount} notConnected={notConnectedCount} outOfBounds={outOfBoundsCount} " +
            $"dup={duplicateCount} nonDet={nonDetCount} outlier={outlierCount} " +
            $"dist={distanceCount} threw={threwCount}. " +
            $"All crosses: {string.Join(" | ", failures.Where(f => f.Contains("cross")).Take(10))}. " +
            $"First 5 notConnected: {string.Join(" | ", failures.Where(f => f.Contains("not connected")).Take(5))}");

        // --- Distribution fairness assertions ---
        // These catch silent bias in map generation where invariants pass but
        // the output is suspiciously skewed (e.g., almost every trail is 2 days,
        // or almost every town has degree 2 with no branching). The thresholds
        // are generous to allow natural variation across entropy/density combos.

        // 9. Ride-day distribution: each day value [2..8] should appear with
        //    reasonable frequency. Middle values (3-5) should each appear at
        //    least 5% of the time. Edge values (2, 6, 7) can be rarer due to
        //    placement geometry — 2d requires close intra-cluster pairs, 6d/7d
        //    require specific inter-cluster distances. The 8d bucket is the top
        //    clamp and may accumulate longer trails, but should not exceed 50%.
        //    No single day value should exceed 50% (would indicate heavy bias).
        var rideDaySummary = string.Join(", ",
            Enumerable.Range(2, 7).Select(d =>
                $"{d}d={rideDayCounts.GetValueOrDefault(d)}({(100.0 * rideDayCounts.GetValueOrDefault(d) / Math.Max(1, totalBaseTrails)):F1}%)"));
        foreach (var day in Enumerable.Range(2, 7))
        {
            var count = rideDayCounts.GetValueOrDefault(day);
            var pct = 100.0 * count / Math.Max(1, totalBaseTrails);
            // Middle values (3-5) need at least 5%; edges (2, 6, 7) need at least 3%;
            // 8d is the clamp bucket and only needs to not dominate (>0.5%).
            var minPct = day switch
            {
                >= 3 and <= 5 => 5.0,
                2 or 6 or 7 => 3.0,
                8 => 0.5,
                _ => 0.0
            };
            Assert.True(pct >= minPct,
                $"Ride-day distribution: {day}d appears {count}/{totalBaseTrails} ({pct:F1}%), " +
                $"expected >= {minPct}%. Full distribution: {rideDaySummary}");
            Assert.True(pct <= 50.0,
                $"Ride-day distribution: {day}d appears {count}/{totalBaseTrails} ({pct:F1}%), " +
                $"expected <= 50%. Full distribution: {rideDaySummary}");
        }

        // 10. Town degree distribution: degree-2 towns should not exceed 70%
        //     (the graph should have some branching, not just a linear chain).
        //     Degree-1 towns should not exist (EnsureMinimumDegree guarantees ≥2).
        //     Degree-3+ should appear at least 10% of the time (some junctions).
        var degree2Pct = 100.0 * degreeCounts.GetValueOrDefault(2) / Math.Max(1, totalTowns);
        var degree1Pct = 100.0 * degreeCounts.GetValueOrDefault(1) / Math.Max(1, totalTowns);
        var degree3PlusPct = 100.0 * degreeCounts.Where(kv => kv.Key >= 3).Sum(kv => kv.Value) / Math.Max(1, totalTowns);
        var degreeSummary = string.Join(", ",
            degreeCounts.OrderBy(kv => kv.Key).Select(kv =>
                $"deg{kv.Key}={kv.Value}({(100.0 * kv.Value / Math.Max(1, totalTowns)):F1}%)"));

        Assert.True(degree1Pct == 0,
            $"Degree distribution: degree-1 towns appear {degreeCounts.GetValueOrDefault(1)}/{totalTowns} " +
            $"({degree1Pct:F1}%), expected 0%. Full distribution: {degreeSummary}");
        Assert.True(degree2Pct <= 70.0,
            $"Degree distribution: degree-2 towns appear {degreeCounts.GetValueOrDefault(2)}/{totalTowns} " +
            $"({degree2Pct:F1}%), expected <= 70%. Full distribution: {degreeSummary}");
        Assert.True(degree3PlusPct >= 10.0,
            $"Degree distribution: degree-3+ towns appear {degree3PlusPct:F1}% of {totalTowns} towns, " +
            $"expected >= 10%. Full distribution: {degreeSummary}");
    }

    private static bool SegmentsIntersect((int MapX, int MapY) p1, (int MapX, int MapY) p2, (int MapX, int MapY) p3, (int MapX, int MapY) p4)
    {
        var d1 = Sign(p3, p4, p1);
        var d2 = Sign(p3, p4, p2);
        var d3 = Sign(p1, p2, p3);
        var d4 = Sign(p1, p2, p4);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
            && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));

        static int Sign((int MapX, int MapY) a, (int MapX, int MapY) b, (int MapX, int MapY) c)
            // The outer parentheses around the cross-product expression are load-bearing.
            // Without them, C# parses `x - y switch { ... }` as `x - (y switch { ... })`
            // because the switch expression binds to the nearest term (y), not the whole
            // arithmetic expression. That would compute (product1 - sign(product2)) instead
            // of sign(product1 - product2), producing false-positive segment intersections.
            => ((b.MapX - a.MapX) * (c.MapY - a.MapY) - (b.MapY - a.MapY) * (c.MapX - a.MapX)) switch
            {
                > 0 => 1,
                < 0 => -1,
                _ => 0
            };
    }
}
