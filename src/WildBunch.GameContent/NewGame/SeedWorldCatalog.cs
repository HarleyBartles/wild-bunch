using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public enum SeedWorldVariant
{
    Canonical = 0,
    Frontier = 1,
    Rail = 2
}

/// <summary>
/// Map layout palette defines how towns are positioned and connected.
/// All layouts are designed with redundancy to support trail removal while maintaining connectivity.
/// Trails only meet at towns - no crossing trails between towns.
/// </summary>
public enum MapLayoutPalette
{
    HubAndSpoke = 0,        // Central hub with outer ring towns connected via spokes
    DoubleLine = 1,          // Two parallel lines of towns, connected at endpoints
    XShaped = 2,             // Four arms meeting at central town, each arm is a line of towns
    Tree = 3,                // Hierarchical structure with main trunk and branches
    Star = 4,                // Central hub with many dead-end spokes
    Cluster = 5,             // Multiple mini-hubs (2-3 towns each) connected together
    Mesh = 6,                // Fully connected network with lots of redundancy
    Grid = 7                 // 2D grid structure (3x3 max) with trails along grid lines
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
        ServicesPalette servicesPalette,
        MapLayoutPalette mapLayoutPalette)
    {
        // Combine encoded fields into a 32-bit seed.
        var seed = (uint)(
            ((int)variant & 0x3) |
            ((accusationIndex & 0xF) << 2) |
            ((defaultCulpritIndex & 0xF) << 6) |
            ((cashBonus & 0xF) << 10) |
            ((townCount & 0xF) << 14) |
            ((int)prosperityPalette & 0x7) << 18 |
            ((int)servicesPalette & 0x7) << 21 |
            ((int)mapLayoutPalette & 0x7) << 24);

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
    /// Builds the trail graph for a world with the given town count, variant,
    /// and map layout palette. Trails are included when both slot indices are
    /// less than the town count. Slot indices are mapped to derived town IDs.
    /// </summary>
    public static IReadOnlyList<SeedWorldTrail> BuildTrails(
        SeedWorldVariant variant,
        IReadOnlyList<TownNameEntry> townNames,
        MapLayoutPalette mapLayoutPalette)
    {
        var trails = new List<SeedWorldTrail>();
        var count = townNames.Count;

        foreach (var def in GenerateTrailsForLayout(mapLayoutPalette, count))
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
    /// Generates trail definitions for a given layout palette and town count.
    /// All layouts are designed with redundancy to support trail removal while maintaining connectivity.
    /// Trails only meet at towns - no crossing trails between towns.
    /// </summary>
    private static IReadOnlyList<SlotTrailDefinition> GenerateTrailsForLayout(
        MapLayoutPalette layout,
        int townCount)
    {
        return layout switch
        {
            MapLayoutPalette.HubAndSpoke => GenerateHubAndSpokeTrails(townCount),
            MapLayoutPalette.DoubleLine => GenerateDoubleLineTrails(townCount),
            MapLayoutPalette.XShaped => GenerateXShapedTrails(townCount),
            MapLayoutPalette.Tree => GenerateTreeTrails(townCount),
            MapLayoutPalette.Star => GenerateStarTrails(townCount),
            MapLayoutPalette.Cluster => GenerateClusterTrails(townCount),
            MapLayoutPalette.Mesh => GenerateMeshTrails(townCount),
            MapLayoutPalette.Grid => GenerateGridTrails(townCount),
            _ => throw new ArgumentOutOfRangeException(nameof(layout), $"Unknown map layout palette: {layout}")
        };
    }

    private static IReadOnlyList<SlotTrailDefinition> GenerateHubAndSpokeTrails(int count)
    {
        var trails = new List<SlotTrailDefinition>();

        // Spokes: hub (slot 0) to each outer town
        for (var i = 1; i < count; i++)
        {
            trails.Add(new SlotTrailDefinition(
                0, i, TrailRisk.Low,
                new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m),
                new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m)));
        }

        // Ring: outer towns connected in a circle
        for (var i = 1; i < count; i++)
        {
            var next = i == count - 1 ? 1 : i + 1;
            trails.Add(new SlotTrailDefinition(
                i, next, TrailRisk.Moderate,
                new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m),
                new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 3m)));
        }

        return trails;
    }

    private static IReadOnlyList<SlotTrailDefinition> GenerateXShapedTrails(int count)
    {
        var trails = new List<SlotTrailDefinition>();
        var canonical = new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m);
        var variant = new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m);

        // Central hub (slot 0) to four arms
        var armCount = Math.Min(4, count - 1);
        for (var i = 0; i < armCount; i++)
        {
            trails.Add(new SlotTrailDefinition(0, i + 1, TrailRisk.Low, canonical, variant));
        }

        // Arm extensions (if town count > 5)
        for (var i = 0; i < armCount; i++)
        {
            var extensionSlot = i + 5;
            if (extensionSlot < count)
            {
                trails.Add(new SlotTrailDefinition(i + 1, extensionSlot, TrailRisk.Moderate, canonical, variant));
            }
        }

        return trails;
    }

    private static IReadOnlyList<SlotTrailDefinition> GenerateTreeTrails(int count)
    {
        var trails = new List<SlotTrailDefinition>();
        var canonical = new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m);
        var variant = new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m);

        // Main trunk
        var trunkLength = Math.Min(4, count);
        for (var i = 0; i < trunkLength - 1; i++)
        {
            var risk = i < 2 ? TrailRisk.Low : TrailRisk.Moderate;
            trails.Add(new SlotTrailDefinition(i, i + 1, risk, canonical, variant));
        }

        // Branches from trunk
        for (var i = 1; i < trunkLength; i++)
        {
            var branchSlot = i + 3;
            if (branchSlot < count)
            {
                trails.Add(new SlotTrailDefinition(i, branchSlot, TrailRisk.Moderate, canonical, variant));
            }
        }

        return trails;
    }

    private static IReadOnlyList<SlotTrailDefinition> GenerateStarTrails(int count)
    {
        var trails = new List<SlotTrailDefinition>();
        var canonical = new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m);
        var variant = new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m);

        // Central hub (slot 0) to all other towns
        for (var i = 1; i < count; i++)
        {
            trails.Add(new SlotTrailDefinition(0, i, TrailRisk.Low, canonical, variant));
        }

        return trails;
    }

    private static IReadOnlyList<SlotTrailDefinition> GenerateClusterTrails(int count)
    {
        var trails = new List<SlotTrailDefinition>();
        var canonical = new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m);
        var variant = new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m);

        // Mini-hub groups (0-1, 2-3, 4-5)
        if (count >= 2) trails.Add(new SlotTrailDefinition(0, 1, TrailRisk.Low, canonical, variant));
        if (count >= 4) trails.Add(new SlotTrailDefinition(2, 3, TrailRisk.Low, canonical, variant));
        if (count >= 6) trails.Add(new SlotTrailDefinition(4, 5, TrailRisk.Low, canonical, variant));

        // Inter-cluster connections
        if (count >= 3) trails.Add(new SlotTrailDefinition(1, 2, TrailRisk.Moderate, canonical, variant));
        if (count >= 5) trails.Add(new SlotTrailDefinition(3, 4, TrailRisk.Moderate, canonical, variant));
        if (count >= 6) trails.Add(new SlotTrailDefinition(5, 0, TrailRisk.Moderate, canonical, variant));

        // Additional towns connect to nearest cluster
        for (var i = 6; i < count; i++)
        {
            var clusterSlot = (i % 3) * 2;
            trails.Add(new SlotTrailDefinition(i, clusterSlot, TrailRisk.Moderate, canonical, variant));
        }

        return trails;
    }

    private static IReadOnlyList<SlotTrailDefinition> GenerateMeshTrails(int count)
    {
        var canonical = new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m);
        var variant = new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m);

        // Fully connected network - every town connected to every other town
        return Enumerable.Range(0, count)
            .SelectMany(i => Enumerable.Range(i + 1, count - i - 1)
                .Select(j => new SlotTrailDefinition(i, j, TrailRisk.Low, canonical, variant)))
            .ToArray();
    }

    private static IReadOnlyList<SlotTrailDefinition> GenerateGridTrails(int count)
    {
        var trails = new List<SlotTrailDefinition>();
        var canonical = new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m);
        var variant = new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 4m);

        // 3x3 grid: rows and columns
        // Row 0: 0-1-2
        if (count >= 2) trails.Add(new SlotTrailDefinition(0, 1, TrailRisk.Low, canonical, variant));
        if (count >= 3) trails.Add(new SlotTrailDefinition(1, 2, TrailRisk.Low, canonical, variant));

        // Row 1: 3-4-5
        if (count >= 4) trails.Add(new SlotTrailDefinition(3, 4, TrailRisk.Low, canonical, variant));
        if (count >= 5) trails.Add(new SlotTrailDefinition(4, 5, TrailRisk.Low, canonical, variant));

        // Row 2: 6-7-8
        if (count >= 7) trails.Add(new SlotTrailDefinition(6, 7, TrailRisk.Low, canonical, variant));
        if (count >= 8) trails.Add(new SlotTrailDefinition(7, 8, TrailRisk.Low, canonical, variant));

        // Columns
        // Column 0: 0-3-6
        if (count >= 4) trails.Add(new SlotTrailDefinition(0, 3, TrailRisk.Low, canonical, variant));
        if (count >= 7) trails.Add(new SlotTrailDefinition(3, 6, TrailRisk.Low, canonical, variant));

        // Column 1: 1-4-7
        if (count >= 5) trails.Add(new SlotTrailDefinition(1, 4, TrailRisk.Low, canonical, variant));
        if (count >= 8) trails.Add(new SlotTrailDefinition(4, 7, TrailRisk.Low, canonical, variant));

        // Column 2: 2-5-8
        if (count >= 6) trails.Add(new SlotTrailDefinition(2, 5, TrailRisk.Low, canonical, variant));
        if (count >= 9) trails.Add(new SlotTrailDefinition(5, 8, TrailRisk.Low, canonical, variant));

        return trails;
    }

    private static IReadOnlyList<SlotTrailDefinition> GenerateDoubleLineTrails(int count)
    {
        var trails = new List<SlotTrailDefinition>();
        var mid = count / 2;
        var canonical = new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m);
        var variant = new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 3m);
        var crossCanonical = new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 4m);
        var crossVariant = new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 4m);

        // Line 1: 0-1-2-...-mid-1
        for (var i = 0; i < mid - 1; i++)
        {
            trails.Add(new SlotTrailDefinition(i, i + 1, TrailRisk.Low, canonical, variant));
        }

        // Line 2: mid-mid+1-...-count-1
        for (var i = mid; i < count - 1; i++)
        {
            trails.Add(new SlotTrailDefinition(i, i + 1, TrailRisk.Low, canonical, variant));
        }

        // Connections between lines (at endpoints only - no crossing trails)
        if (mid > 0 && mid < count)
        {
            // Connect end of line 1 to start of line 2
            trails.Add(new SlotTrailDefinition(mid - 1, mid, TrailRisk.Moderate, crossCanonical, crossVariant));
        }

        if (count >= 2)
        {
            // Connect start of line 1 to end of line 2
            trails.Add(new SlotTrailDefinition(0, count - 1, TrailRisk.Moderate, crossCanonical, crossVariant));
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
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)>? townCoordinates = null,
        int? outlierSlot = null)
    {
        var towns = townNames
            .Select((entry, index) =>
            {
                var services = ServicesPalettes.Resolve(servicesPalette, index);
                var prosperity = ProsperityPalettes.Resolve(prosperityPalette, index);
                var (mapX, mapY) = townCoordinates != null && townCoordinates.TryGetValue(index, out var coords)
                    ? coords
                    : (0, 0);
                var isOutlier = outlierSlot.HasValue && index == outlierSlot.Value;
                return new Town(new TownId(entry.Id), entry.Name, services, prosperity, MapX: mapX, MapY: mapY, IsOutlier: isOutlier);
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
            servicesPalette: ServicesPalette.HubTelegraph,
            mapLayoutPalette: MapLayoutPalette.HubAndSpoke);
        var trails = BuildTrails(SeedWorldVariant.Canonical, townNames, MapLayoutPalette.HubAndSpoke);
        return CreateWorld(
            SeedWorldVariant.Canonical,
            townNames,
            ServicesPalette.HubTelegraph,
            ProsperityPalette.UniformProsperous,
            trails,
            townCoordinates: null,
            outlierSlot: null);
    }
}
