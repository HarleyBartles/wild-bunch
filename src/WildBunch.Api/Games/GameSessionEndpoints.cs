using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Api.Games.Validation;
using WildBunch.Domain.Game;

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

        games.MapPost("{id:guid}/archive", ArchiveGameAsync)
            .WithName("ArchiveGame")
            .Produces<ArchivePlaythroughResultDto>(StatusCodes.Status200OK)
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
            new StartNewGameCommand(validatedRequest.PlayerName, validatedRequest.TravelDifficulty, validatedRequest.SeedCode, validatedRequest.Entropy, validatedRequest.StartingTownId),
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

    private static async Task<IResult> ArchiveGameAsync(
        Guid id,
        ArchivePlaythroughHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(
                new ArchivePlaythroughCommand(new GameSessionId(id), "player-start-over"),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
