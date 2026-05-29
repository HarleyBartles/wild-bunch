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
