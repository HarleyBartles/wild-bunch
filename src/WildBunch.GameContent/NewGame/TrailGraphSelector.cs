// src/WildBunch.GameContent/NewGame/TrailGraphSelector.cs
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

public static class TrailGraphSelector
{
    public static IReadOnlyList<TrailEdgeCandidate> SelectConnectedGraph(
        IReadOnlyList<TrailEdgeCandidate> candidates,
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

        // Build minimum connected graph
        while (connected.Count < townCount && remaining.Count > 0)
        {
            // Find edge that connects to the connected component
            var connectingEdge = remaining.FirstOrDefault(e =>
                connected.Contains(e.FromSlot) && !connected.Contains(e.ToSlot) ||
                connected.Contains(e.ToSlot) && !connected.Contains(e.FromSlot));

            if (connectingEdge != null)
            {
                selected.Add(connectingEdge);
                connected.Add(connectingEdge.FromSlot);
                connected.Add(connectingEdge.ToSlot);
                remaining.Remove(connectingEdge);
            }
            else
            {
                // No connecting edge found, break to avoid infinite loop
                break;
            }
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
            selected.Add(remaining[0]);
            remaining.RemoveAt(0);
        }

        return selected;
    }

    private static int ComputeStableHash(string seedCode, string entropyMode, string salt)
    {
        var input = $"{seedCode}-{entropyMode}-{salt}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return BitConverter.ToInt32(hashBytes, 0);
    }
}
