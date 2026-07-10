using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;

namespace WildBunch.Api.Dev;

public static class DevEndpoints
{
    public static IEndpointRouteBuilder MapDevEndpoints(this IEndpointRouteBuilder app)
    {
        var dev = app.MapGroup("/api/dev");

        dev.MapGet("/sessions/{id:guid}/audit", GetSessionAuditAsync)
            .WithName("GetSessionAudit")
            .Produces<SessionAuditDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        dev.MapGet("/sessions/{id:guid}/travel-context", GetTravelDevContextAsync)
            .WithName("GetTravelDevContext")
            .Produces<TravelDevContextDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        dev.MapPost("/sessions/{id:guid}/travel/force-override", ForceTravelOverrideAsync)
            .WithName("ForceTravelOverride")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        dev.MapPost("/sessions/{id:guid}/travel/clear-override", ClearTravelOverrideAsync)
            .WithName("ClearTravelOverride")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        dev.MapGet("/sessions/{id:guid}/saloon-context", GetSaloonDevContextAsync)
            .WithName("GetSaloonDevContext")
            .Produces<SaloonDevContextDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        dev.MapPost("/sessions/{id:guid}/saloon/force-override", ForceSaloonOverrideAsync)
            .WithName("ForceSaloonOverride")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        dev.MapPost("/sessions/{id:guid}/saloon/clear-override", ClearSaloonOverrideAsync)
            .WithName("ClearSaloonOverride")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        dev.MapGet("/sessions/{id:guid}/session-context", GetSessionDevContextAsync)
            .WithName("GetSessionDevContext")
            .Produces<SessionDevContextDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        // TODO: Re-enable when LockRngHandler and ClearRngHandler are implemented
        // dev.MapPost("/sessions/{id:guid}/session/lock-rng", LockRngAsync)
        //     .WithName("LockRng")
        //     .Produces(StatusCodes.Status204NoContent)
        //     .Produces(StatusCodes.Status403Forbidden)
        //     .Produces(StatusCodes.Status404NotFound);

        // TODO: Re-enable when LockRngHandler and ClearRngHandler are implemented
        // dev.MapPost("/sessions/{id:guid}/session/clear-rng", ClearRngAsync)
        //     .WithName("ClearRng")
        //     .Produces(StatusCodes.Status204NoContent)
        //     .Produces(StatusCodes.Status403Forbidden)
        //     .Produces(StatusCodes.Status404NotFound);

        dev.MapPost("/sessions/{id:guid}/session/force-difficulty", ForceDevDifficultyAsync)
            .WithName("ForceDevDifficulty")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        dev.MapPost("/sessions/{id:guid}/session/set-entropy", SetDevEntropyAsync)
            .WithName("SetDevEntropy")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        dev.MapGet("/sessions/{id:guid}/town-layout/salts", GetTownLayoutSaltsAsync)
            .WithName("GetTownLayoutSalts")
            .Produces<TownLayoutSaltsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        dev.MapPost("/sessions/{id:guid}/town-layout/set-salts", SetTownLayoutSaltsAsync)
            .WithName("SetTownLayoutSalts")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest);

        dev.MapPost("/sessions/{id:guid}/town-layout/generate-random", GenerateRandomTownLayoutSaltsAsync)
            .WithName("GenerateRandomTownLayoutSalts")
            .Produces<TownLayoutSaltsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        dev.MapPost("/games/prep", PrepGameSessionAsync)
            .WithName("PrepGameSession")
            .Accepts<PrepGameSessionCommand>("application/json")
            .Produces<PrepGameSessionResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesValidationProblem();

        dev.MapPost("/games/{id:guid}/start", StartGameSessionAsync)
            .WithName("StartGameSession")
            .Produces<GameSessionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetSessionAuditAsync(
        Guid id,
        DevRoleGuard guard,
        GetSessionAuditHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var result = await handler.HandleAsync(new GetSessionAuditQuery(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetTravelDevContextAsync(
        Guid id,
        DevRoleGuard guard,
        GetTravelDevContextHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var result = await handler.HandleAsync(new GetTravelDevContextQuery(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ForceTravelOverrideAsync(
        Guid id,
        DevRoleGuard guard,
        ForceTravelOverrideHandler handler,
        ForceTravelOverrideRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            if (string.IsNullOrWhiteSpace(request.ForcedCategory))
            {
                return Results.BadRequest("ForcedCategory is required.");
            }
            await handler.HandleAsync(new ForceTravelOverrideCommand(
                id, request.ForcedCategory, request.FoeSpeed,
                request.FoeFightStrength, request.FoeMinimumBribe, request.EncounterMessage),
                cancellationToken);
            return Results.NoContent();
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException)
        {
            return Results.BadRequest("Invalid ForcedCategory value.");
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> ClearTravelOverrideAsync(
        Guid id,
        DevRoleGuard guard,
        ClearTravelOverrideHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            await handler.HandleAsync(new ClearTravelOverrideCommand(id), cancellationToken);
            return Results.NoContent();
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetSaloonDevContextAsync(
        Guid id,
        DevRoleGuard guard,
        GetSaloonDevContextHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var result = await handler.HandleAsync(new GetSaloonDevContextQuery(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ForceSaloonOverrideAsync(
        Guid id,
        DevRoleGuard guard,
        ForceSaloonOverrideHandler handler,
        ForceSaloonOverrideRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            if (string.IsNullOrWhiteSpace(request.ForcedKind))
            {
                return Results.BadRequest("ForcedKind is required.");
            }
            await handler.HandleAsync(new ForceSaloonOverrideCommand(
                id, request.ForcedKind, request.ForcedSuspectId, request.ForcedCitizenRoleKey),
                cancellationToken);
            return Results.NoContent();
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException)
        {
            return Results.BadRequest("Invalid ForcedKind value.");
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> ClearSaloonOverrideAsync(
        Guid id,
        DevRoleGuard guard,
        ClearSaloonOverrideHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            await handler.HandleAsync(new ClearSaloonOverrideCommand(id), cancellationToken);
            return Results.NoContent();
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetSessionDevContextAsync(
        Guid id,
        DevRoleGuard guard,
        GetSessionDevContextHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var result = await handler.HandleAsync(new GetSessionDevContextQuery(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    // TODO: Re-enable when LockRngHandler and ClearRngHandler are implemented
    // private static async Task<IResult> LockRngAsync(
    //     Guid id,
    //     DevRoleGuard guard,
    //     ForceDevSaltSourceHandler handler,
    //     LockRngRequestDto? request,
    //     CancellationToken cancellationToken)
    // {
    //     try
    //     {
    //         guard.EnsureDevAccess();
    //         // Salt contract: null/empty/whitespace → handler generates a fresh fixed salt.
    //         // Non-empty string → handler trims and uses verbatim.
    //         var salt = request?.Salt;
    //         await handler.HandleAsync(new ForceDevSaltSourceCommand(id, salt), cancellationToken);
    //         return Results.NoContent();
    //     }
    //     catch (DevAccessDeniedException)
    //     {
    //         return Results.StatusCode(StatusCodes.Status403Forbidden);
    //     }
    //     catch (GameSessionNotFoundException)
    //     {
    //         return Results.NotFound();
    //     }
    //     catch (ArgumentException ex)
    //     {
    //         return Results.BadRequest(ex.Message);
    //     }
    // }

    // TODO: Re-enable when LockRngHandler and ClearRngHandler are implemented
    // private static async Task<IResult> ClearRngAsync(
    //     Guid id,
    //     DevRoleGuard guard,
    //     ClearDevSaltSourceHandler handler,
    //     CancellationToken cancellationToken)
    // {
    //     try
    //     {
    //         guard.EnsureDevAccess();
    //         await handler.HandleAsync(new ClearDevSaltSourceCommand(id), cancellationToken);
    //         return Results.NoContent();
    //     }
    //     catch (DevAccessDeniedException)
    //     {
    //         return Results.StatusCode(StatusCodes.Status403Forbidden);
    //     }
    //     catch (GameSessionNotFoundException)
    //     {
    //         return Results.NotFound();
    //     }
    // }

    private static async Task<IResult> ForceDevDifficultyAsync(
        Guid id,
        DevRoleGuard guard,
        ForceDevDifficultyHandler handler,
        ForceDevDifficultyRequestDto? request,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            if (request is null || string.IsNullOrWhiteSpace(request.Difficulty))
            {
                return Results.BadRequest("Difficulty is required.");
            }

            if (!Enum.TryParse<GameDifficulty>(request.Difficulty, ignoreCase: true, out var difficulty)
                || !Enum.IsDefined(typeof(GameDifficulty), difficulty))
            {
                return Results.BadRequest($"Invalid difficulty value: {request.Difficulty}");
            }

            await handler.HandleAsync(new ForceDevDifficultyCommand(id, difficulty), cancellationToken);
            return Results.NoContent();
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> SetDevEntropyAsync(
        Guid id,
        DevRoleGuard guard,
        SetDevEntropyHandler handler,
        SetDevEntropyRequestDto? request,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            if (request is null || string.IsNullOrWhiteSpace(request.Entropy))
            {
                return Results.BadRequest("Entropy is required.");
            }

            if (!Enum.TryParse<GameEntropy>(request.Entropy, ignoreCase: true, out var entropy)
                || !Enum.IsDefined(typeof(GameEntropy), entropy))
            {
                return Results.BadRequest($"Invalid entropy value: {request.Entropy}");
            }

            await handler.HandleAsync(new SetDevEntropyCommand(id, entropy), cancellationToken);
            return Results.NoContent();
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> GetTownLayoutSaltsAsync(
        Guid id,
        DevRoleGuard guard,
        GetTownLayoutSaltsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var result = await handler.HandleAsync(new GetTownLayoutSaltsQuery(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> SetTownLayoutSaltsAsync(
        Guid id,
        DevRoleGuard guard,
        SetTownLayoutSaltsHandler handler,
        TownLayoutSaltsDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            await handler.HandleAsync(new SetTownLayoutSaltsCommand(
                id,
                request.BuildingsSalt,
                request.RoadsSalt,
                request.DirtSalt,
                request.PropsSalt),
                cancellationToken);
            return Results.NoContent();
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GenerateRandomTownLayoutSaltsAsync(
        Guid id,
        DevRoleGuard guard,
        GenerateRandomTownLayoutSaltsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var result = await handler.HandleAsync(new GenerateRandomTownLayoutSaltsCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> PrepGameSessionAsync(
        PrepGameSessionCommand command,
        DevRoleGuard guard,
        PrepGameSessionHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }
    }

    private static async Task<IResult> StartGameSessionAsync(
        Guid id,
        DevRoleGuard guard,
        StartGameSessionHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            var command = new StartGameSessionCommand(id);
            var result = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(result);
        }
        catch (DevAccessDeniedException)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
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
