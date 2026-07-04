# Core Pipeline — Test Code Reference

This document contains the test code for each pipeline component. Each section corresponds to a task in the pipeline plan.

---

## 2. ClusterPlacementGeneratorTests

**File:** `tests/WildBunch.GameContent.Tests/ClusterPlacementGeneratorTests.cs`

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class ClusterPlacementGeneratorTests
{
    private static GameSetupDeterministicSource NewSource(Guid? seedCode = null)
        => new(SeedWorldResolver.FormatSeedCode(seedCode ?? SeedWorldResolver.CreateCanonicalSeedCode()));

    private static SeedWorld NewSeedWorld(int townCount = 8, int clusterCount = 1, int outlierSlotType = 0)
    {
        var base_ = SeedWorldResolver.CreateCanonicalSeedWorld();
        return base_ with { TownCount = townCount, ClusterCount = clusterCount, OutlierSlotType = outlierSlotType };
    }

    [Fact]
    public void Place_Boring_SameSeedProducesSameCoordinates()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2);
        var sourceA = NewSource();
        var sourceB = NewSource();
        var salt = SaltSource.CreateFixed("any-salt");

        var a = ClusterPlacementGenerator.Place(seed, sourceA, GameEntropy.Boring, salt);
        var b = ClusterPlacementGenerator.Place(seed, sourceB, GameEntropy.Boring, salt);

        Assert.Equal(a.Towns.Count, b.Towns.Count);
        foreach (var slot in a.Towns.Keys)
        {
            Assert.Equal(a.Towns[slot], b.Towns[slot]);
        }
    }

    [Fact]
    public void Place_Boring_IgnoresSalt()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2);
        var source = NewSource();

        var a = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt-a"));
        var b = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt-b"));

        foreach (var slot in a.Towns.Keys)
        {
            Assert.Equal(a.Towns[slot], b.Towns[slot]);
        }
    }

    [Fact]
    public void Place_NonBoring_SameSeedSameSaltIsDeterministic()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2);
        var source = NewSource();
        var salt = SaltSource.CreateFixed("deterministic-salt");

        var a = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Wild, salt);
        var b = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Wild, salt);

        foreach (var slot in a.Towns.Keys)
        {
            Assert.Equal(a.Towns[slot], b.Towns[slot]);
        }
    }

    [Fact]
    public void Place_NonBoring_DifferentSaltProducesDifferentCoordinates()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2);
        var source = NewSource();

        var a = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Wild, SaltSource.CreateFixed("salt-a"));
        var b = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Wild, SaltSource.CreateFixed("salt-b"));

        var anyDiffer = a.Towns.Any(slot => a.Towns[slot.Key] != b.Towns[slot.Key]);
        Assert.True(anyDiffer, "Different salt in non-Boring mode should produce different coordinates for at least one town.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Place_ClusterCount_ProducesExpectedClusterAssignments(int clusterCount)
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: clusterCount);
        var source = NewSource();

        var result = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        var distinctClusters = result.ClusterAssignments.Values.Distinct().Count();
        Assert.Equal(clusterCount, distinctClusters);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(8)]
    [InlineData(10)]
    public void Place_TownCount_AllTownsPlaced(int townCount)
    {
        var seed = NewSeedWorld(townCount: townCount, clusterCount: 2);
        var source = NewSource();

        var result = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.Equal(townCount, result.Towns.Count);
        Assert.All(result.Towns, kv =>
        {
            Assert.InRange(kv.Value.X, 0, 800);
            Assert.InRange(kv.Value.Y, 0, 500);
        });
    }

    [Fact]
    public void Place_OutlierSlot_NonBoring_AddsOutlierTown()
    {
        // TownCount=5 (below MaxTownCount=10) so OutlierSlotType=1 is valid.
        var seed = NewSeedWorld(townCount: 5, clusterCount: 1, outlierSlotType: 1);
        var source = NewSource();

        var result = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Wild, SaltSource.CreateFixed("salt"));

        Assert.Equal(6, result.Towns.Count);
        Assert.NotNull(result.OutlierSlot);
        Assert.Equal(5, result.OutlierSlot!.Value);

        // Outlier should be far from all other towns (>= 150px from its nearest neighbor).
        var outlierCoords = result.Towns[result.OutlierSlot.Value];
        var minDistance = result.Towns
            .Where(kv => kv.Key != result.OutlierSlot.Value)
            .Select(kv =>
            {
                var dx = kv.Value.X - outlierCoords.X;
                var dy = kv.Value.Y - outlierCoords.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            })
            .Min();
        Assert.True(minDistance >= 150.0, $"Outlier must be >=150px from nearest neighbor, was {minDistance}");
    }

    [Fact]
    public void Place_OutlierSlot_Boring_DoesNotAddOutlierTown()
    {
        var seed = NewSeedWorld(townCount: 5, clusterCount: 1, outlierSlotType: 1);
        var source = NewSource();

        var result = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.Equal(5, result.Towns.Count);
        Assert.Null(result.OutlierSlot);
    }

    [Fact]
    public void Place_ClusterCenters_HaveMinimumSeparation()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 4);
        var source = NewSource();

        var result = ClusterPlacementGenerator.Place(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        // Derive cluster centers by averaging town coordinates within each cluster.
        var centers = result.ClusterAssignments
            .GroupBy(kv => kv.Value, kv => result.Towns[kv.Key])
            .Select(g => (
                Cluster: g.Key,
                X: g.Average(c => c.X),
                Y: g.Average(c => c.Y)))
            .ToList();

        for (var i = 0; i < centers.Count; i++)
        {
            for (var j = i + 1; j < centers.Count; j++)
            {
                var dx = centers[i].X - centers[j].X;
                var dy = centers[i].Y - centers[j].Y;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                Assert.True(distance >= 150.0,
                    $"Cluster centers {i} and {j} are {distance:F1}px apart, expected >=150px");
            }
        }
    }
}
```

---

## 3. TrailGraphGeneratorTests

**File:** `tests/WildBunch.GameContent.Tests/TrailGraphGeneratorTests.cs`

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests;

public sealed class TrailGraphGeneratorTests
{
    private static GameSetupDeterministicSource NewSource(Guid? seedCode = null)
        => new(SeedWorldResolver.FormatSeedCode(seedCode ?? SeedWorldResolver.CreateCanonicalSeedCode()));

    private static SeedWorld NewSeedWorld(int townCount = 8, int clusterCount = 2, GraphDensity density = GraphDensity.Sparse)
    {
        var base_ = SeedWorldResolver.CreateCanonicalSeedWorld();
        return base_ with { TownCount = townCount, ClusterCount = clusterCount, GraphDensity = density };
    }

    private static (Dictionary<int, (int X, int Y)> Towns, Dictionary<int, int> ClusterAssignments, int? OutlierSlot) PlaceTowns(
        SeedWorld seed, GameEntropy entropy, SaltSource? salt)
    {
        var source = NewSource();
        return ClusterPlacementGenerator.Place(seed, source, entropy, salt);
    }

    [Fact]
    public void Generate_ProducesConnectedGraph()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var placement = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt"));
        var source = NewSource();

        var edges = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        var adjacency = BuildAdjacency(edges, seed.TownCount);
        Assert.True(IsConnected(adjacency, seed.TownCount), "Generated graph must be connected.");
    }

    [Fact]
    public void Generate_ProducesPlanarGraph_NoCrossings()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Dense);
        var placement = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt"));
        var source = NewSource();

        var edges = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.False(HasCrossing(edges, placement.Towns), "Generated graph must not have crossing edges.");
    }

    [Theory]
    [InlineData(GraphDensity.Sparse)]
    [InlineData(GraphDensity.Dense)]
    public void Generate_Boring_SameSeedProducesSameGraph(GraphDensity density)
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: density);
        var placementA = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt-a"));
        var placementB = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt-b"));
        var source = NewSource();

        var a = TrailGraphGenerator.Generate(seed, placementA.Towns, placementA.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt-a"));
        var b = TrailGraphGenerator.Generate(seed, placementB.Towns, placementB.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt-b"));

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].OrderedSlots, b[i].OrderedSlots);
            Assert.Equal(a[i].PixelDistance, b[i].PixelDistance);
        }
    }

    [Fact]
    public void Generate_DenseHasAtLeastAsManyEdgesAsSparse()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var placement = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt"));
        var source = NewSource();

        var sparseEdges = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        var denseSeed = seed with { GraphDensity = GraphDensity.Dense };
        var denseEdges = TrailGraphGenerator.Generate(denseSeed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.True(denseEdges.Count >= sparseEdges.Count,
            $"Dense ({denseEdges.Count} edges) should have at least as many edges as Sparse ({sparseEdges.Count} edges).");
    }

    [Fact]
    public void Generate_SparseBoring_IsExactlyMST()
    {
        // Sparse + Boring = MST only. MST of N nodes has N-1 edges.
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var placement = PlaceTowns(seed, GameEntropy.Boring, SaltSource.CreateFixed("salt"));
        var source = NewSource();

        var edges = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.Equal(seed.TownCount - 1, edges.Count);
    }

    [Fact]
    public void Generate_NonBoring_SameSeedSameSaltIsDeterministic()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2, density: GraphDensity.Sparse);
        var placement = PlaceTowns(seed, GameEntropy.Classic, SaltSource.CreateFixed("deterministic-salt"));
        var source = NewSource();
        var salt = SaltSource.CreateFixed("deterministic-salt");

        var a = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Classic, salt);
        var b = TrailGraphGenerator.Generate(seed, placement.Towns, placement.ClusterAssignments, source, GameEntropy.Classic, salt);

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].OrderedSlots, b[i].OrderedSlots);
        }
    }

    [Fact]
    public void Generate_RedundantCorridorFilter_RemovesAcWhenAbAndBcExist()
    {
        // Construct a synthetic town layout where town B lies near the midpoint
        // of the line from A to C. The redundant-corridor filter should remove
        // edge A-C when both A-B and B-C are accepted.
        var towns = new Dictionary<int, (int X, int Y)>
        {
            { 0, (100, 100) },  // A
            { 1, (200, 105) },  // B — near midpoint of A-C, slightly off the line
            { 2, (300, 100) },  // C
            { 3, (200, 250) },  // D — off to the side so the graph is not degenerate
        };
        var clusters = new Dictionary<int, int> { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 1 } };
        var seed = NewSeedWorld(townCount: 4, clusterCount: 2, density: GraphDensity.Dense);
        var source = NewSource();

        var edges = TrailGraphGenerator.Generate(seed, towns, clusters, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        var hasAc = edges.Any(e =>
            (e.FromSlot == 0 && e.ToSlot == 2) || (e.FromSlot == 2 && e.ToSlot == 0));
        Assert.False(hasAc, "Redundant-corridor filter should remove edge A-C when A-B and B-C are accepted and B is near the A-C line.");
    }

    private static Dictionary<int, HashSet<int>> BuildAdjacency(IReadOnlyList<TrailEdge> edges, int townCount)
    {
        var adjacency = new Dictionary<int, HashSet<int>>();
        for (var i = 0; i < townCount; i++) adjacency[i] = new HashSet<int>();
        foreach (var edge in edges)
        {
            adjacency[edge.FromSlot].Add(edge.ToSlot);
            adjacency[edge.ToSlot].Add(edge.FromSlot);
        }
        return adjacency;
    }

    private static bool IsConnected(Dictionary<int, HashSet<int>> adjacency, int townCount)
    {
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0);
        visited.Add(0);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!adjacency.ContainsKey(current)) continue;
            foreach (var neighbor in adjacency[current])
            {
                if (visited.Add(neighbor)) queue.Enqueue(neighbor);
            }
        }
        return visited.Count == townCount;
    }

    private static bool HasCrossing(IReadOnlyList<TrailEdge> edges, Dictionary<int, (int X, int Y)> towns)
    {
        for (var i = 0; i < edges.Count; i++)
        {
            for (var j = i + 1; j < edges.Count; j++)
            {
                var a = edges[i];
                var b = edges[j];
                var sharedEndpoint = a.FromSlot == b.FromSlot || a.FromSlot == b.ToSlot
                    || a.ToSlot == b.FromSlot || a.ToSlot == b.ToSlot;
                if (sharedEndpoint) continue;

                var p1 = towns[a.FromSlot];
                var p2 = towns[a.ToSlot];
                var p3 = towns[b.FromSlot];
                var p4 = towns[b.ToSlot];

                if (SegmentsIntersect(p1, p2, p3, p4)) return true;
            }
        }
        return false;
    }

    private static bool SegmentsIntersect((int X, int Y) p1, (int X, int Y) p2, (int X, int Y) p3, (int X, int Y) p4)
    {
        var d1 = Sign(p3, p4, p1);
        var d2 = Sign(p3, p4, p2);
        var d3 = Sign(p1, p2, p3);
        var d4 = Sign(p1, p2, p4);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
            && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));

        static int Sign((int X, int Y) a, (int X, int Y) b, (int X, int Y) c)
            => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X) switch
            {
                > 0 => 1,
                < 0 => -1,
                _ => 0
            };
    }
}
```

---

## 4. TerrainAssignerTests

**File:** `tests/WildBunch.GameContent.Tests/TerrainAssignerTests.cs`

```csharp
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
```

---

## 5. OutlierGuaranteeTests

**File:** `tests/WildBunch.GameContent.Tests/OutlierGuaranteeTests.cs`

```csharp
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
```

---

## 6. MapGeneratorTests

**File:** `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs`

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
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

        Assert.Equal(a.Towns.Count, b.Towns.Count);
        for (var i = 0; i < a.Towns.Count; i++)
        {
            Assert.Equal(a.Towns[i].MapX, b.Towns[i].MapX);
            Assert.Equal(a.Towns[i].MapY, b.Towns[i].MapY);
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

        Assert.Equal(a.Towns.Count, b.Towns.Count);
        for (var i = 0; i < a.Towns.Count; i++)
        {
            Assert.Equal(a.Towns[i].MapX, b.Towns[i].MapX);
            Assert.Equal(a.Towns[i].MapY, b.Towns[i].MapY);
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
    public void Generate_NormalTrails_In2To5DayRange()
    {
        var seed = NewSeedWorld(townCount: 8, clusterCount: 2);
        var source = NewSource();

        var world = MapGenerator.Generate(seed, source, GameEntropy.Boring, SaltSource.CreateFixed("salt"));

        Assert.All(world.Trails, t => Assert.InRange(t.RideDayDistance, 2m, 6m));
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

    private static bool SegmentsIntersect((int MapX, int MapY) p1, (int MapX, int MapY) p2, (int MapX, int MapY) p3, (int MapX, int MapY) p4)
    {
        var d1 = Sign(p3, p4, p1);
        var d2 = Sign(p3, p4, p2);
        var d3 = Sign(p1, p2, p3);
        var d4 = Sign(p1, p2, p4);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
            && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));

        static int Sign((int MapX, int MapY) a, (int MapX, int MapY) b, (int MapX, int MapY) c)
            => (b.MapX - a.MapX) * (c.MapY - a.MapY) - (b.MapY - a.MapY) * (c.MapX - a.MapX) switch
            {
                > 0 => 1,
                < 0 => -1,
                _ => 0
            };
    }
}
```
