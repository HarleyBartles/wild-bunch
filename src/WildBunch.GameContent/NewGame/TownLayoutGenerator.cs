using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Generates deterministic town hub surface layouts. The same seed code, town
/// identity, and <see cref="TownServices"/> always produce the same layout — no
/// unseeded randomness is used. Buildings are placed on a fixed logical grid
/// (0-100 in both dimensions) with small deterministic +/-2 jitter derived
/// from <see cref="GameSetupDeterministicSource.PickIndex"/> so each town
/// looks slightly different while remaining reproducible. The frontend scales
/// these logical units to actual canvas pixels.
/// </summary>
internal static class TownLayoutGenerator
{
    private const int SceneWidth = 100;
    private const int SceneHeight = 100;
    private const int PlayerSpawnX = 50;
    private const int PlayerSpawnY = 50;

    private const int BuildingWidth = 8;
    private const int BuildingHeight = 10;

    // Jitter range: PickIndex(label, 5) yields 0..4, subtract 2 -> -2..+2.
    private const int JitterRange = 5;
    private const int JitterOffset = 2;

    /// <summary>
    /// Generates a deterministic <see cref="TownLayout"/> for a town hub surface.
    /// Always emits the baseline navigation buildings (Store, Sheriff, Saloon,
    /// Trailhead). Emits Telegraph only when <paramref name="services"/> has the
    /// <see cref="TownServices.Telegraph"/> flag set. Uses the
    /// <paramref name="layoutPalette"/> to select the building layout pattern from
    /// <see cref="BuildingLayoutCatalog"/>.
    /// </summary>
    public static TownLayout GenerateLayout(
        TownServices services,
        TownProsperity prosperity,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        BuildingLayoutPalette layoutPalette = BuildingLayoutPalette.HubAndSpoke)
    {
        ArgumentNullException.ThrowIfNull(source);

        var layoutPattern = BuildingLayoutCatalog.GetLayout(layoutPalette);
        var buildings = new List<BuildingPlacement>();
        var paths = new List<PathSegment>();

        // Place buildings from the layout pattern
        foreach (var spec in layoutPattern.BuildingPlacements)
        {
            // Skip telegraph if not in services
            if (spec.Kind == BuildingKind.Telegraph && (services & TownServices.Telegraph) != TownServices.Telegraph)
            {
                continue;
            }

            var placement = PlaceBuildingFromSpec(spec, townId, townSlotIndex, source, saltSource);
            buildings.Add(placement);
        }

        // Ensure baseline buildings are present if not in the pattern
        EnsureBaselineBuildings(buildings, services, townId, townSlotIndex, source, saltSource);

        // TODO: Generate path segments based on layout pattern spurs (Task 8 part 2)
        // For now, use empty paths as placeholder

        return new TownLayout(buildings, PlayerSpawnX, PlayerSpawnY, prosperity, paths);
    }

    private static BuildingPlacement PlaceBuildingFromSpec(
        BuildingPlacementSpec spec,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        var saltSegment = saltSource is null ? string.Empty : $"|{saltSource.Salt}";
        var kindName = spec.Kind.ToString().ToLowerInvariant();
        var xLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kindName}-x{saltSegment}";
        var yLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kindName}-y{saltSegment}";

        var x = ClampToScene(spec.X + Jitter(source, xLabel), SceneWidth);
        var y = ClampToScene(spec.Y + Jitter(source, yLabel), SceneHeight);

        return new BuildingPlacement(spec.Kind, x, y, spec.View, BuildingWidth, BuildingHeight);
    }

    private static void EnsureBaselineBuildings(
        List<BuildingPlacement> buildings,
        TownServices services,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        var existingKinds = buildings.Select(b => b.Kind).ToHashSet();
        var baselineKinds = new[] { BuildingKind.Store, BuildingKind.Sheriff, BuildingKind.Saloon, BuildingKind.Trailhead };

        foreach (var kind in baselineKinds)
        {
            if (!existingKinds.Contains(kind))
            {
                var (baseX, baseY) = GetBaselinePosition(kind);
                buildings.Add(PlaceBuilding(kind, baseX, baseY, townId, townSlotIndex, source, saltSource));
            }
        }

        // Ensure telegraph if in services
        if ((services & TownServices.Telegraph) == TownServices.Telegraph && !existingKinds.Contains(BuildingKind.Telegraph))
        {
            buildings.Add(PlaceBuilding(BuildingKind.Telegraph, baseX: 46, baseY: 70, townId, townSlotIndex, source, saltSource));
        }
    }

    private static (int X, int Y) GetBaselinePosition(BuildingKind kind)
    {
        return kind switch
        {
            BuildingKind.Store => (12, 15),
            BuildingKind.Sheriff => (46, 15),
            BuildingKind.Saloon => (80, 15),
            BuildingKind.Trailhead => (90, 50),
            BuildingKind.Telegraph => (46, 70),
            _ => (50, 50)
        };
    }

    private static BuildingPlacement PlaceBuilding(
        BuildingKind kind,
        int baseX,
        int baseY,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        BuildingView view = BuildingView.FrontOblique)
    {
        var saltSegment = saltSource is null ? string.Empty : $"|{saltSource.Salt}";
        var kindName = kind.ToString().ToLowerInvariant();
        var xLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kindName}-x{saltSegment}";
        var yLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kindName}-y{saltSegment}";

        var x = ClampToScene(baseX + Jitter(source, xLabel), SceneWidth);
        var y = ClampToScene(baseY + Jitter(source, yLabel), SceneHeight);

        return new BuildingPlacement(kind, x, y, view, BuildingWidth, BuildingHeight);
    }

    private static int Jitter(GameSetupDeterministicSource source, string label)
        => source.PickIndex(label, JitterRange) - JitterOffset;

    private static int ClampToScene(int value, int max)
    {
        if (value < 0) return 0;
        if (value > max) return max;
        return value;
    }
}
