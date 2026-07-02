using System;
using System.Collections.Generic;
using System.Linq;
using WildBunch.GameContent.NewGame;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.GameContent.Tests;

/// <summary>
/// Tests that verify map layouts work correctly at different town count scales.
/// Seeds are generated dynamically via codec roundtrip to remain resilient to codec changes.
/// </summary>
public class MapLayoutScaleTests
{
    private static readonly MapLayoutPalette[] AllLayouts =
    [
        MapLayoutPalette.HubAndSpoke,
        MapLayoutPalette.LinearChain,
        MapLayoutPalette.Ring,
        MapLayoutPalette.DoubleLine
    ];

    private static readonly int[] RepresentativeTownCounts = [5, 8, 10];

    [Theory]
    [MemberData(nameof(GetLayoutAndTownCountCombinations))]
    public void SeedCodesRoundTripCorrectly(MapLayoutPalette layout, int townCount)
    {
        var seedCode = GenerateSeedCodeForLayout(layout, townCount);
        var seedWorld = SeedWorldResolver.Resolve(seedCode);

        Assert.Equal(layout, seedWorld.MapLayoutPalette);
        Assert.Equal(townCount, seedWorld.TownCount);
    }

    [Theory]
    [MemberData(nameof(GetLayoutAndTownCountCombinations))]
    public void LayoutsGenerateConnectedGraphs(MapLayoutPalette layout, int townCount)
    {
        var variant = SeedWorldVariant.Canonical;
        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, 1, 3, 0,
            ProsperityPalette.UniformProsperous,
            ServicesPalette.HubTelegraph,
            layout);

        var trails = SeedWorldCatalog.BuildTrails(variant, townNames, layout);

        // Build adjacency list
        var adjacency = new Dictionary<string, HashSet<string>>();
        foreach (var town in townNames)
        {
            adjacency[town.Id] = new HashSet<string>();
        }

        foreach (var trail in trails)
        {
            adjacency[trail.FromTownId].Add(trail.ToTownId);
            adjacency[trail.ToTownId].Add(trail.FromTownId);
        }

        // Check that every town has at least one connection
        foreach (var town in townNames)
        {
            Assert.True(adjacency[town.Id].Count > 0,
                $"Town {town.Name} ({town.Id}) has no connections in {layout} with {townCount} towns");
        }

        // Check that the graph is connected (BFS from first town reaches all towns)
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(townNames[0].Id);
        visited.Add(townNames[0].Id);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        Assert.Equal(townCount, visited.Count);
    }

    public static IEnumerable<object[]> GetLayoutAndTownCountCombinations()
    {
        foreach (var layout in AllLayouts)
        {
            foreach (var townCount in RepresentativeTownCounts)
            {
                yield return new object[] { layout, townCount };
            }
        }
    }

    private static Guid GenerateSeedCodeForLayout(MapLayoutPalette layout, int townCount)
    {
        var variant = SeedWorldVariant.Canonical;
        var accusationIndex = 1;
        var defaultCulpritIndex = 3;
        var cashBonus = 0;
        var prosperityPalette = ProsperityPalette.UniformProsperous;
        var servicesPalette = ServicesPalette.HubTelegraph;

        var dummyTownIds = new string[townCount];
        for (int i = 0; i < townCount; i++)
        {
            dummyTownIds[i] = Guid.NewGuid().ToString();
        }

        var dummyTownServices = new Dictionary<string, TownServices>();
        var dummyTrails = Array.Empty<SeedWorldTrail>();

        var seedWorld = new SeedWorld(
            Guid.Empty,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            layout,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            dummyTownIds,
            dummyTownServices,
            dummyTrails,
            HasOutlierSlot: false);

        return SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld);
    }
}
