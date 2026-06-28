using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public enum SeedWorldVariant
{
    Canonical = 0,
    Frontier = 1,
    Rail = 2
}

internal sealed record SeedTownDefinition(
    string Id,
    string Name,
    TownServices CanonicalServices,
    TownServices FrontierServices,
    TownServices RailServices)
{
    public Town Create(SeedWorldVariant variant)
        => new(
            new TownId(Id),
            Name,
            variant switch
            {
                SeedWorldVariant.Canonical => CanonicalServices,
                SeedWorldVariant.Frontier => FrontierServices,
                SeedWorldVariant.Rail => RailServices,
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported seed world variant.")
            });
}

internal sealed record SeedTrailVariant(
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    decimal RideDayDistance);

internal sealed record SeedTrailDefinition(
    string Id,
    string FromTownId,
    string ToTownId,
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

    public Trail Create(SeedWorldVariant variant)
    {
        var selected = ForVariant(variant);
        return new Trail(
            new TrailId(Id),
            new TownId(FromTownId),
            new TownId(ToTownId),
            Risk,
            selected.Terrain,
            selected.WaterFeature,
            selected.RideDayDistance);
    }
}

/// <summary>
/// The full town and trail catalog. The seed-derived town selection model
/// selects a subset of towns from this catalog and includes trails where
/// both endpoints are selected. Terrain/water/distance are indexed by
/// world variant. The seed determines which towns and trails are included;
/// the catalog provides the base definitions.
/// </summary>
internal static class SeedWorldCatalog
{
    public static TownId PinecrossId { get; } = new("pinecross");

    /// <summary>
    /// All towns in the catalog, available for seed-derived selection.
    /// </summary>
    public static IReadOnlyList<SeedTownDefinition> AllTowns { get; } =
    [
        new("pinecross", "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard, TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard, TownServices.Supplies | TownServices.Lodging),
        new("redmesa", "Red Mesa", TownServices.Supplies | TownServices.Telegraph, TownServices.Supplies | TownServices.Telegraph, TownServices.Supplies | TownServices.Telegraph | TownServices.NoticeBoard),
        new("holloway", "Holloway", TownServices.Doctor, TownServices.Doctor | TownServices.NoticeBoard, TownServices.Doctor),
        new("sagewell", "Sagewell", TownServices.Supplies | TownServices.Doctor, TownServices.Supplies | TownServices.Doctor, TownServices.Supplies | TownServices.Doctor | TownServices.NoticeBoard),
        new("dryfork", "Dry Fork", TownServices.None, TownServices.None, TownServices.None),
        new("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph, TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph, TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph),
        new("hardpan", "Hardpan", TownServices.None, TownServices.None, TownServices.None),
        new("openpass", "Open Pass", TownServices.None, TownServices.None, TownServices.None)
    ];

    /// <summary>
    /// All trail definitions in the catalog. A trail is included in a
    /// seed world only if both endpoints are in the selected town set.
    /// </summary>
    public static IReadOnlyList<SeedTrailDefinition> AllTrails { get; } =
    [
        new("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m)),
        new("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 2m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 2m)),
        new("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 3m)),
        new("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 5m)),
        new("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.River, 3m)),
        new("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Mountains, WaterFeature.Spring, 5m)),
        new("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 5m)),
        new("trail-pine-hardpan", "pinecross", "hardpan", TrailRisk.Low, new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 3m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 3m)),
        new("trail-pine-openpass", "pinecross", "openpass", TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.None, 3m))
    ];

    /// <summary>
    /// Anchor towns that must always be selected to guarantee trail graph
    /// connectivity. pinecross is the safe starting-town default; redmesa
    /// and holloway connect pinecross to the rest of the map.
    /// </summary>
    public static IReadOnlyList<string> AnchorTownIds { get; } = ["pinecross", "redmesa", "holloway"];

    /// <summary>
    /// Towns available for seed-derived selection (all catalog towns except
    /// the anchors, which are always included).
    /// </summary>
    public static IReadOnlyList<string> SelectableTownIds { get; } =
        AllTowns.Select(t => t.Id).Where(id => !AnchorTownIds.Contains(id)).ToArray();

    public static SeedTownDefinition GetTown(string id)
        => AllTowns.First(t => t.Id == id);

    /// <summary>
    /// Builds a World from a seed world's selected town IDs and trail graph.
    /// The seed world holds the trail terrain/water/distance; the catalog
    /// provides town definitions (services per variant).
    /// Future seam: DifficultyEnvelope may modify terrain/distance downstream.
    /// </summary>
    public static World CreateWorld(
        SeedWorldVariant variant,
        IReadOnlyList<string> selectedTownIds,
        IReadOnlyList<SeedWorldTrail> trails)
    {
        var towns = selectedTownIds
            .Select(id => GetTown(id).Create(variant))
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
    /// The canonical world: all 8 towns, all 9 trails, Canonical variant.
    /// Used by SeedWorldMapLayout for the start-screen map.
    /// </summary>
    public static World CreateCanonicalWorld()
    {
        var allTownIds = AllTowns.Select(t => t.Id).ToArray();
        var canonicalTrails = AllTrails
            .Select(t => new SeedWorldTrail(
                t.Id,
                t.FromTownId,
                t.ToTownId,
                t.Risk,
                t.Canonical.Terrain,
                t.Canonical.WaterFeature,
                t.Canonical.RideDayDistance))
            .ToArray();
        return CreateWorld(SeedWorldVariant.Canonical, allTownIds, canonicalTrails);
    }
}
