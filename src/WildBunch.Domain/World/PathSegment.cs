namespace WildBunch.Domain.World;

/// <summary>
/// A line segment connecting a building to a road segment in a town hub surface.
/// Coordinates are in logical units (0-100) matching building placement.
/// Used for path connectivity visualization (line drawing for now, tiles in future work).
/// </summary>
public sealed record PathSegment(int StartX, int StartY, int EndX, int EndY);
