using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class MapGenerator
{
    private const double OutlierPlacementDistance = 150.0;
    /// <summary>
    /// Minimum distance the outlier must maintain from all base towns (not just
    /// its connection target). Without this, the outlier can be placed 150px from
    /// its connection target but end up within 50px of a different base town in a
    /// dense cluster, defeating the "isolated outlier" design intent.
    /// </summary>
    private const double OutlierMinDistanceFromAllTowns = 100.0;
    private const int MapWidth = 800;
    private const int MapHeight = 500;

    public static World Generate(SeedWorld seedWorld, GameSetupDeterministicSource source,
        GameEntropy entropy, SaltSource? saltSource)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(source);

        var townNames = SeedWorldFactory.DeriveTownNames(
            seedWorld.WorldVariant,
            seedWorld.TownCount,
            seedWorld.AccusationIndex,
            seedWorld.DefaultCulpritIndex,
            seedWorld.CashBonus,
            seedWorld.ProsperityPalette,
            seedWorld.ServicesPalette);

        // Place base towns only — the outlier is added later as a purely additive step.
        var (towns, clusterAssignments) = ClusterPlacementGenerator.Place(seedWorld, source, entropy, saltSource);

        // Generate the trail graph for base towns only. The outlier is not part
        // of this graph, so the Delaunay/MST/filters never see it and never
        // create incident trails that would need pruning.
        var edges = TrailGraphGenerator.Generate(seedWorld, towns, clusterAssignments,
            source, entropy, saltSource);
        var townIds = townNames.Select(t => t.Id).ToArray();
        var trails = TerrainAssigner.Assign(edges, towns, clusterAssignments,
            seedWorld.WorldVariant, townIds, source, saltSource, outlierSlot: null).ToList();

        int? outlierSlot = null;

        // Additive outlier: when activated (OutlierSlotType == 1 and non-Boring entropy),
        // append one extra town and connect it to the nearest base town via a single
        // 6-day trail. No existing trails are pruned — the outlier is purely additive.
        if (seedWorld.OutlierSlotType == 1 && entropy != GameEntropy.Boring)
        {
            outlierSlot = seedWorld.TownCount;

            // Derive a unique name for the outlier from the full name pool.
            var existingIds = new HashSet<string>(townNames.Select(t => t.Id));
            var outlierPool = SeedWorldFactory.DeriveTownNames(
                seedWorld.WorldVariant,
                townCount: SeedWorldFactory.NamePool.Count,
                accusationIndex: 0,
                defaultCulpritIndex: 0,
                cashBonus: 0,
                prosperityPalette: seedWorld.ProsperityPalette,
                servicesPalette: seedWorld.ServicesPalette);
            var outlierName = outlierPool.First(entry => !existingIds.Contains(entry.Id));
            var townNamesList = townNames.ToList();
            townNamesList.Add(outlierName);
            townNames = townNamesList;

            // Extend the town IDs and coordinate arrays to include the outlier slot.
            townIds = townNames.Select(t => t.Id).ToArray();
            var (outlierPos, connectSlot) = PlaceOutlierAdditive(
                towns, source, saltSource, trails, townIds, outlierSlot.Value);
            towns[outlierSlot.Value] = outlierPos;
            clusterAssignments[outlierSlot.Value] = -1;

            // Add the single 6-day trail connecting the outlier to the chosen base town.
            var trailId = $"trail-{outlierSlot.Value}-{connectSlot}";
            trails.Add(new SeedWorldTrail(
                trailId,
                townIds[outlierSlot.Value],
                townIds[connectSlot],
                TrailRisk.High,
                TrailTerrain.Mountains,
                WaterFeature.None,
                6m));
        }

        return SeedWorldFactory.CreateWorld(
            seedWorld.WorldVariant,
            townNames,
            seedWorld.ServicesPalette,
            seedWorld.ProsperityPalette,
            trails,
            townCoordinates: towns,
            outlierSlot: outlierSlot,
            entropy,
            saltSource,
            seedWorld.SeedCode);
    }

    /// <summary>
    /// Places the outlier town at <see cref="OutlierPlacementDistance"/> pixels from a
    /// base town, choosing a connection target and angle that does not cause the new
    /// incident trail to cross any existing trail. Tries each base town as a connection
    /// target (nearest to town 0 first), sweeping 360 angles per target. Falls back to
    /// the nearest-town placement if all combinations cross.
    /// </summary>
    private static ((int X, int Y) Pos, int ConnectSlot) PlaceOutlierAdditive(
        Dictionary<int, (int X, int Y)> baseTowns,
        GameSetupDeterministicSource source,
        SaltSource? saltSource,
        IReadOnlyList<SeedWorldTrail> existingTrails,
        string[] townIds,
        int outlierSlot)
    {
        // Try each base town as a connection target, nearest to town 0 first.
        // This gives the best chance of finding a non-crossing placement when
        // the base graph is dense around one town.
        var candidateSlots = baseTowns.Keys
            .OrderBy(slot =>
            {
                var dx = baseTowns[slot].X - baseTowns[0].X;
                var dy = baseTowns[slot].Y - baseTowns[0].Y;
                return Math.Sqrt(dx * dx + dy * dy);
            })
            .ToArray();

        var salt = saltSource?.Salt ?? "default";
        var roll = source.Roll($"outlier-angle-{salt}");
        var baseAngle = (roll % 360UL) * (Math.PI / 180.0);

        foreach (var connectSlot in candidateSlots)
        {
            var neighbor = baseTowns[connectSlot];

            // Try the seeded angle first, then sweep all 360 degrees to find a
            // position where the outlier trail does not cross any existing trail
            // AND the outlier is far enough from all base towns to remain isolated.
            for (var attempt = 0; attempt < 360; attempt++)
            {
                var angle = baseAngle + attempt * (Math.PI / 180.0);
                var x = (int)(neighbor.X + OutlierPlacementDistance * Math.Cos(angle));
                var y = (int)(neighbor.Y + OutlierPlacementDistance * Math.Sin(angle));
                var candidate = (ClampToBounds(x, MapWidth), ClampToBounds(y, MapHeight));

                if (!WouldCrossAnyTrail(candidate, neighbor, existingTrails, baseTowns, townIds) &&
                    IsFarEnoughFromAllTowns(candidate, baseTowns, connectSlot))
                {
                    return (candidate, connectSlot);
                }
            }
        }

        // All connection targets and angles cross — fall back to the nearest town
        // with the seeded angle. This is extremely unlikely with a connected planar
        // base graph and multiple connection targets, but we guarantee a placement.
        var fallbackSlot = candidateSlots[0];
        var fallbackNeighbor = baseTowns[fallbackSlot];
        var fallbackX = (int)(fallbackNeighbor.X + OutlierPlacementDistance * Math.Cos(baseAngle));
        var fallbackY = (int)(fallbackNeighbor.Y + OutlierPlacementDistance * Math.Sin(baseAngle));
        return ((ClampToBounds(fallbackX, MapWidth), ClampToBounds(fallbackY, MapHeight)), fallbackSlot);
    }

    /// <summary>
    /// Returns true if a segment from <paramref name="outlierPos"/> to
    /// <paramref name="neighbor"/> would cross any existing trail.
    /// </summary>
    private static bool WouldCrossAnyTrail(
        (int X, int Y) outlierPos,
        (int X, int Y) neighbor,
        IReadOnlyList<SeedWorldTrail> existingTrails,
        Dictionary<int, (int X, int Y)> towns,
        string[] townIds)
    {
        // Build a lookup from town ID to coordinates for the base towns.
        var coordById = new Dictionary<string, (int X, int Y)>();
        for (var i = 0; i < townIds.Length - 1; i++) // exclude the outlier slot itself
        {
            if (towns.TryGetValue(i, out var coords))
                coordById[townIds[i]] = coords;
        }

        foreach (var trail in existingTrails)
        {
            if (!coordById.TryGetValue(trail.FromTownId, out var p3)) continue;
            if (!coordById.TryGetValue(trail.ToTownId, out var p4)) continue;

            // The outlier trail shares no endpoints with existing trails
            // (the outlier is a new town), so no shared-endpoint skip is needed.
            if (SegmentsIntersect(outlierPos, neighbor, p3, p4))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the candidate position is at least
    /// <see cref="OutlierMinDistanceFromAllTowns"/> pixels from every base town
    /// (excluding the connection target, which is at <see cref="OutlierPlacementDistance"/>
    /// by construction). This prevents the outlier from being placed 150px from its
    /// connection target but ending up inside a dense cluster near other base towns.
    /// </summary>
    private static bool IsFarEnoughFromAllTowns(
        (int X, int Y) candidate,
        Dictionary<int, (int X, int Y)> baseTowns,
        int connectSlot)
    {
        foreach (var (slot, pos) in baseTowns)
        {
            if (slot == connectSlot) continue; // connection target is already 150px away
            var dx = candidate.X - pos.X;
            var dy = candidate.Y - pos.Y;
            var dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < OutlierMinDistanceFromAllTowns)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Standard segment intersection test using cross-product orientation signs.
    /// Two segments AB and CD intersect iff A and B are on opposite sides of CD
    /// AND C and D are on opposite sides of AB.
    /// </summary>
    private static bool SegmentsIntersect(
        (int X, int Y) a, (int X, int Y) b,
        (int X, int Y) c, (int X, int Y) d)
    {
        var d1 = CrossSign(c, d, a);
        var d2 = CrossSign(c, d, b);
        var d3 = CrossSign(a, b, c);
        var d4 = CrossSign(a, b, d);

        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0))
            && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));

        // Cross product sign of (B-A) × (C-A).
        // The outer parentheses around the subtraction are load-bearing: without them,
        // C# parses `x - y switch { ... }` as `x - (y switch { ... })` because the
        // switch expression binds to the nearest term, not the whole arithmetic expression.
        // That would compute (term1 - sign(term2)) instead of sign(term1 - term2).
        static int CrossSign((int X, int Y) a, (int X, int Y) b, (int X, int Y) c)
            => ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) switch
            {
                > 0 => 1,
                < 0 => -1,
                _ => 0
            };
    }

    private static int ClampToBounds(int value, int max) => Math.Max(0, Math.Min(max, value));
}
