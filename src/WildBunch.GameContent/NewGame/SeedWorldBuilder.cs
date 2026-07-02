using System.Linq;
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

        // Derive canonical distances from geometry
        var trailsWithGeometryDistances = DeriveDistancesFromGeometry(trails, townCoordinates);

        // Trim outlier trail sets for Classic, Adventurous, and Wild entropy.
        // Boring keeps full trail connectivity.
        // No towns are removed — only trail sets are trimmed so that a remote
        // outlier town keeps exactly one 6-day trail (dead-end) and other
        // excessive 6-day trails are reduced to 5 days.
        var trimmedTrails = entropy != GameEntropy.Boring
            ? TrimOutlierTrailSets(trailsWithGeometryDistances, townNames.Count)
            : trailsWithGeometryDistances;

        return SeedWorldCatalog.CreateWorld(
            seedWorld.WorldVariant,
            townNames,
            seedWorld.ServicesPalette,
            seedWorld.ProsperityPalette,
            trimmedTrails,
            townCoordinates);
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
                
                // Reduce variance by 4 to keep trails under 6 days
                varianceX /= 4;
                varianceY /= 4;
                
                // Layout-specific variance preferences
                if (layout is MapLayoutPalette.LinearChain or MapLayoutPalette.DoubleLine)
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
        var hashBytes = System.Security.Cryptography.SHA256.HashData(bytes);
        return BitConverter.ToInt32(hashBytes, 0);
    }

    /// <summary>
    /// Derives canonical ride-day distances from the Euclidean geometry of town coordinates.
    /// Distance is calculated as the Euclidean distance between towns, scaled to ride-day units
    /// (approximately 1 ride-day per 25 coordinate units), rounded to 1 decimal place.
    /// Capped to 2-6 days.
    /// </summary>
    private static IReadOnlyList<SeedWorldTrail> DeriveDistancesFromGeometry(
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)> townCoordinates)
    {
        const double CoordinateScale = 25.0; // 1 ride-day per 25 coordinate units
        const decimal MinDays = 2m;
        const decimal MaxDays = 6m;

        return trails.Select(trail =>
        {
            // Extract slot indices from trail ID (format: "trail-{fromSlot}-{toSlot}")
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);

            var fromCoords = townCoordinates[fromSlot];
            var toCoords = townCoordinates[toSlot];

            // Calculate Euclidean distance
            var dx = toCoords.X - fromCoords.X;
            var dy = toCoords.Y - fromCoords.Y;
            var coordinateDistance = Math.Sqrt(dx * dx + dy * dy);

            // Scale to ride-day distance and round to 1 decimal place
            var rideDayDistance = Math.Round(coordinateDistance / CoordinateScale, 1);
            
            // Cap to 2-6 days
            var cappedDistance = Math.Max(MinDays, Math.Min(MaxDays, (decimal)rideDayDistance));

            return trail with { RideDayDistance = cappedDistance };
        }).ToArray();
    }

    /// <summary>
    /// Trims outlier trail sets for non-Boring entropy. No towns are removed
    /// from the world — only trail sets are adjusted:
    /// - If any town has a 6-day trail, one outlier town is selected (the town
    ///   with the most 6-day trails, breaking ties by lowest degree then lowest
    ///   slot index). That outlier keeps exactly one 6-day trail (its remote
    ///   dead-end) and its other trails are removed.
    /// - Other towns that also have 6-day trails have those trails reduced to
    ///   5 days so the outlier is the only true remote dead-end.
    /// - Connectivity is preserved: the outlier remains reachable via its one
    ///   retained trail, and all other towns keep their existing connections.
    /// </summary>
    private static IReadOnlyList<SeedWorldTrail> TrimOutlierTrailSets(
        IReadOnlyList<SeedWorldTrail> trails,
        int townCount)
    {
        // Find all 6-day trails and which towns they connect
        var sixDayTrails = trails.Where(t => t.RideDayDistance == 6m).ToList();
        if (sixDayTrails.Count == 0)
            return trails;

        // Count how many 6-day trails each town is involved in
        var sixDayCountByTown = new Dictionary<int, int>();
        for (var i = 0; i < townCount; i++)
            sixDayCountByTown[i] = 0;

        foreach (var trail in sixDayTrails)
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);
            if (fromSlot < townCount) sixDayCountByTown[fromSlot]++;
            if (toSlot < townCount) sixDayCountByTown[toSlot]++;
        }

        // Build overall degree (all trails, not just 6-day) for tie-breaking
        var totalDegree = new Dictionary<int, int>();
        for (var i = 0; i < townCount; i++)
            totalDegree[i] = 0;
        foreach (var trail in trails)
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);
            if (fromSlot < townCount) totalDegree[fromSlot]++;
            if (toSlot < townCount) totalDegree[toSlot]++;
        }

        // Select the outlier: town with the most 6-day trails,
        // breaking ties by lowest total degree, then lowest slot index.
        var outlierSlot = sixDayCountByTown
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => totalDegree[kvp.Key])
            .ThenBy(kvp => kvp.Key)
            .First().Key;

        // Pick the one 6-day trail to retain for the outlier.
        // Prefer the 6-day trail that connects to the highest-degree neighbor
        // (keeps the outlier connected to the most-connected hub).
        var outlierSixDayTrails = sixDayTrails.Where(t =>
        {
            var parts = t.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);
            return fromSlot == outlierSlot || toSlot == outlierSlot;
        }).ToList();

        var retainedTrail = outlierSixDayTrails
            .OrderByDescending(t =>
            {
                var parts = t.Id.Split('-');
                var fromSlot = int.Parse(parts[1]);
                var toSlot = int.Parse(parts[2]);
                var neighborSlot = fromSlot == outlierSlot ? toSlot : fromSlot;
                return totalDegree[neighborSlot];
            })
            .ThenBy(t => t.Id)
            .First();

        // Build the trimmed trail list:
        // - Remove all trails touching the outlier except the retained one
        // - Reduce other 6-day trails (not touching the outlier) to 5 days
        var result = new List<SeedWorldTrail>();
        foreach (var trail in trails)
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);
            var touchesOutlier = fromSlot == outlierSlot || toSlot == outlierSlot;

            if (touchesOutlier)
            {
                // Keep only the retained trail for the outlier
                if (trail.Id == retainedTrail.Id)
                    result.Add(trail);
                // All other trails touching the outlier are dropped
            }
            else
            {
                // Reduce non-outlier 6-day trails to 5 days
                if (trail.RideDayDistance == 6m)
                    result.Add(trail with { RideDayDistance = 5m });
                else
                    result.Add(trail);
            }
        }

        // Verify connectivity is preserved with all towns still present
        if (!VerifyConnectivity(townCount, result))
        {
            // If trimming broke connectivity, fall back to just reducing
            // all 6-day trails to 5 days without removing any trails
            return trails.Select(t => t.RideDayDistance == 6m ? t with { RideDayDistance = 5m } : t).ToArray();
        }

        return result;
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
