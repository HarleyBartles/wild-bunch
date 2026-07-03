using WildBunch.Domain.World;
using WildBunch.Domain.Travel;
using WildBunch.Domain.Game;
using System.Collections.Generic;

namespace WildBunch.GameContent.NewGame;

public enum SeedWorldVariant
{
    Canonical = 0,
    Frontier = 1,
    Rail = 2,
    Outback = 3
}

/// <summary>
/// Map layout palette defines how towns are positioned and connected.
/// All layouts are designed with redundancy to support trail removal while maintaining connectivity.
/// Trails only meet at towns - no crossing trails between towns.
/// 2-bit encoding (4 layouts) with room for expansion to 8 layouts in future.
/// </summary>
public enum MapLayoutPalette
{
    HubAndSpoke = 0,        // Central hub with outer ring towns connected via spokes
    DoubleLine = 1,          // Two parallel lines of towns, connected at endpoints
    Tree = 2,                // Hierarchical structure with main trunk and branches
    Star = 3                 // Central hub with dead-end spokes (natural outlier positions)
}

/// <summary>
/// A flavor name entry in the town name pool. Names are purely cosmetic —
/// the seed derives which names go to which slots. Gameplay properties
/// (services, prosperity, trails) are all slot-based and independent of names.
/// </summary>
public sealed record TownNameEntry(string Id, string Name);

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
        int? outlierSlot = null,
        GameEntropy entropy = GameEntropy.Boring,
        SaltSource? saltSource = null,
        Guid? seedCode = null)
    {
        var towns = townNames
            .Select((entry, index) =>
            {
                var services = ServicesPalettes.Resolve(servicesPalette, index);
                var prosperity = ProsperityPalettes.Resolve(prosperityPalette, index);
                var (mapX, mapY) = townCoordinates != null && townCoordinates.TryGetValue(index, out var coords)
                    ? coords
                    : SeedWorldMapLayout.GetCoordinatesForSlot(index, townNames.Count, MapLayoutPalette.HubAndSpoke);

                // Apply rotation if seed code and entropy are provided
                if (seedCode.HasValue && entropy != GameEntropy.Boring)
                {
                    var rotation = SeedWorldMapLayout.DeriveRotation(seedCode.Value, entropy, saltSource);
                    (mapX, mapY) = SeedWorldMapLayout.RotateCoordinates(mapX, mapY, rotation, entropy);
                }

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
        
        // Generate town coordinates for canonical layout
        var townCoordinates = new Dictionary<int, (int X, int Y)>();
        for (var i = 0; i < townNames.Count; i++)
        {
            townCoordinates[i] = SeedWorldMapLayout.GetCoordinatesForSlot(i, townNames.Count, MapLayoutPalette.HubAndSpoke);
        }
        
        // Generate trails using geometry-first approach
        var trails = TrailTopologyGenerator.GenerateTrailTopology(
            townCoordinates,
            townNames,
            GameEntropy.Boring,
            null,
            new GameSetupDeterministicSource(Guid.Empty.ToString("D")),
            null);
        
        return CreateWorld(
            SeedWorldVariant.Canonical,
            townNames,
            ServicesPalette.HubTelegraph,
            ProsperityPalette.UniformProsperous,
            trails,
            townCoordinates,
            outlierSlot: null);
    }
}
