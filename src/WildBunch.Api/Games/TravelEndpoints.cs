using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Api.Games.Validation;

namespace WildBunch.Api.Games;

public static class TravelEndpoints
{
    public static IEndpointRouteBuilder MapTravelEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapGet("{id:guid}/travel/preview/{destinationTownId}", PreviewTravelAsync)
            .WithName("PreviewTravel")
            .Produces<TravelPreviewResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/travel", TravelAsync)
            .WithName("TravelGame")
            .Accepts<TravelRequest>("application/json")
            .Produces<GameTurnResultDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/travel/advance", AdvanceTravelDayAsync)
            .WithName("AdvanceTravelDay")
            .Produces<GameTurnResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/travel/arrival/acknowledge", AcknowledgeJourneyArrivalAsync)
            .WithName("AcknowledgeJourneyArrival")
            .Produces<GameTurnResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/travel/encounter/resolve", ResolveTravelEncounterAsync)
            .WithName("ResolveTravelEncounter")
            .Accepts<ResolveJourneyEncounterRequest>("application/json")
            .Produces<GameTurnResultDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> PreviewTravelAsync(
        Guid id,
        string destinationTownId,
        PreviewTravelHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new PreviewTravelQuery(id, destinationTownId), cancellationToken);
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

    private static async Task<IResult> AdvanceTravelDayAsync(
        Guid id,
        AdvanceTravelDayHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new AdvanceTravelDayCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> AcknowledgeJourneyArrivalAsync(
        Guid id,
        AcknowledgeJourneyArrivalHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new AcknowledgeJourneyArrivalCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ResolveTravelEncounterAsync(
        Guid id,
        ResolveJourneyEncounterRequest? request,
        ResolveJourneyEncounterHandler handler,
        CancellationToken cancellationToken)
    {
        if (!RequestValidation.TryValidate(request, out var validationResult))
        {
            return validationResult!;
        }

        var validatedRequest = request!;
        try
        {
            var result = await handler.HandleAsync(
                new ResolveJourneyEncounterCommand(id, validatedRequest.ChoiceId, validatedRequest.BulletSpend, validatedRequest.BribeAmount),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
