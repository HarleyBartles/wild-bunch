using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Resolves the seed-owned <see cref="SeedWorld"/> from a UUID seed code,
/// and encodes a SeedWorld back to a representative UUID via round-trip search.
/// The codec does NOT reference GameDifficulty or GameEntropy — those are
/// pressure-owned and applied downstream by DifficultyEnvelope and EntropyPolicy.
/// The codec does NOT select the starting town — that is a player/setup choice
/// validated by <see cref="StartingTownPolicy"/>.
///
/// The seed deterministically derives:
/// - WorldVariant (Canonical/Frontier/Rail)
/// - TownCount (6-8, safe range for playability)
/// - SelectedTownIds (anchor towns always included, rest seed-selected from catalog)
/// - Trails (catalog trails where both endpoints are selected, with terrain/water/distance from catalog)
/// - AccusationIndex, DefaultCulpritIndex, CashBonus
/// </summary>
public static class SeedWorldResolver
{
    public const string ResolverContractVersion = "resolver-v4";
    private const string SeedCodeFormat = "D";
    private const int RepresentativeSeedSearchLimit = 131072;
    private const int MinTownCount = 6;
    private const int MaxTownCount = 8;

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
        var townCount = ResolveTownCount(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.WorldTownCount));
        var selectedTownIds = SelectTowns(
            townCount,
            StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.WorldTownSelection));
        var trails = BuildTrails(worldVariant, selectedTownIds);
        var accusationIndex = ResolveAccusationIndex(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.CaseAccusationIndex));
        var defaultCulpritIndex = ResolveDefaultCulpritIndex(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.CaseDefaultCulprit));
        var cashBonus = ResolveCashBonus(StartingWorldDescriptorSeedMixer.GetFieldSeed(seedRoot, GameSetupDeterministicLabels.PlayerCashBonus));

        return new SeedWorld(
            seedCode,
            worldVariant,
            selectedTownIds,
            trails,
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

        if (seedWorld.SelectedTownIds is null || seedWorld.SelectedTownIds.Count < MinTownCount)
        {
            return SeedWorldValidationResult.Failed($"Selected town count must be at least {MinTownCount}.");
        }

        if (seedWorld.SelectedTownIds.Count > MaxTownCount)
        {
            return SeedWorldValidationResult.Failed($"Selected town count must be at most {MaxTownCount}.");
        }

        foreach (var anchorId in SeedWorldCatalog.AnchorTownIds)
        {
            if (!seedWorld.SelectedTownIds.Contains(anchorId))
            {
                return SeedWorldValidationResult.Failed($"Anchor town '{anchorId}' must be in the selected town set.");
            }
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

    /// <summary>
    /// Selects towns deterministically from the catalog. Anchor towns are
    /// always included. The remaining slots are filled from the selectable
    /// pool using a seed-derived permutation.
    /// </summary>
    internal static IReadOnlyList<string> SelectTowns(int townCount, ulong selectionSeed)
    {
        var anchors = SeedWorldCatalog.AnchorTownIds;
        var selectable = SeedWorldCatalog.SelectableTownIds;
        var remainingSlots = townCount - anchors.Count;

        if (remainingSlots <= 0)
            return anchors.ToArray();

        if (remainingSlots >= selectable.Count)
            return anchors.Concat(selectable).ToArray();

        // Deterministic Fisher-Yates-like shuffle of the selectable pool
        var pool = selectable.ToArray();
        for (var i = pool.Length - 1; i > 0; i--)
        {
            var j = (int)(DeriveValue(selectionSeed, i) % (ulong)(i + 1));
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        var selected = pool.Take(remainingSlots).ToArray();
        return anchors.Concat(selected).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Builds the trail graph for the selected towns. Includes catalog trails
    /// where both endpoints are in the selected set. Terrain/water/distance
    /// come from the catalog indexed by world variant.
    /// </summary>
    internal static IReadOnlyList<SeedWorldTrail> BuildTrails(SeedWorldVariant variant, IReadOnlyList<string> selectedTownIds)
    {
        var selectedSet = selectedTownIds.ToHashSet();
        var trails = new List<SeedWorldTrail>();

        foreach (var def in SeedWorldCatalog.AllTrails)
        {
            if (selectedSet.Contains(def.FromTownId) && selectedSet.Contains(def.ToTownId))
            {
                var tv = def.ForVariant(variant);
                trails.Add(new SeedWorldTrail(
                    def.Id,
                    def.FromTownId,
                    def.ToTownId,
                    def.Risk,
                    tv.Terrain,
                    tv.WaterFeature,
                    tv.RideDayDistance));
            }
        }

        return trails;
    }

    private static SeedWorld CreateCanonicalSeedWorldShape()
    {
        var allTownIds = SeedWorldCatalog.AllTowns.Select(t => t.Id).ToArray();
        var trails = BuildTrails(SeedWorldVariant.Canonical, allTownIds);
        return new SeedWorld(
            Guid.Empty,
            SeedWorldVariant.Canonical,
            allTownIds,
            trails,
            AccusationIndex: 1,
            DefaultCulpritIndex: 3,
            CashBonus: 0);
    }

    private static Guid CreateCanonicalSeedCodeCore()
        => CreateRepresentativeSeedCode(CreateCanonicalSeedWorldShape());

    private static bool HasSameSemantics(SeedWorld left, SeedWorld right)
        => left.WorldVariant == right.WorldVariant
            && left.SelectedTownIds.SequenceEqual(right.SelectedTownIds)
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

    private static int ResolveTownCount(ulong seedValue)
        => MinTownCount + (int)(seedValue % (ulong)(MaxTownCount - MinTownCount + 1));

    private static int ResolveAccusationIndex(ulong seedValue)
        => (int)(seedValue % 7UL);

    private static int ResolveDefaultCulpritIndex(ulong seedValue)
        => (int)(seedValue % 7UL);

    private static int ResolveCashBonus(ulong seedValue)
        => (int)(seedValue % 9UL);

    private static ulong DeriveValue(ulong baseSeed, int index)
    {
        var mixed = baseSeed ^ ((ulong)index * 0x9E3779B97F4A7C15UL);
        mixed ^= mixed >> 30;
        mixed *= 0xBF58476D1CE4E5B9UL;
        mixed ^= mixed >> 27;
        mixed *= 0x94D049BB133111EBUL;
        mixed ^= mixed >> 31;
        return mixed;
    }
}
