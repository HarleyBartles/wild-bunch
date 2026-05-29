using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;

namespace WildBunch.Domain.Travel;

public sealed record TravelResult(bool Success, string Message, string LogMessage, int HeatIncrease)
{
    public static TravelResult Failed(string message) => new(false, message, message, 0);

    public static TravelResult Succeeded(string message, string logMessage, int heatIncrease)
        => new(true, message, logMessage, heatIncrease);
}

public sealed class TravelResolver
{
    public TravelResult Travel(
        DomainWorld world,
        TownId currentTownId,
        TownId destinationTownId)
    {
        ArgumentNullException.ThrowIfNull(world);

        var trail = world.FindConnectedTrail(currentTownId, destinationTownId);

        if (trail is null)
        {
            return TravelResult.Failed("No trail connects those towns.");
        }

        var heatIncrease = Math.Max(1, (int)trail.Risk);
        return TravelResult.Succeeded(
            $"Travelled to {destinationTownId.Value}.",
            $"You travel from {currentTownId.Value} to {destinationTownId.Value}.",
            heatIncrease);
    }
}
