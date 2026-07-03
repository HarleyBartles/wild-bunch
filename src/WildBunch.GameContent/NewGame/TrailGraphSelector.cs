// src/WildBunch.GameContent/NewGame/TrailGraphSelector.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public static class TrailGraphSelector
{
    public static IReadOnlyList<TrailEdgeCandidate> SelectConnectedGraph(
        IReadOnlyList<TrailEdgeCandidate> candidates,
        Dictionary<int, (int X, int Y)> townCoordinates,
        IReadOnlyList<TownNameEntry> townNames,
        int townCount,
        GameEntropy entropy,
        SaltSource? saltSource,
        GameSetupDeterministicSource source)
    {
        if (townCount < 2)
            return candidates;

        // Use minimum spanning tree approach for connectivity
        // Start with all towns unconnected
        var connected = new HashSet<int> { 0 };
        var selected = new List<TrailEdgeCandidate>();
        var remaining = candidates.ToList();

        // For Boring mode, use deterministic selection by distance (shortest first)
        // For entropic modes, use salt-based selection
        if (entropy == GameEntropy.Boring)
        {
            remaining = remaining.OrderBy(e => e.PixelDistance).ToList();
        }
        else if (saltSource != null)
        {
            var salt = saltSource.Salt;
            var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));
            remaining = remaining.OrderBy(_ => random.Next()).ToList();
        }

        // Build minimum connected graph with incremental edge-quality filtering
        // Only apply quality filters when there are alternative edges available
        while (connected.Count < townCount && remaining.Count > 0)
        {
            // Find edge that connects to the connected component
            var connectingEdge = remaining.FirstOrDefault(e =>
                connected.Contains(e.FromSlot) && !connected.Contains(e.ToSlot) ||
                connected.Contains(e.ToSlot) && !connected.Contains(e.FromSlot));

            if (connectingEdge != null)
            {
                // Try to find a non-crossing edge if possible
                var alternativeEdge = remaining.FirstOrDefault(e =>
                    (connected.Contains(e.FromSlot) && !connected.Contains(e.ToSlot) ||
                     connected.Contains(e.ToSlot) && !connected.Contains(e.FromSlot)) &&
                    !WouldCrossExistingEdges(e, selected, townCoordinates) &&
                    !WouldCreateParallelCorridor(e, selected, townCoordinates));

                // Use the alternative if found, otherwise use the connecting edge
                var edgeToAdd = alternativeEdge ?? connectingEdge;

                selected.Add(edgeToAdd);
                connected.Add(edgeToAdd.FromSlot);
                connected.Add(edgeToAdd.ToSlot);
                remaining.Remove(edgeToAdd);
            }
            else
            {
                // No connecting edge found, break to avoid infinite loop
                break;
            }
        }

        // Verify connectivity - if we failed to connect all towns, this is a critical failure
        if (connected.Count < townCount)
        {
            throw new InvalidOperationException(
                $"Failed to build connected trail graph: only {connected.Count} of {townCount} towns connected. " +
                "This indicates a problem with town placement or edge generation that prevents connectivity.");
        }

        // Add extra edges based on entropy level for variety
        var extraEdges = entropy switch
        {
            GameEntropy.Boring => 0,
            GameEntropy.Classic => 1,
            GameEntropy.Adventurous => 2,
            GameEntropy.Wild => 3,
            _ => 0
        };

        for (var i = 0; i < extraEdges && remaining.Count > 0; i++)
        {
            var extraEdge = remaining[0];
            // Apply edge-quality filters to extra edges as well
            if (!WouldCrossExistingEdges(extraEdge, selected, townCoordinates) &&
                !WouldCreateParallelCorridor(extraEdge, selected, townCoordinates))
            {
                selected.Add(extraEdge);
            }
            remaining.RemoveAt(0);
        }

        return selected;
    }

    private static bool WouldCrossExistingEdges(
        TrailEdgeCandidate candidate,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> townCoordinates)
    {
        foreach (var acceptedEdge in accepted)
        {
            if (TrailEdgeFilter.EdgesCross(candidate, acceptedEdge, townCoordinates))
                return true;
        }
        return false;
    }

    private static bool WouldCreateParallelCorridor(
        TrailEdgeCandidate candidate,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> townCoordinates)
    {
        foreach (var acceptedEdge in accepted)
        {
            if (TrailEdgeFilter.AreParallelCorridors(candidate, acceptedEdge, townCoordinates))
                return true;
        }
        return false;
    }

    private static bool WouldCreateRedundantRoute(
        TrailEdgeCandidate candidate,
        IReadOnlyList<TrailEdgeCandidate> accepted,
        Dictionary<int, (int X, int Y)> townCoordinates)
    {
        return TrailEdgeFilter.AreRedundantRoutes(candidate, candidate, accepted);
    }

    private static int ComputeStableHash(string seedCode, string entropyMode, string salt)
    {
        var input = $"{seedCode}-{entropyMode}-{salt}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return BitConverter.ToInt32(hashBytes, 0);
    }
}
