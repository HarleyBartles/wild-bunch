namespace WildBunch.Application.Games.Models;

/// <summary>
/// DTO for the immutable layout of a town hub surface: the set of placed
/// buildings and the player spawn position (surface-space pixels). Mirrors the
/// domain <see cref="WildBunch.Domain.World.TownLayout"/>. Consumed by the
/// frontend Phaser surface for rendering and click-to-navigate routing.
/// </summary>
public sealed record TownLayoutDto(
    IReadOnlyList<BuildingPlacementDto> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY);
