namespace WildBunch.Domain.Cases;

public readonly record struct WarrantId(string Value);

public enum InvestigationTargetKind
{
    Unknown = 0,
    Suspected = 1,
    GangMember = 2,
    TrueCulprit = 3,
    UnrelatedWantedCriminal = 4
}

public enum WarrantDisposition
{
    AliveOnly = 0,
    DeadOrAlive = 1
}

public sealed record WarrantTerms
{
    public WarrantTerms(
        WarrantDisposition disposition,
        decimal bountyAmount,
        IEnumerable<string> knownAliases,
        IEnumerable<string> knownFeatures,
        string issuingSource,
        InvestigationTargetKind targetKind,
        IEnumerable<OutlawGangId> gangAffiliations,
        OutlawGangId? advancesGangPressureFor,
        InvestigationSourceKind? sourceKind = null)
    {
        ArgumentNullException.ThrowIfNull(knownAliases);
        ArgumentNullException.ThrowIfNull(knownFeatures);
        ArgumentNullException.ThrowIfNull(gangAffiliations);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuingSource);

        Disposition = disposition;
        BountyAmount = bountyAmount;
        KnownAliases = knownAliases.Where(alias => !string.IsNullOrWhiteSpace(alias)).Select(alias => alias.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        KnownFeatures = knownFeatures.Where(feature => !string.IsNullOrWhiteSpace(feature)).Select(feature => feature.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        IssuingSource = issuingSource.Trim();
        TargetKind = targetKind;
        GangAffiliations = gangAffiliations.DistinctBy(gang => gang.Value).ToArray();
        AdvancesGangPressureFor = advancesGangPressureFor;
        SourceKind = sourceKind;
    }

    public WarrantDisposition Disposition { get; }

    public decimal BountyAmount { get; }

    public IReadOnlyList<string> KnownAliases { get; }

    public IReadOnlyList<string> KnownFeatures { get; }

    public string IssuingSource { get; }

    public InvestigationTargetKind TargetKind { get; }

    public IReadOnlyList<OutlawGangId> GangAffiliations { get; }

    public OutlawGangId? AdvancesGangPressureFor { get; }

    public InvestigationSourceKind? SourceKind { get; }
}

public sealed record Warrant
{
    public Warrant(WarrantId id, string targetName, WarrantTerms terms)
        : this(id, targetName, terms, string.Empty)
    {
    }

    public Warrant(WarrantId id, string targetName, WarrantTerms terms, string summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetName);
        ArgumentNullException.ThrowIfNull(terms);

        Id = id;
        TargetName = targetName.Trim();
        Terms = terms;
        Summary = summary?.Trim() ?? string.Empty;
    }

    public WarrantId Id { get; }

    public string TargetName { get; }

    public WarrantTerms Terms { get; }

    public string Summary { get; }
}
