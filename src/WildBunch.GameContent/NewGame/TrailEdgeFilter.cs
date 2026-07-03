// src/WildBunch.GameContent/NewGame/TrailEdgeFilter.cs
using System.Numerics;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public static class TrailEdgeFilter
{
    public static IReadOnlyList<TrailEdgeCandidate> FilterCrossingEdges(
        IReadOnlyList<TrailEdgeCandidate> candidates,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> coordinates)
    {
        return candidates.Where(candidate =>
        {
            foreach (var acceptedEdge in accepted)
            {
                if (EdgesCross(candidate, acceptedEdge, coordinates))
                {
                    return false;
                }
            }
            return true;
        }).ToList();
    }

    public static bool EdgesCross(
        TrailEdgeCandidate candidate,
        TrailEdgeCandidate acceptedEdge,
        Dictionary<int, (int X, int Y)> coordinates)
    {
        var from1 = new Vector2(coordinates[candidate.FromSlot].X, coordinates[candidate.FromSlot].Y);
        var to1 = new Vector2(coordinates[candidate.ToSlot].X, coordinates[candidate.ToSlot].Y);
        var from2 = new Vector2(coordinates[acceptedEdge.FromSlot].X, coordinates[acceptedEdge.FromSlot].Y);
        var to2 = new Vector2(coordinates[acceptedEdge.ToSlot].X, coordinates[acceptedEdge.ToSlot].Y);

        // Don't filter if edges share a town (they meet at the town, not crossing)
        if (candidate.FromSlot == acceptedEdge.FromSlot ||
            candidate.FromSlot == acceptedEdge.ToSlot ||
            candidate.ToSlot == acceptedEdge.FromSlot ||
            candidate.ToSlot == acceptedEdge.ToSlot)
        {
            return false;
        }

        return TrailGeometry.LinesIntersect(from1, to1, from2, to2);
    }

    public static IReadOnlyList<TrailEdgeCandidate> FilterParallelCorridors(
        IReadOnlyList<TrailEdgeCandidate> candidates,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> coordinates,
        double threshold = 0.1)
    {
        return candidates.Where(candidate =>
        {
            foreach (var acceptedEdge in accepted)
            {
                if (AreParallelCorridors(candidate, acceptedEdge, coordinates, threshold))
                {
                    return false;
                }
            }
            return true;
        }).ToList();
    }

    public static bool AreParallelCorridors(
        TrailEdgeCandidate candidate,
        TrailEdgeCandidate acceptedEdge,
        Dictionary<int, (int X, int Y)> coordinates,
        double threshold = 0.1)
    {
        // Don't filter if edges share a town
        if (candidate.FromSlot == acceptedEdge.FromSlot ||
            candidate.FromSlot == acceptedEdge.ToSlot ||
            candidate.ToSlot == acceptedEdge.FromSlot ||
            candidate.ToSlot == acceptedEdge.ToSlot)
        {
            return false;
        }

        var from1 = new Vector2(coordinates[candidate.FromSlot].X, coordinates[candidate.FromSlot].Y);
        var to1 = new Vector2(coordinates[candidate.ToSlot].X, coordinates[candidate.ToSlot].Y);
        var from2 = new Vector2(coordinates[acceptedEdge.FromSlot].X, coordinates[acceptedEdge.FromSlot].Y);
        var to2 = new Vector2(coordinates[acceptedEdge.ToSlot].X, coordinates[acceptedEdge.ToSlot].Y);

        return TrailGeometry.AreLinesParallel(from1, to1, from2, to2, threshold);
    }

    public static IReadOnlyList<TrailEdgeCandidate> FilterRedundantRoutes(
        IReadOnlyList<TrailEdgeCandidate> candidates,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> coordinates)
    {
        return candidates.Where(candidate =>
        {
            foreach (var acceptedEdge in accepted)
            {
                if (AreRedundantRoutes(candidate, acceptedEdge, accepted))
                {
                    return false;
                }
            }
            return true;
        }).ToList();
    }

    public static bool AreRedundantRoutes(
        TrailEdgeCandidate candidate,
        TrailEdgeCandidate acceptedEdge,
        IReadOnlyList<TrailEdgeCandidate> accepted)
    {
        // Check if there's already an indirect route between these towns
        var reachableFromCandidate = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(candidate.FromSlot);
        reachableFromCandidate.Add(candidate.FromSlot);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var edge in accepted)
            {
                if (edge.FromSlot == current && !reachableFromCandidate.Contains(edge.ToSlot))
                {
                    if (edge.ToSlot == candidate.ToSlot)
                    {
                        // Found indirect route
                        return true;
                    }
                    reachableFromCandidate.Add(edge.ToSlot);
                    queue.Enqueue(edge.ToSlot);
                }
                else if (edge.ToSlot == current && !reachableFromCandidate.Contains(edge.FromSlot))
                {
                    if (edge.FromSlot == candidate.ToSlot)
                    {
                        // Found indirect route
                        return true;
                    }
                    reachableFromCandidate.Add(edge.FromSlot);
                    queue.Enqueue(edge.FromSlot);
                }
            }
        }

        return false;
    }
}
