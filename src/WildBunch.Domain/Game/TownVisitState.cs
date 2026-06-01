using WildBunch.Domain.Cases;
using WildBunch.Domain.Actions;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

public sealed class TownVisitState
{
    private readonly Dictionary<TownId, TownVisitTownState> _townStates = [];

    public TownVisitState(TownId currentTownId)
    {
        CurrentTownId = currentTownId;
        GetOrCreateTownState(currentTownId, initialVisitNumber: 1);
    }

    public TownId CurrentTownId { get; private set; }

    public TownId TownId => CurrentTownId;

    public TownVisitTownState CurrentTownState => GetOrCreateTownState(CurrentTownId);

    public IReadOnlyCollection<TownVisitTownState> TownStates => _townStates.Values.ToArray();

    public IReadOnlyCollection<InvestigationSourceKind> SpentInvestigationSources => CurrentTownState.SpentInvestigationSources;

    public bool WantedPostersSpent => CurrentTownState.WantedPostersSpent;

    public TownSourceCheckOutcome CheckSource(InvestigationSourceKind sourceKind)
        => CurrentTownState.CheckSource(sourceKind);

    public TownSourceCheckOutcome CheckSource(TownSourceDefinition sourceDefinition)
        => CurrentTownState.CheckSource(sourceDefinition);

    public bool TrySpend(InvestigationSourceKind sourceKind)
        => CheckSource(sourceKind) == TownSourceCheckOutcome.FirstCheck;

    public bool IsSpent(InvestigationSourceKind sourceKind)
        => CurrentTownState.IsSpent(sourceKind);

    public bool IsSpent(TownSourceDefinition sourceDefinition)
        => CurrentTownState.IsSpent(sourceDefinition);

    public TownSourceCheckOutcome CheckWantedPosters()
        => CurrentTownState.CheckWantedPosters();

    public bool TrySpendWantedPosters()
        => CheckWantedPosters() == TownSourceCheckOutcome.FirstCheck;

    public void Reset(TownId townId)
        => EnterTown(townId);

    public void EnterTown(TownId townId, TownSourceCatalog? sourceCatalog = null)
    {
        CurrentTownId = townId;
        var townState = GetOrCreateTownState(townId);
        townState.AdvanceVisit();

        if (sourceCatalog is not null)
        {
            townState.RefreshSources(sourceCatalog);
        }
    }

    public void PrimeCurrentTown(TownSourceCatalog sourceCatalog)
    {
        ArgumentNullException.ThrowIfNull(sourceCatalog);
        CurrentTownState.RefreshSources(sourceCatalog);
    }

    public bool TryGetTownState(TownId townId, out TownVisitTownState? townState)
        => _townStates.TryGetValue(townId, out townState);

    public static TownVisitState FromLegacy(
        TownId currentTownId,
        IEnumerable<InvestigationSourceKind>? spentInvestigationSources,
        bool wantedPostersSpent)
    {
        var state = new TownVisitState(currentTownId);
        state._townStates[currentTownId] = new TownVisitTownState(
            currentTownId,
            visitNumber: 1,
            spentInvestigationSources: spentInvestigationSources,
            wantedPostersSpent: wantedPostersSpent);
        return state;
    }

    public static TownVisitState FromTownStates(TownId currentTownId, IEnumerable<TownVisitTownState> townStates)
    {
        ArgumentNullException.ThrowIfNull(townStates);

        var state = new TownVisitState(currentTownId);
        state._townStates.Clear();
        foreach (var townState in townStates)
        {
            state._townStates[townState.TownId] = townState;
        }

        state.GetOrCreateTownState(currentTownId, initialVisitNumber: 1);
        return state;
    }

    private TownVisitTownState GetOrCreateTownState(TownId townId, int initialVisitNumber = 0)
    {
        if (_townStates.TryGetValue(townId, out var townState))
        {
            return townState;
        }

        townState = new TownVisitTownState(townId, visitNumber: initialVisitNumber);
        _townStates[townId] = townState;
        return townState;
    }
}
