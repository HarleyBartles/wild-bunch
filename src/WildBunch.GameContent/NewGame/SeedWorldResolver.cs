using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Resolves the seed-owned <see cref="SeedWorld"/> from a UUID seed code,
/// and encodes a SeedWorld back to a UUID by direct bit packing.
///
/// The codec does NOT reference GameDifficulty or GameEntropy — those are
/// pressure-owned and applied downstream by DifficultyEnvelope and EntropyPolicy.
/// The codec does NOT select the starting town — that is a player/setup choice
/// validated by <see cref="StartingTownPolicy"/>.
///
/// UUID bit layout (128 bits = 16 bytes, packed as two ulongs):
/// <code>
/// Bytes 0-7 (low):
///   bits  0-1:   variant (2)
///   bits  2-4:   accusationIndex (3)
///   bits  5-7:   defaultCulpritIndex (3)
///   bits  8-11:  cashBonus (4)
///   bits 12-15:  townCount (4, offset-encoded: 0-15 → 5-20 towns)
///   bits 16-18:  prosperityPaletteIndex (3, indexes 8 palettes)
///   bits 19-21:  servicesPaletteIndex (3, indexes 8 palettes)
///   bits 22-63:  reserved (42)
///
/// Bytes 8-15 (high): reserved (64)
/// </code>
///
/// Total used: 22 bits. Reserved: 106 bits for future seed-owned fields
/// (gang members, warrants, etc.). Bandwidth scales with max selection (20),
/// not catalog size — the name pool can grow to any size with zero bit cost.
///
/// Town names are derived from the encoded fields via a deterministic
/// shuffle of the name pool. They are not encoded in the UUID. Both
/// directions (UUID → SeedWorld and SeedWorld → UUID) are O(1) — no
/// search or hashing required.
/// </summary>
public static class SeedWorldResolver
{
    /// <summary>
    /// Resolver contract version. Increment when the codec layout changes
    /// in a way that breaks round-trip compatibility.
    ///
    /// Version history:
    /// - v1-v5: Legacy codec versions (pre-BUNCH-107)
    /// - v6: BUNCH-107 refactoring — direct bit-packing (O(1)), 22 bits used,
    ///       106 reserved. Separated seed-owned (map/variant) from pressure-owned
    ///       (difficulty/entropy) and player/setup-owned (starting town).
    /// </summary>
    public const string ResolverContractVersion = "resolver-v6";
    private const string SeedCodeFormat = "D";

    /// <summary>Minimum number of towns in a valid world.</summary>
    public const int MinTownCount = 5;

    /// <summary>Maximum number of towns in a valid world. Offset-encoded in 4 bits (0-15 → 5-20).</summary>
    public const int MaxTownCount = 20;

    private const int TownCountOffset = 5;

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

    /// <summary>
    /// Decodes a UUID into a SeedWorld by extracting fields from specific
    /// bit positions, then deriving town names, services, and trails.
    /// This is the inverse of <see cref="CreateRepresentativeSeedCode"/>.
    /// </summary>
    public static SeedWorld Resolve(Guid seedCode)
    {
        var bytes = seedCode.ToByteArray();
        var low = BitConverter.ToUInt64(bytes, 0);

        var variant = (SeedWorldVariant)(low & 0x3UL);
        var accusationIndex = (int)((low >> 2) & 0x7UL);
        var defaultCulpritIndex = (int)((low >> 5) & 0x7UL);
        var cashBonus = (int)((low >> 8) & 0xFUL);
        var townCountEncoded = (int)((low >> 12) & 0xFUL);
        var prosperityPalette = (ProsperityPalette)((low >> 16) & 0x7UL);
        var servicesPalette = (ServicesPalette)((low >> 19) & 0x7UL);

        // Decode town count with offset: 4-bit value 0-15 → town count 5-20.
        var townCount = townCountEncoded + TownCountOffset;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperityPalette, servicesPalette);

        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(servicesPalette, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = SeedWorldCatalog.BuildTrails(variant, townNames);

        return new SeedWorld(
            seedCode,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            selectedTownIds,
            townServices,
            trails);
    }

    internal static SeedWorldValidationResult Validate(SeedWorld seedWorld)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);

        if (!Enum.IsDefined(typeof(SeedWorldVariant), seedWorld.WorldVariant))
        {
            return SeedWorldValidationResult.Failed("World variant is invalid.");
        }

        if (seedWorld.TownCount < MinTownCount || seedWorld.TownCount > MaxTownCount)
        {
            return SeedWorldValidationResult.Failed(
                $"Town count must be between {MinTownCount} and {MaxTownCount}.");
        }

        if (!Enum.IsDefined(typeof(ProsperityPalette), seedWorld.ProsperityPalette))
        {
            return SeedWorldValidationResult.Failed("Prosperity palette is invalid.");
        }

        if (!Enum.IsDefined(typeof(ServicesPalette), seedWorld.ServicesPalette))
        {
            return SeedWorldValidationResult.Failed("Services palette is invalid.");
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

    /// <summary>
    /// Encodes a SeedWorld into a UUID by packing the encoded fields into
    /// specific bit positions. Derived fields (town names, services dict,
    /// trails) are ignored — they are re-derived on decode. This is the
    /// inverse of <see cref="Resolve(Guid)"/> and is O(1).
    /// </summary>
    public static Guid CreateRepresentativeSeedCode(SeedWorld seedWorld)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);

        var validation = Validate(seedWorld);
        if (!validation.Success)
        {
            throw new ArgumentException(validation.ErrorMessage ?? "Seed world is invalid.", nameof(seedWorld));
        }

        ulong low = 0;
        low |= (ulong)((int)seedWorld.WorldVariant & 0x3);
        low |= (ulong)(seedWorld.AccusationIndex & 0x7) << 2;
        low |= (ulong)(seedWorld.DefaultCulpritIndex & 0x7) << 5;
        low |= (ulong)(seedWorld.CashBonus & 0xF) << 8;
        low |= (ulong)((seedWorld.TownCount - TownCountOffset) & 0xF) << 12;
        low |= (ulong)((int)seedWorld.ProsperityPalette & 0x7) << 16;
        low |= (ulong)((int)seedWorld.ServicesPalette & 0x7) << 19;

        ulong high = 0UL;

        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0), low);
        BitConverter.TryWriteBytes(bytes.AsSpan(8), high);
        return new Guid(bytes);
    }

    private static SeedWorld CreateCanonicalSeedWorldShape()
    {
        var variant = SeedWorldVariant.Canonical;
        var townCount = 8;
        var accusationIndex = 1;
        var defaultCulpritIndex = 3;
        var cashBonus = 0;
        var prosperityPalette = ProsperityPalette.UniformProsperous;
        var servicesPalette = ServicesPalette.HubTelegraph;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperityPalette, servicesPalette);

        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(servicesPalette, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = SeedWorldCatalog.BuildTrails(variant, townNames);

        return new SeedWorld(
            Guid.Empty,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            selectedTownIds,
            townServices,
            trails);
    }

    private static Guid CreateCanonicalSeedCodeCore()
        => CreateRepresentativeSeedCode(CreateCanonicalSeedWorldShape());
}
