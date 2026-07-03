using System.Linq;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class OutlierGuarantee
{
    private const double OutlierTargetDistancePx = 150.0;

    public static (IReadOnlyList<SeedWorldTrail> Trails, Dictionary<int, (int X, int Y)> Towns) Enforce(
        IReadOnlyList<SeedWorldTrail> trails,
        Dictionary<int, (int X, int Y)> towns,
        int? outlierSlot,
        IReadOnlyList<string> townIds)
    {
        ArgumentNullException.ThrowIfNull(trails);
        ArgumentNullException.ThrowIfNull(towns);
        ArgumentNullException.ThrowIfNull(townIds);

        if (!outlierSlot.HasValue) return (trails, towns);

        var outlier = outlierSlot.Value;
        var incident = trails.Select((t, i) => (Index: i, Trail: t))
            .Where(x => x.Trail.FromTownId == townIds[outlier] || x.Trail.ToTownId == townIds[outlier])
            .ToList();

        if (incident.Count == 0) return (trails, towns);

        var kept = incident.OrderBy(x => x.Trail.RideDayDistance).First();
        var resultTrails = new List<SeedWorldTrail>();

        // Add non-incident trails
        for (var i = 0; i < trails.Count; i++)
        {
            if (!incident.Any(x => x.Index == i))
            {
                resultTrails.Add(trails[i]);
            }
        }

        // Add the kept outlier trail with 6 ride-days
        var updatedTrail = new SeedWorldTrail(
            kept.Trail.Id,
            kept.Trail.FromTownId,
            kept.Trail.ToTownId,
            kept.Trail.Risk,
            kept.Trail.Terrain,
            kept.Trail.WaterFeature,
            6m);
        resultTrails.Add(updatedTrail);

        var connectedTownId = kept.Trail.FromTownId == townIds[outlier]
            ? kept.Trail.ToTownId
            : kept.Trail.FromTownId;
        var connectedSlot = townIds.ToList().IndexOf(connectedTownId);

        var adjustedTowns = new Dictionary<int, (int X, int Y)>(towns);
        var neighbor = towns[connectedSlot];
        var outlierPos = towns[outlier];
        var dx = outlierPos.X - neighbor.X;
        var dy = outlierPos.Y - neighbor.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (Math.Abs(distance - OutlierTargetDistancePx) > 0.5)
        {
            if (distance == 0)
            {
                adjustedTowns[outlier] = (neighbor.X + (int)OutlierTargetDistancePx, neighbor.Y);
            }
            else
            {
                var scale = OutlierTargetDistancePx / distance;
                adjustedTowns[outlier] = (
                    (int)(neighbor.X + dx * scale),
                    (int)(neighbor.Y + dy * scale));
            }
        }

        return (resultTrails, adjustedTowns);
    }
}
