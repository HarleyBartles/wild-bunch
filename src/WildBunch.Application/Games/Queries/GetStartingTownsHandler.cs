using WildBunch.Application.Games.Models;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Application.Games.Queries;

public sealed class GetStartingTownsHandler
{
    public Task<IReadOnlyList<StartingTownDto>> HandleAsync(GetStartingTownsQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var towns = StartingTownCatalog.GetStartingTownCandidates();
        var dtos = towns.Select(town => new StartingTownDto(town.Id.Value, town.Name, town.Services)).ToArray();
        return Task.FromResult<IReadOnlyList<StartingTownDto>>(dtos);
    }
}
