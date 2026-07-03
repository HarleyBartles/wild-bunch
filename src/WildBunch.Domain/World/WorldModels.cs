namespace WildBunch.Domain.World;

public readonly record struct TownId(string Value);

public readonly record struct TrailId(string Value);

/// <summary>
/// Prosperity tier for a town. Drives stock profile (available items,
/// price multiplier, store sections). The seed encodes a global default;
/// difficulty can later adjust per-town prosperity.
/// </summary>
public enum TownProsperity
{
    Boomtown = 0,
    Prosperous = 1,
    Poor = 2,
    Destitute = 3
}

public sealed record Town(
    TownId Id,
    string Name,
    TownServices Services,
    TownProsperity Prosperity = TownProsperity.Prosperous,
    TownSourceCatalog? SourceCatalog = null,
    int MapX = 0,
    int MapY = 0,
    bool IsOutlier = false)
{
    public TownSourceCatalog Sources => SourceCatalog ?? TownSourceCatalog.Default;
}

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
    Telegraph = 1
}

/// <summary>
/// Catalog-defined prosperity palettes. The seed encodes a 3-bit palette
/// index; the palette maps each town position to a <see cref="TownProsperity"/>.
/// Positional patterns apply by index into the selected town list, with a
/// fallback to <see cref="TownProsperity.Prosperous"/> if a palette slot is
/// beyond the selected town count. This saves 17 bits vs per-town encoding.
/// </summary>
public enum ProsperityPalette
{
    UniformProsperous = 0,
    BoomtownHub = 1,
    FrontierMix = 2,
    RichCenter = 3,
    Dustbowl = 4,
    GoldRush = 5,
    Struggling = 6,
    MixedBag = 7
}

/// <summary>
/// Catalog-defined services palettes. The seed encodes a 3-bit palette
/// index; the palette maps each town position to a <see cref="TownServices"/>
/// flags value. Adding new service flags means defining new palette entries
/// that use them — zero additional bit cost. Only if more than 8 service
/// patterns are needed would this expand to 4 bits.
/// </summary>
public enum ServicesPalette
{
    NoTelegraph = 0,
    HubTelegraph = 1,
    TwinTelegraph = 2,
    RegionalTelegraph = 3,
    FrontierTelegraph = 4,
    TelegraphWeb = 5,
    SparseTelegraph = 6,
    AllTelegraph = 7
}

public enum TrailRisk
{
    Low = 1,
    Moderate = 2,
    High = 3
}
