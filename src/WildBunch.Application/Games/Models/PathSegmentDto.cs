namespace WildBunch.Application.Games.Models;

/// <summary>
/// DTO representation of a path segment connecting a building to a road segment.
/// Coordinates are in logical units (0-100) matching building placement.
/// </summary>
public sealed record PathSegmentDto(int StartX, int StartY, int EndX, int EndY);
