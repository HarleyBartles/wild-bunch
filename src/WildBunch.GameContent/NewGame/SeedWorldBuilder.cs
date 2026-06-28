using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class SeedWorldBuilder
{
    public static World CreateCanonicalWorld()
        => SeedWorldCatalog.CreateWorld(SeedWorldVariant.Canonical);

    public static World CreateWorld(SeedWorld seedWorld, GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        if (IsCanonicalSeedWorld(seedWorld))
        {
            return CreateCanonicalWorld();
        }

        return SeedWorldCatalog.CreateWorld(seedWorld.WorldVariant);
    }

    private static bool IsCanonicalSeedWorld(SeedWorld seedWorld)
        => seedWorld.WorldVariant == SeedWorldVariant.Canonical
            && seedWorld.TownSetKey == GameSetupDeterministicLabels.WorldTownSetDefault
            && seedWorld.AccusationIndex == 1
            && seedWorld.DefaultCulpritIndex == 3
            && seedWorld.CashBonus == 0;
}
