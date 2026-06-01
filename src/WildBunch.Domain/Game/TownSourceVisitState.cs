using WildBunch.Domain.Cases;
using WildBunch.Domain.Actions;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

public enum TownSourceCheckOutcome
{
    FirstCheck = 0,
    RepeatNoNewInfo = 1
}

public sealed class TownSourceVisitState
{
    public TownSourceVisitState(
        TownId townId,
        InvestigationSourceKind sourceKind,
        TownSourceRefreshPolicy refreshPolicy,
        int lastRefreshedVisitNumber = 0,
        int lastCheckedVisitNumber = 0)
    {
        TownId = townId;
        SourceKind = sourceKind;
        RefreshPolicy = refreshPolicy;
        LastRefreshedVisitNumber = lastRefreshedVisitNumber;
        LastCheckedVisitNumber = lastCheckedVisitNumber;
    }

    public TownId TownId { get; }

    public InvestigationSourceKind SourceKind { get; }

    public TownSourceRefreshPolicy RefreshPolicy { get; }

    public int LastRefreshedVisitNumber { get; private set; }

    public int LastCheckedVisitNumber { get; private set; }

    public bool IsFreshForVisit(int visitNumber)
        => LastRefreshedVisitNumber >= visitNumber;

    public TownSourceCheckOutcome CheckForVisit(int visitNumber)
    {
        if (LastCheckedVisitNumber == visitNumber)
        {
            return TownSourceCheckOutcome.RepeatNoNewInfo;
        }

        LastCheckedVisitNumber = visitNumber;
        return TownSourceCheckOutcome.FirstCheck;
    }

    public void RefreshForVisit(int visitNumber)
    {
        switch (RefreshPolicy)
        {
            case TownSourceRefreshPolicy.PerVisit:
            case TownSourceRefreshPolicy.OnTownReturn:
                LastRefreshedVisitNumber = visitNumber;
                break;
            default:
                LastRefreshedVisitNumber = visitNumber;
                break;
        }
    }
}

public sealed class TownVisitTownState
{
    private readonly Dictionary<InvestigationSourceKind, TownSourceVisitState> _sourceStates = [];

    public TownVisitTownState(
        TownId townId,
        int visitNumber = 1,
        IEnumerable<TownSourceVisitState>? sourceStates = null,
        IEnumerable<InvestigationSourceKind>? spentInvestigationSources = null,
        bool wantedPostersSpent = false)
    {
        TownId = townId;
        VisitNumber = visitNumber < 1 ? 1 : visitNumber;

        if (sourceStates is not null)
        {
            foreach (var sourceState in sourceStates)
            {
                _sourceStates[sourceState.SourceKind] = sourceState;
            }
        }
        else if (spentInvestigationSources is not null)
        {
            foreach (var sourceKind in spentInvestigationSources.Distinct())
            {
                _sourceStates[sourceKind] = new TownSourceVisitState(
                    townId,
                    sourceKind,
                    TownSourceRefreshPolicy.PerVisit,
                    lastRefreshedVisitNumber: VisitNumber,
                    lastCheckedVisitNumber: VisitNumber);
            }
        }

        WantedPostersLastCheckedVisitNumber = wantedPostersSpent ? VisitNumber : 0;
    }

    public TownId TownId { get; }

    public int VisitNumber { get; private set; }

    public int WantedPostersLastCheckedVisitNumber { get; private set; }

    public IReadOnlyCollection<TownSourceVisitState> SourceStates => _sourceStates.Values.ToArray();

    public IReadOnlyCollection<InvestigationSourceKind> SpentInvestigationSources
        => _sourceStates.Values
            .Where(sourceState => sourceState.LastCheckedVisitNumber == VisitNumber)
            .Select(sourceState => sourceState.SourceKind)
            .ToArray();

    public bool WantedPostersSpent => WantedPostersLastCheckedVisitNumber == VisitNumber;

    public TownSourceCheckOutcome CheckSource(TownSourceDefinition sourceDefinition)
    {
        ArgumentNullException.ThrowIfNull(sourceDefinition);
        return CheckSource(sourceDefinition.Kind, sourceDefinition.RefreshPolicy);
    }

    public TownSourceCheckOutcome CheckSource(InvestigationSourceKind sourceKind, TownSourceRefreshPolicy refreshPolicy = TownSourceRefreshPolicy.PerVisit)
    {
        var sourceState = GetOrCreateSourceState(sourceKind, refreshPolicy);
        return sourceState.CheckForVisit(VisitNumber);
    }

    public bool TrySpend(TownSourceDefinition sourceDefinition)
        => CheckSource(sourceDefinition) == TownSourceCheckOutcome.FirstCheck;

    public bool TrySpend(InvestigationSourceKind sourceKind)
        => CheckSource(sourceKind) == TownSourceCheckOutcome.FirstCheck;

    public bool IsSpent(TownSourceDefinition sourceDefinition)
    {
        ArgumentNullException.ThrowIfNull(sourceDefinition);
        return IsSpent(sourceDefinition.Kind);
    }

    public bool IsSpent(InvestigationSourceKind sourceKind)
        => _sourceStates.TryGetValue(sourceKind, out var sourceState) && sourceState.LastCheckedVisitNumber == VisitNumber;

    public TownSourceCheckOutcome CheckWantedPosters()
    {
        if (WantedPostersLastCheckedVisitNumber == VisitNumber)
        {
            return TownSourceCheckOutcome.RepeatNoNewInfo;
        }

        WantedPostersLastCheckedVisitNumber = VisitNumber;
        return TownSourceCheckOutcome.FirstCheck;
    }

    public bool TrySpendWantedPosters()
        => CheckWantedPosters() == TownSourceCheckOutcome.FirstCheck;

    public void AdvanceVisit()
    {
        VisitNumber++;
    }

    public void RefreshSources(TownSourceCatalog sourceCatalog)
    {
        ArgumentNullException.ThrowIfNull(sourceCatalog);

        foreach (var definition in sourceCatalog.Definitions)
        {
            var sourceState = GetOrCreateSourceState(definition.Kind, definition.RefreshPolicy);
            sourceState.RefreshForVisit(VisitNumber);
        }
    }

    public bool TryGetSourceState(InvestigationSourceKind sourceKind, out TownSourceVisitState? sourceState)
        => _sourceStates.TryGetValue(sourceKind, out sourceState);

    private TownSourceVisitState GetOrCreateSourceState(InvestigationSourceKind sourceKind, TownSourceRefreshPolicy refreshPolicy)
    {
        if (_sourceStates.TryGetValue(sourceKind, out var sourceState))
        {
            return sourceState;
        }

        sourceState = new TownSourceVisitState(TownId, sourceKind, refreshPolicy);
        _sourceStates[sourceKind] = sourceState;
        return sourceState;
    }
}
