using Microsoft.AspNetCore.Mvc;
using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Games.Exceptions;
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

        return app;
    }

    private static async Task<IResult> GetSessionAuditAsync(
        Guid id,
        [FromServices] DevRoleGuard guard,
        [FromServices] GetSessionAuditHandler handler,
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
        [FromServices] DevRoleGuard guard,
        [FromServices] GetTravelDevContextHandler handler,
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
        [FromServices] DevRoleGuard guard,
        [FromServices] ForceTravelOverrideHandler handler,
        [FromBody] ForceTravelOverrideRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            guard.EnsureDevAccess();
            if (string.IsNullOrWhiteSpace(request.ForcedCategory))
            {
                return Results.BadRequest("ForcedCategory is required.");
            }
            if (!Enum.TryParse<TravelDayEncounterCategory>(request.ForcedCategory, ignoreCase: true, out var category)
                || !Enum.IsDefined(category))
            {
                return Results.BadRequest("Invalid ForcedCategory value.");
            }
            await handler.HandleAsync(new ForceTravelOverrideCommand(
                id, category, request.FoeSpeed,
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
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    }

    private static async Task<IResult> ClearTravelOverrideAsync(
        Guid id,
        [FromServices] DevRoleGuard guard,
        [FromServices] ClearTravelOverrideHandler handler,
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
        [FromServices] DevRoleGuard guard,
        [FromServices] GetSaloonDevContextHandler handler,
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
        [FromServices] DevRoleGuard guard,
        [FromServices] ForceSaloonOverrideHandler handler,
        [FromBody] ForceSaloonOverrideRequestDto request,
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
        [FromServices] DevRoleGuard guard,
        [FromServices] ClearSaloonOverrideHandler handler,
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
        [FromServices] DevRoleGuard guard,
        [FromServices] GetSessionDevContextHandler handler,
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

    private static async Task<IResult> ForceDevDifficultyAsync(
        Guid id,
        [FromServices] DevRoleGuard guard,
        [FromServices] ForceDevDifficultyHandler handler,
        [FromBody] ForceDevDifficultyRequestDto? request,
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
        [FromServices] DevRoleGuard guard,
        [FromServices] SetDevEntropyHandler handler,
        [FromBody] SetDevEntropyRequestDto? request,
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

}
