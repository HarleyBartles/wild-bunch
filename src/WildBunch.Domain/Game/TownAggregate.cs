using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Session-owned town boundary that keeps the static town definition and the
/// visit-scoped town state together while <see cref="GameSession"/> stays the
/// live-play command route.
/// </summary>
public sealed class TownAggregate
{
    public TownAggregate(Town definition, TownVisitState visitState)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        VisitState = visitState ?? throw new ArgumentNullException(nameof(visitState));
    }

    public Town Definition { get; private set; }

    public TownVisitState VisitState { get; }

    public TownId TownId => Definition.Id;

    public string TownName => Definition.Name;

    public TownSourceCatalog Sources => TownSourceCatalog.Default;

    public IReadOnlyList<AvailableAction> GetInvestigationActions()
        => Sources.GetInvestigationActions();

    public bool IsAvailable(InvestigationSourceKind sourceKind)
        => Sources.IsAvailable(sourceKind);

    public TownSourceDefinition GetRequiredSourceDefinition(InvestigationSourceKind sourceKind)
        => Sources.GetRequiredDefinition(sourceKind);

    public TownSourceCheckOutcome CheckSource(InvestigationSourceKind sourceKind)
        => VisitState.CheckSource(GetRequiredSourceDefinition(sourceKind));

    public TownSourceCheckOutcome CheckSource(TownSourceDefinition sourceDefinition)
        => VisitState.CheckSource(sourceDefinition);

    public TownSourceCheckOutcome CheckWantedPosters()
        => VisitState.CheckWantedPosters();

    public void EnterTown(Town town)
    {
        Definition = town ?? throw new ArgumentNullException(nameof(town));
        VisitState.EnterTown(town.Id);
    }

    public void PrimeCurrentTown()
        => VisitState.PrimeCurrentTown();
}
