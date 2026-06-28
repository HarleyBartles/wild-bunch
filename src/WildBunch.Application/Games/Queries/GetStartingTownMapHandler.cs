using WildBunch.Application.Games.Models;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Application.Games.Queries;

public sealed class GetStartingTownMapHandler
{
    public Task<StartingTownMapDto> HandleAsync(GetStartingTownMapQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var candidateIds = StartingTownCatalog.GetStartingTownCandidates()
            .Select(town => town.Id.Value)
            .ToHashSet();

        var towns = SeedWorldMapLayout.GetMapTowns()
            .Select(town => new StartingTownMapTownDto(
                town.Id,
                town.Name,
                town.Services,
                town.X,
                town.Y,
                candidateIds.Contains(town.Id)))
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
