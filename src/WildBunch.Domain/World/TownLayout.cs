namespace WildBunch.Domain.World;

/// <summary>
/// Immutable layout of a town hub surface: the set of placed buildings and
/// the player spawn position. All coordinates are in logical units (0-100)
/// relative to the town hub surface. The frontend scales these to actual
/// canvas pixels. Produced by town layout generation and consumed by the
/// frontend Phaser surface for rendering and click-to-navigate routing.
/// </summary>
public sealed record TownLayout(
    IReadOnlyList<BuildingPlacement> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY);
