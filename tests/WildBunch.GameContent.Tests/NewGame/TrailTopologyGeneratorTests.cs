// tests/WildBunch.GameContent.Tests/NewGame/TrailTopologyGeneratorTests.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

public class TrailTopologyGeneratorTests
{
    [Fact]
    public void GenerateTrailTopology_ProducesConnectedGraph()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250),
            [2] = (400, 50)
        };

        var townNames = new[]
        {
            new TownNameEntry("town-0", "Town 0"),
            new TownNameEntry("town-1", "Town 1"),
            new TownNameEntry("town-2", "Town 2")
        };

        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var trails = TrailTopologyGenerator.GenerateTrailTopology(
            coordinates,
            townNames,
            GameEntropy.Boring,
            null,
            source,
            outlierSlot: null);

        Assert.True(IsConnected(trails, townNames.Length));
    }

    [Fact]
    public void GenerateTrailTopology_DerivesRideDaysFromGeometry()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250) // 200 pixels apart = 8 ride days, should clamp to 5
        };

        var townNames = new[]
        {
            new TownNameEntry("town-0", "Town 0"),
            new TownNameEntry("town-1", "Town 1")
        };

        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var trails = TrailTopologyGenerator.GenerateTrailTopology(
            coordinates,
            townNames,
            GameEntropy.Boring,
            null,
            source,
            outlierSlot: null);

        var trail = trails.First();
        Assert.InRange(trail.RideDayDistance, 2m, 5m);
    }

    [Fact]
    public void GenerateTrailTopology_OutlierTownHasSixDayTrail()
    {
        var coordinates = new Dictionary<int, (int X, int Y)>
        {
            [0] = (400, 250),
            [1] = (600, 250)
        };

        var townNames = new[]
        {
            new TownNameEntry("town-0", "Town 0"),
            new TownNameEntry("town-1", "Town 1")
        };

        var source = new GameSetupDeterministicSource("00000000-0000-0000-0000-000000000001");
        
        var trails = TrailTopologyGenerator.GenerateTrailTopology(
            coordinates,
            townNames,
            GameEntropy.Boring,
            null,
            source,
            outlierSlot: 1);

        var trail = trails.First();
        Assert.Equal(6m, trail.RideDayDistance);
    }

    private bool IsConnected(IReadOnlyList<SeedWorldTrail> trails, int townCount)
    {
        if (townCount == 0) return true;
        
        var adjacency = new Dictionary<string, List<string>>();
        foreach (var trail in trails)
        {
            if (!adjacency.ContainsKey(trail.FromTownId))
                adjacency[trail.FromTownId] = new List<string>();
            if (!adjacency.ContainsKey(trail.ToTownId))
                adjacency[trail.ToTownId] = new List<string>();
            
            adjacency[trail.FromTownId].Add(trail.ToTownId);
            adjacency[trail.ToTownId].Add(trail.FromTownId);
        }

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        var startTown = trails.First().FromTownId;
        queue.Enqueue(startTown);
        visited.Add(startTown);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (adjacency.ContainsKey(current))
            {
                foreach (var neighbor in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return visited.Count == townCount;
    }
}
