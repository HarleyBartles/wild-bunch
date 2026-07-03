// src/WildBunch.GameContent/NewGame/TrailTopologyGenerator.cs
using System.Numerics;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public static class TrailTopologyGenerator
{
    private const double CoordinateScale = 25.0; // 1 ride-day per 25 coordinate units

    public static IReadOnlyList<SeedWorldTrail> GenerateTrailTopology(
        Dictionary<int, (int X, int Y)> townCoordinates,
        IReadOnlyList<TownNameEntry> townNames,
        GameEntropy entropy,
        SaltSource? saltSource,
        GameSetupDeterministicSource source,
        int? outlierSlot)
    {
        // Step 1: Generate all candidate edges
        var candidates = TrailEdgeGenerator.GenerateCandidateEdges(townCoordinates);

        // Step 2: Select connected graph using deterministic entropy with incremental edge-quality filtering
        var selected = TrailGraphSelector.SelectConnectedGraph(
            candidates,
            townCoordinates,
            townNames,
            townCoordinates.Count,
            entropy,
            saltSource,
            source);

        // Step 3: Convert to SeedWorldTrail with ride-day distances
        var trails = new List<SeedWorldTrail>();
        foreach (var edge in selected)
        {
            var rideDays = RideDayCalculator.CalculateRideDays(edge, CoordinateScale, outlierSlot);
            
            trails.Add(new SeedWorldTrail(
                $"trail-{edge.FromSlot}-{edge.ToSlot}",
                townNames[edge.FromSlot].Id,
                townNames[edge.ToSlot].Id,
                TrailRisk.Moderate, // Default risk - can be enhanced later
                TrailTerrain.OpenRange, // Default terrain - can be enhanced later
                WaterFeature.Creek, // Default water - can be enhanced later
                rideDays));
        }

        return trails;
    }
}
