// src/WildBunch.GameContent/NewGame/TrailEdgeGenerator.cs
using System.Numerics;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public sealed record TrailEdgeCandidate(
    int FromSlot,
    int ToSlot,
    double PixelDistance);

public static class TrailEdgeGenerator
{
    public static IReadOnlyList<TrailEdgeCandidate> GenerateCandidateEdges(
        Dictionary<int, (int X, int Y)> townCoordinates)
    {
        var edges = new List<TrailEdgeCandidate>();
        var slots = townCoordinates.Keys.OrderBy(x => x).ToList();

        for (var i = 0; i < slots.Count; i++)
        {
            for (var j = i + 1; j < slots.Count; j++)
            {
                var fromSlot = slots[i];
                var toSlot = slots[j];
                var fromCoords = townCoordinates[fromSlot];
                var toCoords = townCoordinates[toSlot];
                
                var from = new Vector2(fromCoords.X, fromCoords.Y);
                var to = new Vector2(toCoords.X, toCoords.Y);
                var distance = TrailGeometry.CalculatePixelDistance(from, to);
                
                edges.Add(new TrailEdgeCandidate(fromSlot, toSlot, distance));
            }
        }

        return edges;
    }
}
