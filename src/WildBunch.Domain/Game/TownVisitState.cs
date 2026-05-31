using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

public sealed class TownVisitState
{
    private readonly HashSet<InvestigationSourceKind> _spentInvestigationSources = [];

    public TownVisitState(
        TownId townId,
        IEnumerable<InvestigationSourceKind>? spentInvestigationSources = null,
        bool wantedPostersSpent = false)
    {
        TownId = townId;
        if (spentInvestigationSources is not null)
        {
            _spentInvestigationSources.UnionWith(spentInvestigationSources);
        }

        WantedPostersSpent = wantedPostersSpent;
    }

    public TownId TownId { get; private set; }

    public IReadOnlyCollection<InvestigationSourceKind> SpentInvestigationSources => _spentInvestigationSources;

    public bool WantedPostersSpent { get; private set; }

    public bool IsSpent(InvestigationSourceKind sourceKind)
        => _spentInvestigationSources.Contains(sourceKind);

    public bool TrySpend(InvestigationSourceKind sourceKind)
        => _spentInvestigationSources.Add(sourceKind);

    public bool TrySpendWantedPosters()
    {
        if (WantedPostersSpent)
        {
            return false;
        }

        WantedPostersSpent = true;
        return true;
    }

    public void Reset(TownId townId)
    {
        TownId = townId;
        _spentInvestigationSources.Clear();
        WantedPostersSpent = false;
    }
}
