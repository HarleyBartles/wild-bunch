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
            isTrueCulpritEligible: true,
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
            isTrueCulpritEligible: true,
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
            isTrueCulpritEligible: true,
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
            isTrueCulpritEligible: true,
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
            isTrueCulpritEligible: true,
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
            isTrueCulpritEligible: true,
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
            isTrueCulpritEligible: true,
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
            isTrueCulpritEligible: true,
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
            isTrueCulpritEligible: true,
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
            null),
        Wanted(
            "cole-rance",
            "Cole Rance",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["The Drifter", "C. Rance"],
            ["Tobacco-stained vest", "Notched left ear"],
            "Dustwell Sheriff",
            WarrantDisposition.DeadOrAlive,
            280m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "mira-ash",
            "Mira Ash",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Ash Mira", "M. Ash"],
            ["Burn scar on left wrist", "Green bandana"],
            "Cottonwood Marshal",
            WarrantDisposition.AliveOnly,
            210m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "tobias-rudd",
            "Tobias Rudd",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["T. Rudd", "Ruddy Tob"],
            ["Missing left thumb", "Canvas duster"],
            "Iron Springs Deputy",
            WarrantDisposition.DeadOrAlive,
            245m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "cora-dell",
            "Cora Dell",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Dell Cora", "C. Dell"],
            ["Silver locket", "Frayged left cuff"],
            "Pinecross Sheriff",
            WarrantDisposition.AliveOnly,
            195m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "silas-marsh",
            "Silas Marsh",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["S. Marsh", "The Reed"],
            ["Limp on right leg", "Oil-stained gloves"],
            "Red Mesa Sheriff",
            WarrantDisposition.DeadOrAlive,
            320m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "delia-wren",
            "Delia Wren",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Wren Delia", "D. Wren"],
            ["Feather earring on right ear", "Calico blouse"],
            "Sagewell Marshal",
            WarrantDisposition.AliveOnly,
            205m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "ezra-quill",
            "Ezra Quill",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["E. Quill", "Quill Ez"],
            ["Ink stain on right hand", "Wire-rim spectacles"],
            "Holloway Deputy",
            WarrantDisposition.DeadOrAlive,
            270m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "rosa-vane",
            "Rosa Vane",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Vane Rosa", "R. Vane"],
            ["Red hair ribbon", "Scar across right brow"],
            "Silver Creek Marshal",
            WarrantDisposition.AliveOnly,
            230m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "gideon-fay",
            "Gideon Fay",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["G. Fay", "Fay Gid"],
            ["Broken nose", "Brass belt buckle"],
            "Emberfall Sheriff",
            WarrantDisposition.DeadOrAlive,
            255m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "lila-brent",
            "Lila Brent",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Brent Lila", "L. Brent"],
            ["Moth-eaten shawl", "Chipped front tooth"],
            "Dustwell Deputy",
            WarrantDisposition.AliveOnly,
            185m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "amos-tye",
            "Amos Tye",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["A. Tye", "Tye Am"],
            ["Patch over left eye", "Carved wooden pipe"],
            "Cottonwood Sheriff",
            WarrantDisposition.DeadOrAlive,
            295m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "pearl-hask",
            "Pearl Hask",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["Hask Pearl", "P. Hask"],
            ["Pearl-handled revolver", "Dusty blue bonnet"],
            "Iron Springs Marshal",
            WarrantDisposition.AliveOnly,
            215m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "virgil-cole",
            "Virgil Cole",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["V. Cole", "Cole Virg"],
            ["Long gray beard", "Buckskin vest"],
            "Pinecross Marshal",
            WarrantDisposition.DeadOrAlive,
            310m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "etta-quin",
            "Etta Quin",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["E. Quin", "Quin Ett"],
            ["Tattoo of a star on left hand", "Frayged hatband"],
            "Sagewell Sheriff",
            WarrantDisposition.AliveOnly,
            200m,
            InvestigationTargetKind.UnrelatedWantedCriminal,
            [],
            null),
        Wanted(
            "bart-low",
            "Bart Low",
            CaseRosterSourceCategory.FictionalEconomyWarrant,
            "Fictional economy warrant pool entry; source notes are not historical claims.",
            ["B. Low", "Low Bart"],
            ["Stutter-step gait", "Tin star pinned to coat"],
            "Holloway Marshal",
            WarrantDisposition.DeadOrAlive,
            250m,
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

    public static OutlawWarrantProfile CreateTrueCulpritWarrant(CaseCharacterProfile culprit, CaseSuspectFeatureProfile? openingLeadFeature = null)
    {
        ArgumentNullException.ThrowIfNull(culprit);

        return new OutlawWarrantProfile(
            $"warrant-{culprit.Key}",
            culprit.DisplayName,
            culprit.SourceAliases,
            ["Red Wren", "Aunt Tess"],
            BuildTrueCulpritKnownFeatures(openingLeadFeature),
            "Dodge City Marshal",
            culprit.SourceCategory,
            $"Source-derived culprit warrant built from {culprit.SourceNote}.",
            WarrantDisposition.DeadOrAlive,
            2500m,
            InvestigationTargetKind.TrueCulprit,
            [OutlawGangIds.WildBunch],
            OutlawGangIds.WildBunch);
    }

    public static OutlawWarrantProfile CreateGangMemberWarrant(CaseCharacterProfile gangMember)
    {
        ArgumentNullException.ThrowIfNull(gangMember);

        return new OutlawWarrantProfile(
            $"warrant-{gangMember.Key}",
            gangMember.DisplayName,
            gangMember.SourceAliases,
            gangMember.SourceAliases
                .Concat(gangMember.GameAliases.Select(alias => alias.Name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            BuildGangMemberKnownFeatures(),
            "Dodge City Marshal",
            gangMember.SourceCategory,
            $"Source-derived gang warrant built from {gangMember.SourceNote}.",
            WarrantDisposition.DeadOrAlive,
            1800m,
            InvestigationTargetKind.GangMember,
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

    private static IReadOnlyList<string> BuildTrueCulpritKnownFeatures(CaseSuspectFeatureProfile? openingLeadFeature)
    {
        var featurePool = new[]
        {
            "Raven-feather pin",
            "Black felt hat",
            "Split-finger glove"
        };

        if (openingLeadFeature is null)
        {
            return featurePool.Take(2).ToArray();
        }

        var openingLeadTokens = new HashSet<string>(
            Tokenize(openingLeadFeature.Language.HasForm).Where(token => token.Length > 3),
            StringComparer.OrdinalIgnoreCase);

        var selectedFeatures = featurePool
            .Where(feature => !Tokenize(feature).Any(token => openingLeadTokens.Contains(token)))
            .Take(2)
            .ToArray();

        return selectedFeatures.Length > 0 ? selectedFeatures : featurePool.Take(2).ToArray();
    }

    private static IReadOnlyList<string> BuildGangMemberKnownFeatures()
        => new[]
        {
            "Raven-feather pin",
            "Black felt hat"
        };

    private static IEnumerable<string> Tokenize(string text)
        => text
            .Split([' ', ',', '.', ';', ':', '-', '(', ')', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim().TrimEnd('!', '?').ToLowerInvariant());
}
