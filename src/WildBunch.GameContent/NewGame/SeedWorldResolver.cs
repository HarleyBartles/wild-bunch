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
///   bits  2-5:   accusationIndex (4, bandwidth for up to 16 suspects; current roster is 7)
///   bits  6-9:   defaultCulpritIndex (4, bandwidth for up to 16 suspects; current roster is 7)
///   bits 10-13:  cashBonus (4)
///   bits 14-17:  townCount (4, offset-encoded: 0-15 → 5-20 towns)
///   bits 18-20:  prosperityPaletteIndex (3, indexes 8 palettes)
///   bits 21-23:  servicesPaletteIndex (3, indexes 8 palettes)
///   bits 24-26:  mapLayoutPalette (3, indexes layout palettes; 4 used, 4 reserved)
///   bit  27:     hasOutlierSlot (1, indicates presence of outlier town slot)
///   bits 28-63:  reserved (36)
///
/// Bytes 8-15 (high): reserved (64)
/// </code>
///
/// Total used: 28 bits. Reserved: 100 bits for future seed-owned fields
/// (warrants, etc.). Bandwidth scales with max selection (20),
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
    /// - v7: Expanded accusationIndex and defaultCulpritIndex from 3 bits to 4 bits
    ///       (bandwidth for up to 16 suspects; current roster is 7). 24 bits used,
    ///       104 reserved. No domain behavior change — validation still enforces 0-6.
    /// - v8: Added MapLayoutPalette (2 bits, positions 24-25). Supports HubAndSpoke,
    ///       LinearChain, Ring layouts. 26 bits used, 102 reserved.
    /// - v9: Expanded MapLayoutPalette from 2 bits to 3 bits (positions 24-26).
    ///       Supports HubAndSpoke, LinearChain, Ring, DoubleLine layouts. 27 bits used,
    ///       101 reserved.
    /// - v10: Added modulo wrapping for prosperity and services palettes (8 palettes each).
    ///       27 bits used, 101 reserved.
    /// - v11: Capped town count at 10 via modulo wrapping (4-bit field 0-15 → 5-20, wrapped to 5-10).
    ///       Bit layout unchanged from v10. 27 bits used, 101 reserved.
    /// - v12: Added HasOutlierSlot bit (position 27). Indicates presence of outlier town slot.
    ///       28 bits used, 100 reserved.
    /// </summary>
    public const string ResolverContractVersion = "resolver-v12";
    private const string SeedCodeFormat = "D";

    /// <summary>Minimum number of towns in a valid world.</summary>
    public const int MinTownCount = 5;

    /// <summary>Maximum number of towns in a valid world. Offset-encoded in 4 bits (0-15 → 5-20), wrapped to 5-10 via modulo.</summary>
    public const int MaxTownCount = 10;

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
        var accusationIndex = (int)((low >> 2) & 0xFUL);
        var defaultCulpritIndex = (int)((low >> 6) & 0xFUL);
        var cashBonus = (int)((low >> 10) & 0xFUL);
        var townCountEncoded = (int)((low >> 14) & 0xFUL);
        var prosperityPalette = (ProsperityPalette)((low >> 18) & 0x7UL);
        var servicesPalette = (ServicesPalette)((low >> 21) & 0x7UL);
        var mapLayoutPalette = (MapLayoutPalette)((low >> 24) & 0x7UL);
        var hasOutlierSlot = ((low >> 27) & 0x1UL) == 1UL;

        // 4-bit suspect fields produce 0-15, but the current roster is 7 suspects (indices 0-6).
        // Clamp to the current legal range. When the roster grows, raise this clamp.
        // The codec has bandwidth for up to 16 suspects without refactoring.
        if (accusationIndex > 6) accusationIndex = 6;
        if (defaultCulpritIndex > 6) defaultCulpritIndex = 6;

        // 3-bit mapLayoutPalette produces 0-7, but we currently define 4 layouts (indices 0-3).
        // Wrap within the current legal range using modulo. When more layouts are added,
        // this naturally expands the modulo divisor.
        mapLayoutPalette = (MapLayoutPalette)((int)mapLayoutPalette % 4);

        // 3-bit prosperityPalette produces 0-7, which maps to 8 palettes.
        // Wrap within the current legal range using modulo.
        prosperityPalette = (ProsperityPalette)((int)prosperityPalette % 8);

        // 3-bit servicesPalette produces 0-7, which maps to 8 palettes.
        // Wrap within the current legal range using modulo.
        servicesPalette = (ServicesPalette)((int)servicesPalette % 8);

        // Decode town count with offset: 4-bit value 0-15 → town count 5-20.
        // Wrap to 5-10 via modulo for v11.
        var townCount = (townCountEncoded + TownCountOffset) % (MaxTownCount + 1);
        if (townCount < MinTownCount) townCount += (MaxTownCount + 1);

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperityPalette, servicesPalette, mapLayoutPalette);

        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(servicesPalette, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = SeedWorldCatalog.BuildTrails(variant, townNames, mapLayoutPalette);

        return new SeedWorld(
            seedCode,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            mapLayoutPalette,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            selectedTownIds,
            townServices,
            trails,
            HasOutlierSlot: hasOutlierSlot);
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

        if (!Enum.IsDefined(typeof(MapLayoutPalette), seedWorld.MapLayoutPalette))
        {
            return SeedWorldValidationResult.Failed("Map layout palette is invalid.");
        }

        // Suspect indices: the codec allocates 4 bits each (0-15) for forward
        // compatibility, but the current gang roster is 7 suspects (indices 0-6).
        // Validation enforces the current legal range; raise both the validation
        // and the clamp in Resolve() when the roster grows.
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

        if (seedWorld.HasOutlierSlot && seedWorld.TownCount >= MaxTownCount)
        {
            return SeedWorldValidationResult.Failed("Cannot have outlier slot when town count is at maximum.");
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
        low |= (ulong)(uint)((int)seedWorld.WorldVariant & 0x3);
        low |= (ulong)(seedWorld.AccusationIndex & 0xF) << 2;
        low |= (ulong)(seedWorld.DefaultCulpritIndex & 0xF) << 6;
        low |= (ulong)(seedWorld.CashBonus & 0xF) << 10;
        low |= (ulong)((seedWorld.TownCount - TownCountOffset) & 0xF) << 14;
        low |= (ulong)((int)seedWorld.ProsperityPalette & 0x7) << 18;
        low |= (ulong)((int)seedWorld.ServicesPalette & 0x7) << 21;
        low |= (ulong)((int)seedWorld.MapLayoutPalette & 0x7) << 24;
        low |= (ulong)(seedWorld.HasOutlierSlot ? 1u : 0u) << 27;

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
        var mapLayoutPalette = MapLayoutPalette.HubAndSpoke;

        var townNames = SeedWorldCatalog.DeriveTownNames(
            variant, townCount, accusationIndex, defaultCulpritIndex,
            cashBonus, prosperityPalette, servicesPalette, mapLayoutPalette);

        var selectedTownIds = townNames.Select(t => t.Id).ToArray();
        var townServices = townNames
            .Select((t, i) => (t.Id, Services: ServicesPalettes.Resolve(servicesPalette, i)))
            .ToDictionary(x => x.Id, x => x.Services);
        var trails = SeedWorldCatalog.BuildTrails(variant, townNames, mapLayoutPalette);

        return new SeedWorld(
            Guid.Empty,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            mapLayoutPalette,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            selectedTownIds,
            townServices,
            trails,
            HasOutlierSlot: false);
    }

    private static Guid CreateCanonicalSeedCodeCore()
        => CreateRepresentativeSeedCode(CreateCanonicalSeedWorldShape());
}
