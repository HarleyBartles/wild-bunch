namespace WildBunch.Domain.World;

/// <summary>
/// A single building placed on a town hub surface. Coordinates (X, Y) and
/// dimensions (Width, Height) are in logical units relative to the town hub
/// surface. X and Y are the building center and must be within 0-100;
/// dimensions must be positive. The frontend scales these to canvas pixels.
/// Width/Height default to the standard building footprint (8x10 logical
/// units) and may be overridden for non-standard buildings. View determines
/// which sprite asset to load for rendering.
/// </summary>
public sealed record BuildingPlacement(
    BuildingKind Kind,
    int X,
    int Y,
    BuildingView View = BuildingView.FrontOblique,
    int Width = 8,
    int Height = 10)
{
    private const int MinCoordinate = 0;
    private const int MaxCoordinate = 100;

    private int _x = ValidateCoordinate(X, nameof(X));
    private int _y = ValidateCoordinate(Y, nameof(Y));
    private int _width = ValidateDimension(Width, nameof(Width));
    private int _height = ValidateDimension(Height, nameof(Height));

    public int X
    {
        get => _x;
        init => _x = ValidateCoordinate(value, nameof(X));
    }

    public int Y
    {
        get => _y;
        init => _y = ValidateCoordinate(value, nameof(Y));
    }

    public int Width
    {
        get => _width;
        init => _width = ValidateDimension(value, nameof(Width));
    }

    public int Height
    {
        get => _height;
        init => _height = ValidateDimension(value, nameof(Height));
    }

    private static int ValidateCoordinate(int value, string parameterName)
    {
        if (value is < MinCoordinate or > MaxCoordinate)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"Coordinates must be between {MinCoordinate} and {MaxCoordinate}");
        }

        return value;
    }

    private static int ValidateDimension(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Building dimensions must be positive");
        }

        return value;
    }
}
