using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.World;

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
    TownSourceLocality Locality,
    TownSourceRefreshPolicy RefreshPolicy);

public sealed record TownSourceCatalog(IReadOnlyList<TownSourceDefinition> Definitions)
{
    public static TownSourceCatalog Default { get; } = new(
        [
            new TownSourceDefinition(
                "town-source.notice-board",
                InvestigationSourceKind.NoticeBoard,
                AvailableActionKind.InspectNoticeBoard,
                "Inspect notice board",
                TownSourceLocality.TownLocal,
                TownSourceRefreshPolicy.PerVisit),
            new TownSourceDefinition(
                "town-source.local-records",
                InvestigationSourceKind.LocalRecords,
                AvailableActionKind.CheckSheriffRecords,
                "Check local records",
                TownSourceLocality.TownLocal,
                TownSourceRefreshPolicy.PerVisit),
            new TownSourceDefinition(
                "town-source.local-gossip",
                InvestigationSourceKind.LocalGossip,
                AvailableActionKind.GatherLocalGossip,
                "Gather local gossip",
                TownSourceLocality.TownLocal,
                TownSourceRefreshPolicy.PerVisit),
            new TownSourceDefinition(
                "town-source.saloon-look-around",
                InvestigationSourceKind.SaloonLookAround,
                AvailableActionKind.LookAroundSaloon,
                "Look around saloon",
                TownSourceLocality.TownLocal,
                TownSourceRefreshPolicy.PerVisit),
        ]);

    public TownSourceDefinition GetRequiredDefinition(InvestigationSourceKind kind)
        => Definitions.Single(definition => definition.Kind == kind);

    public bool IsAvailable(InvestigationSourceKind kind)
        => Definitions.Any(definition => definition.Kind == kind);

    public IReadOnlyList<AvailableAction> GetInvestigationActions()
        => Definitions
            .Select(definition => new AvailableAction(definition.ActionKind, definition.Label))
            .ToArray();
}
