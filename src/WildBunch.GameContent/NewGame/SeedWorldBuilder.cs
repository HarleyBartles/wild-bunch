using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class SeedWorldBuilder
{
    /// <summary>
    /// Builds the canonical world (all 8 towns, all 9 trails).
    /// Used by SeedWorldMapLayout for the start-screen map.
    /// </summary>
    public static World CreateCanonicalWorld()
        => SeedWorldCatalog.CreateCanonicalWorld();

    /// <summary>
    /// Builds a World from a SeedWorld template. The seed world holds the
    /// selected town IDs and trail graph with baseline terrain/water/distance.
    /// The catalog provides town definitions (services per variant).
    /// Future seam: DifficultyEnvelope may modify terrain/distance downstream.
    /// </summary>
    public static World CreateWorld(SeedWorld seedWorld, GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        return SeedWorldCatalog.CreateWorld(
            seedWorld.WorldVariant,
            seedWorld.SelectedTownIds,
            seedWorld.Trails);
    }

    /// <summary>
    /// Checks whether the seed world is the canonical shape (all 8 towns,
    /// all 9 trails, Canonical variant, specific case fields).
    /// </summary>
    internal static bool IsCanonicalSeedWorld(SeedWorld seedWorld)
        => seedWorld.WorldVariant == SeedWorldVariant.Canonical
            && seedWorld.SelectedTownIds.Count == SeedWorldCatalog.AllTowns.Count
            && seedWorld.Trails.Count == SeedWorldCatalog.AllTrails.Count
            && seedWorld.AccusationIndex == 1
            && seedWorld.DefaultCulpritIndex == 3
            && seedWorld.CashBonus == 0;
}
