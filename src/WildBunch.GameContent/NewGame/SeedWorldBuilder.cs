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
