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
        // TODO: Task 2 will implement tile-based layout generation
        // For now, map all palettes to a single fallback layout to allow build to pass
        return HubAndSpokeLayout;
    }

    // HubAndSpoke: Buildings arranged around a central hub with one main road and one side spur.
    // Store and Sheriff on left side, Saloon and Telegraph on right side, Trailhead at bottom center.
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

    // LinearChain: Buildings arranged in a linear chain from left to right.
    // Store, Saloon, Telegraph on left side, Sheriff on right side, Trailhead at bottom center.
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

    // DoubleLine: Two parallel lines of buildings. Store/Saloon on left line, Sheriff/Telegraph on right line.
    // Two side spurs connect to the main road at positions 40 and 60.
    private static readonly BuildingLayoutPattern DoubleLineLayout = new(
        BuildingPlacements: new[]
        {
            new BuildingPlacementSpec(BuildingKind.Store, 25, 20, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Saloon, 25, 40, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Sheriff, 75, 20, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Telegraph, 75, 40, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Trailhead, 50, 85, BuildingView.Rear)
        },
        SpurCount: 2,
        SpurPositions: new[] { 40, 60 },
        SpurDirections: new[] { SpurDirection.East, SpurDirection.West });

    // Tree: Store at top center as the "trunk", Saloon and Sheriff as branches left and right.
    // Telegraph at center, Trailhead at bottom. Two side spurs at positions 35 and 65.
    private static readonly BuildingLayoutPattern TreeLayout = new(
        BuildingPlacements: new[]
        {
            new BuildingPlacementSpec(BuildingKind.Store, 50, 15, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Saloon, 30, 35, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Sheriff, 70, 35, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Telegraph, 50, 55, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Trailhead, 50, 85, BuildingView.Rear)
        },
        SpurCount: 2,
        SpurPositions: new[] { 35, 65 },
        SpurDirections: new[] { SpurDirection.West, SpurDirection.East });

    // Star: Store at center, Saloon and Sheriff on left and right, Telegraph at bottom.
    // Four side spurs at positions 25, 45, 55, 75 creating a star pattern.
    private static readonly BuildingLayoutPattern StarLayout = new(
        BuildingPlacements: new[]
        {
            new BuildingPlacementSpec(BuildingKind.Store, 50, 20, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Saloon, 20, 50, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Sheriff, 80, 50, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Telegraph, 50, 80, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Trailhead, 50, 85, BuildingView.Rear)
        },
        SpurCount: 4,
        SpurPositions: new[] { 25, 45, 55, 75 },
        SpurDirections: new[] { SpurDirection.West, SpurDirection.East, SpurDirection.West, SpurDirection.East });

    // XShaped: Buildings arranged in an X pattern. Store and Sheriff on top-left and bottom-left,
    // Saloon and Telegraph on top-right and bottom-right. Four side spurs at positions 35 and 65.
    private static readonly BuildingLayoutPattern XShapedLayout = new(
        BuildingPlacements: new[]
        {
            new BuildingPlacementSpec(BuildingKind.Store, 30, 30, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Saloon, 70, 30, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Sheriff, 30, 70, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Telegraph, 70, 70, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Trailhead, 50, 85, BuildingView.Rear)
        },
        SpurCount: 4,
        SpurPositions: new[] { 35, 65, 35, 65 },
        SpurDirections: new[] { SpurDirection.West, SpurDirection.East, SpurDirection.West, SpurDirection.East });

    // Cluster: Buildings clustered in the center. Store and Saloon on top row, Sheriff and Telegraph
    // on bottom row. Two side spurs at positions 45 and 55 close to the cluster.
    private static readonly BuildingLayoutPattern ClusterLayout = new(
        BuildingPlacements: new[]
        {
            new BuildingPlacementSpec(BuildingKind.Store, 40, 25, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Saloon, 60, 25, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Sheriff, 40, 45, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Telegraph, 60, 45, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Trailhead, 50, 85, BuildingView.Rear)
        },
        SpurCount: 2,
        SpurPositions: new[] { 45, 55 },
        SpurDirections: new[] { SpurDirection.West, SpurDirection.East });

    // GridLayout: Buildings arranged in a 2x2 grid. Store and Saloon on top row, Sheriff and Telegraph
    // on bottom row. Two side spurs at positions 40 and 60.
    private static readonly BuildingLayoutPattern GridLayout = new(
        BuildingPlacements: new[]
        {
            new BuildingPlacementSpec(BuildingKind.Store, 30, 25, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Saloon, 70, 25, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Sheriff, 30, 55, BuildingView.Profile),
            new BuildingPlacementSpec(BuildingKind.Telegraph, 70, 55, BuildingView.FrontOblique),
            new BuildingPlacementSpec(BuildingKind.Trailhead, 50, 85, BuildingView.Rear)
        },
        SpurCount: 2,
        SpurPositions: new[] { 40, 60 },
        SpurDirections: new[] { SpurDirection.West, SpurDirection.East });
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
