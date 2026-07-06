using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Catalog of canonical building layout patterns for town hub surfaces.
/// Each layout pattern defines building positions, views, spur count, and
/// spur positions. Used by TownLayoutGenerator to select the layout based
/// on the BuildingLayoutPalette from the seed.
/// </summary>
public static class BuildingLayoutCatalog
{
    public static BuildingLayoutPattern GetLayout(BuildingLayoutPalette palette)
    {
        return palette switch
        {
            BuildingLayoutPalette.HubAndSpoke => HubAndSpokeLayout,
            BuildingLayoutPalette.LinearChain => LinearChainLayout,
            BuildingLayoutPalette.DoubleLine => DoubleLineLayout,
            BuildingLayoutPalette.Tree => TreeLayout,
            BuildingLayoutPalette.Star => StarLayout,
            BuildingLayoutPalette.XShaped => XShapedLayout,
            BuildingLayoutPalette.Cluster => ClusterLayout,
            BuildingLayoutPalette.Grid => GridLayout,
            _ => HubAndSpokeLayout
        };
    }

    private static readonly BuildingLayoutPattern HubAndSpokeLayout = new(
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

    private static readonly BuildingLayoutPattern LinearChainLayout = new(
        BuildingPlacements: new[]
        {
            new BuildingPlacementSpec(BuildingKind.Store, 35, 15, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Sheriff, 65, 15, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Saloon, 35, 35, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Telegraph, 65, 55, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Trailhead, 50, 85, BuildingView.Rear)
        },
        SpurCount: 1,
        SpurPositions: new[] { 70 },
        SpurDirections: new[] { SpurDirection.West });

    // Placeholder layouts for other palettes - will be filled with actual patterns
    private static readonly BuildingLayoutPattern DoubleLineLayout = HubAndSpokeLayout;
    private static readonly BuildingLayoutPattern TreeLayout = HubAndSpokeLayout;
    private static readonly BuildingLayoutPattern StarLayout = HubAndSpokeLayout;
    private static readonly BuildingLayoutPattern XShapedLayout = HubAndSpokeLayout;
    private static readonly BuildingLayoutPattern ClusterLayout = HubAndSpokeLayout;
    private static readonly BuildingLayoutPattern GridLayout = HubAndSpokeLayout;
}

/// <summary>
/// Canonical building layout pattern specification.
/// </summary>
public sealed record BuildingLayoutPattern(
    BuildingPlacementSpec[] BuildingPlacements,
    int SpurCount,
    int[] SpurPositions,
    SpurDirection[] SpurDirections);

/// <summary>
/// Building placement specification within a layout pattern.
/// </summary>
public sealed record BuildingPlacementSpec(
    BuildingKind Kind,
    int X,
    int Y,
    BuildingView View);

/// <summary>
/// Direction of a side spur branching from the main road.
/// </summary>
public enum SpurDirection
{
    East,
    West
}
