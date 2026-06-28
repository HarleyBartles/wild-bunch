using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

internal static class SeedWorldSeedCodeFactory
{
    private const int SearchLimit = 131072;

    /// <summary>
    /// Finds a UUID seed code that produces a SeedWorld with the given
    /// world variant and case fields. The town selection is seed-derived
    /// and cannot be directly controlled — the factory finds a seed that
    /// matches the non-town fields.
    /// </summary>
    internal static Guid CreateSeedCode(byte worldVariant, byte accusationIndex, byte defaultCulpritIndex, byte cashBonus, ulong salt)
    {
        // Build a target SeedWorld shape with all towns (canonical selection)
        // and search for a seed that matches the variant + case fields.
        var variant = (SeedWorldVariant)worldVariant;
        var allTownIds = SeedWorldCatalog.AllTowns.Select(t => t.Id).ToArray();
        var trails = SeedWorldResolver.BuildTrails(variant, allTownIds);

        var target = new SeedWorld(
            Guid.Empty,
            variant,
            allTownIds,
            trails,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus);

        return FindSeedCode(target, salt);
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
            && left.SelectedTownIds.SequenceEqual(right.SelectedTownIds)
            && left.AccusationIndex == right.AccusationIndex
            && left.DefaultCulpritIndex == right.DefaultCulpritIndex
            && left.CashBonus == right.CashBonus;
}
