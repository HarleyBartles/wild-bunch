using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;

namespace WildBunch.Api.Games;

public static class ActionEndpoints
{
    public static IEndpointRouteBuilder MapActionEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapGet("{id:guid}/actions", GetAvailableActionsAsync)
            .WithName("GetAvailableActions")
            .Produces<IReadOnlyList<AvailableActionDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> GetAvailableActionsAsync(
        Guid id,
        GetAvailableActionsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var availableActions = await handler.HandleAsync(new GetAvailableActionsQuery(id), cancellationToken);
            return Results.Ok(availableActions);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
