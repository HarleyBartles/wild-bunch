namespace WildBunch.Domain.World;

/// <summary>
/// A single building placed on a town hub surface. Coordinates (X, Y) and
/// dimensions (Width, Height) are in logical units (0-100) relative to the
/// town hub surface. The frontend scales these to actual canvas pixels.
/// Width/Height default to the standard building footprint (8x10 logical
/// units) and may be overridden for non-standard buildings.
/// </summary>
public sealed record BuildingPlacement(
    BuildingKind Kind,
    int X,
    int Y,
    int Width = 8,
    int Height = 10);
