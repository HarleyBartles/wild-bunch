using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public enum SeedWorldVariant
{
    Canonical = 0,
    Frontier = 1,
    Rail = 2
}

internal sealed record SeedTrailVariant(
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    decimal RideDayDistance);

/// <summary>
/// A trail definition between two slot indices in the slot-based topology.
/// Trails are included when both slot indices are less than the town count.
/// Terrain/water/distance are indexed by world variant.
/// </summary>
internal sealed record SlotTrailDefinition(
    int FromSlot,
    int ToSlot,
    TrailRisk Risk,
    SeedTrailVariant Canonical,
    SeedTrailVariant Variant)
{
    public SeedTrailVariant ForVariant(SeedWorldVariant variant)
        => variant switch
        {
            SeedWorldVariant.Canonical => Canonical,
            SeedWorldVariant.Frontier => Variant,
            SeedWorldVariant.Rail => Variant,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported seed world variant.")
        };
}

/// <summary>
/// A flavor name entry in the town name pool. Names are purely cosmetic —
/// the seed derives which names go to which slots. Gameplay properties
/// (services, prosperity, trails) are all slot-based and independent of names.
/// </summary>
internal sealed record TownNameEntry(string Id, string Name);

/// <summary>
/// Catalog-defined prosperity palettes. Each palette is a fixed array of 10
/// <see cref="TownProsperity"/> values; the seed encodes a 3-bit index into
/// this catalog. When applying to a world with N selected towns, the first N
/// entries are used. This is the positional-pattern approach: palettes apply
/// by position in the selected town list, not by catalog index.
/// </summary>
internal static class ProsperityPalettes
{
    public const int Count = 8;

    private static readonly TownProsperity[][] Palettes =
    [
        // 0: UniformProsperous — all towns Prosperous
        [TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous],
        // 1: BoomtownHub — first town Boomtown, rest Prosperous
        [TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous],
        // 2: FrontierMix — alternating Prosperous/Poor
        [TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Poor],
        // 3: RichCenter — boomtowns in the middle, poor at edges
        [TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Boomtown, TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Boomtown, TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Boomtown, TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Boomtown, TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Poor],
        // 4: Dustbowl — mostly Poor, some Destitute
        [TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Poor, TownProsperity.Poor],
        // 5: GoldRush — one Boomtown, two Poor, rest Prosperous
        [TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Prosperous, TownProsperity.Poor],
        // 6: Struggling — one Prosperous hub, rest Poor, two Destitute
        [TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Poor, TownProsperity.Destitute],
        // 7: MixedBag — spread across all four tiers
        [TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Prosperous, TownProsperity.Boomtown, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Destitute, TownProsperity.Poor, TownProsperity.Boomtown, TownProsperity.Prosperous, TownProsperity.Poor, TownProsperity.Destitute, TownProsperity.Prosperous, TownProsperity.Boomtown, TownProsperity.Poor, TownProsperity.Prosperous, TownProsperity.Destitute, TownProsperity.Poor]
    ];

    /// <summary>
    /// Resolves the prosperity tier for a town at the given slot index.
    /// Falls back to Prosperous if the index is beyond the palette's slots.
    /// </summary>
    public static TownProsperity Resolve(ProsperityPalette palette, int slotIndex)
    {
        var tiers = Palettes[(int)palette];
        return (uint)slotIndex < (uint)tiers.Length
            ? tiers[slotIndex]
            : TownProsperity.Prosperous;
    }
}

/// <summary>
/// Catalog-defined services palettes. Each palette maps slot indices to
/// <see cref="TownServices"/> flags. Adding new service flags means defining
/// new palette entries that use them — zero additional bit cost.
/// </summary>
internal static class ServicesPalettes
{
    public const int Count = 8;

    private static readonly TownServices[][] Palettes =
    [
        // 0: NoTelegraph — no town has telegraph
        [TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None],
        // 1: HubTelegraph — only slot 0 has telegraph
        [TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None],
        // 2: TwinTelegraph — slots 0 and 10
        [TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None],
        // 3: RegionalTelegraph — slots 0, 6, 13
        [TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None],
        // 4: FrontierTelegraph — slots 0 and 1
        [TownServices.Telegraph, TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None],
        // 5: TelegraphWeb — alternating
        [TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.Telegraph, TownServices.None],
        // 6: SparseTelegraph — slots 0, 6, 13
        [TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.Telegraph, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None, TownServices.None],
        // 7: AllTelegraph — every slot
        [TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph, TownServices.Telegraph]
    ];

    /// <summary>
    /// Resolves the services flags for a town at the given slot index.
    /// </summary>
    public static TownServices Resolve(ServicesPalette palette, int slotIndex)
    {
        var services = Palettes[(int)palette];
        return (uint)slotIndex < (uint)services.Length
            ? services[slotIndex]
            : TownServices.None;
    }
}

/// <summary>
/// The slot-based world catalog. Town names are flavor — derived from the
/// seed, not encoded. The catalog provides a name pool (40 entries, twice
/// the max town count of 20) and a slot-based trail topology covering
/// slots 0-19. Services and prosperity are palette-indexed. The seed
/// encodes only: town count, variant, services palette, prosperity palette,
/// accusation index, culprit index, and cash bonus. Bandwidth scales with
/// max selection (20), not catalog size.
/// </summary>
internal static class SeedWorldCatalog
{
    /// <summary>
    /// The flavor name pool. At least twice the max town count (20), so
    /// different seeds can produce different name selections. Names are
    /// purely cosmetic — no gameplay properties are tied to names.
    /// </summary>
    public static IReadOnlyList<TownNameEntry> NamePool { get; } =
    [
        new("pinecross", "Pinecross"),
        new("redmesa", "Red Mesa"),
        new("holloway", "Holloway"),
        new("sagewell", "Sagewell"),
        new("dryfork", "Dry Fork"),
        new("emberfall", "Emberfall"),
        new("hardpan", "Hardpan"),
        new("openpass", "Open Pass"),
        new("dustwell", "Dustwell"),
        new("silvercreek", "Silver Creek"),
        new("rattleridge", "Rattle Ridge"),
        new("cottonwood", "Cottonwood"),
        new("boulderwash", "Boulder Wash"),
        new("mesaverde", "Mesa Verde"),
        new("coyotesprings", "Coyote Springs"),
        new("ironflats", "Iron Flats"),
        new("canyonfalls", "Canyon Falls"),
        new("tumbleweed", "Tumbleweed"),
        new("goldgulch", "Gold Gulch"),
        new("brokenarrow", "Broken Arrow"),
        new("whiskeyflats", "Whiskey Flats"),
        new("ravenwood", "Ravenwood"),
        new("saltflats", "Salt Flats"),
        new("deadman", "Deadman's Crossing"),
        new("buffalocreek", "Buffalo Creek"),
        new("thunderbutte", "Thunder Butte"),
        new("lassoranch", "Lasso Ranch"),
        new("windygap", "Windy Gap"),
        new("cinnabar", "Cinnabar"),
        new("rustedspur", "Rusted Spur"),
        new("hangmanstree", "Hangman's Tree"),
        new("quartzsite", "Quartzsite"),
        new("dovecreek", "Dove Creek"),
        new("shadowridge", "Shadow Ridge"),
        new("wildrose", "Wild Rose"),
        new("barrelcactus", "Barrel Cactus"),
        new("drygulch", "Dry Gulch"),
        new("silverton", "Silverton"),
        new("rattlesnake", "Rattlesnake"),
        new("lostcanyon", "Lost Canyon")
    ];

    /// <summary>
    /// Slot-based trail topology. Trails connect slot indices; a trail is
    /// included when both slot indices are less than the town count.
    /// Terrain/water/distance come from the variant. This guarantees a
    /// connected graph for any town count from 5 to 20.
    /// </summary>
    public static IReadOnlyList<SlotTrailDefinition> SlotTrails { get; } =
    [
        // Base connectivity (slots 0-4, always present for min 5 towns)
        new(0, 1, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m)),
        new(0, 2, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 2m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 2m)),
        new(1, 3, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 3m)),
        new(2, 4, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.River, 3m)),
        new(1, 4, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Mountains, WaterFeature.Spring, 5m)),
        new(0, 3, TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 5m)),
        // Additional trails for slot 5 (count >= 6)
        new(3, 5, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m)),
        new(4, 5, TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 5m)),
        // Additional trails for slot 6 (count >= 7)
        new(5, 6, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m)),
        new(0, 6, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 3m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 3m)),
        // Additional trails for slot 7 (count >= 8)
        new(6, 7, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m)),
        new(3, 7, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 4m)),
        // Additional trails for slot 8 (count >= 9)
        new(7, 8, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.River, 4m)),
        new(4, 8, TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 6m), new SeedTrailVariant(TrailTerrain.Mountains, WaterFeature.None, 6m)),
        // Additional trails for slot 9 (count >= 10)
        new(8, 9, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m)),
        new(5, 9, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 4m)),
        // Additional trails for slot 10 (count >= 11)
        new(9, 10, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m)),
        new(2, 10, TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 6m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 6m)),
        // Additional trails for slot 11 (count >= 12)
        new(10, 11, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m)),
        new(6, 11, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 5m)),
        // Additional trails for slot 12 (count >= 13)
        new(11, 12, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.River, 4m)),
        new(7, 12, TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 6m), new SeedTrailVariant(TrailTerrain.Mountains, WaterFeature.None, 6m)),
        // Additional trails for slot 13 (count >= 14)
        new(12, 13, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m)),
        new(8, 13, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 5m)),
        // Additional trails for slot 14 (count >= 15)
        new(13, 14, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m)),
        new(3, 14, TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 7m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 7m)),
        // Additional trails for slot 15 (count >= 16)
        new(14, 15, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m)),
        new(9, 15, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 5m)),
        // Additional trails for slot 16 (count >= 17)
        new(15, 16, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.River, 4m)),
        new(10, 16, TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 6m), new SeedTrailVariant(TrailTerrain.Mountains, WaterFeature.None, 6m)),
        // Additional trails for slot 17 (count >= 18)
        new(16, 17, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m)),
        new(11, 17, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 5m)),
        // Additional trails for slot 18 (count >= 19)
        new(17, 18, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m)),
        new(4, 18, TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 7m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 7m)),
        // Additional trails for slot 19 (count >= 20)
        new(18, 19, TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m)),
        new(12, 19, TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 5m))
    ];

    /// <summary>
    /// Derives town names from the encoded seed fields. Uses a deterministic
    /// xorshift PRNG seeded from the encoded fields to shuffle the name pool,
    /// then takes the first N entries. This is round-trip stable: the same
    /// encoded fields always produce the same name selection, and names are
    /// not encoded in the UUID — they are a derived view.
    /// </summary>
    public static IReadOnlyList<TownNameEntry> DeriveTownNames(
        SeedWorldVariant variant,
        int townCount,
        int accusationIndex,
        int defaultCulpritIndex,
        int cashBonus,
        ProsperityPalette prosperityPalette,
        ServicesPalette servicesPalette)
    {
        // Combine encoded fields into a 32-bit seed.
        var seed = (uint)(
            ((int)variant & 0x3) |
            ((accusationIndex & 0x7) << 2) |
            ((defaultCulpritIndex & 0x7) << 5) |
            ((cashBonus & 0xF) << 8) |
            ((townCount & 0xF) << 12) |
            ((int)prosperityPalette & 0x7) << 16 |
            ((int)servicesPalette & 0x7) << 19);

        // xorshift32 PRNG — deterministic, stable across runs.
        // Guard against seed=0: xorshift32 has 0 as a fixed point (produces all
        // zeros), which would make the shuffle a no-op. OR with 1 ensures the
        // seed is always non-zero while preserving all other bit patterns.
        if (seed == 0)
        {
            seed = 1;
        }

        var indices = new int[NamePool.Count];
        for (var i = 0; i < indices.Length; i++) indices[i] = i;

        // Fisher-Yates shuffle with xorshift32.
        for (var i = indices.Length - 1; i > 0; i--)
        {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            var j = (int)(seed % (uint)(i + 1));
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var result = new TownNameEntry[townCount];
        for (var i = 0; i < townCount; i++)
        {
            result[i] = NamePool[indices[i]];
        }
        return result;
    }

    /// <summary>
    /// Builds the trail graph for a world with the given town count and
    /// variant. Trails are included when both slot indices are less than
    /// the town count. Slot indices are mapped to derived town IDs.
    /// </summary>
    public static IReadOnlyList<SeedWorldTrail> BuildTrails(
        SeedWorldVariant variant,
        IReadOnlyList<TownNameEntry> townNames)
    {
        var trails = new List<SeedWorldTrail>();
        var count = townNames.Count;

        foreach (var def in SlotTrails)
        {
            if (def.FromSlot < count && def.ToSlot < count)
            {
                var tv = def.ForVariant(variant);
                trails.Add(new SeedWorldTrail(
                    $"trail-{def.FromSlot}-{def.ToSlot}",
                    townNames[def.FromSlot].Id,
                    townNames[def.ToSlot].Id,
                    def.Risk,
                    tv.Terrain,
                    tv.WaterFeature,
                    tv.RideDayDistance));
            }
        }

        return trails;
    }

    /// <summary>
    /// Builds a World from the seed-derived town names, services palette,
    /// prosperity palette, and trail graph. Prosperity and services are
    /// applied by slot position.
    /// </summary>
    public static World CreateWorld(
        SeedWorldVariant variant,
        IReadOnlyList<TownNameEntry> townNames,
        ServicesPalette servicesPalette,
        ProsperityPalette prosperityPalette,
        IReadOnlyList<SeedWorldTrail> trails)
    {
        var towns = townNames
            .Select((entry, index) =>
            {
                var services = ServicesPalettes.Resolve(servicesPalette, index);
                var prosperity = ProsperityPalettes.Resolve(prosperityPalette, index);
                return new Town(new TownId(entry.Id), entry.Name, services, prosperity);
            })
            .ToArray();
        var domainTrails = trails
            .Select(t => new Trail(
                new TrailId(t.Id),
                new TownId(t.FromTownId),
                new TownId(t.ToTownId),
                t.Risk,
                t.Terrain,
                t.WaterFeature,
                t.RideDayDistance))
            .ToArray();
        return new World(towns, domainTrails);
    }

    /// <summary>
    /// The canonical world: 8 towns, Canonical variant, UniformProsperous
    /// palette, HubTelegraph services palette. Used by SeedWorldMapLayout
    /// for the start-screen map.
    /// </summary>
    public static World CreateCanonicalWorld()
    {
        var townNames = DeriveTownNames(
            SeedWorldVariant.Canonical,
            townCount: 8,
            accusationIndex: 1,
            defaultCulpritIndex: 3,
            cashBonus: 0,
            prosperityPalette: ProsperityPalette.UniformProsperous,
            servicesPalette: ServicesPalette.HubTelegraph);
        var trails = BuildTrails(SeedWorldVariant.Canonical, townNames);
        return CreateWorld(
            SeedWorldVariant.Canonical,
            townNames,
            ServicesPalette.HubTelegraph,
            ProsperityPalette.UniformProsperous,
            trails);
    }
}
