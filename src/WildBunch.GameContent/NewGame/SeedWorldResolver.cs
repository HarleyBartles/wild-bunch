using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Resolves the seed-owned <see cref="SeedWorld"/> from a UUID seed code,
/// and encodes a SeedWorld back to a representative UUID via round-trip search.
/// The codec does NOT reference GameDifficulty or GameEntropy — those are
/// pressure-owned and applied downstream by DifficultyEnvelope and EntropyPolicy.
/// The codec does NOT select the starting town — that is a player/setup choice
/// validated by <see cref="StartingTownPolicy"/>.
/// </summary>
public static class SeedWorldResolver
{
    public const string ResolverContractVersion = "resolver-v3";
    private const string SeedCodeFormat = "D";
    private const int RepresentativeSeedSearchLimit = 131072;

    private static readonly Lazy<Guid> CanonicalSeedCode = new(() => CreateCanonicalSeedCodeCore(), true);

    public static Guid CreateCanonicalSeedCode()
        => CanonicalSeedCode.Value;

    public static Guid GenerateRandomSeedCode()
        => Guid.NewGuid();

    public static SeedWorld CreateCanonicalSeedWorld()
    {
        var seedWorld = CreateCanonicalSeedWorldShape();
        return seedWorld with { SeedCode = CanonicalSeedCode.Value };
    }

    internal static SeedWorld Resolve(string? seedCode)
    {
        if (string.IsNullOrWhiteSpace(seedCode))
        {
            return CreateCanonicalSeedWorld();
        }

        if (!TryParseSeedCode(seedCode, out var seed))
        {
            throw new ArgumentException("Seed code must be a UUID-shaped string.", nameof(seedCode));
        }

        return Resolve(seed);
    }

    public static SeedWorld Resolve(Guid seedCode)
    {
        var seedRoot = StartingWorldDescriptorSeedMixer.CreateSeedRoot(seedCode);
        var worldVariant = ResolveWorldVariant(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.WorldVariant));
        var townSetKey = ResolveTownSetKey(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.WorldTownSet));
        var accusationIndex = ResolveAccusationIndex(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.CaseAccusationIndex));
        var defaultCulpritIndex = ResolveDefaultCulpritIndex(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.CaseDefaultCulprit));
        var cashBonus = ResolveCashBonus(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.PlayerCashBonus));

        return new SeedWorld(
            seedCode,
            worldVariant,
            townSetKey,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus);
    }

    internal static SeedWorldValidationResult Validate(SeedWorld seedWorld)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);

        if (!Enum.IsDefined(typeof(SeedWorldVariant), seedWorld.WorldVariant))
        {
            return SeedWorldValidationResult.Failed("World variant is invalid.");
        }

        if (string.IsNullOrWhiteSpace(seedWorld.TownSetKey))
        {
            return SeedWorldValidationResult.Failed("Town set key is invalid.");
        }

        if (seedWorld.AccusationIndex is < 0 or > 6)
        {
            return SeedWorldValidationResult.Failed("Accusation index is outside the legal envelope.");
        }

        if (seedWorld.DefaultCulpritIndex is < 0 or > 6)
        {
            return SeedWorldValidationResult.Failed("Default culprit index is outside the legal envelope.");
        }

        if (seedWorld.CashBonus is < 0 or > 8)
        {
            return SeedWorldValidationResult.Failed("Cash bonus is outside the legal envelope.");
        }

        return SeedWorldValidationResult.Ok();
    }

    public static bool TryParseSeedCode(string? seedCode, out Guid seed)
    {
        seed = default;
        if (string.IsNullOrWhiteSpace(seedCode))
        {
            return false;
        }

        return Guid.TryParseExact(seedCode.Trim(), SeedCodeFormat, out seed);
    }

    public static string FormatSeedCode(Guid seedCode)
        => seedCode.ToString(SeedCodeFormat);

    public static Guid CreateRepresentativeSeedCode(SeedWorld seedWorld)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);

        var validation = Validate(seedWorld);
        if (!validation.Success)
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Seed world is invalid.", nameof(seedWorld));
        }

        var seedWorldSignature = StartingWorldDescriptorSeedMixer.CreateSeedWorldSignature(seedWorld);
        for (var attempt = 0; attempt < RepresentativeSeedSearchLimit; attempt++)
        {
            var candidateSeed = StartingWorldDescriptorSeedMixer.CreateCandidateSeed(seedWorldSignature, salt: 0, attempt);
            var resolvedSeedWorld = Resolve(candidateSeed);
            if (HasSameSemantics(seedWorld, resolvedSeedWorld))
            {
                return candidateSeed;
            }
        }

        throw new InvalidOperationException("Could not derive a representative UUID-shaped seed for the requested seed world.");
    }

    private static SeedWorld CreateCanonicalSeedWorldShape()
        => new(
            Guid.Empty,
            SeedWorldVariant.Canonical,
            GameSetupDeterministicLabels.WorldTownSetDefault,
            AccusationIndex: 1,
            DefaultCulpritIndex: 3,
            CashBonus: 0);

    private static Guid CreateCanonicalSeedCodeCore()
        => CreateRepresentativeSeedCode(CreateCanonicalSeedWorldShape());

    private static bool HasSameSemantics(SeedWorld left, SeedWorld right)
        => left.WorldVariant == right.WorldVariant
            && left.TownSetKey == right.TownSetKey
            && left.AccusationIndex == right.AccusationIndex
            && left.DefaultCulpritIndex == right.DefaultCulpritIndex
            && left.CashBonus == right.CashBonus;

    private static SeedWorldVariant ResolveWorldVariant(ulong seedValue)
        => (seedValue % 3UL) switch
        {
            0 => SeedWorldVariant.Canonical,
            1 => SeedWorldVariant.Frontier,
            _ => SeedWorldVariant.Rail
        };

    private static string ResolveTownSetKey(ulong seedValue)
        => (seedValue % 2UL) switch
        {
            0 => GameSetupDeterministicLabels.WorldTownSetDefault,
            _ => GameSetupDeterministicLabels.WorldTownSetAlternate
        };

    private static int ResolveAccusationIndex(ulong seedValue)
        => (int)(seedValue % 7UL);

    private static int ResolveDefaultCulpritIndex(ulong seedValue)
        => (int)(seedValue % 7UL);

    private static int ResolveCashBonus(ulong seedValue)
        => (int)(seedValue % 9UL);
}
