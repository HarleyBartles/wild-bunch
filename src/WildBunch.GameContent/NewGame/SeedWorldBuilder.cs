using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class SeedWorldBuilder
{
    /// <summary>
    /// Builds the canonical world (8 towns, Canonical variant).
    /// Used by SeedWorldMapLayout for the start-screen map.
    /// </summary>
    public static World CreateCanonicalWorld()
        => SeedWorldCatalog.CreateCanonicalWorld();

    /// <summary>
    /// Builds a World from a SeedWorld template. The seed world holds the
    /// encoded fields (town count, palettes, variant) and derived fields
    /// (town names, services, trails). The catalog provides the name pool
    /// and slot-based topology.
    /// Future seam: DifficultyEnvelope may modify terrain/distance downstream.
    /// </summary>
    public static World CreateWorld(SeedWorld seedWorld, GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        var townNames = SeedWorldCatalog.DeriveTownNames(
            seedWorld.WorldVariant,
            seedWorld.TownCount,
            seedWorld.AccusationIndex,
            seedWorld.DefaultCulpritIndex,
            seedWorld.CashBonus,
            seedWorld.ProsperityPalette,
            seedWorld.ServicesPalette,
            seedWorld.MapLayoutPalette);

        var trails = SeedWorldCatalog.BuildTrails(seedWorld.WorldVariant, townNames, seedWorld.MapLayoutPalette);

        // Derive town coordinates from map layout geometry
        var townCoordinates = DeriveTownCoordinates(townNames.Count, seedWorld.MapLayoutPalette);

        // Derive canonical distances from geometry
        var trailsWithGeometryDistances = DeriveDistancesFromGeometry(trails, townCoordinates);

        return SeedWorldCatalog.CreateWorld(
            seedWorld.WorldVariant,
            townNames,
            seedWorld.ServicesPalette,
            seedWorld.ProsperityPalette,
            trailsWithGeometryDistances);
    }

    /// <summary>
    /// Derives map coordinates for each town slot based on the map layout palette.
    /// Returns a dictionary mapping slot index to (X, Y) coordinates.
    /// </summary>
    private static Dictionary<int, (int X, int Y)> DeriveTownCoordinates(int townCount, MapLayoutPalette layout)
    {
        var coordinates = new Dictionary<int, (int, int)>();
        for (var i = 0; i < townCount; i++)
        {
            coordinates[i] = SeedWorldMapLayout.GetCoordinatesForSlot(i, townCount, layout);
        }
        return coordinates;
    }

    /// <summary>
    /// Derives canonical ride-day distances from the Euclidean geometry of town coordinates.
    /// Distance is calculated as the Euclidean distance between towns, scaled to ride-day units
    /// (approximately 1 ride-day per 50 coordinate units), rounded to 1 decimal place.
    /// </summary>
    private static IReadOnlyList<SeedWorldTrail> DeriveDistancesFromGeometry(
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)> townCoordinates)
    {
        const double CoordinateScale = 50.0; // 1 ride-day per 50 coordinate units

        return trails.Select(trail =>
        {
            // Extract slot indices from trail ID (format: "trail-{fromSlot}-{toSlot}")
            var parts = trail.Id.Split('-');
            var fromSlot = int.Parse(parts[1]);
            var toSlot = int.Parse(parts[2]);

            var fromCoords = townCoordinates[fromSlot];
            var toCoords = townCoordinates[toSlot];

            // Calculate Euclidean distance
            var dx = toCoords.X - fromCoords.X;
            var dy = toCoords.Y - fromCoords.Y;
            var coordinateDistance = Math.Sqrt(dx * dx + dy * dy);

            // Scale to ride-day distance and round to 1 decimal place
            var rideDayDistance = Math.Round(coordinateDistance / CoordinateScale, 1);

            return trail with { RideDayDistance = (decimal)rideDayDistance };
        }).ToArray();
    }

    /// <summary>
    /// Checks whether the seed world is the canonical shape (8 towns,
    /// Canonical variant, HubTelegraph services, UniformProsperous prosperity,
    /// specific case fields).
    /// </summary>
    internal static bool IsCanonicalSeedWorld(SeedWorld seedWorld)
        => seedWorld.WorldVariant == SeedWorldVariant.Canonical
            && seedWorld.TownCount == 8
            && seedWorld.ServicesPalette == ServicesPalette.HubTelegraph
            && seedWorld.ProsperityPalette == ProsperityPalette.UniformProsperous
            && seedWorld.MapLayoutPalette == MapLayoutPalette.HubAndSpoke
            && seedWorld.AccusationIndex == 1
            && seedWorld.DefaultCulpritIndex == 3
            && seedWorld.CashBonus == 0;
}
