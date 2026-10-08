using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Actions;

public sealed class ActionAvailabilityResolver
{
    public IReadOnlyList<AvailableAction> Resolve(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.IsSetupPhase)
        {
            return [];
        }

        var currentTown = session.World.GetTown(session.Player.CurrentTownId!.Value);
        var availableActions = new List<AvailableAction>
        {
            new(AvailableActionKind.Travel, "Travel"),
            new(AvailableActionKind.ViewMap, "View map"),
            new(AvailableActionKind.ViewJournal, "View journal"),
            new(AvailableActionKind.ReadWantedPosters, "Read wanted posters")
        };

        // Every town has a shop with prosperity-driven stock, so BuySupplies
        // is always available when not traveling.
        availableActions.Add(new AvailableAction(AvailableActionKind.BuySupplies, "Buy supplies"));

        availableActions.AddRange(session.CurrentTown.GetInvestigationActions());

        if (session.Journey is not null)
        {
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.Travel);
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.BuySupplies);
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.ReadWantedPosters);
            foreach (var source in session.CurrentTown.Sources.Definitions)
            {
                availableActions.RemoveAll(action => action.Kind == source.ActionKind);
            }
            if (session.Journey.PendingEncounter is not null)
            {
                availableActions.Add(new AvailableAction(AvailableActionKind.ResolveTravelEncounter, "Resolve travel encounter"));
            }
            else
            {
                availableActions.Add(new AvailableAction(AvailableActionKind.AdvanceTravelDay, "Advance travel day"));
            }

            return availableActions;
        }

        if (session.World.ListTrailsFromTown(currentTown.Id).Count == 0)
        {
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.Travel);
        }

        return availableActions;
    }
}
