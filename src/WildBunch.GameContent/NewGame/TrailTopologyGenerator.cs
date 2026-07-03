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

        // Step 2: Filter edges to remove crossings, parallel corridors, and redundant routes
        var filtered = TrailEdgeFilter.FilterCrossingEdges(candidates, new List<TrailEdgeCandidate>(), townCoordinates);
        filtered = TrailEdgeFilter.FilterParallelCorridors(filtered, new List<TrailEdgeCandidate>(), townCoordinates);
        filtered = TrailEdgeFilter.FilterRedundantRoutes(filtered, new List<TrailEdgeCandidate>(), townCoordinates);

        // Step 3: Select connected graph using deterministic entropy
        var selected = TrailGraphSelector.SelectConnectedGraph(
            filtered,
            townCoordinates.Count,
            entropy,
            saltSource,
            source);

        // Step 4: Apply filtering iteratively as edges are added to prevent crossings with newly added edges
        var finalEdges = new List<TrailEdgeCandidate>();
        var remaining = selected.ToList();
        
        while (remaining.Count > 0)
        {
            var edge = remaining[0];
            remaining.RemoveAt(0);
            
            // Check if this edge would cross or be parallel to any already-selected edge
            var canAdd = true;
            foreach (var existing in finalEdges)
            {
                var from1 = new Vector2(townCoordinates[edge.FromSlot].X, townCoordinates[edge.FromSlot].Y);
                var to1 = new Vector2(townCoordinates[edge.ToSlot].X, townCoordinates[edge.ToSlot].Y);
                var from2 = new Vector2(townCoordinates[existing.FromSlot].X, townCoordinates[existing.FromSlot].Y);
                var to2 = new Vector2(townCoordinates[existing.ToSlot].X, townCoordinates[existing.ToSlot].Y);

                // Skip if they share a town
                if (edge.FromSlot == existing.FromSlot || edge.FromSlot == existing.ToSlot ||
                    edge.ToSlot == existing.FromSlot || edge.ToSlot == existing.ToSlot)
                {
                    continue;
                }

                if (TrailGeometry.LinesIntersect(from1, to1, from2, to2))
                {
                    canAdd = false;
                    break;
                }

                if (TrailGeometry.AreLinesParallel(from1, to1, from2, to2, threshold: 0.1))
                {
                    canAdd = false;
                    break;
                }
            }

            if (canAdd)
            {
                finalEdges.Add(edge);
            }
        }

        // Step 5: Convert to SeedWorldTrail with ride-day distances
        var trails = new List<SeedWorldTrail>();
        foreach (var edge in finalEdges)
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
