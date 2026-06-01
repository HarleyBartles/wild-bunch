using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal enum SeedWorldVariant
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
        new("emberfall", "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph, TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph, TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph)
    ];

    private static readonly SeedTrailDefinition[] Trails =
    [
        new("trail-pine-red", "pinecross", "redmesa", TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m), new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 4m)),
        new("trail-pine-hollow", "pinecross", "holloway", TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 2m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Spring, 2m)),
        new("trail-red-sage", "redmesa", "sagewell", TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.Creek, 3m)),
        new("trail-red-dry", "redmesa", "dryfork", TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 5m)),
        new("trail-hollow-sage", "holloway", "sagewell", TrailRisk.Low, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 3m), new SeedTrailVariant(TrailTerrain.Hills, WaterFeature.River, 3m)),
        new("trail-sage-ember", "sagewell", "emberfall", TrailRisk.Moderate, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Mountains, WaterFeature.Spring, 5m)),
        new("trail-red-ember", "redmesa", "emberfall", TrailRisk.High, new SeedTrailVariant(TrailTerrain.OpenRange, WaterFeature.Creek, 5m), new SeedTrailVariant(TrailTerrain.Badlands, WaterFeature.None, 5m))
    ];

    public static World CreateWorld(SeedWorldVariant variant)
        => new(
            Towns.Select(town => town.Create(variant)),
            Trails.Select(trail => trail.Create(variant)));
}
