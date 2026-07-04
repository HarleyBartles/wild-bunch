namespace WildBunch.Domain.World;

/// <summary>
/// Immutable snapshot of a generated world for event storage and replay.
/// Carried by the WorldGenerated domain event.
/// </summary>
public sealed record WorldSnapshot(IReadOnlyList<TownSnapshot> Towns, IReadOnlyList<TrailSnapshot> Trails)
{
    public static WorldSnapshot FromDomain(World world)
        => new(
            world.Towns.Select(t => TownSnapshot.FromDomain(t)).ToArray(),
            world.Trails.Select(t => TrailSnapshot.FromDomain(t)).ToArray());

    public World ToDomain()
        => new(Towns.Select(t => t.ToDomain()), Trails.Select(t => t.ToDomain()));
}

public sealed record TownSnapshot(
    string Id,
    string Name,
    TownServices Services,
    TownProsperity Prosperity,
    int MapX,
    int MapY,
    bool IsOutlier)
{
    public static TownSnapshot FromDomain(Town town)
        => new(town.Id.Value, town.Name, town.Services, town.Prosperity, town.MapX, town.MapY, town.IsOutlier);

    public Town ToDomain()
        => new(new TownId(Id), Name, Services, Prosperity, MapX: MapX, MapY: MapY, IsOutlier: IsOutlier);
}

public sealed record TrailSnapshot(
    string Id,
    string FromTownId,
    string ToTownId,
    TrailRisk Risk,
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    decimal RideDayDistance)
{
    public static TrailSnapshot FromDomain(Trail trail)
        => new(trail.Id.Value, trail.FromTownId.Value, trail.ToTownId.Value, trail.Risk, trail.Terrain, trail.WaterFeature, trail.RideDayDistance);

    public Trail ToDomain()
        => new(new TrailId(Id), new TownId(FromTownId), new TownId(ToTownId), Risk, Terrain, WaterFeature, RideDayDistance);
}
