using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;

namespace WildBunch.Api.Games;

public static class WantedPosterEndpoints
{
    public static IEndpointRouteBuilder MapWantedPosterEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapPost("{id:guid}/wanted-posters/read", ReadWantedPostersAsync)
            .WithName("ReadWantedPosters")
            .Produces<WantedPostersResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> ReadWantedPostersAsync(
        Guid id,
        ReadWantedPostersHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new ReadWantedPostersCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
