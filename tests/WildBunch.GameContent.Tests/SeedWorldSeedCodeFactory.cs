using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

internal static class SeedWorldSeedCodeFactory
{
    /// <summary>
    /// Creates a UUID seed code that produces a SeedWorld with the given
    /// world variant and case fields. Uses 8 towns with default palettes.
    /// </summary>
    internal static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong salt)
    {
        var variant = (SeedWorldVariant)worldVariant;
        var townCount = 8;
        var prosperityPalette = ProsperityPalette.UniformProsperous;
        const int reservedTownNameDerivationBits = 1;
        var clusterCount = 1; var graphDensity = GraphDensity.Sparse;

        var target = new SeedWorld(
            Guid.Empty,
            variant,
            townCount,
            reservedTownNameDerivationBits,
            prosperityPalette,
            clusterCount, graphDensity,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            OutlierSlotType: 0);

        return SeedWorldResolver.CreateRepresentativeSeedCode(target);
    }

    /// <summary>
}
