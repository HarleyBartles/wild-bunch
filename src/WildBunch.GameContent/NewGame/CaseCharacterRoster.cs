using WildBunch.Domain.Cases;

namespace WildBunch.GameContent.NewGame;

internal enum CaseRosterSourceCategory
{
    ButchCassidyWildBunch = 0,
    DoolinDaltonOklahombres = 1,
    FictionalEconomyWarrant = 2
}

internal sealed record CaseCharacterProfile(
    string Key,
    string DisplayName,
    IReadOnlyList<string> SourceAliases,
    IReadOnlyList<SuspectAlias> GameAliases,
    IReadOnlyList<string> CrimeTags,
    IReadOnlyList<string> ClueSourceTags,
    SuspectTraits Traits,
    CaseRosterSourceCategory SourceCategory,
    string SourceNote,
    bool IsGangEligible,
    bool IsTrueCulpritEligible,
    bool IsAssociatedCharacter,
    IReadOnlyList<OutlawGangId> GangAffiliations);

internal sealed record OutlawWarrantProfile(
    string Key,
    string TargetName,
    IReadOnlyList<string> SourceAliases,
    IReadOnlyList<string> KnownAliases,
    IReadOnlyList<string> KnownFeatures,
    string IssuingSource,
    CaseRosterSourceCategory SourceCategory,
    string SourceNote,
    WarrantDisposition Disposition,
    decimal BountyAmount,
    InvestigationTargetKind TargetKind,
    IReadOnlyList<OutlawGangId> GangAffiliations,
    OutlawGangId? AdvancesGangPressureFor);

internal static class CaseCharacterRoster
{
    private const string ButchCassidyWildBunchUrl = "https://en.wikipedia.org/wiki/Butch_Cassidy%27s_Wild_Bunch";
    private const string WildBunchUrl = "https://en.wikipedia.org/wiki/Wild_Bunch";

    private static readonly CaseCharacterProfile[] GangCandidates =
    [
        Gang(
            "butch-cassidy",
            "Butch Cassidy",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | founder / source identity",
            [],
            [new SuspectAlias("Grey Jay", AliasKind.Nickname), new SuspectAlias("J. Pike", AliasKind.FormerName)],
            ["train robbery", "bank robbery"],
            ["trail witness", "telegraph ledger"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate, SuspectTraitTags.Leader, SuspectTraitTags.WellKnown),
            isGangEligible: true,
            isTrueCulpritEligible: true,
            isAssociatedCharacter: false),
        Gang(
            "sundance-kid",
            "Sundance Kid",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | close companion from the source pool",
            [],
            [new SuspectAlias("M.K. Rook", AliasKind.KnownAs)],
            ["train robbery", "stagecoach robbery"],
            ["notice board", "sheriff record"],
            SuspectTraits.FromTags(SuspectTraitTags.GangLoyal, SuspectTraitTags.Rider, SuspectTraitTags.Cautious),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false),
        Gang(
            "elzy-lay",
            "Elzy Lay",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed outlaw companion",
            ["Lay"],
            [new SuspectAlias("Inkshot", AliasKind.Nickname), new SuspectAlias("E. Quill", AliasKind.FormerName)],
            ["train robbery", "horse theft"],
            ["waystation clerk", "rail ledger"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Lookout),
            isGangEligible: true,
            isTrueCulpritEligible: true,
            isAssociatedCharacter: false),
        Gang(
            "kid-curry",
            "Kid Curry",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed violent rider",
            ["George Curry", "Flat-Nose Curry"],
            [new SuspectAlias("Red Wren", AliasKind.Nickname), new SuspectAlias("Aunt Tess", AliasKind.KnownAs)],
            ["train robbery", "lawman killing"],
            ["trail witness", "poster sketch"],
            SuspectTraits.FromTags(SuspectTraitTags.Armed, SuspectTraitTags.Desperate, SuspectTraitTags.Violent, SuspectTraitTags.Enforcer),
            isGangEligible: true,
            isTrueCulpritEligible: true,
            isAssociatedCharacter: false),
        Gang(
            "laura-bullion",
            "Laura Bullion",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed gang associate",
            [],
            [new SuspectAlias("Hollow Boone", AliasKind.Nickname)],
            ["theft", "fraud"],
            ["station clerk", "ledger note"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Fence, SuspectTraitTags.Cautious),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false),
        Gang(
            "news-carver",
            "Will \"News\" Carver",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed gang member",
            ["Will Carver", "News Carver"],
            [new SuspectAlias("Cedar Vale", AliasKind.Nickname)],
            ["train robbery", "bank robbery"],
            ["notice board", "telegraph ledger"],
            SuspectTraits.FromTags(SuspectTraitTags.Desperate, SuspectTraitTags.Talkative, SuspectTraitTags.Lookout),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false),
        Gang(
            "camillo-hanks",
            "Camillo Hanks",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed gang member",
            ["Deaf Charley Hanks", "Camillo \"Deaf Charley\" Hanks"],
            [new SuspectAlias("O. Nash", AliasKind.FormerName)],
            ["train robbery", "store robbery"],
            ["waystation clerk", "sheriff record"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Desperate, SuspectTraitTags.GangLoyal, SuspectTraitTags.Violent),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false),
        Gang(
            "flat-nose-curry",
            "George Curry",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed gang member",
            ["Flat-Nose Curry", "Kid Curry"],
            [new SuspectAlias("The Magpie", AliasKind.Nickname), new SuspectAlias("R. Pike", AliasKind.FormerName)],
            ["train robbery", "lawman killing"],
            ["notice board", "marshal report"],
            SuspectTraits.FromTags(SuspectTraitTags.Armed, SuspectTraitTags.Desperate, SuspectTraitTags.Violent, SuspectTraitTags.Rider),
            isGangEligible: true,
            isTrueCulpritEligible: true,
            isAssociatedCharacter: false),
        Gang(
            "bub-meeks",
            "Bub Meeks",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed gang member",
            [],
            [new SuspectAlias("Aunt Tess", AliasKind.KnownAs)],
            ["robbery", "horse theft"],
            ["trail witness", "rail ledger"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Cautious, SuspectTraitTags.Fence),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false),
        Gang(
            "bill-doolin",
            "Bill Doolin",
            CaseRosterSourceCategory.DoolinDaltonOklahombres,
            $"{WildBunchUrl} | source-listed gang leader",
            [],
            [new SuspectAlias("Grey Jay", AliasKind.Nickname), new SuspectAlias("J. Pike", AliasKind.FormerName)],
            ["train robbery", "bank robbery"],
            ["trail witness", "telegraph ledger"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate, SuspectTraitTags.Leader, SuspectTraitTags.GangLoyal),
            isGangEligible: true,
            isTrueCulpritEligible: true,
            isAssociatedCharacter: false),
        Gang(
            "bill-dalton",
            "Bill Dalton",
            CaseRosterSourceCategory.DoolinDaltonOklahombres,
            $"{WildBunchUrl} | source-listed gang founder",
            ["William Marion Dalton"],
            [new SuspectAlias("M.K. Rook", AliasKind.KnownAs)],
            ["train robbery", "bank robbery"],
            ["notice board", "sheriff record"],
            SuspectTraits.FromTags(SuspectTraitTags.GangLoyal, SuspectTraitTags.Cautious, SuspectTraitTags.Talkative),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false),
        Gang(
            "dynamite-dick-clifton",
            "Dan Clifton",
            CaseRosterSourceCategory.DoolinDaltonOklahombres,
            $"{WildBunchUrl} | source-listed gang member",
            ["Dynamite Dick Clifton"],
            [new SuspectAlias("Inkshot", AliasKind.Nickname), new SuspectAlias("E. Quill", AliasKind.FormerName)],
            ["train robbery", "store robbery"],
            ["waystation clerk", "rail ledger"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Enforcer, SuspectTraitTags.Tenacious),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false),
        Gang(
            "roy-daugherty",
            "Roy Daugherty",
            CaseRosterSourceCategory.DoolinDaltonOklahombres,
            $"{WildBunchUrl} | source-listed gang member",
            ["Arkansas Tom Jones"],
            [new SuspectAlias("Red Wren", AliasKind.Nickname), new SuspectAlias("Aunt Tess", AliasKind.KnownAs)],
            ["train robbery", "lawman killing"],
            ["trail witness", "poster sketch"],
            SuspectTraits.FromTags(SuspectTraitTags.Armed, SuspectTraitTags.Desperate, SuspectTraitTags.Violent, SuspectTraitTags.Rider),
            isGangEligible: true,
            isTrueCulpritEligible: true,
            isAssociatedCharacter: false),
        Gang(
            "george-newcomb",
            "George Newcomb",
            CaseRosterSourceCategory.DoolinDaltonOklahombres,
            $"{WildBunchUrl} | source-listed gang member",
            ["Bitter Creek", "Slaughter Kid"],
            [new SuspectAlias("Hollow Boone", AliasKind.Nickname)],
            ["bank robbery", "train robbery"],
            ["station clerk", "ledger note"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Lookout, SuspectTraitTags.Tenacious),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false),
        Gang(
            "charley-pierce",
            "Charley Pierce",
            CaseRosterSourceCategory.DoolinDaltonOklahombres,
            $"{WildBunchUrl} | source-listed gang member",
            [],
            [new SuspectAlias("Cedar Vale", AliasKind.Nickname)],
            ["store robbery", "horse theft"],
            ["notice board", "telegraph ledger"],
            SuspectTraits.FromTags(SuspectTraitTags.Desperate, SuspectTraitTags.Talkative, SuspectTraitTags.Cautious),
            isGangEligible: true,
            isTrueCulpritEligible: false,
            isAssociatedCharacter: false)
    ];

    private static readonly CaseCharacterProfile[] AssociatedCharacters =
    [
        Orbit(
            "ann-bassett",
            "Ann Bassett",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed allied rancher",
            ["Bassett"],
            [new SuspectAlias("Ann Bassett", AliasKind.KnownAs)],
            ["rancher", "fence contact"],
            ["ranch ledger", "trail witness"],
            SuspectTraits.FromTags(SuspectTraitTags.Fence, SuspectTraitTags.Bribeable, SuspectTraitTags.Cautious)),
        Orbit(
            "josie-bassett",
            "Josie Bassett",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed allied rancher",
            ["Bassett"],
            [new SuspectAlias("Josie Bassett", AliasKind.KnownAs)],
            ["rancher", "fence contact"],
            ["ranch ledger", "trail witness"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Fence, SuspectTraitTags.LocalToTurf)),
        Orbit(
            "etta-place",
            "Etta Place",
            CaseRosterSourceCategory.ButchCassidyWildBunch,
            $"{ButchCassidyWildBunchUrl} | source-listed companion",
            [],
            [new SuspectAlias("Etta Place", AliasKind.KnownAs)],
            ["traveler", "companion"],
            ["station clerk", "hotel register"],
            SuspectTraits.FromTags(SuspectTraitTags.Talkative, SuspectTraitTags.Cautious, SuspectTraitTags.WellKnown)),
        Orbit(
            "ed-nix",
            "E.D. Nix",
            CaseRosterSourceCategory.DoolinDaltonOklahombres,
            $"{WildBunchUrl} | source-listed marshal",
            [],
            [new SuspectAlias("E.D. Nix", AliasKind.KnownAs)],
            ["marshal", "lawman"],
            ["sheriff record", "notice board"],
            SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Unbribeable, SuspectTraitTags.Cautious))
    ];

    private static readonly OutlawWarrantProfile[] UnrelatedWantedCriminals =
    [
        Wanted(
            "reno-pike",
            "Reno Pike",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["The Magpie", "R. Pike"],
            ["Mismatched spurs", "Black felt hat"],
            "Silver Creek Sheriff",
            WarrantDisposition.AliveOnly,
            300m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "maddox-vale",
            "Maddox Vale",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Dust Kite", "M. Vale"],
            ["White dust coat", "Split spur strap"],
            "Red Mesa Marshal",
            WarrantDisposition.AliveOnly,
            225m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "ivy-calder",
            "Ivy Calder",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Calico Ivy", "I. Calder"],
            ["Needle scar on right hand", "Blue scarf"],
            "Pinecross Deputy",
            WarrantDisposition.DeadOrAlive,
            175m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "harlan-bowe",
            "Harlan Bowe",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Copper Bowe", "H. Bowe"],
            ["Bent-brim hat", "Copper ring"],
            "Holloway Sheriff",
            WarrantDisposition.AliveOnly,
            260m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "nell-vera",
            "Nell Vera",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Nell V.", "Sky Nell"],
            ["Hickory braid", "Brown gloves"],
            "Sagewell Clerk",
            WarrantDisposition.AliveOnly,
            190m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "oscar-holt",
            "Oscar Holt",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["O. Holt", "The Sawtooth"],
            ["Sawtooth scar", "Gray duster"],
            "Emberfall Marshal",
            WarrantDisposition.DeadOrAlive,
            340m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null)
    ];

    public static IReadOnlyList<CaseCharacterProfile> GangCandidatePool => GangCandidates;

    public static IReadOnlyList<CaseCharacterProfile> AssociatedCharacterPool => AssociatedCharacters;

    public static IReadOnlyList<OutlawWarrantProfile> UnrelatedWantedCriminalPool => UnrelatedWantedCriminals;

    public static IReadOnlyList<CaseCharacterProfile> SelectCanonicalGangRoster()
        => new[]
        {
            GetGangCandidate("butch-cassidy"),
            GetGangCandidate("sundance-kid"),
            GetGangCandidate("elzy-lay"),
            GetGangCandidate("kid-curry"),
            GetGangCandidate("laura-bullion"),
            GetGangCandidate("bill-doolin"),
            GetGangCandidate("roy-daugherty")
        };

    public static CaseCharacterProfile SelectCanonicalCulprit()
        => GetGangCandidate("kid-curry");

    public static IReadOnlyList<CaseCharacterProfile> SelectGangRoster(GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var culprit = SelectByScore(source, "case.roster.culprit", GangCandidates.Where(candidate => candidate.IsTrueCulpritEligible), 1, candidate => candidate.Key).Single();
        var support = SelectByScore(source, "case.roster.support", GangCandidates.Where(candidate => candidate.IsGangEligible && candidate.Key != culprit.Key), 6, candidate => candidate.Key);

        return new[]
        {
            support[0],
            support[1],
            support[2],
            culprit,
            support[3],
            support[4],
            support[5]
        };
    }

    public static OutlawWarrantProfile SelectUnrelatedWarrant(GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return SelectByScore(source, "case.roster.unrelated-warrant", UnrelatedWantedCriminals, 1, warrant => warrant.Key).Single();
    }

    public static OutlawWarrantProfile CreateTrueCulpritWarrant(CaseCharacterProfile culprit)
    {
        ArgumentNullException.ThrowIfNull(culprit);

        return new OutlawWarrantProfile(
            $"warrant-{culprit.Key}",
            culprit.DisplayName,
            culprit.SourceAliases,
            ["Red Wren", "Aunt Tess"],
            ["Pale scar across the left cheek", "Raven-feather pin"],
            "Dodge City Marshal",
            culprit.SourceCategory,
            $"Source-derived culprit warrant built from {culprit.SourceNote}.",
            WarrantDisposition.DeadOrAlive,
            2500m,
            InvestigationTargetKind.TrueCulprit,
            [OutlawGangIds.WildBunch],
            OutlawGangIds.WildBunch);
    }

    public static OutlawWarrantProfile CreateCanonicalTrueCulpritWarrant()
        => CreateTrueCulpritWarrant(SelectCanonicalCulprit());

    public static OutlawWarrantProfile CreateCanonicalUnrelatedWarrant()
        => UnrelatedWantedCriminals[0];

    public static IReadOnlyList<OutlawWarrantProfile> SelectUnrelatedWantedCriminals(GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new[] { SelectUnrelatedWarrant(source) };
    }

    private static CaseCharacterProfile GetGangCandidate(string key)
        => GangCandidates.Single(candidate => candidate.Key == key);

    private static IReadOnlyList<T> SelectByScore<T>(
        GameSetupDeterministicSource source,
        string label,
        IEnumerable<T> entries,
        int count,
        Func<T, string> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(keySelector);

        return entries
            .Select(entry => new
            {
                Entry = entry,
                Score = source.Roll($"{label}.{keySelector(entry)}")
            })
            .OrderBy(candidate => candidate.Score)
            .ThenBy(candidate => keySelector(candidate.Entry), StringComparer.Ordinal)
            .Take(count)
            .Select(candidate => candidate.Entry)
            .ToArray();
    }

    private static CaseCharacterProfile Gang(
        string key,
        string displayName,
        CaseRosterSourceCategory sourceCategory,
        string sourceNote,
        IReadOnlyList<string> sourceAliases,
        IReadOnlyList<SuspectAlias> aliases,
        IReadOnlyList<string> crimeTags,
        IReadOnlyList<string> clueSourceTags,
        SuspectTraits traits,
        bool isGangEligible,
        bool isTrueCulpritEligible,
        bool isAssociatedCharacter)
        => new(
            key,
            displayName,
            sourceAliases,
            aliases,
            crimeTags,
            clueSourceTags,
            traits,
            sourceCategory,
            sourceNote,
            isGangEligible,
            isTrueCulpritEligible,
            isAssociatedCharacter,
            [OutlawGangIds.WildBunch]);

    private static CaseCharacterProfile Orbit(
        string key,
        string displayName,
        CaseRosterSourceCategory sourceCategory,
        string sourceNote,
        IReadOnlyList<string> sourceAliases,
        IReadOnlyList<SuspectAlias> aliases,
        IReadOnlyList<string> crimeTags,
        IReadOnlyList<string> clueSourceTags,
        SuspectTraits traits)
        => new(
            key,
            displayName,
            sourceAliases,
            aliases,
            crimeTags,
            clueSourceTags,
            traits,
            sourceCategory,
            sourceNote,
            false,
            false,
            true,
            Array.Empty<OutlawGangId>());

    private static OutlawWarrantProfile Wanted(
        string key,
        string targetName,
        CaseRosterSourceCategory sourceCategory,
        string sourceNote,
        IReadOnlyList<string> sourceAliases,
        IReadOnlyList<string> knownAliases,
        string issuingSource,
        WarrantDisposition disposition,
        decimal bountyAmount,
        InvestigationTargetKind targetKind,
        IReadOnlyList<OutlawGangId> gangAffiliations,
        OutlawGangId? advancesGangPressureFor,
        IReadOnlyList<string>? knownFeatures = null)
        => new(
            key,
            targetName,
            sourceAliases,
            knownAliases,
            knownFeatures ?? ["Mismatched spurs", "Black felt hat"],
            issuingSource,
            sourceCategory,
            sourceNote,
            disposition,
            bountyAmount,
            targetKind,
            gangAffiliations,
            advancesGangPressureFor);
}
