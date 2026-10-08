namespace WildBunch.Domain.World;

/// <summary>
/// Kind of building placed on a town hub surface. Core service buildings
/// (Store, Sheriff, Saloon, Telegraph) and navigation Trailheads are present
/// in every town. The available-action contract determines which are usable.
/// Values are explicitly numbered to leave room for future building types.
/// </summary>
public enum BuildingKind
{
    Store = 0,
    Sheriff = 1,
    Saloon = 2,
    Trailhead = 3,
    Telegraph = 4,
}
