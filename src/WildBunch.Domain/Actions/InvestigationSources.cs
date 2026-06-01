using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Actions;

public static class InvestigationSources
{
    public static TownSourceCatalog Catalog => TownSourceCatalog.Default;

    public static IReadOnlyList<TownSourceDefinition> All => Catalog.Definitions;

    public static TownSourceDefinition NoticeBoard => Catalog.GetRequiredDefinition(InvestigationSourceKind.NoticeBoard);

    public static TownSourceDefinition SheriffRecords => Catalog.GetRequiredDefinition(InvestigationSourceKind.SheriffRecords);

    public static TownSourceDefinition TelegraphLead => Catalog.GetRequiredDefinition(InvestigationSourceKind.TelegraphLead);

    public static TownSourceDefinition LocalGossip => Catalog.GetRequiredDefinition(InvestigationSourceKind.LocalGossip);
}
