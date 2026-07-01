using WildBunch.Domain.Cases;

namespace WildBunch.GameContent.NewGame;

internal readonly record struct CaseSuspectFeatureTag(string Value)
{
    public override string ToString() => Value;
}

internal static class CaseSuspectFeatureTags
{
    public static readonly CaseSuspectFeatureTag OpeningLeadCapable = new("opening-lead-capable");
    public static readonly CaseSuspectFeatureTag ClassicNod = new("classic-nod");
    public static readonly CaseSuspectFeatureTag PhysicalMarker = new("physical-marker");
    public static readonly CaseSuspectFeatureTag Accessory = new("accessory");
    public static readonly CaseSuspectFeatureTag SideAware = new("side-aware");
    public static readonly CaseSuspectFeatureTag Face = new("face");
    public static readonly CaseSuspectFeatureTag Leg = new("leg");
    public static readonly CaseSuspectFeatureTag Ear = new("ear");
    public static readonly CaseSuspectFeatureTag Eye = new("eye");
    public static readonly CaseSuspectFeatureTag Visible = new("visible");
    public static readonly CaseSuspectFeatureTag Wearable = new("wearable");
    public static readonly CaseSuspectFeatureTag Gait = new("gait");
    public static readonly CaseSuspectFeatureTag Scar = new("scar");
    public static readonly CaseSuspectFeatureTag MissingPart = new("missing-part");
    public static readonly CaseSuspectFeatureTag DistinctiveItem = new("distinctive-item");
}

internal enum CaseFeatureKind
{
    PrimaryMarker = 0,
    AccessoryMarker = 1
}

internal enum CaseFeatureSide
{
    None = 0,
    Left = 1,
    Right = 2
}

internal sealed record CaseSuspectFeatureProfile(
    string Key,
    FeatureLanguage Language,
    CaseFeatureKind Kind,
    string FamilyKey,
    CaseFeatureSide Side,
    IReadOnlyList<CaseSuspectFeatureTag> Tags,
    IReadOnlyList<string> IncompatibleKeys,
    string SourceNote)
{
    public bool SupportsOpeningLead => HasTag(CaseSuspectFeatureTags.OpeningLeadCapable);

    public bool IsClassicNod => HasTag(CaseSuspectFeatureTags.ClassicNod);

    public bool HasTag(CaseSuspectFeatureTag tag)
        => Tags.Any(existing => string.Equals(existing.Value, tag.Value, StringComparison.OrdinalIgnoreCase));

    public bool IsCompatibleWith(CaseSuspectFeatureProfile other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (FamilyKey.Equals(other.FamilyKey, StringComparison.OrdinalIgnoreCase)
            && Side != CaseFeatureSide.None
            && other.Side != CaseFeatureSide.None
            && Side != other.Side)
        {
            return false;
        }

        return !IncompatibleKeys.Contains(other.Key, StringComparer.OrdinalIgnoreCase)
            && !other.IncompatibleKeys.Contains(Key, StringComparer.OrdinalIgnoreCase);
    }
}

internal sealed record CaseSuspectFeatureAssignment(
    CaseSuspectFeatureProfile PrimaryFeature,
    IReadOnlyList<CaseSuspectFeatureProfile> AdditionalFeatures)
{
    public IReadOnlyList<CaseSuspectFeatureProfile> AllFeatures
        => new[] { PrimaryFeature }.Concat(AdditionalFeatures).ToArray();
}

internal static class CaseSuspectFeaturePool
{
    private static readonly CaseSuspectFeatureProfile[] PrimaryFeatures =
    [
        NodFeature("limp-left-leg", FeatureCategory.Limp, "leg", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Gait, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Leg, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("limp-right-leg", FeatureCategory.Limp, "leg", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Gait, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Leg, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("no-left-ear", FeatureCategory.MissingPart, "ear", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Ear, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.", "distinctive-left-earring"),
        NodFeature("no-right-ear", FeatureCategory.MissingPart, "ear", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Ear, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.", "distinctive-right-earring"),
        NodFeature("scar-left-cheek", FeatureCategory.Scar, "cheek", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Scar, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("scar-right-cheek", FeatureCategory.Scar, "cheek", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Scar, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("no-eyebrows", FeatureCategory.Absence, "eyebrows", CaseFeatureSide.None, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.")
    ];

    private static readonly CaseSuspectFeatureProfile[] AccessoryFeatures =
    [
        AccessoryFeature("distinctive-left-earring",
            new FeatureLanguage("Wears a distinctive earring in the left ear.", "a distinctive earring in the left ear", "wears a distinctive earring in the left ear", null),
            "earring", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.DistinctiveItem, CaseSuspectFeatureTags.Ear], "Original feature text.", "no-left-ear"),
        AccessoryFeature("distinctive-right-earring",
            new FeatureLanguage("Wears a distinctive earring in the right ear.", "a distinctive earring in the right ear", "wears a distinctive earring in the right ear", null),
            "earring", CaseFeatureSide.Right, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.DistinctiveItem, CaseSuspectFeatureTags.Ear], "Original feature text.", "no-right-ear"),
        AccessoryFeature("eyepatch-left",
            new FeatureLanguage("Wears an eyepatch over the left eye.", "an eyepatch over the left eye", "wears an eyepatch over the left eye", null),
            "eyepatch", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Eye, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("eyepatch-right",
            new FeatureLanguage("Wears an eyepatch over the right eye.", "an eyepatch over the right eye", "wears an eyepatch over the right eye", null),
            "eyepatch", CaseFeatureSide.Right, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Eye, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("cracked-gauntlet",
            new FeatureLanguage("Wears a cracked leather gauntlet on the right hand.", "a cracked leather gauntlet on the right hand", "wears a cracked leather gauntlet on the right hand", null),
            "gauntlet", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("stitched-brim-hat",
            new FeatureLanguage("Prefers a sand-colored hat with the brim stitched flat.", "a sand-colored hat with the brim stitched flat", "prefers a sand-colored hat with the brim stitched flat", null),
            "hat", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("black-stained-cuff",
            new FeatureLanguage("Has a black-stained cuff on the left sleeve.", "a black-stained cuff on the left sleeve", "has a black-stained cuff on the left sleeve", null),
            "cuff", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("split-finger-glove",
            new FeatureLanguage("Keeps a split-finger glove tucked into a coat pocket.", "a split-finger glove tucked into a coat pocket", "keeps a split-finger glove tucked into a coat pocket", null),
            "glove", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("silver-tooth",
            new FeatureLanguage("Has a silver tooth that catches the light when he smiles.", "a silver tooth that catches the light when he smiles", "has a silver tooth that catches the light when he smiles", null),
            "tooth", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("copper-ribbon",
            new FeatureLanguage("Keeps a copper ribbon tied in her hair.", "a copper ribbon tied in her hair", "keeps a copper ribbon tied in her hair", null),
            "ribbon", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("rope-burn-scar",
            new FeatureLanguage("Carries a rope-burn scar on the left wrist.", "a rope-burn scar on the left wrist", "carries a rope-burn scar on the left wrist", null),
            "scar", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Scar, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("faded-blue-scarf",
            new FeatureLanguage("Wears a faded blue scarf over a dark vest.", "a faded blue scarf over a dark vest", "wears a faded blue scarf over a dark vest", null),
            "scarf", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("iron-rim-spectacles",
            new FeatureLanguage("Keeps iron-rim spectacles tucked into a coat pocket.", "iron-rim spectacles tucked into a coat pocket", "keeps iron-rim spectacles tucked into a coat pocket", null),
            "spectacles", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("dust-colored-duster",
            new FeatureLanguage("Wears a long dust-colored duster with a frayed hem.", "a long dust-colored duster with a frayed hem", "wears a long dust-colored duster with a frayed hem", null),
            "duster", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("brass-spur",
            new FeatureLanguage("Keeps a brass spur tucked into a coat pocket.", "a brass spur tucked into a coat pocket", "keeps a brass spur tucked into a coat pocket", null),
            "spur", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("tobacco-stained-gloves",
            new FeatureLanguage("Leaves tobacco-stained glove prints on ledgers and rail notices.", "tobacco-stained glove prints on ledgers and rail notices", "leaves tobacco-stained glove prints on ledgers and rail notices", null),
            "glove", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("copper-spur-ribbon",
            new FeatureLanguage("Keeps a brass spur tied to a faded blue sash.", "a brass spur tied to a faded blue sash", "keeps a brass spur tied to a faded blue sash", null),
            "spur", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("straw-hat",
            new FeatureLanguage("Wears a straw hat with the crown creased low.", "a straw hat with the crown creased low", "wears a straw hat with the crown creased low", null),
            "hat", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text.")
    ];

    public static IReadOnlyList<CaseSuspectFeatureProfile> FeaturePool
        => PrimaryFeatures.Concat(AccessoryFeatures).ToArray();

    public static IReadOnlyList<CaseSuspectFeatureAssignment> SelectAssignedFeatures(GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var primaryFeatures = SelectByScore(source, "case.features.primary", PrimaryFeatures, 7, feature => feature.Key);
        var accessoryCandidates = SelectByScore(source, "case.features.accessory", AccessoryFeatures, AccessoryFeatures.Length, feature => feature.Key);
        var usedAccessoryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var assignments = new List<CaseSuspectFeatureAssignment>(7);

        foreach (var primary in primaryFeatures)
        {
            var additionalFeatures = new List<CaseSuspectFeatureProfile>(2);
            var classicNodCount = primary.HasTag(CaseSuspectFeatureTags.ClassicNod) ? 1 : 0;

            foreach (var candidate in accessoryCandidates)
            {
                if (usedAccessoryKeys.Contains(candidate.Key))
                {
                    continue;
                }

                if (!primary.IsCompatibleWith(candidate))
                {
                    continue;
                }

                if (additionalFeatures.Any(existing => !existing.IsCompatibleWith(candidate)))
                {
                    continue;
                }

                if (classicNodCount >= 2 && candidate.HasTag(CaseSuspectFeatureTags.ClassicNod))
                {
                    continue;
                }

                additionalFeatures.Add(candidate);
                usedAccessoryKeys.Add(candidate.Key);

                if (candidate.HasTag(CaseSuspectFeatureTags.ClassicNod))
                {
                    classicNodCount++;
                }

                if (additionalFeatures.Count == 2)
                {
                    break;
                }
            }

            assignments.Add(new CaseSuspectFeatureAssignment(primary, additionalFeatures));
        }

        return assignments;
    }

    public static IReadOnlyList<CaseSuspectFeatureAssignment> SelectCanonicalAssignedFeatures(GameSetupDeterministicSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var assignments = SelectAssignedFeatures(source).ToArray();
        var culpritIndex = 3;
        var canonicalLeadIndex = Array.FindIndex(assignments, assignment => assignment.PrimaryFeature.Key == "scar-left-cheek");

        if (canonicalLeadIndex >= 0 && canonicalLeadIndex != culpritIndex)
        {
            (assignments[culpritIndex], assignments[canonicalLeadIndex]) = (assignments[canonicalLeadIndex], assignments[culpritIndex]);
        }

        return assignments;
    }

    public static bool AreCompatible(CaseSuspectFeatureProfile left, CaseSuspectFeatureProfile right)
        => left.IsCompatibleWith(right);

    public static string BuildOpeningLead(CaseSuspectFeatureProfile feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (!feature.HasTag(CaseSuspectFeatureTags.OpeningLeadCapable) || string.IsNullOrWhiteSpace(feature.Language.OpeningLeadForm))
        {
            throw new InvalidOperationException($"Feature '{feature.Key}' does not support an opening lead.");
        }

        return feature.Language.OpeningLeadForm!;
    }

    private static CaseSuspectFeatureProfile NodFeature(
        string key,
        FeatureCategory category,
        string bodyPart,
        CaseFeatureSide side,
        IReadOnlyList<CaseSuspectFeatureTag> tags,
        string sourceNote,
        params string[] incompatibleKeys)
    {
        var featureSide = side switch
        {
            CaseFeatureSide.Left => FeatureSide.Left,
            CaseFeatureSide.Right => FeatureSide.Right,
            _ => FeatureSide.None
        };
        var descriptor = new FeatureDescriptor(category, bodyPart, featureSide);
        var language = FeatureLanguageService.For(descriptor);
        return new CaseSuspectFeatureProfile(
            key,
            language,
            CaseFeatureKind.PrimaryMarker,
            FamilyKey: bodyPart == "leg" ? "limp" : bodyPart == "ear" ? "ear" : bodyPart == "cheek" ? "cheek-scar" : "brow",
            side,
            tags,
            incompatibleKeys,
            sourceNote);
    }

    private static CaseSuspectFeatureProfile AccessoryFeature(
        string key,
        FeatureLanguage language,
        string familyKey,
        CaseFeatureSide side,
        IReadOnlyList<CaseSuspectFeatureTag> tags,
        string sourceNote,
        params string[] incompatibleKeys)
        => new(
            key,
            language,
            CaseFeatureKind.AccessoryMarker,
            familyKey,
            side,
            tags,
            incompatibleKeys,
            sourceNote);

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
}
