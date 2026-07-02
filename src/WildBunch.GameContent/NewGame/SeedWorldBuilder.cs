using System.Linq;
using System.Security.Cryptography;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class SeedWorldBuilder
{
    /// <summary>
    /// Builds the canonical world (8 towns, Canonical variant).
    /// Used by SeedWorldMapLayout for the start-screen map.
    /// </summary>
    public static World CreateCanonicalWorld()
        => SeedWorldCatalog.CreateCanonicalWorld();

    /// <summary>
    /// Builds a World from a SeedWorld template. The seed world holds the
    /// encoded fields (town count, palettes, variant) and derived fields
    /// (town names, services, trails). The catalog provides the name pool
    /// and slot-based topology.
    /// Future seam: DifficultyEnvelope may modify terrain/distance downstream.
    /// </summary>
    public static World CreateWorld(
        SeedWorld seedWorld,
        GameSetupDeterministicSource source,
        GameEntropy entropy = GameEntropy.Boring,
        SaltSource? saltSource = null)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        var townNames = SeedWorldCatalog.DeriveTownNames(
            seedWorld.WorldVariant,
            seedWorld.TownCount,
            seedWorld.AccusationIndex,
            seedWorld.DefaultCulpritIndex,
            seedWorld.CashBonus,
            seedWorld.ProsperityPalette,
            seedWorld.ServicesPalette,
            seedWorld.MapLayoutPalette);

        var trails = SeedWorldCatalog.BuildTrails(seedWorld.WorldVariant, townNames, seedWorld.MapLayoutPalette);

        // Derive town coordinates from map layout geometry
        var townCoordinates = DeriveTownCoordinates(townNames.Count, seedWorld.MapLayoutPalette, entropy, source, saltSource);

        // Derive ride-day distances and adjust coordinates in two passes:
        // Pass 1: full variance → geometry → ride days → clamp → trail removal
        // Pass 2: adjust coordinates to match final ride days for visual legibility
        var (trimmedTrails, adjustedCoordinates) = DeriveDistancesAndAdjustCoordinates(
            trails,
            townCoordinates,
            seedWorld.MapLayoutPalette,
            entropy,
            source,
            saltSource);

        return SeedWorldCatalog.CreateWorld(
            seedWorld.WorldVariant,
            townNames,
            seedWorld.ServicesPalette,
            seedWorld.ProsperityPalette,
            trimmedTrails,
            adjustedCoordinates,
            null);
    }

    /// <summary>
    /// Derives map coordinates for each town slot based on the map layout palette.
    /// Applies entropy-based coordinate variance for non-Boring modes.
    /// Returns a dictionary mapping slot index to (X, Y) coordinates.
    /// </summary>
    private static Dictionary<int, (int X, int Y)> DeriveTownCoordinates(
        int townCount,
        MapLayoutPalette layout,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        var coordinates = new Dictionary<int, (int, int)>();
        for (var i = 0; i < townCount; i++)
        {
            var baseCoords = SeedWorldMapLayout.GetCoordinatesForSlot(i, townCount, layout);
            
            // Apply entropy-based variance
            if (entropy != GameEntropy.Boring && saltSource != null)
            {
                var varianceRange = entropy switch
                {
                    GameEntropy.Classic => 40,
                    GameEntropy.Adventurous => 80,
                    GameEntropy.Wild => 120,
                    _ => 0
                };
                
                // Use salt source for variance (runtime salt varies by playthrough)
                var salt = saltSource.Salt;
                var hash = ComputeStableHash(source.SeedCode, i, entropy.ToString(), salt);
                var varianceX = (int)((hash % (varianceRange * 2 + 1)) - varianceRange);
                var varianceY = (int)(((hash >> 16) % (varianceRange * 2 + 1)) - varianceRange);

                // Layout-specific variance preferences
                if (layout is MapLayoutPalette.DoubleLine)
                {
                    // Prefer Y variance for wavy patterns without crossings
                    varianceX /= 2;
                    varianceY *= 2;
                }

                baseCoords = (baseCoords.X + varianceX, baseCoords.Y + varianceY);
            }
            
            coordinates[i] = baseCoords;
        }
        return coordinates;
    }

    /// <summary>
    /// Computes a stable deterministic hash for entropy variance.
    /// Uses SHA256 over explicit inputs to ensure consistency across runs.
    /// </summary>
    private static int ComputeStableHash(string seedCode, int slot, string entropyMode, string salt)
    {
        var input = $"{seedCode}-{slot}-{entropyMode}-{salt}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return BitConverter.ToInt32(hashBytes, 0);
    }

    /// <summary>
    /// Computes a stable deterministic hash for trail removal (no slot).
    /// Uses SHA256 over explicit inputs to ensure consistency across runs.
    /// </summary>
    private static int ComputeStableHash(string seedCode, string entropyMode, string salt)
    {
        var input = $"{seedCode}-{entropyMode}-{salt}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return BitConverter.ToInt32(hashBytes, 0);
    }

    /// <summary>
    /// Derives ride-day distances from geometry in two passes:
    /// Pass 1: Derive raw distances from geometry, clamp all 6-day trails to 5 days,
    ///         apply layout-specific trail removal.
    /// Pass 2: Adjust coordinates to match final ride days so visual lines make sense relative to labels.
    /// Returns the trimmed trails and adjusted coordinates.
    /// </summary>
    private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, Dictionary<int, (int X, int Y)> AdjustedCoordinates) DeriveDistancesAndAdjustCoordinates(
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)> townCoordinates,
        MapLayoutPalette layout,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        const double CoordinateScale = 25.0; // 1 ride-day per 25 coordinate units
        const decimal MinDays = 2m;
        const decimal MaxDays = 5m; // Reduced from 6m to 5m - no outlier special case

        // Pass 1: Derive raw distances from geometry and clamp to max 5 days
        var trailsWithClampedDistances = trails.Select(trail =>
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);
            var fromCoords = townCoordinates[fromSlot];
            var toCoords = townCoordinates[toSlot];
            var dx = toCoords.X - fromCoords.X;
            var dy = toCoords.Y - fromCoords.Y;
            var coordinateDistance = Math.Sqrt(dx * dx + dy * dy);
            var rawRideDays = Math.Round(coordinateDistance / CoordinateScale, 1);
            var clampedDistance = Math.Max(MinDays, Math.Min(MaxDays, (decimal)rawRideDays));
            return trail with { RideDayDistance = clampedDistance };
        }).ToArray();

        // Apply layout-specific trail removal by salt
        var trailsAfterRemoval = ApplyLayoutSpecificTrailRemoval(trailsWithClampedDistances, layout, entropy, source, saltSource, townCoordinates.Count);

        // Pass 2: Adjust coordinates to match final ride days
        var adjustedCoordinates = AdjustCoordinatesToMatchRideDays(trailsAfterRemoval, townCoordinates, CoordinateScale, MinDays, MaxDays, entropy);

        return (trailsAfterRemoval, adjustedCoordinates);
    }

    /// <summary>
    /// Applies layout-specific trail removal by salt.
    /// HubAndSpoke: remove random spokes/edges based on entropy
    /// Ring: remove 1 trail (or replace to maintain connectivity)
    /// LinearChain: no removal (line breaks)
    /// DoubleLine: remove trails between lines, preserve crossing trails (1-3)
    /// Returns the trimmed trails.
    /// </summary>
    private static IReadOnlyList<SeedWorldTrail> ApplyLayoutSpecificTrailRemoval(
        IReadOnlyList<SeedWorldTrail> trails,
        MapLayoutPalette layout,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        int townCount)
    {
        if (entropy == GameEntropy.Boring)
            return trails;

        return layout switch
        {
            MapLayoutPalette.HubAndSpoke => ApplyHubAndSpokeTrailRemoval(trails, entropy, source, saltSource, townCount),
            MapLayoutPalette.DoubleLine => ApplyDoubleLineTrailRemoval(trails, entropy, source, saltSource, townCount),
            MapLayoutPalette.XShaped => trails, // TODO: Implement XShaped trail removal
            MapLayoutPalette.Tree => trails, // TODO: Implement Tree trail removal
            MapLayoutPalette.Star => trails, // TODO: Implement Star trail removal
            MapLayoutPalette.Cluster => trails, // TODO: Implement Cluster trail removal
            MapLayoutPalette.Mesh => trails, // TODO: Implement Mesh trail removal
            MapLayoutPalette.Grid => trails, // TODO: Implement Grid trail removal
            _ => trails
        };
    }

    /// <summary>
    /// Applies HubAndSpoke-specific trail removal by salt.
    /// Hub is slot 0, spokes are trails from slot 0 to outer slots.
    /// Edge trails are trails between outer slots (the ring).
    /// </summary>
    private static IReadOnlyList<SeedWorldTrail> ApplyHubAndSpokeTrailRemoval(
        IReadOnlyList<SeedWorldTrail> trails,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        int townCount)
    {
        if (townCount < 3)
            return trails; // Need at least 3 towns for meaningful removal

        // Identify spokes (from slot 0) and edge trails (between outer slots)
        var spokes = new List<SeedWorldTrail>();
        var edgeTrails = new List<SeedWorldTrail>();

        foreach (var trail in trails)
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);

            if (fromSlot == 0 || toSlot == 0)
                spokes.Add(trail);
            else
                edgeTrails.Add(trail);
        }

        // Determine how many trails to remove based on entropy
        var (spokesToRemove, edgesToRemove) = entropy switch
        {
            GameEntropy.Classic => (1, 1),
            GameEntropy.Adventurous => (2, 2),
            GameEntropy.Wild => (3, 3),
            _ => (0, 0)
        };

        // Clamp to available trails
        spokesToRemove = Math.Min(spokesToRemove, spokes.Count - 1); // Keep at least 1 spoke
        edgesToRemove = Math.Min(edgesToRemove, edgeTrails.Count - 1); // Keep at least 1 edge

        if (spokesToRemove == 0 && edgesToRemove == 0)
            return trails;

        // Use salt for deterministic selection
        if (saltSource == null)
            return trails;

        var salt = saltSource.Salt;
        var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));

        // Select random spokes to remove
        var spokesToRemoveList = SelectRandomTrails(spokes, spokesToRemove, random);
        var edgesToRemoveList = SelectRandomTrails(edgeTrails, edgesToRemove, random);

        // Build result without removed trails
        var removedIds = new HashSet<string>(spokesToRemoveList.Concat(edgesToRemoveList).Select(t => t.Id));
        var result = trails.Where(t => !removedIds.Contains(t.Id)).ToList();

        // Verify connectivity
        if (!VerifyConnectivity(townCount, result))
        {
            // If removal broke connectivity, return original trails
            return trails;
        }

        return result;
    }

    /// <summary>
    /// Selects random trails for removal.
    /// </summary>
    private static List<SeedWorldTrail> SelectRandomTrails(
        List<SeedWorldTrail> trails,
        int count,
        Random random)
    {
        if (count == 0 || trails.Count == 0)
            return new List<SeedWorldTrail>();

        var result = new List<SeedWorldTrail>();
        var available = trails.ToList();

        while (result.Count < count && available.Count > 0)
        {
            var index = random.Next(available.Count);
            var trail = available[index];
            result.Add(trail);
            available.RemoveAt(index);
        }

        return result;
    }

    /// <summary>
    /// Applies DoubleLine-specific trail removal by salt.
    /// DoubleLine: remove trails between lines, preserve crossing trails (1-3).
    /// Crossing trails are slot 1-3 (they cross top to bottom between two towns).
    /// </summary>
    private static IReadOnlyList<SeedWorldTrail> ApplyDoubleLineTrailRemoval(
        IReadOnlyList<SeedWorldTrail> trails,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        int townCount)
    {
        if (townCount < 4)
            return trails; // Need at least 4 towns for DoubleLine

        // Identify crossing trails (1-3) and other trails
        var crossingTrails = new List<SeedWorldTrail>();
        var otherTrails = new List<SeedWorldTrail>();

        foreach (var trail in trails)
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);

            // Crossing trails are 1-3
            if ((fromSlot == 1 && toSlot == 3) || (fromSlot == 3 && toSlot == 1))
                crossingTrails.Add(trail);
            else
                otherTrails.Add(trail);
        }

        // Determine how many trails to remove based on entropy
        var trailsToRemove = entropy switch
        {
            GameEntropy.Classic => 1,
            GameEntropy.Adventurous => 2,
            GameEntropy.Wild => 3,
            _ => 0
        };

        trailsToRemove = Math.Min(trailsToRemove, otherTrails.Count - 1); // Keep at least 1 other trail

        if (trailsToRemove == 0)
            return trails;

        if (saltSource == null)
            return trails;

        var salt = saltSource.Salt;
        var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));

        // Select random trails to remove from otherTrails (not crossing trails)
        var trailsToRemoveList = SelectRandomTrails(otherTrails, trailsToRemove, random);

        // Build result: keep all crossing trails + remaining other trails
        var removedIds = new HashSet<string>(trailsToRemoveList.Select(t => t.Id));
        var result = crossingTrails.Concat(otherTrails.Where(t => !removedIds.Contains(t.Id))).ToList();

        // Verify connectivity
        if (!VerifyConnectivity(townCount, result))
        {
            // If removal broke connectivity, return original trails
            return trails;
        }

        return result;
    }

    /// <summary>
    /// Adjusts coordinates to match final ride days so visual lines make sense.
    /// For Boring mode (no variance), scales coordinates to match ride days.
    /// For non-Boring modes, keeps the original variance-based coordinates since
    /// they already produce visible geometric differences.
    /// </summary>
    private static Dictionary<int, (int X, int Y)> AdjustCoordinatesToMatchRideDays(
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)> originalCoordinates,
        double coordinateScale,
        decimal minDays,
        decimal maxDays,
        GameEntropy entropy)
    {
        // Only adjust coordinates for Boring mode (no variance)
        // For non-Boring modes, the variance-based coordinates already produce
        // visible geometric differences, so we keep them as-is
        if (entropy != GameEntropy.Boring)
            return originalCoordinates;

        // For Boring mode, scale coordinates to match ride days
        var totalCurrentDistance = 0.0;
        var totalTargetDistance = 0.0;
        var trailCount = 0;

        foreach (var trail in trails)
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);

            if (!originalCoordinates.TryGetValue(fromSlot, out var fromCoords) ||
                !originalCoordinates.TryGetValue(toSlot, out var toCoords))
                continue;

            var dx = toCoords.X - fromCoords.X;
            var dy = toCoords.Y - fromCoords.Y;
            var currentDistance = Math.Sqrt(dx * dx + dy * dy);
            var targetDistance = (double)trail.RideDayDistance * coordinateScale;

            totalCurrentDistance += currentDistance;
            totalTargetDistance += targetDistance;
            trailCount++;
        }

        if (trailCount == 0 || totalCurrentDistance == 0)
            return originalCoordinates;

        var scaleFactor = totalTargetDistance / totalCurrentDistance;

        // Scale all coordinates uniformly around the center
        var centerX = originalCoordinates.Values.Average(c => c.X);
        var centerY = originalCoordinates.Values.Average(c => c.Y);

        var adjustedCoordinates = new Dictionary<int, (int X, int Y)>();
        foreach (var kvp in originalCoordinates)
        {
            var slot = kvp.Key;
            var (x, y) = kvp.Value;

            var scaledX = (int)Math.Round(centerX + (x - centerX) * scaleFactor);
            var scaledY = (int)Math.Round(centerY + (y - centerY) * scaleFactor);

            adjustedCoordinates[slot] = (scaledX, scaledY);
        }

        return adjustedCoordinates;
    }

    /// <summary>
    /// Verifies that all towns in the trimmed world are reachable from each other.
    /// Uses BFS to check graph connectivity.
    /// </summary>
    private static bool VerifyConnectivity(
        int townCount,
        IReadOnlyList<SeedWorldTrail> trails)
    {
        if (townCount == 0)
            return true;

        // Build adjacency list
        var adjacency = new Dictionary<int, HashSet<int>>();
        for (var i = 0; i < townCount; i++)
        {
            adjacency[i] = new HashSet<int>();
        }

        foreach (var trail in trails)
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);
            if (fromSlot < townCount && toSlot < townCount)
            {
                adjacency[fromSlot].Add(toSlot);
                adjacency[toSlot].Add(fromSlot);
            }
        }

        // BFS from the first town
        var startSlot = 0;
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(startSlot);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current))
                continue;

            visited.Add(current);
            if (adjacency.ContainsKey(current))
            {
                foreach (var neighbor in adjacency[current])
                {
                    if (!visited.Contains(neighbor))
                        queue.Enqueue(neighbor);
                }
            }
        }

        // Check that all town slots were visited
        return visited.Count == townCount;
    }

    /// <summary>
    /// Checks whether the seed world is the canonical shape (8 towns,
    /// Canonical variant, HubTelegraph services, UniformProsperous prosperity,
    /// specific case fields).
    /// </summary>
    internal static bool IsCanonicalSeedWorld(SeedWorld seedWorld)
        => seedWorld.WorldVariant == SeedWorldVariant.Canonical
            && seedWorld.TownCount == 8
            && seedWorld.ServicesPalette == ServicesPalette.HubTelegraph
            && seedWorld.ProsperityPalette == ProsperityPalette.UniformProsperous
            && seedWorld.MapLayoutPalette == MapLayoutPalette.HubAndSpoke
            && seedWorld.AccusationIndex == 1
            && seedWorld.DefaultCulpritIndex == 3
            && seedWorld.CashBonus == 0;
}
