namespace WildBunch.Domain.World;

/// <summary>
/// Immutable layout of a town hub surface: the set of placed buildings and
/// the player spawn position (surface-space pixels). Produced by town layout
/// generation and consumed by the frontend Phaser surface for rendering and
/// click-to-navigate routing.
/// </summary>
public sealed record TownLayout(
    IReadOnlyList<BuildingPlacement> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY);
