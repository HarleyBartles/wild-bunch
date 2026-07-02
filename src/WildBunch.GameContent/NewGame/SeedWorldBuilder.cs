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
        // Pass 1: full variance → geometry → ride days → clamp → outlier selection → trail removal
        // Pass 2: adjust coordinates to match final ride days for visual legibility
        var (trimmedTrails, adjustedCoordinates, outlierSlot) = DeriveDistancesAndAdjustCoordinates(
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
            outlierSlot);
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
    /// Pass 1: Derive raw distances from geometry, clamp to max 6 days, select outlier if any 6-day trails,
    ///         clamp others to 2-5 days via modulo clamping, apply layout-specific trail removal.
    /// Pass 2: Adjust coordinates to match final ride days so visual lines make sense relative to labels.
    /// Returns the trimmed trails, adjusted coordinates, and the outlier town slot (if any).
    /// </summary>
    private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, Dictionary<int, (int X, int Y)> AdjustedCoordinates, int? OutlierSlot) DeriveDistancesAndAdjustCoordinates(
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)> townCoordinates,
        MapLayoutPalette layout,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        const double CoordinateScale = 25.0; // 1 ride-day per 25 coordinate units
        const decimal MinDays = 2m;
        const decimal MaxDays = 6m;

        // Pass 1: Derive raw distances from geometry
        var trailsWithRawDistances = trails.Select(trail =>
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
            var clampedRaw = Math.Max(MinDays, Math.Min(MaxDays, (decimal)rawRideDays));
            return (trail, clampedRaw);
        }).ToArray();

        // Check if any 6-day trails exist after geometry skewing
        var hasSixDayTrails = trailsWithRawDistances.Any(t => t.clampedRaw == 6m);

        // If any 6-day trails, select outlier and clamp others to 2-5 days
        var (trailsAfterOutlierClamp, outlierSlot) = hasSixDayTrails
            ? ApplyOutlierClamping(trailsWithRawDistances, townCoordinates.Count, source, saltSource)
            : (trailsWithRawDistances.Select(t => t.trail with { RideDayDistance = t.clampedRaw }).ToArray(), (int?)null);

        // Apply layout-specific trail removal by salt
        var (trailsAfterRemoval, _) = ApplyLayoutSpecificTrailRemoval(trailsAfterOutlierClamp, layout, entropy, source, saltSource, townCoordinates.Count, outlierSlot);

        // Pass 2: Adjust coordinates to match final ride days
        var adjustedCoordinates = AdjustCoordinatesToMatchRideDays(trailsAfterRemoval, townCoordinates, CoordinateScale, MinDays, MaxDays, entropy);

        return (trailsAfterRemoval, adjustedCoordinates, outlierSlot);
    }

    /// <summary>
    /// Applies outlier clamping: if any 6-day trails exist, select one outlier town,
    /// keep exactly one 6-day trail for the outlier, clamp all other trails to 2-5 days
    /// via modulo clamping for fair spread.
    /// Returns the trimmed trails and the outlier town slot (if any).
    /// </summary>
    private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyOutlierClamping(
        (SeedWorldTrail trail, decimal rawDistance)[] trailsWithDistances,
        int townCount,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        // Find towns with 6-day trails
        var towns6Day = new HashSet<int>();
        foreach (var (trail, rawDistance) in trailsWithDistances)
        {
            if (rawDistance == 6m)
            {
                var parts = trail.Id.Split('-');
                var fromSlot = int.Parse(parts[1]);
                var toSlot = int.Parse(parts[2]);
                towns6Day.Add(fromSlot);
                towns6Day.Add(toSlot);
            }
        }

        if (towns6Day.Count == 0)
            return (trailsWithDistances.Select(t => t.trail with { RideDayDistance = t.rawDistance }).ToArray(), (int?)null);

        // Select outlier: town with most 6-day trails, tie-broken by degree then slot
        var town6DayCount = towns6Day.ToDictionary(t => t, t => 0);
        foreach (var (trail, rawDistance) in trailsWithDistances)
        {
            if (rawDistance == 6m)
            {
                var parts = trail.Id.Split('-');
                var fromSlot = int.Parse(parts[1]);
                var toSlot = int.Parse(parts[2]);
                if (towns6Day.Contains(fromSlot)) town6DayCount[fromSlot]++;
                if (towns6Day.Contains(toSlot)) town6DayCount[toSlot]++;
            }
        }

        var outlierSlot = town6DayCount.OrderByDescending(kvp => kvp.Value).First().Key;

        // Count total degree for tie-breaking
        var totalDegree = new Dictionary<int, int>();
        for (var i = 0; i < townCount; i++) totalDegree[i] = 0;
        foreach (var (trail, _) in trailsWithDistances)
        {
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);
            totalDegree[fromSlot]++;
            totalDegree[toSlot]++;
        }

        // Re-select outlier with degree tie-break
        var outlierCandidates = towns6Day.OrderByDescending(t => town6DayCount[t]).ThenBy(t => totalDegree[t]).ThenBy(t => t).ToList();
        outlierSlot = outlierCandidates.First();

        // Pick one 6-day trail to retain for the outlier (prefer highest-degree neighbor)
        var outlier6DayTrails = trailsWithDistances
            .Where(t => t.rawDistance == 6m)
            .Where(t =>
            {
                var parts = t.trail.Id.Split('-');
                var fromSlot = int.Parse(parts[1]);
                var toSlot = int.Parse(parts[2]);
                return fromSlot == outlierSlot || toSlot == outlierSlot;
            })
            .OrderByDescending(t =>
            {
                var parts = t.trail.Id.Split('-');
                var fromSlot = int.Parse(parts[1]);
                var toSlot = int.Parse(parts[2]);
                var neighborSlot = fromSlot == outlierSlot ? toSlot : fromSlot;
                return totalDegree[neighborSlot];
            })
            .ThenBy(t => t.trail.Id)
            .First();

        // Build result: outlier keeps one 6-day trail, others clamped to 2-5 via modulo
        var result = new List<SeedWorldTrail>();
        foreach (var (trail, rawDistance) in trailsWithDistances)
        {
            if (trail.Id == outlier6DayTrails.trail.Id)
            {
                result.Add(trail with { RideDayDistance = 6m });
            }
            else if (rawDistance == 6m)
            {
                // Clamp 6-day trails to 2-5 via modulo clamping
                var clamped = ((rawDistance - 2m) % 4m) + 2m;
                result.Add(trail with { RideDayDistance = clamped });
            }
            else
            {
                result.Add(trail with { RideDayDistance = rawDistance });
            }
        }

        return (result, outlierSlot);
    }

    /// <summary>
    /// Applies layout-specific trail removal by salt.
    /// HubAndSpoke: remove random spokes/edges based on entropy
    /// Ring: remove 1 trail (or replace to maintain connectivity)
    /// LinearChain: no removal (line breaks)
    /// DoubleLine: remove trails between lines, preserve crossing trails (1-3)
    /// Returns the trimmed trails and the outlier slot (unchanged from input).
    /// </summary>
    private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyLayoutSpecificTrailRemoval(
        IReadOnlyList<SeedWorldTrail> trails,
        MapLayoutPalette layout,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        int townCount,
        int? outlierSlot)
    {
        if (entropy == GameEntropy.Boring)
            return (trails, outlierSlot);

        return layout switch
        {
            MapLayoutPalette.HubAndSpoke => ApplyHubAndSpokeTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
            MapLayoutPalette.Ring => ApplyRingTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
            MapLayoutPalette.LinearChain => (trails, outlierSlot), // No removal - line breaks
            MapLayoutPalette.DoubleLine => ApplyDoubleLineTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
            _ => (trails, outlierSlot)
        };
    }

    /// <summary>
    /// Applies HubAndSpoke-specific trail removal by salt.
    /// Hub is slot 0, spokes are trails from slot 0 to outer slots.
    /// Edge trails are trails between outer slots (the ring).
    /// </summary>
    private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyHubAndSpokeTrailRemoval(
        IReadOnlyList<SeedWorldTrail> trails,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        int townCount,
        int? outlierSlot)
    {
        if (townCount < 3)
            return (trails, outlierSlot); // Need at least 3 towns for meaningful removal

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
            return (trails, outlierSlot);

        // Use salt for deterministic selection
        if (saltSource == null)
            return (trails, outlierSlot);

        var salt = saltSource.Salt;
        var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));

        // Select random spokes to remove (avoid removing the outlier's spoke if outlier exists)
        var spokesToRemoveList = SelectRandomTrails(spokes, spokesToRemove, random, outlierSlot, trails);
        var edgesToRemoveList = SelectRandomTrails(edgeTrails, edgesToRemove, random, null, trails);

        // Build result without removed trails
        var removedIds = new HashSet<string>(spokesToRemoveList.Concat(edgesToRemoveList).Select(t => t.Id));
        var result = trails.Where(t => !removedIds.Contains(t.Id)).ToList();

        // Verify connectivity
        if (!VerifyConnectivity(townCount, result))
        {
            // If removal broke connectivity, return original trails
            return (trails, outlierSlot);
        }

        return (result, outlierSlot);
    }

    /// <summary>
    /// Selects random trails for removal, avoiding trails that would disconnect the outlier.
    /// </summary>
    private static List<SeedWorldTrail> SelectRandomTrails(
        List<SeedWorldTrail> trails,
        int count,
        Random random,
        int? outlierSlot,
        IReadOnlyList<SeedWorldTrail> allTrails)
    {
        if (count == 0 || trails.Count == 0)
            return new List<SeedWorldTrail>();

        var result = new List<SeedWorldTrail>();
        var available = trails.ToList();

        while (result.Count < count && available.Count > 0)
        {
            var index = random.Next(available.Count);
            var trail = available[index];

            // If outlier exists, avoid removing the trail that connects to it
            if (outlierSlot.HasValue)
            {
                var parts = trail.Id.Split('-');
                var fromSlot = int.Parse(parts[1]);
                var toSlot = int.Parse(parts[2]);

                // Check if this trail connects to the outlier
                if (fromSlot == outlierSlot.Value || toSlot == outlierSlot.Value)
                {
                    // Find if the outlier has other trails remaining
                    var outlierOtherTrails = allTrails
                        .Where(t => t.Id != trail.Id)
                        .Where(t =>
                        {
                            var p = t.Id.Split('-');
                            var f = int.Parse(p[1]);
                            var t2 = int.Parse(p[2]);
                            return f == outlierSlot.Value || t2 == outlierSlot.Value;
                        })
                        .ToList();

                    if (outlierOtherTrails.Count == 0)
                    {
                        // This is the only trail to the outlier, don't remove it
                        available.RemoveAt(index);
                        continue;
                    }
                }
            }

            result.Add(trail);
            available.RemoveAt(index);
        }

        return result;
    }

    /// <summary>
    /// Applies Ring-specific trail removal by salt.
    /// Ring: remove 1 trail (or replace with a link to another town to maintain connectivity).
    /// </summary>
    private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyRingTrailRemoval(
        IReadOnlyList<SeedWorldTrail> trails,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        int townCount,
        int? outlierSlot)
    {
        if (townCount < 3)
            return (trails, outlierSlot); // Need at least 3 towns for meaningful removal

        // Ring only removes 1 trail per entropy level
        var trailsToRemove = entropy switch
        {
            GameEntropy.Classic => 1,
            GameEntropy.Adventurous => 1,
            GameEntropy.Wild => 1,
            _ => 0
        };

        if (trailsToRemove == 0 || trails.Count <= 3)
            return (trails, outlierSlot); // Keep at least 3 trails for connectivity

        if (saltSource == null)
            return (trails, outlierSlot);

        var salt = saltSource.Salt;
        var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));

        // Select random trail to remove
        var trailToRemove = trails[random.Next(trails.Count)];

        // Try removing the trail and check connectivity
        var result = trails.Where(t => t.Id != trailToRemove.Id).ToList();

        if (VerifyConnectivity(townCount, result))
        {
            return (result, outlierSlot);
        }

        // If removal broke connectivity, try replacing with a link to another town
        // Find two towns that are not directly connected and add a trail between them
        var parts = trailToRemove.Id.Split('-');
        var fromSlot = int.Parse(parts[1]);
        var toSlot = int.Parse(parts[2]);

        // Find a town that can serve as a bridge
        var bridgeSlot = -1;
        for (var i = 0; i < townCount; i++)
        {
            if (i != fromSlot && i != toSlot)
            {
                bridgeSlot = i;
                break;
            }
        }

        if (bridgeSlot >= 0)
        {
            // Add trails: fromSlot -> bridgeSlot and bridgeSlot -> toSlot
            var fromTownId = trails.First(t => t.Id.Contains($"-{fromSlot}-")).FromTownId;
            var toTownId = trails.First(t => t.Id.Contains($"-{toSlot}-")).ToTownId;
            var bridgeTownId = trails.First(t => t.Id.Contains($"-{bridgeSlot}-")).FromTownId;

            var replacementTrails = new List<SeedWorldTrail>(result);
            replacementTrails.Add(new SeedWorldTrail(
                $"trail-{fromSlot}-{bridgeSlot}",
                fromTownId,
                bridgeTownId,
                trailToRemove.Risk,
                trailToRemove.Terrain,
                trailToRemove.WaterFeature,
                trailToRemove.RideDayDistance));
            replacementTrails.Add(new SeedWorldTrail(
                $"trail-{bridgeSlot}-{toSlot}",
                bridgeTownId,
                toTownId,
                trailToRemove.Risk,
                trailToRemove.Terrain,
                trailToRemove.WaterFeature,
                trailToRemove.RideDayDistance));

            if (VerifyConnectivity(townCount, replacementTrails))
            {
                return (replacementTrails, outlierSlot);
            }
        }

        // If replacement didn't work, return original trails
        return (trails, outlierSlot);
    }

    /// <summary>
    /// Applies DoubleLine-specific trail removal by salt.
    /// DoubleLine: remove trails between lines, preserve crossing trails (1-3).
    /// Crossing trails are slot 1-3 (they cross top to bottom between two towns).
    /// </summary>
    private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyDoubleLineTrailRemoval(
        IReadOnlyList<SeedWorldTrail> trails,
        GameEntropy entropy,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        int townCount,
        int? outlierSlot)
    {
        if (townCount < 4)
            return (trails, outlierSlot); // Need at least 4 towns for DoubleLine

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
            return (trails, outlierSlot);

        if (saltSource == null)
            return (trails, outlierSlot);

        var salt = saltSource.Salt;
        var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));

        // Select random trails to remove from otherTrails (not crossing trails)
        var trailsToRemoveList = SelectRandomTrails(otherTrails, trailsToRemove, random, outlierSlot, trails);

        // Build result: keep all crossing trails + remaining other trails
        var removedIds = new HashSet<string>(trailsToRemoveList.Select(t => t.Id));
        var result = crossingTrails.Concat(otherTrails.Where(t => !removedIds.Contains(t.Id))).ToList();

        // Verify connectivity
        if (!VerifyConnectivity(townCount, result))
        {
            // If removal broke connectivity, return original trails
            return (trails, outlierSlot);
        }

        return (result, outlierSlot);
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
