using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

internal static class SeedWorldSeedCodeFactory
{
    private const int SearchLimit = 131072;

    internal static Guid CreateSeedCode(byte worldVariant, byte townSetKey, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong salt)
    {
        var seedWorld = CreateSeedWorld(worldVariant, townSetKey, accusationIndex, defaultCulpritIndex, cashBonus);
        return FindSeedCode(seedWorld, salt);
    }

    internal static Guid FindSeedCode(SeedWorld seedWorld, ulong salt = 0)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);

        var validation = SeedWorldResolver.Validate(seedWorld);
        if (!validation.Success)
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Seed world is invalid.", nameof(seedWorld));
        }

        var seedWorldSignature = StartingWorldDescriptorSeedMixer.CreateSeedWorldSignature(seedWorld);
        for (var attempt = 0; attempt < SearchLimit; attempt++)
        {
            var candidate = StartingWorldDescriptorSeedMixer.CreateCandidateSeed(seedWorldSignature, salt, attempt);
            var resolved = SeedWorldResolver.Resolve(candidate);
            if (HasSameSemantics(seedWorld, resolved))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not find a representative UUID-shaped seed for the requested seed world.");
    }

    private static bool HasSameSemantics(SeedWorld left, SeedWorld right)
        => left.WorldVariant == right.WorldVariant
            && left.TownSetKey == right.TownSetKey
            && left.AccusationIndex == right.AccusationIndex
            && left.DefaultCulpritIndex == right.DefaultCulpritIndex
            && left.CashBonus == right.CashBonus;

    private static SeedWorld CreateSeedWorld(byte worldVariant, byte townSetKey, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus)
    {
        var world = (SeedWorldVariant)worldVariant;
        var key = townSetKey == 0
            ? GameSetupDeterministicLabels.WorldTownSetDefault
            : GameSetupDeterministicLabels.WorldTownSetAlternate;

        return new SeedWorld(
            Guid.Empty,
            world,
            key,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus);
    }
}
