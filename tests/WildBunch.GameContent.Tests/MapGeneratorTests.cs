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
