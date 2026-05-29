using Microsoft.AspNetCore.Http.HttpResults;
using WildBunch.Api.Games;

namespace WildBunch.Api.Games.Validation;

public static class RequestValidation
{
    public static bool TryValidate(StartGameRequest? request, out IResult? result)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null || string.IsNullOrWhiteSpace(request.PlayerName))
        {
            errors["playerName"] = ["Player name is required."];
        }

        return WriteResult(errors, out result);
    }

    public static bool TryValidate(TravelRequest? request, out IResult? result)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null || string.IsNullOrWhiteSpace(request.DestinationTownId))
        {
            errors["destinationTownId"] = ["Destination town id is required."];
        }

        return WriteResult(errors, out result);
    }

    public static bool TryValidate(BuyStoreItemRequest? request, out IResult? result)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null || request.VendorType is null)
        {
            errors["vendorType"] = ["Vendor type is required."];
        }

        if (request is null || request.ItemKind is null)
        {
            errors["itemKind"] = ["Item kind is required."];
        }

        if (request is null || request.Quantity < 1)
        {
            errors["quantity"] = ["Quantity must be at least 1."];
        }

        return WriteResult(errors, out result);
    }

    private static bool WriteResult(Dictionary<string, string[]> errors, out IResult? result)
    {
        if (errors.Count == 0)
        {
            result = null;
            return true;
        }

        result = Results.ValidationProblem(errors);
        return false;
    }
}
