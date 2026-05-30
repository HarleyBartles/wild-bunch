using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Queries;
using WildBunch.Api.Games.Validation;

namespace WildBunch.Api.Games;

public static class TownStoreEndpoints
{
    public static IEndpointRouteBuilder MapTownStoreEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapGet("{gameSessionId:guid}/towns/{townId}/store-offers", GetTownStoreOffersAsync)
            .WithName("GetTownStoreOffers")
            .Produces<TownStoreOffersDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{gameSessionId:guid}/towns/{townId}/store/buy", BuyStoreItemAsync)
            .WithName("BuyStoreItem")
            .Accepts<BuyStoreItemRequest>("application/json")
            .Produces<GameTurnResultDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> GetTownStoreOffersAsync(
        Guid gameSessionId,
        string townId,
        GetTownStoreOffersHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var storeOffers = await handler.HandleAsync(new GetTownStoreOffersQuery(gameSessionId, townId), cancellationToken);
            return Results.Ok(storeOffers);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
        catch (TownNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> BuyStoreItemAsync(
        Guid gameSessionId,
        string townId,
        BuyStoreItemRequest? request,
        PurchaseStoreItemHandler handler,
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
                new PurchaseStoreItemCommand(
                    gameSessionId,
                    townId,
                    validatedRequest.VendorType,
                    validatedRequest.ItemKind,
                    validatedRequest.Quantity),
                cancellationToken);

            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
        catch (TownNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
