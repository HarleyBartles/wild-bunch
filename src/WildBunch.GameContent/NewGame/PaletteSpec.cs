namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Palette specification for tile-based town hub layout. Encodes spur configuration
/// and placement strategy for a BuildingLayoutPalette value.
/// </summary>
public sealed record PaletteSpec(
    int SpurCount,
    int[] SpurRows,
    SpurDirection[] SpurDirections,
    PlacementStrategy PlacementStrategy);

/// <summary>
/// Placement strategy for distributing buildings across available tile positions.
/// </summary>
public enum PlacementStrategy
{
    SpreadEvenly,
    ClusterMiddle,
    FavorLeft,
    FavorRight
}
