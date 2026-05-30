using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Api.Games.Validation;

namespace WildBunch.Api.Games;

public static class GameSessionEndpoints
{
    public static IEndpointRouteBuilder MapGameSessionEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapPost(string.Empty, CreateGameAsync)
            .WithName("CreateGame")
            .Accepts<StartGameRequest>("application/json")
            .Produces<GameSessionDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        games.MapGet("{id:guid}", GetGameAsync)
            .WithName("GetGame")
            .Produces<GameSessionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> CreateGameAsync(
        StartGameRequest? request,
        StartNewGameHandler handler,
        CancellationToken cancellationToken)
    {
        if (!RequestValidation.TryValidate(request, out var validationResult))
        {
            return validationResult!;
        }

        var validatedRequest = request!;
        var session = await handler.HandleAsync(
            new StartNewGameCommand(validatedRequest.PlayerName, validatedRequest.TravelDifficulty, validatedRequest.SeedCode),
            cancellationToken);
        return Results.Created($"/api/games/{session.Id}", session);
    }

    private static async Task<IResult> GetGameAsync(
        Guid id,
        GetGameSessionHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await handler.HandleAsync(new GetGameSessionQuery(id), cancellationToken);
            return Results.Ok(session);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
