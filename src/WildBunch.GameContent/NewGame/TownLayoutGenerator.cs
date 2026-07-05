using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Generates deterministic town hub surface layouts. The same seed code, town
/// identity, and <see cref="TownServices"/> always produce the same layout — no
/// unseeded randomness is used. Buildings are placed on a fixed grid (scene
/// 800x500) with small deterministic +/-20px jitter derived from
/// <see cref="GameSetupDeterministicSource.PickIndex"/> so each town looks
/// slightly different while remaining reproducible.
/// </summary>
internal static class TownLayoutGenerator
{
    private const int SceneWidth = 800;
    private const int SceneHeight = 500;
    private const int PlayerSpawnX = 400;
    private const int PlayerSpawnY = 250;

    private const int BuildingWidth = 60;
    private const int BuildingHeight = 50;

    // Jitter range: PickIndex(label, 41) yields 0..40, subtract 20 -> -20..+20.
    private const int JitterRange = 41;
    private const int JitterOffset = 20;

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
        int totalTownCount,
        GameSetupDeterministicSource source,
        SaltSource? saltSource)
    {
        ArgumentNullException.ThrowIfNull(source);

        var buildings = new List<BuildingPlacement>
        {
            PlaceBuilding(BuildingKind.Store, baseX: 100, baseY: 100, townId, townSlotIndex, source, saltSource),
            PlaceBuilding(BuildingKind.Sheriff, baseX: 370, baseY: 100, townId, townSlotIndex, source, saltSource),
            PlaceBuilding(BuildingKind.Saloon, baseX: 640, baseY: 100, townId, townSlotIndex, source, saltSource),
            PlaceBuilding(BuildingKind.Trailhead, baseX: 720, baseY: 250, townId, townSlotIndex, source, saltSource),
        };

        if ((services & TownServices.Telegraph) == TownServices.Telegraph)
        {
            buildings.Add(PlaceBuilding(BuildingKind.Telegraph, baseX: 370, baseY: 350, townId, townSlotIndex, source, saltSource));
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
