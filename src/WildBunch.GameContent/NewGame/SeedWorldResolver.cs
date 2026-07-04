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
///   bits 24-25:  clusterCount (2, 0-3 → 1-4 clusters)
///   bit  26:     graphDensity (1, 0=Sparse, 1=Dense)
///   bits 27-28:  outlierSlotType (2, 0=no outlier, 1=simple outlier, 2-3 reserved)
///   bits 29-63:  reserved (35)
///
/// Bytes 8-15 (high): reserved (64)
/// </code>
///
/// Total used: 29 bits. Reserved: 99 bits for future seed-owned fields
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
    /// - v13: Expanded MapLayoutPalette to 8 layouts (HubAndSpoke, DoubleLine, XShaped, Tree,
    ///       Star, Cluster, Mesh, Grid). Removed LinearChain and Ring. Bit layout unchanged from v12.
    ///       28 bits used, 100 reserved.
    /// - v14: Expanded outlier slot from 1 bit to 2 bits (positions 27-28). Supports outlier type encoding
    ///       (0=no outlier, 1=simple outlier, 2-3 reserved). 29 bits used, 99 reserved.
    /// - v15: Reduced MapLayoutPalette from 8 layouts to 4 layouts (HubAndSpoke, DoubleLine, Tree, Star).
    ///       Dropped XShaped, Cluster, Mesh, Grid. Kept 3-bit encoding for future expansion to 8 layouts.
    ///       OutlierSlotType remains at bits 27-28. 29 bits used, 99 reserved.
    /// - v16: Replaced MapLayoutPalette (3 bits at 24-26) with ClusterCount (2 bits at 24-25, 0-3 → 1-4 clusters)
    ///       and GraphDensity (1 bit at 26, 0=Sparse, 1=Dense). MapLayoutPalette enum deleted.
    ///       29 bits used, 99 reserved. No domain behavior change to other fields.
    /// </summary>
    public const string ResolverContractVersion = "resolver-v16";
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
        var clusterCountEncoded = (int)((low >> 24) & 0x3UL); // 2 bits for cluster count
        var graphDensity = (GraphDensity)((low >> 26) & 0x1UL); // 1 bit for graph density
        var outlierSlotType = (int)((low >> 27) & 0x3UL); // 2 bits for outlier type

        // 4-bit suspect fields produce 0-15, but the current roster is 7 suspects (indices 0-6).
        // Clamp to the current legal range. When the roster grows, raise this clamp.
        // The codec has bandwidth for up to 16 suspects without refactoring.
        if (accusationIndex > 6) accusationIndex = 6;
        if (defaultCulpritIndex > 6) defaultCulpritIndex = 6;

        // 2-bit OutlierSlotType produces 0-3, but only 0-1 are currently implemented.
        // Clamp to the current legal range. Values 2-3 are reserved for future expansion.
        if (outlierSlotType > 1) outlierSlotType = 1;

        // 2-bit clusterCountEncoded produces 0-3, mapped to 1-4 clusters.
        var clusterCount = clusterCountEncoded + 1;

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

        return new SeedWorld(
            seedCode,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            clusterCount,
            graphDensity,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            OutlierSlotType: outlierSlotType);
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

        if (seedWorld.ClusterCount is < 1 or > 4)
        {
            return SeedWorldValidationResult.Failed("Cluster count must be between 1 and 4.");
        }

        if (!Enum.IsDefined(typeof(GraphDensity), seedWorld.GraphDensity))
        {
            return SeedWorldValidationResult.Failed("Graph density is invalid.");
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

        if (seedWorld.OutlierSlotType > 0 && seedWorld.TownCount >= MaxTownCount)
        {
            return SeedWorldValidationResult.Failed("Cannot have outlier slot when town count is at maximum.");
        }

        if (seedWorld.OutlierSlotType is < 0 or > 1)
        {
            return SeedWorldValidationResult.Failed("Outlier slot type must be 0 (no outlier) or 1 (simple outlier). Values 2-3 are reserved for future expansion.");
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
        low |= (ulong)((seedWorld.ClusterCount - 1) & 0x3) << 24; // 2 bits for cluster count (1-4 → 0-3)
        low |= (ulong)((int)seedWorld.GraphDensity & 0x1) << 26; // 1 bit for graph density
        low |= (ulong)(seedWorld.OutlierSlotType & 0x3) << 27; // 2 bits for outlier type

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
        var clusterCount = 1;
        var graphDensity = GraphDensity.Sparse;

        return new SeedWorld(
            Guid.Empty,
            variant,
            townCount,
            servicesPalette,
            prosperityPalette,
            clusterCount,
            graphDensity,
            accusationIndex,
            defaultCulpritIndex,
            cashBonus,
            OutlierSlotType: 0);
    }

    private static Guid CreateCanonicalSeedCodeCore()
        => CreateRepresentativeSeedCode(CreateCanonicalSeedWorldShape());
}
