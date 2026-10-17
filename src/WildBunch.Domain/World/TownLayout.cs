namespace WildBunch.Domain.World;

/// <summary>
/// Immutable layout of a town hub surface: the set of placed buildings,
/// the player spawn position, the town prosperity tier, path segments
/// connecting buildings to roads, and the tile grid for visualization.
/// All coordinates are in logical units (0-100) relative to the town hub surface.
/// The frontend scales these to actual canvas pixels.
/// Prosperity drives which asset tier (boomtown/prosperous/poor/destitute) to use
/// for sprite selection. Produced by town layout generation and consumed by the
/// frontend Phaser surface for rendering and click-to-navigate routing.
/// The resolver version identifies the algorithm version used to generate the layout,
/// supporting migration when the resolver algorithm changes.
/// Layout salts are the salts used during layout generation, persisted for reproducibility.
/// </summary>
public sealed record TownLayout(
    IReadOnlyList<BuildingPlacement> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY,
    TownProsperity Prosperity,
    IReadOnlyList<PathSegment> Paths,
    int[][]? TileGrid = null,
    string ResolverVersion = TownLayout.CurrentResolverVersion,
    LayoutSalts? LayoutSalts = null)
{
    private const int MinCoordinate = 0;
    private const int MaxCoordinate = 100;

    private int _playerSpawnX = ValidateCoordinate(PlayerSpawnX, nameof(PlayerSpawnX));
    private int _playerSpawnY = ValidateCoordinate(PlayerSpawnY, nameof(PlayerSpawnY));

    public int PlayerSpawnX
    {
        get => _playerSpawnX;
        init => _playerSpawnX = ValidateCoordinate(value, nameof(PlayerSpawnX));
    }

    public int PlayerSpawnY
    {
        get => _playerSpawnY;
        init => _playerSpawnY = ValidateCoordinate(value, nameof(PlayerSpawnY));
    }

    /// <summary>
    /// Identifies the current deterministic town-layout algorithm. Generated
    /// layouts persist this value so rendering and replay retain its identity.
    /// </summary>
    public const string CurrentResolverVersion = "1.0.0";

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
}
