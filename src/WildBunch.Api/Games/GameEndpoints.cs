using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;

namespace WildBunch.Api.Games;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/api/games");

        games.MapPost(string.Empty, CreateGameAsync);
        games.MapGet("{id:guid}", GetGameAsync);
        games.MapPost("{id:guid}/travel", TravelAsync);

        return app;
    }

    private static async Task<IResult> CreateGameAsync(
        StartGameRequest request,
        StartNewGameHandler handler,
        CancellationToken cancellationToken)
    {
        var session = await handler.HandleAsync(new StartNewGameCommand(request.PlayerName), cancellationToken);
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

    private static async Task<IResult> TravelAsync(
        Guid id,
        TravelRequest request,
        TravelToTownHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new TravelToTownCommand(id, request.DestinationTownId), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
