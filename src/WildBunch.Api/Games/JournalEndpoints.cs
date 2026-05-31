using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Api.Games.Validation;

namespace WildBunch.Api.Games;

public static class JournalEndpoints
{
    public static IEndpointRouteBuilder MapJournalEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapGet("{id:guid}/journal", GetJournalAsync)
            .WithName("GetJournal")
            .Produces<JournalDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> GetJournalAsync(
        Guid id,
        int? skip,
        int? take,
        GetJournalHandler handler,
        CancellationToken cancellationToken)
    {
        if (!RequestValidation.TryValidateJournalPaging(skip, take, out var validationResult))
        {
            return validationResult!;
        }

        try
        {
            var journal = await handler.HandleAsync(new GetJournalQuery(id, skip ?? 0, take), cancellationToken);
            return Results.Ok(journal);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
