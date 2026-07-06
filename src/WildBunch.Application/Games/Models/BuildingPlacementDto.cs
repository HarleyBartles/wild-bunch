using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

/// <summary>
/// DTO for a single building placed on a town hub surface. Mirrors the domain
/// <see cref="BuildingPlacement"/>. Coordinates are in logical units (0-100).
/// The <see cref="Kind"/> enum is carried in full so the frontend can route
/// clicks to the correct place navigation. View determines which sprite asset to load.
/// </summary>
public sealed record BuildingPlacementDto(
    BuildingKind Kind,
    int X,
    int Y,
    BuildingView View,
    int Width,
    int Height);
