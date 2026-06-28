using WildBunch.Application.Games.Models;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Application.Games.Queries;

public sealed class GetStartingTownMapHandler
{
    public Task<StartingTownMapDto> HandleAsync(GetStartingTownMapQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // For this POC, every listed town on the starting map is a valid starting-town selection.
        // The "town you can never travel to" (where the player was falsely accused) is not part of
        // this listed map — it is offscreen/unlisted conceptually. So we do not filter or mark towns
        // as non-selectable. All towns returned by SeedWorldMapLayout are startable.
        var towns = SeedWorldMapLayout.GetMapTowns()
            .Select(town => new StartingTownMapTownDto(
                town.Id,
                town.Name,
                town.Services,
                town.X,
                town.Y))
            .ToArray();

        var trails = SeedWorldMapLayout.GetMapTrails()
            .Select(trail => new StartingTownMapTrailDto(
                trail.Id,
                trail.FromTownId,
                trail.ToTownId,
                trail.RideDayDistance))
            .ToArray();

        return Task.FromResult(new StartingTownMapDto(towns, trails));
    }
}
