using Microsoft.AspNetCore.Http.HttpResults;
using WildBunch.Api.Games;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Api.Games.Validation;

public static class RequestValidation
{
    public static bool TryValidate(SetupGameRequest? request, out IResult? result)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null || string.IsNullOrWhiteSpace(request.PlayerName))
        {
            errors["playerName"] = ["Player name is required."];
        }

        if (!string.IsNullOrWhiteSpace(request?.SeedCode))
        {
            if (!StartingWorldDescriptorCodeValidator.TryValidate(request.SeedCode, out var errorMessage))
            {
                errors["seedCode"] = [errorMessage ?? "Seed code is invalid."];
            }
        }

        return WriteResult(errors, out result);
    }

    public static bool TryValidate(StartGameWithTownRequest? request, out IResult? result)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null || string.IsNullOrWhiteSpace(request.StartingTownId))
        {
            errors["startingTownId"] = ["Starting town id is required."];
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

    public static bool TryValidate(ResolveJourneyEncounterRequest? request, out IResult? result)
    {
        var errors = new Dictionary<string, string[]>();

        if (request is null || string.IsNullOrWhiteSpace(request.ChoiceId))
        {
            errors["choiceId"] = ["Choice id is required."];
        }

        return WriteResult(errors, out result);
    }

    public static bool TryValidateJournalPaging(int? skip, int? take, out IResult? result)
    {
        var errors = new Dictionary<string, string[]>();

        if (skip is < 0)
        {
            errors["skip"] = ["Skip must be at least 0."]; 
        }

        if (take is not null && take < 1)
        {
            errors["take"] = ["Take must be at least 1 when provided."];
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
