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
    /// Stub: builds a World with a minimal linear trail chain (0→1→2→...→N-1).
    /// The real pipeline (MapGenerator) replaces this in Plan 2.
    /// First trail is Low/OpenRange/Creek/4m to keep travel tests green.
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
            seedWorld.WorldVariant, seedWorld.TownCount,
            seedWorld.AccusationIndex, seedWorld.DefaultCulpritIndex,
            seedWorld.CashBonus, seedWorld.ProsperityPalette, seedWorld.ServicesPalette);

        var trails = new List<SeedWorldTrail>();
        for (var i = 1; i < townNames.Count; i++)
        {
            var water = i == 1 ? WaterFeature.Creek : WaterFeature.None;
            var distance = i == 1 ? 5m : 4m;
            trails.Add(new SeedWorldTrail(
                $"trail-0-{i}",
                townNames[0].Id,
                townNames[i].Id,
                TrailRisk.Low,
                TrailTerrain.OpenRange,
                water,
                distance));
        }

        return SeedWorldCatalog.CreateWorld(
            seedWorld.WorldVariant, townNames, seedWorld.ServicesPalette,
            seedWorld.ProsperityPalette, trails,
            townCoordinates: null, outlierSlot: null,
            entropy, saltSource, seedWorld.SeedCode);
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
    /// Checks whether the seed world is the canonical shape (8 towns,
    /// Canonical variant, HubTelegraph services, UniformProsperous prosperity,
    /// specific case fields).
    /// </summary>
    internal static bool IsCanonicalSeedWorld(SeedWorld seedWorld)
        => seedWorld.WorldVariant == SeedWorldVariant.Canonical
            && seedWorld.TownCount == 8
            && seedWorld.ServicesPalette == ServicesPalette.HubTelegraph
            && seedWorld.ProsperityPalette == ProsperityPalette.UniformProsperous
            && seedWorld.ClusterCount == 1
            && seedWorld.GraphDensity == GraphDensity.Sparse
            && seedWorld.AccusationIndex == 1
            && seedWorld.DefaultCulpritIndex == 3
            && seedWorld.CashBonus == 0;
}
