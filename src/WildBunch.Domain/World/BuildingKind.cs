namespace WildBunch.Domain.World;

/// <summary>
/// Kind of building placed on a town hub surface. Baseline members
/// (Store, Sheriff, Saloon, Trailhead) drive click-to-navigate routing for
/// every town; Telegraph is service-driven and only present when the town
/// has the <see cref="TownServices.Telegraph"/> service. The domain is the
/// source of truth — the frontend consumes this for routing, not the reverse.
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
