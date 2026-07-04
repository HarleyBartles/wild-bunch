using WildBunch.Domain.Game;
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
