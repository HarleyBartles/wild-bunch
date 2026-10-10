using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Actions;

public sealed class ActionAvailabilityResolver
{
    public IReadOnlyList<AvailableAction> Resolve(ActionAvailabilityContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.StartFlowPhase < StartFlowPhase.GameStarted)
        {
            return [];
        }

        var currentTownId = context.CurrentTownId
            ?? throw new InvalidOperationException("A started game must have a current town before actions are available.");
        var currentTown = context.World.GetTown(currentTownId);
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

        availableActions.AddRange(TownSourceCatalog.Default.GetInvestigationActions());

        if (context.Journey is { } journey)
        {
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.Travel);
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.BuySupplies);
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.ReadWantedPosters);
            foreach (var source in TownSourceCatalog.Default.Definitions)
            {
                availableActions.RemoveAll(action => action.Kind == source.ActionKind);
            }
            if (journey.Status == JourneyStatus.Completed)
            {
                return availableActions;
            }

            if (journey.PendingEncounter is not null)
            {
                availableActions.Add(new AvailableAction(AvailableActionKind.ResolveTravelEncounter, "Resolve travel encounter"));
            }
            else
            {
                availableActions.Add(new AvailableAction(AvailableActionKind.AdvanceTravelDay, "Advance travel day"));
            }

            return availableActions;
        }

        if (context.World.ListTrailsFromTown(currentTown.Id).Count == 0)
        {
            availableActions.RemoveAll(action => action.Kind == AvailableActionKind.Travel);
        }

        return availableActions;
    }
}
