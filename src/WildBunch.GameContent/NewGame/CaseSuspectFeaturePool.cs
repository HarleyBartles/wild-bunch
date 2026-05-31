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
    string Description,
    string OpeningLeadText,
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
        NodFeature("limp-left-leg", "Has a limp in the left leg.", "The culprit walks with a limp in the left leg.", "limp", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Gait, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Leg, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("limp-right-leg", "Has a limp in the right leg.", "The culprit walks with a limp in the right leg.", "limp", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Gait, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Leg, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("no-left-ear", "Is missing the left ear.", "The culprit is missing the left ear.", "ear", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Ear, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.", "distinctive-left-earring"),
        NodFeature("no-right-ear", "Is missing the right ear.", "The culprit is missing the right ear.", "ear", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Ear, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.", "distinctive-right-earring"),
        NodFeature("scar-left-cheek", "Has a scar on the left cheek.", "The culprit has a scar on his left cheek.", "cheek-scar", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Scar, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("scar-right-cheek", "Has a scar on the right cheek.", "The culprit has a scar on his right cheek.", "cheek-scar", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Scar, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("no-eyebrows", "Has no eyebrows.", "The culprit has no eyebrows.", "brow", CaseFeatureSide.None, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.")
    ];

    private static readonly CaseSuspectFeatureProfile[] AccessoryFeatures =
    [
        AccessoryFeature("distinctive-left-earring", "Wears a distinctive earring in the left ear.", "earring", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.DistinctiveItem, CaseSuspectFeatureTags.Ear], "Original feature text.", "no-left-ear"),
        AccessoryFeature("distinctive-right-earring", "Wears a distinctive earring in the right ear.", "earring", CaseFeatureSide.Right, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.DistinctiveItem, CaseSuspectFeatureTags.Ear], "Original feature text.", "no-right-ear"),
        AccessoryFeature("eyepatch-left", "Wears an eyepatch over the left eye.", "eyepatch", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Eye, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("eyepatch-right", "Wears an eyepatch over the right eye.", "eyepatch", CaseFeatureSide.Right, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Eye, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("cracked-gauntlet", "Wears a cracked leather gauntlet on the right hand.", "gauntlet", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("stitched-brim-hat", "Prefers a sand-colored hat with the brim stitched flat.", "hat", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("black-stained-cuff", "Has a black-stained cuff on the left sleeve.", "cuff", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("split-finger-glove", "Keeps a split-finger glove tucked into a coat pocket.", "glove", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("silver-tooth", "Has a silver tooth that catches the light when he smiles.", "tooth", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("copper-ribbon", "Keeps a copper ribbon tied in her hair.", "ribbon", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("rope-burn-scar", "Carries a rope-burn scar on the left wrist.", "scar", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Scar, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("faded-blue-scarf", "Wears a faded blue scarf over a dark vest.", "scarf", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("iron-rim-spectacles", "Keeps iron-rim spectacles tucked into a coat pocket.", "spectacles", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("dust-colored-duster", "Wears a long dust-colored duster with a frayed hem.", "duster", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("brass-spur", "Keeps a brass spur tucked into a coat pocket.", "spur", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("tobacco-stained-gloves", "Leaves tobacco-stained glove prints on ledgers and rail notices.", "glove", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("copper-spur-ribbon", "Keeps a brass spur tied to a faded blue sash.", "spur", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        AccessoryFeature("straw-hat", "Wears a straw hat with the crown creased low.", "hat", CaseFeatureSide.None, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text.")
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

        if (!feature.HasTag(CaseSuspectFeatureTags.OpeningLeadCapable) || string.IsNullOrWhiteSpace(feature.OpeningLeadText))
        {
            throw new InvalidOperationException($"Feature '{feature.Key}' does not support an opening lead.");
        }

        return feature.OpeningLeadText;
    }

    private static CaseSuspectFeatureProfile NodFeature(
        string key,
        string description,
        string openingLeadText,
        string familyKey,
        CaseFeatureSide side,
        IReadOnlyList<CaseSuspectFeatureTag> tags,
        string sourceNote,
        params string[] incompatibleKeys)
        => new(
            key,
            description,
            openingLeadText,
            CaseFeatureKind.PrimaryMarker,
            familyKey,
            side,
            tags,
            incompatibleKeys,
            sourceNote);

    private static CaseSuspectFeatureProfile AccessoryFeature(
        string key,
        string description,
        string familyKey,
        CaseFeatureSide side,
        IReadOnlyList<CaseSuspectFeatureTag> tags,
        string sourceNote,
        params string[] incompatibleKeys)
        => new(
            key,
            description,
            string.Empty,
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
