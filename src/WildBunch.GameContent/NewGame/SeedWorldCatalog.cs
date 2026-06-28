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
    public Trail Create(SeedWorldVariant variant)
    {
        var selected = variant switch
        {
            SeedWorldVariant.Canonical => Canonical,
            SeedWorldVariant.Frontier => Variant,
            SeedWorldVariant.Rail => Variant,
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unsupported seed world variant.")
        };

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

internal static class SeedWorldCatalog
{
    public static TownId PinecrossId { get; } = new("pinecross");

    private static readonly SeedTownDefinition[] Towns =
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

    private static readonly SeedTrailDefinition[] Trails =
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

    // Alternate town set: replaces openpass with coppercreek.
    // Copper Creek has Supplies (Open Pass has None) and a different
    // trail profile (Hills/Spring/4m vs OpenRange/None/3m).
    private static readonly SeedTownDefinition AlternateCoppercreek =
        new("coppercreek", "Copper Creek",
            TownServices.Supplies,
            TownServices.Supplies | TownServices.NoticeBoard,
            TownServices.Supplies);

    private static readonly SeedTrailDefinition AlternateTrailPineCoppercreek =
        new("trail-pine-coppercreek", "pinecross", "coppercreek", TrailRisk.Low,
            new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 4m),
            new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 4m));

    private const string DefaultTownSet = GameSetupDeterministicLabels.WorldTownSetDefault;
    private const string OpenPassTownId = "openpass";
    private const string OpenPassTrailId = "trail-pine-openpass";

    public static World CreateWorld(SeedWorldVariant variant, string townSetKey)
    {
        var useAlternate = townSetKey != DefaultTownSet;
        var towns = GetTowns(useAlternate);
        var trails = GetTrails(useAlternate);
        return new World(
            towns.Select(town => town.Create(variant)),
            trails.Select(trail => trail.Create(variant)));
    }

    private static IEnumerable<SeedTownDefinition> GetTowns(bool useAlternate)
    {
        foreach (var town in Towns)
        {
            if (useAlternate && town.Id == OpenPassTownId)
                continue;
            yield return town;
        }

        if (useAlternate)
            yield return AlternateCoppercreek;
    }

    private static IEnumerable<SeedTrailDefinition> GetTrails(bool useAlternate)
    {
        foreach (var trail in Trails)
        {
            if (useAlternate && trail.Id == OpenPassTrailId)
                continue;
            yield return trail;
        }

        if (useAlternate)
            yield return AlternateTrailPineCoppercreek;
    }
}
