using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Actions;

public static class InvestigationSources
{
    public static TownSourceCatalog Catalog => TownSourceCatalog.Default;

    public static IReadOnlyList<TownSourceDefinition> All => Catalog.Definitions;

    public static TownSourceDefinition LocalGossip => Catalog.GetRequiredDefinition(InvestigationSourceKind.LocalGossip);

    public static TownSourceDefinition SaloonLookAround => Catalog.GetRequiredDefinition(InvestigationSourceKind.SaloonLookAround);
}
