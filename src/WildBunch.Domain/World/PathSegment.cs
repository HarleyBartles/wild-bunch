namespace WildBunch.Domain.World;

/// <summary>
/// A line segment connecting a building to a road segment in a town hub surface.
/// Coordinates are in logical units (0-100) matching building placement.
/// Used for path connectivity visualization (line drawing for now, tiles in future work).
/// </summary>
public sealed record PathSegment(int StartX, int StartY, int EndX, int EndY)
{
    private const int MinCoordinate = 0;
    private const int MaxCoordinate = 100;

    public static PathSegment Create(int startX, int startY, int endX, int endY)
    {
        if (startX < MinCoordinate || startX > MaxCoordinate)
            throw new ArgumentOutOfRangeException(nameof(startX), $"StartX must be between {MinCoordinate} and {MaxCoordinate}");
        if (startY < MinCoordinate || startY > MaxCoordinate)
            throw new ArgumentOutOfRangeException(nameof(startY), $"StartY must be between {MinCoordinate} and {MaxCoordinate}");
        if (endX < MinCoordinate || endX > MaxCoordinate)
            throw new ArgumentOutOfRangeException(nameof(endX), $"EndX must be between {MinCoordinate} and {MaxCoordinate}");
        if (endY < MinCoordinate || endY > MaxCoordinate)
            throw new ArgumentOutOfRangeException(nameof(endY), $"EndY must be between {MinCoordinate} and {MaxCoordinate}");
        
        return new PathSegment(startX, startY, endX, endY);
    }
}
