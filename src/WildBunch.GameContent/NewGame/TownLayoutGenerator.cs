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
    /// <see cref="TownServices.Telegraph"/> flag set.
    /// </summary>
    public static TownLayout GenerateLayout(
        TownServices services,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        ArgumentNullException.ThrowIfNull(source);

        var buildings = new List<BuildingPlacement>
        {
            PlaceBuilding(BuildingKind.Store, baseX: 12, baseY: 15, townId, townSlotIndex, source, saltSource),
            PlaceBuilding(BuildingKind.Sheriff, baseX: 46, baseY: 15, townId, townSlotIndex, source, saltSource),
            PlaceBuilding(BuildingKind.Saloon, baseX: 80, baseY: 15, townId, townSlotIndex, source, saltSource),
            PlaceBuilding(BuildingKind.Trailhead, baseX: 90, baseY: 50, townId, townSlotIndex, source, saltSource),
        };

        if ((services & TownServices.Telegraph) == TownServices.Telegraph)
        {
            buildings.Add(PlaceBuilding(BuildingKind.Telegraph, baseX: 46, baseY: 70, townId, townSlotIndex, source, saltSource));
        }

        return new TownLayout(buildings, PlayerSpawnX, PlayerSpawnY);
    }

    private static BuildingPlacement PlaceBuilding(
        BuildingKind kind,
        int baseX,
        int baseY,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        var saltSegment = saltSource is null ? string.Empty : $"|{saltSource.Salt}";
        var kindName = kind.ToString().ToLowerInvariant();
        var xLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kindName}-x{saltSegment}";
        var yLabel = $"town-{townId.Value}-slot-{townSlotIndex}-building-{kindName}-y{saltSegment}";

        var x = ClampToScene(baseX + Jitter(source, xLabel), SceneWidth);
        var y = ClampToScene(baseY + Jitter(source, yLabel), SceneHeight);

        return new BuildingPlacement(kind, x, y, BuildingWidth, BuildingHeight);
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
