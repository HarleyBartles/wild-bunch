using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

/// <summary>
/// DTO for the immutable layout of a town hub surface: the set of placed
/// buildings, the player spawn position, the town prosperity tier, and path segments
/// connecting buildings to roads. Coordinates are in logical units (0-100).
/// Mirrors the domain <see cref="WildBunch.Domain.World.TownLayout"/>. Consumed by the frontend
/// Phaser surface for rendering and click-to-navigate routing.
/// </summary>
public sealed record TownLayoutDto(
    IReadOnlyList<BuildingPlacementDto> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY,
    TownProsperity Prosperity,
    IReadOnlyList<PathSegmentDto> Paths);
