using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Api.Games.Validation;

namespace WildBunch.Api.Games;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/api/games");

        games.MapPost(string.Empty, CreateGameAsync)
            .WithName("CreateGame")
            .Accepts<StartGameRequest>("application/json")
            .Produces<GameSessionDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        games.MapGet("{id:guid}", GetGameAsync)
            .WithName("GetGame")
            .Produces<GameSessionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapGet("{id:guid}/actions", GetAvailableActionsAsync)
            .WithName("GetAvailableActions")
            .Produces<IReadOnlyList<AvailableActionDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapGet("{id:guid}/journal", GetJournalAsync)
            .WithName("GetJournal")
            .Produces<JournalDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/wanted-posters/read", ReadWantedPostersAsync)
            .WithName("ReadWantedPosters")
            .Produces<WantedPostersResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/travel", TravelAsync)
            .WithName("TravelGame")
            .Accepts<TravelRequest>("application/json")
            .Produces<GameTurnResultDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        return app;
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
        var session = await handler.HandleAsync(new StartNewGameCommand(validatedRequest.PlayerName), cancellationToken);
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

    private static async Task<IResult> TravelAsync(
        Guid id,
        TravelRequest? request,
        TravelToTownHandler handler,
        CancellationToken cancellationToken)
    {
        if (!RequestValidation.TryValidate(request, out var validationResult))
        {
            return validationResult!;
        }

        var validatedRequest = request!;
        try
        {
            var result = await handler.HandleAsync(new TravelToTownCommand(id, validatedRequest.DestinationTownId), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
