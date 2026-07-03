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
    {
        var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
        var source = new GameSetupDeterministicSource(seedWorld.SeedCode.ToString("D"));
        return CreateWorld(seedWorld, source, GameEntropy.Boring, null);
    }

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

        // Determine if outlier slot should be activated
        var shouldActivateOutlier = seedWorld.OutlierSlotType == 1 && entropy != GameEntropy.Boring;
        var finalTownCount = shouldActivateOutlier ? seedWorld.TownCount + 1 : seedWorld.TownCount;

        // Derive town names for base count only (without outlier)
        var townNames = SeedWorldCatalog.DeriveTownNames(
            seedWorld.WorldVariant,
            seedWorld.TownCount,
            seedWorld.AccusationIndex,
            seedWorld.DefaultCulpritIndex,
            seedWorld.CashBonus,
            seedWorld.ProsperityPalette,
            seedWorld.ServicesPalette,
            seedWorld.MapLayoutPalette);

        // Derive town coordinates from map layout geometry
        var townCoordinates = DeriveTownCoordinates(townNames.Count, seedWorld.MapLayoutPalette, entropy, source, saltSource);

        // Generate trails from settled town coordinates using geometry-first approach
        var trails = TrailTopologyGenerator.GenerateTrailTopology(
            townCoordinates,
            townNames,
            entropy,
            saltSource,
            source,
            null); // outlierSlot is null during initial generation

        // Activate outlier slot if needed
        int? outlierSlot = null;
        if (shouldActivateOutlier)
        {
            var outlierSlotIndex = seedWorld.TownCount; // Outlier is at the next slot
            var (trailsWithOutlier, activatedSlot, extendedTownNames, extendedCoordinates) = ActivateOutlierSlot(
                trails,
                townCoordinates,
                townNames,
                seedWorld.WorldVariant,
                seedWorld.AccusationIndex,
                seedWorld.DefaultCulpritIndex,
                seedWorld.CashBonus,
                seedWorld.ProsperityPalette,
                seedWorld.ServicesPalette,
                seedWorld.MapLayoutPalette,
                source,
                saltSource,
                entropy,
                outlierSlotIndex);
            trails = trailsWithOutlier;
            outlierSlot = activatedSlot;
            townNames = extendedTownNames;
            townCoordinates = extendedCoordinates;
        }

        return SeedWorldCatalog.CreateWorld(
            seedWorld.WorldVariant,
            townNames,
            seedWorld.ServicesPalette,
            seedWorld.ProsperityPalette,
            trails,
            townCoordinates,
            outlierSlot,
            entropy,
            saltSource,
            seedWorld.SeedCode);
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
    /// Computes a stable deterministic hash for entropy variance with string slot identifier.
    /// Uses SHA256 over explicit inputs to ensure consistency across runs.
    /// </summary>
    private static int ComputeStableHash(string seedCode, string slotIdentifier, string entropyMode, string salt)
    {
        var input = $"{seedCode}-{slotIdentifier}-{entropyMode}-{salt}";
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
    /// Converts a signed hash to a guaranteed non-negative value for safe modulo indexing.
    /// Safe for all int values including int.MinValue by using long arithmetic.
    /// </summary>
    internal static int NonNegativeModulo(int value, int modulo) => (int)(((long)value - int.MinValue) % modulo);

    /// <summary>
    /// Activates the hidden outlier slot by creating an outlier town and trail.
    /// The outlier is placed 6 days away from a deterministically selected target town.
    /// Returns the updated trails list, outlier slot index, extended town names, and extended coordinates.
    /// </summary>
    private static (IReadOnlyList<SeedWorldTrail> Trails, int? OutlierSlot, IReadOnlyList<TownNameEntry> TownNames, Dictionary<int, (int X, int Y)> Coordinates) ActivateOutlierSlot(
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)> townCoordinates,
        IReadOnlyList<TownNameEntry> townNames,
        SeedWorldVariant variant,
        int accusationIndex,
        int defaultCulpritIndex,
        int cashBonus,
        ProsperityPalette prosperityPalette,
        ServicesPalette servicesPalette,
        MapLayoutPalette mapLayoutPalette,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        GameEntropy entropy,
        int outlierSlotIndex)
    {
        const double CoordinateScale = 25.0; // 1 ride-day per 25 coordinate units

        // Select connection target using deterministic hash
        var connectionTargetSlot = SelectOutlierConnectionTarget(townCoordinates, source, saltSource, entropy);

        // Create outlier town coordinates (6 days away from target)
        var targetCoords = townCoordinates[connectionTargetSlot];
        var salt = saltSource?.Salt ?? "default";
        var angle = ComputeStableHash(source.SeedCode, outlierSlotIndex, entropy.ToString(), salt) % 360;
        var angleRad = angle * Math.PI / 180.0;
        var outlierX = targetCoords.X + (int)(6 * CoordinateScale * Math.Cos(angleRad));
        var outlierY = targetCoords.Y + (int)(6 * CoordinateScale * Math.Sin(angleRad));

        townCoordinates[outlierSlotIndex] = (outlierX, outlierY);

        // Select an unused town name from the name pool
        var usedTownIds = townNames.Select(t => t.Id).ToHashSet();
        var unusedNames = SeedWorldCatalog.NamePool
            .Where(t => !usedTownIds.Contains(t.Id))
            .ToList();
        var outlierTownName = unusedNames[NonNegativeModulo(ComputeStableHash(source.SeedCode, outlierSlotIndex, "outlier-name", salt), unusedNames.Count)];

        // Append outlier town name to existing list
        var extendedTownNames = townNames.ToList();
        extendedTownNames.Add(outlierTownName);

        // Create outlier trail using actual derived TownId values from extended town names
        var targetTownId = extendedTownNames[connectionTargetSlot].Id;
        var outlierTownId = extendedTownNames[outlierSlotIndex].Id;
        var outlierTrail = new SeedWorldTrail(
            $"outlier-trail-{connectionTargetSlot}-{outlierSlotIndex}",
            targetTownId,
            outlierTownId,
            TrailRisk.High,
            TrailTerrain.Mountains,
            WaterFeature.None,
            6m); // Exactly 6 days

        var result = new List<SeedWorldTrail>(trails) { outlierTrail };
        return (result, outlierSlotIndex, extendedTownNames, townCoordinates);
    }

    /// <summary>
    /// Selects a target town for the outlier to connect to using deterministic hash.
    /// </summary>
    private static int SelectOutlierConnectionTarget(
        Dictionary<int, (int X, int Y)> townCoordinates,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        GameEntropy entropy)
    {
        var slots = townCoordinates.Keys.ToList();
        var salt = saltSource?.Salt ?? "default";
        var hash = ComputeStableHash(source.SeedCode, "outlier-target", entropy.ToString(), salt);
        return slots[Math.Abs(hash) % slots.Count];
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
