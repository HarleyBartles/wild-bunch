namespace WildBunch.Domain.World;

public readonly record struct TownId(string Value);

public readonly record struct TrailId(string Value);

public sealed record Town(TownId Id, string Name, TownServices Services);

public enum TrailTerrain
{
    OpenRange = 0,
    Badlands = 1,
    Hills = 2,
    Mountains = 3
}

public enum WaterFeature
{
    None = 0,
    Creek = 1,
    River = 2,
    Spring = 3
}

public sealed record Trail
{
    public Trail(
        TrailId id,
        TownId fromTownId,
        TownId toTownId,
        TrailRisk risk,
        TrailTerrain terrain = TrailTerrain.OpenRange,
        WaterFeature waterFeature = WaterFeature.Creek,
        decimal rideDayDistance = 0m)
    {
        Id = id;
        FromTownId = fromTownId;
        ToTownId = toTownId;
        Risk = risk;
        Terrain = terrain;
        WaterFeature = waterFeature;
        RideDayDistance = rideDayDistance > 0 ? rideDayDistance : DefaultRideDayDistance(risk);
    }

    public TrailId Id { get; }

    public TownId FromTownId { get; }

    public TownId ToTownId { get; }

    public TrailRisk Risk { get; }

    public TrailTerrain Terrain { get; }

    public WaterFeature WaterFeature { get; }

    public decimal RideDayDistance { get; }

    public bool Connects(TownId townId) => FromTownId.Equals(townId) || ToTownId.Equals(townId);

    public bool Connects(TownId originTownId, TownId destinationTownId)
        => (FromTownId.Equals(originTownId) && ToTownId.Equals(destinationTownId))
            || (FromTownId.Equals(destinationTownId) && ToTownId.Equals(originTownId));

    private static decimal DefaultRideDayDistance(TrailRisk risk)
        => risk switch
        {
            TrailRisk.Low => 1m,
            TrailRisk.Moderate => 2m,
            TrailRisk.High => 3m,
            _ => 2m
        };
}

public sealed class World
{
    private readonly IReadOnlyDictionary<TownId, Town> _towns;
    private readonly IReadOnlyList<Trail> _trails;

    public World(IEnumerable<Town> towns, IEnumerable<Trail> trails)
    {
        ArgumentNullException.ThrowIfNull(towns);
        ArgumentNullException.ThrowIfNull(trails);

        _towns = towns.ToDictionary(town => town.Id);
        _trails = trails.ToList();
    }

    public IReadOnlyCollection<Town> Towns => _towns.Values.ToList();

    public IReadOnlyList<Trail> Trails => _trails;

    public Town GetTown(TownId townId) => _towns[townId];

    public bool TryGetTown(TownId townId, out Town? town)
        => _towns.TryGetValue(townId, out town);

    public Trail? FindConnectedTrail(TownId originTownId, TownId destinationTownId)
        => _trails.FirstOrDefault(trail => trail.Connects(originTownId, destinationTownId));

    public IReadOnlyList<Trail> ListTrailsFromTown(TownId townId)
        => _trails.Where(trail => trail.Connects(townId)).ToList();

    public IReadOnlyList<TownId> GetTownIds() => _towns.Keys.ToList();
}

[Flags]
public enum TownServices
{
    None = 0,
    Supplies = 1,
    Lodging = 2,
    Doctor = 4,
    Telegraph = 8,
    NoticeBoard = 16
}

public enum TrailRisk
{
    Low = 1,
    Moderate = 2,
    High = 3
}
