namespace WildBunch.Domain.World;

/// <summary>
/// A single building placed on a town hub surface. Coordinates (X, Y) are
/// surface-space pixels relative to the town hub canvas. Width/Height default
/// to the standard building footprint (60x50) and may be overridden for
/// non-standard buildings.
/// </summary>
public sealed record BuildingPlacement(
    BuildingKind Kind,
    int X,
    int Y,
    int Width = 60,
    int Height = 50);
