using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;

namespace WildBunch.Api.Games;

public static class JournalEndpoints
{
    public static IEndpointRouteBuilder MapJournalEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapGet("{id:guid}/journal", GetJournalAsync)
            .WithName("GetJournal")
            .Produces<JournalDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> GetJournalAsync(
        Guid id,
        GetJournalHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var journal = await handler.HandleAsync(new GetJournalQuery(id), cancellationToken);
            return Results.Ok(journal);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
