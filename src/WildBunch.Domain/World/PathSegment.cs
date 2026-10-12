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

    private int _startX = ValidateCoordinate(StartX, nameof(StartX));
    private int _startY = ValidateCoordinate(StartY, nameof(StartY));
    private int _endX = ValidateCoordinate(EndX, nameof(EndX));
    private int _endY = ValidateCoordinate(EndY, nameof(EndY));

    public int StartX
    {
        get => _startX;
        init => _startX = ValidateCoordinate(value, nameof(StartX));
    }

    public int StartY
    {
        get => _startY;
        init => _startY = ValidateCoordinate(value, nameof(StartY));
    }

    public int EndX
    {
        get => _endX;
        init => _endX = ValidateCoordinate(value, nameof(EndX));
    }

    public int EndY
    {
        get => _endY;
        init => _endY = ValidateCoordinate(value, nameof(EndY));
    }

    public static PathSegment Create(int startX, int startY, int endX, int endY)
    {
        return new PathSegment(startX, startY, endX, endY);
    }

    private static int ValidateCoordinate(int value, string parameterName)
    {
        if (value is < MinCoordinate or > MaxCoordinate)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Coordinates must be between {MinCoordinate} and {MaxCoordinate}");
        }

        return value;
    }
}
