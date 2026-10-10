using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Api.Games.Validation;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Application.Projections;

namespace WildBunch.Api.Games;

public static class GameSessionEndpoints
{
    public static IEndpointRouteBuilder MapGameSessionEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapPost("setup", SetupGameAsync)
            .WithName("SetupGame")
            .Accepts<SetupGameRequest>("application/json")
            .Produces<GameSessionDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict);

        games.MapPost("{id:guid}/prologue-viewed", MarkPrologueViewedAsync)
            .WithName("MarkPrologueViewed")
            .Produces<GameSessionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/start", StartGameAsync)
            .WithName("StartGameWithTown")
            .Accepts<StartGameWithTownRequest>("application/json")
            .Produces<GameSessionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        games.MapGet("{id:guid}/world-map", GetWorldMapAsync)
            .WithName("GetWorldMap")
            .Produces<WorldMapDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapGet("{id:guid}/prologue", GetPrologueAsync)
            .WithName("GetPrologue")
            .Produces<PrologueDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

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

    private static async Task<IResult> SetupGameAsync(
        HttpRequest httpRequest,
        SetupGameRequest? request,
        CompletePlayerSetupHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetStartupOperationId(httpRequest, out var operationId))
        {
            return Results.BadRequest(new { error = "A valid Idempotency-Key header is required." });
        }

        if (!RequestValidation.TryValidate(request, out var validationResult))
        {
            return validationResult!;
        }

        var validatedRequest = request!;
        try
        {
            var session = await handler.HandleAsync(
                new CompletePlayerSetupCommand
                {
                    OperationId = operationId,
                    PlayerName = validatedRequest.PlayerName,
                    GameDifficulty = validatedRequest.GameDifficulty,
                    GameEntropy = validatedRequest.GameEntropy,
                    SeedCode = validatedRequest.SeedCode ?? string.Empty
                },
                cancellationToken);
            return Results.Created($"/api/games/{session.Id}", session);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> MarkPrologueViewedAsync(
        Guid id,
        ViewPrologueHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var session = await handler.HandleAsync(
                new ViewPrologueCommand(),
                new GameSessionId(id),
                cancellationToken);
            return Results.Ok(session);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> StartGameAsync(
        Guid id,
        HttpRequest httpRequest,
        StartGameWithTownRequest? request,
        CompleteGameStartHandler handler,
        CancellationToken cancellationToken)
    {
        if (!TryGetStartupOperationId(httpRequest, out var operationId))
        {
            return Results.BadRequest(new { error = "A valid Idempotency-Key header is required." });
        }

        if (!RequestValidation.TryValidate(request, out var validationResult))
        {
            return validationResult!;
        }

        var validatedRequest = request!;
        try
        {
            var session = await handler.HandleAsync(
                new CompleteGameStartCommand
                {
                    OperationId = operationId,
                    SessionId = new GameSessionId(id),
                    StartingTownId = validatedRequest.StartingTownId
                },
                cancellationToken);
            return Results.Ok(session);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static bool TryGetStartupOperationId(HttpRequest request, out Guid operationId)
    {
        operationId = Guid.Empty;
        return request.Headers.TryGetValue("Idempotency-Key", out var value) &&
            Guid.TryParse(value, out operationId) && operationId != Guid.Empty;
    }

    private static async Task<IResult> GetWorldMapAsync(
        Guid id,
        GetWorldMapHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var map = await handler.HandleAsync(new GetWorldMapQuery(id), cancellationToken);
            return Results.Ok(map);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetPrologueAsync(
        Guid id,
        GetPrologueHandler handler,
        string? variantId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = await handler.HandleAsync(new GetPrologueQuery(id, variantId), cancellationToken);
            return Results.Ok(dto);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
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
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
