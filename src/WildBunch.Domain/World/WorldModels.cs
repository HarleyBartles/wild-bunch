namespace WildBunch.Domain.World;

public readonly record struct TownId(string Value);

public readonly record struct TrailId(string Value);

public sealed record Town(TownId Id, string Name, TownServices Services);

public sealed record Trail(
    TrailId Id,
    TownId FromTownId,
    TownId ToTownId,
    int SupplyCost,
    TrailRisk Risk)
{
    public bool Connects(TownId townId) => FromTownId.Equals(townId) || ToTownId.Equals(townId);

    public bool Connects(TownId originTownId, TownId destinationTownId)
        => (FromTownId.Equals(originTownId) && ToTownId.Equals(destinationTownId))
            || (FromTownId.Equals(destinationTownId) && ToTownId.Equals(originTownId));
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
    Telegraph = 8
}

public enum TrailRisk
{
    Low = 1,
    Moderate = 2,
    High = 3
}
