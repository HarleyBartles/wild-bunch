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
