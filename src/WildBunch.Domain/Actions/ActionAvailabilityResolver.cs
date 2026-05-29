using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Actions;

public sealed class ActionAvailabilityResolver
{
    public IReadOnlyList<AvailableAction> Resolve(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var currentTown = session.World.GetTown(session.Player.CurrentTownId);
        var availableActions = new List<AvailableAction>
        {
            new(AvailableActionKind.Travel, "Travel"),
            new(AvailableActionKind.ViewMap, "View map"),
            new(AvailableActionKind.ViewJournal, "View journal")
        };

        if ((currentTown.Services & TownServices.Supplies) != 0)
        {
            availableActions.Add(new AvailableAction(AvailableActionKind.BuySupplies, "Buy supplies"));
        }

        if ((currentTown.Services & TownServices.Lodging) != 0)
        {
            availableActions.Add(new AvailableAction(AvailableActionKind.StayAtLodging, "Stay at lodging"));
        }

        if ((currentTown.Services & TownServices.Doctor) != 0)
        {
            availableActions.Add(new AvailableAction(AvailableActionKind.VisitDoctor, "Visit doctor"));
        }

        if ((currentTown.Services & TownServices.Telegraph) != 0)
        {
            availableActions.Add(new AvailableAction(AvailableActionKind.SendTelegram, "Send telegram"));
        }

        if (session.World.ListTrailsFromTown(currentTown.Id).Count == 0)
        {
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.Travel);
        }

        return availableActions;
    }
}
