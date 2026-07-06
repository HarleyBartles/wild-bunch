using WildBunch.Domain.World;
using BuildingKind = WildBunch.Domain.World.BuildingKind;
using BuildingView = WildBunch.Domain.World.BuildingView;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Catalog of palette specifications for tile-based town hub layouts.
/// Each palette encodes spur configuration and placement strategy.
/// Used by TownLayoutGenerator to generate tile-based layouts.
/// </summary>
public static class BuildingLayoutCatalog
{
    // TODO: This method is temporary - will be removed in Task 7 when TownLayoutGenerator is rewritten
    // to use the new PaletteSpec-based tile grid system
    public static BuildingLayoutPattern GetLayout(BuildingLayoutPalette palette)
    {
        // Return a stub layout for all palettes until Task 7
        return FallbackLayout;
    }

    public static PaletteSpec GetPaletteSpec(BuildingLayoutPalette palette)
    {
        return palette switch
        {
            // 0 spurs
            BuildingLayoutPalette.NoSpurs_SpreadEvenly => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.NoSpurs_ClusterMiddle => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.ClusterMiddle),
            BuildingLayoutPalette.NoSpurs_FavorLeft => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.FavorLeft),
            BuildingLayoutPalette.NoSpurs_FavorRight => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.FavorRight),

            // 1 spur (at middle row)
            BuildingLayoutPalette.OneSpurLeft_SpreadEvenly => new PaletteSpec(1, new[] { 4 }, new[] { SpurDirection.West }, PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.OneSpurLeft_ClusterMiddle => new PaletteSpec(1, new[] { 4 }, new[] { SpurDirection.West }, PlacementStrategy.ClusterMiddle),
            BuildingLayoutPalette.OneSpurRight_SpreadEvenly => new PaletteSpec(1, new[] { 4 }, new[] { SpurDirection.East }, PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.OneSpurRight_ClusterMiddle => new PaletteSpec(1, new[] { 4 }, new[] { SpurDirection.East }, PlacementStrategy.ClusterMiddle),

            // 2 spurs (at upper and lower middle rows)
            BuildingLayoutPalette.TwoSpursLeftRight_SpreadEvenly => new PaletteSpec(2, new[] { 3, 6 }, new[] { SpurDirection.West, SpurDirection.East }, PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.TwoSpursLeftRight_ClusterMiddle => new PaletteSpec(2, new[] { 3, 6 }, new[] { SpurDirection.West, SpurDirection.East }, PlacementStrategy.ClusterMiddle),
            BuildingLayoutPalette.TwoSpursRightLeft_SpreadEvenly => new PaletteSpec(2, new[] { 3, 6 }, new[] { SpurDirection.East, SpurDirection.West }, PlacementStrategy.SpreadEvenly),
            BuildingLayoutPalette.TwoSpursRightLeft_ClusterMiddle => new PaletteSpec(2, new[] { 3, 6 }, new[] { SpurDirection.East, SpurDirection.West }, PlacementStrategy.ClusterMiddle),

            // Reserved values default to no spurs
            _ => new PaletteSpec(0, Array.Empty<int>(), Array.Empty<SpurDirection>(), PlacementStrategy.SpreadEvenly)
        };
    }

    // TODO: This fallback layout is temporary - will be removed in Task 7
    private static readonly BuildingLayoutPattern FallbackLayout = new(
        BuildingPlacements: new[]
        {
            new BuildingPlacementSpec(BuildingKind.Store, 35, 20, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Sheriff, 65, 20, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Saloon, 35, 40, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Telegraph, 75, 60, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Trailhead, 50, 85, BuildingView.Rear)
        },
        SpurCount: 1,
        SpurPositions: new[] { 60 },
        SpurDirections: new[] { SpurDirection.East });
}

/// <summary>
/// Direction of a side spur branching from the main road.
/// </summary>
public enum SpurDirection
{
    East,
    West
}

// TODO: These types are temporary - will be removed in Task 7 when TownLayoutGenerator is rewritten
// to use the new PaletteSpec-based tile grid system
public sealed record BuildingLayoutPattern(
    BuildingPlacementSpec[] BuildingPlacements,
    int SpurCount,
    int[] SpurPositions,
    SpurDirection[] SpurDirections);

public sealed record BuildingPlacementSpec(
    BuildingKind Kind,
    int X,
    int Y,
    BuildingView View);
