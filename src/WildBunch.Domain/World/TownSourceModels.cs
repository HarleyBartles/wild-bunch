using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.World;

public enum TownSourceAvailability
{
    Baseline = 0,
    Conditional = 1
}

public enum TownSourceLocality
{
    TownLocal = 0,
    TownWide = 1,
    Regional = 2,
    Distant = 3
}

public enum TownSourceRefreshPolicy
{
    PerVisit = 0,
    OnTownReturn = 1
}

public sealed record TownSourceDefinition(
    string Id,
    InvestigationSourceKind Kind,
    AvailableActionKind ActionKind,
    string Label,
    TownSourceAvailability Availability,
    TownServices RequiredServices,
    TownSourceLocality Locality,
    TownSourceRefreshPolicy RefreshPolicy)
{
    public bool IsAvailableFor(TownServices townServices)
        => Availability == TownSourceAvailability.Baseline
            || (townServices & RequiredServices) == RequiredServices;
}

public sealed record TownSourceCatalog(IReadOnlyList<TownSourceDefinition> Definitions)
{
    public static TownSourceCatalog Default { get; } = new(
        [
            new TownSourceDefinition(
                "town-source.notice-board",
                InvestigationSourceKind.NoticeBoard,
                AvailableActionKind.InspectNoticeBoard,
                "Inspect notice board",
                TownSourceAvailability.Baseline,
                TownServices.None,
                TownSourceLocality.TownLocal,
                TownSourceRefreshPolicy.PerVisit),
            new TownSourceDefinition(
                "town-source.local-records",
                InvestigationSourceKind.LocalRecords,
                AvailableActionKind.CheckSheriffRecords,
                "Check local records",
                TownSourceAvailability.Baseline,
                TownServices.None,
                TownSourceLocality.TownLocal,
                TownSourceRefreshPolicy.PerVisit),
            new TownSourceDefinition(
                "town-source.local-gossip",
                InvestigationSourceKind.LocalGossip,
                AvailableActionKind.GatherLocalGossip,
                "Gather local gossip",
                TownSourceAvailability.Baseline,
                TownServices.None,
                TownSourceLocality.TownLocal,
                TownSourceRefreshPolicy.PerVisit),
            new TownSourceDefinition(
                "town-source.saloon-look-around",
                InvestigationSourceKind.SaloonLookAround,
                AvailableActionKind.LookAroundSaloon,
                "Look around saloon",
                TownSourceAvailability.Conditional,
                TownServices.Saloon,
                TownSourceLocality.TownLocal,
                TownSourceRefreshPolicy.PerVisit),
            new TownSourceDefinition(
                "town-source.telegraph-leads",
                InvestigationSourceKind.TelegraphLead,
                AvailableActionKind.FollowTelegraphLeads,
                "Follow telegraph leads",
                TownSourceAvailability.Conditional,
                TownServices.Telegraph,
                TownSourceLocality.Distant,
                TownSourceRefreshPolicy.PerVisit)
        ]);

    public TownSourceDefinition GetRequiredDefinition(InvestigationSourceKind kind)
        => Definitions.Single(definition => definition.Kind == kind);

    public bool IsAvailable(InvestigationSourceKind kind, TownServices townServices)
        => GetRequiredDefinition(kind).IsAvailableFor(townServices);

    public IReadOnlyList<AvailableAction> GetInvestigationActions(TownServices townServices)
        => Definitions
            .Where(definition => definition.IsAvailableFor(townServices))
            .Select(definition => new AvailableAction(definition.ActionKind, definition.Label))
            .ToArray();

    public TownSourceCatalog WithDefinitions(IEnumerable<TownSourceDefinition> definitions)
        => new(definitions.ToArray());
}
