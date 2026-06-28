using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// A named town role for an innocent citizen (e.g. butcher, mortician, doctor).
/// The <see cref="Key"/> is a stable identifier used in events and dev overrides.
/// The <see cref="DisplayName"/> is the reveal name shown after a mistaken take-in
/// (e.g. "the town butcher"). The <see cref="ShortName"/> is the bare role label
/// (e.g. "butcher") used for guardrail checks.
/// </summary>
public sealed record CitizenRole(string Key, string DisplayName, string ShortName);

/// <summary>
/// A resolved citizen encounter: a <see cref="CitizenRole"/> paired with a
/// <see cref="FeatureDescription"/> drawn from the shared suspect feature
/// vocabulary (the <see cref="Cases.SuspectProfile.IdentifyingFacts"/> descriptions
/// from <see cref="CaseFile.Suspects"/>). There is NO separate citizen-only
/// feature pool — citizens and suspects share the same visible feature vocabulary
/// so a citizen can plausibly be mistaken for a wanted suspect.
/// </summary>
public sealed record CitizenEncounter(CitizenRole Role, string? FeatureDescription);

/// <summary>
/// Static content catalog of source-backed innocent town roles for POI encounters.
/// Citizens draw distinguishing features from the same shared vocabulary as suspects
/// (the <see cref="Cases.SuspectProfile.IdentifyingFacts"/> descriptions), NOT a
/// separate citizen-only feature pool. This preserves mistaken-identity play: a
/// citizen can have the same visible feature as a wanted suspect, and the player
/// cannot tell them apart by feature alone. The citizen is innocent because of
/// their role, not because of a separate civilian-only feature set.
/// </summary>
public static class CitizenCast
{
    /// <summary>
    /// The full flavour cast of innocent town roles. At least 12 roles to prove a
    /// full source-backed cast. Role keys are stable identifiers; display names are
    /// the reveal names shown after a mistaken take-in.
    /// </summary>
    public static readonly IReadOnlyList<CitizenRole> Roles =
    [
        new("butcher", "the town butcher", "butcher"),
        new("mortician", "the town mortician", "mortician"),
        new("doctor", "the town doctor", "doctor"),
        new("blacksmith", "the town blacksmith", "blacksmith"),
        new("schoolteacher", "the schoolteacher", "schoolteacher"),
        new("preacher", "the town preacher", "preacher"),
        new("seamstress", "the town seamstress", "seamstress"),
        new("hotel-keeper", "the hotel keeper", "hotel-keeper"),
        new("banker", "the town banker", "banker"),
        new("newspaperman", "the newspaperman", "newspaperman"),
        new("stable-hand", "the stable hand", "stable-hand"),
        new("telegraph-operator", "the telegraph operator", "telegraph-operator"),
        new("barber", "the town barber", "barber"),
        new("undertaker", "the town undertaker", "undertaker"),
        new("prospector", "a local prospector", "prospector"),
        new("cook", "the town cook", "cook"),
        new("stagecoach-agent", "the stagecoach agent", "stagecoach-agent"),
        new("gunsmith", "the town gunsmith", "gunsmith"),
        new("town-clerk", "the town clerk", "town-clerk")
    ];

    /// <summary>
    /// Deterministically selects a <see cref="CitizenEncounter"/> (role + feature)
    /// based on a stable manual hash of <paramref name="townId"/>, <paramref name="day"/>,
    /// <paramref name="turn"/>, and <paramref name="visitNumber"/>. The role is picked
    /// from <see cref="Roles"/> and the feature is picked from the provided
    /// <paramref name="featureDescriptions"/> (the shared suspect feature vocabulary).
    /// Using all four inputs provides substantially more variety than townId + turn alone.
    /// Does NOT use <see cref="string.GetHashCode()"/> (not stable across process restarts).
    /// If <paramref name="featureDescriptions"/> is empty, the encounter's
    /// <see cref="CitizenEncounter.FeatureDescription"/> is null and
    /// <see cref="ResolveDescriptor"/> falls back to "an unfamiliar face".
    /// </summary>
    public static CitizenEncounter Select(TownId townId, int day, int turn, int visitNumber, IReadOnlyList<string> featureDescriptions)
    {
        var roleHash = StableHash(townId.Value, day, turn, visitNumber);
        var role = Roles[roleHash % Roles.Count];

        string? feature = null;
        if (featureDescriptions.Count > 0)
        {
            var featureHash = StableHash(townId.Value, day, turn, visitNumber, "feature");
            feature = featureDescriptions[featureHash % featureDescriptions.Count];
        }

        return new CitizenEncounter(role, feature);
    }

    /// <summary>
    /// Looks up a specific citizen by role key and picks a feature from the provided
    /// <paramref name="featureDescriptions"/> (for dev overlay forcing). The feature
    /// pick is deterministic based on the role key + feature descriptions.
    /// Throws <see cref="ArgumentException"/> if the role key is not found.
    /// </summary>
    public static CitizenEncounter SelectByRoleKey(string roleKey, IReadOnlyList<string> featureDescriptions)
    {
        var role = GetRoleByKey(roleKey);

        string? feature = null;
        if (featureDescriptions.Count > 0)
        {
            var featureHash = StableHash(roleKey, "feature");
            feature = featureDescriptions[featureHash % featureDescriptions.Count];
        }

        return new CitizenEncounter(role, feature);
    }

    /// <summary>
    /// Looks up a <see cref="CitizenRole"/> by key only — no feature, no
    /// featureDescriptions parameter. Used by the confrontation reveal path
    /// (<c>BuildCitizenRevealNarration</c>), which only needs the role display name
    /// and already has the concealment descriptor from active state. Does NOT call
    /// <see cref="Select"/>, does NOT re-pick a feature.
    /// Throws <see cref="ArgumentException"/> if the role key is not found.
    /// </summary>
    public static CitizenRole GetRoleByKey(string roleKey)
    {
        var role = Roles.FirstOrDefault(r => string.Equals(r.Key, roleKey, StringComparison.OrdinalIgnoreCase));
        if (role is null)
        {
            throw new ArgumentException($"Unknown citizen role key: '{roleKey}'.", nameof(roleKey));
        }

        return role;
    }

    /// <summary>
    /// Returns the concealment descriptor shown during lookaround:
    /// "a stranger with {normalized feature}". If the encounter has no feature
    /// description, returns "an unfamiliar face". Reuses the same normalization
    /// logic as <see cref="Cases.SaloonPersonOfInterestDescriptor"/> (strip
    /// "has a"/"wears a" prefixes to "a"/"an").
    /// </summary>
    public static string ResolveDescriptor(CitizenEncounter encounter)
    {
        if (string.IsNullOrWhiteSpace(encounter.FeatureDescription))
        {
            return "an unfamiliar face";
        }

        return $"a stranger with {NormalizeFeatureDescriptor(encounter.FeatureDescription.Trim().TrimEnd('.', '!', '?'))}";
    }

    /// <summary>
    /// Returns the role display name (e.g. "the town butcher") — used in contexts
    /// where an encounter is already available. The confrontation reveal path uses
    /// <see cref="GetRoleByKey"/> + <c>DisplayName</c> directly instead, since it
    /// does not have or need an encounter.
    /// </summary>
    public static string ResolveRevealName(CitizenEncounter encounter)
        => encounter.Role.DisplayName;

    /// <summary>
    /// Convenience helper: builds the full mistaken-arrest reveal narration for an
    /// encounter. The actual confrontation path in <c>BountyLoopCoordinator</c> does
    /// NOT use this method — it uses <see cref="GetRoleByKey"/> + the stored
    /// concealment descriptor. This helper is for other contexts if needed.
    /// </summary>
    public static string ResolveRevealNarration(CitizenEncounter encounter, decimal fineAmount)
        => $"You bring {ResolveDescriptor(encounter)} to the sheriff. The sheriff identifies them as {encounter.Role.DisplayName}, releases them, and fines you ${fineAmount:0.00}.";

    /// <summary>
    /// Normalizes a feature description by stripping common prefixes ("has a",
    /// "wears a", "wearing a", etc.) down to "a"/"an". Mirrors the normalization in
    /// <see cref="Cases.SaloonPersonOfInterestDescriptor.NormalizeFeatureDescriptor"/>.
    /// </summary>
    private static string NormalizeFeatureDescriptor(string descriptor)
    {
        foreach (var (prefix, replacement) in new[]
        {
            ("has a ", "a "),
            ("has an ", "an "),
            ("wore a ", "a "),
            ("wore an ", "an "),
            ("wears a ", "a "),
            ("wears an ", "an "),
            ("wearing a ", "a "),
            ("wearing an ", "an "),
            ("is missing the ", "a missing "),
            ("is missing ", "a missing "),
            ("has no ", "a missing "),
            ("prefers a ", "a "),
            ("keeps a ", "a "),
        })
        {
            if (descriptor.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return replacement + descriptor[prefix.Length..];
            }
        }

        return descriptor;
    }

    /// <summary>
    /// Stable manual hash over the concatenated string representation of the inputs.
    /// Does NOT use <see cref="string.GetHashCode()"/> (not stable across process
    /// restarts). Uses a prime multiplier over char codes for distribution.
    /// </summary>
    private static int StableHash(params object[] parts)
    {
        unchecked
        {
            var hash = 17;
            foreach (var part in parts)
            {
                var s = part?.ToString() ?? string.Empty;
                foreach (var c in s)
                {
                    hash = (hash * 31) + c;
                }
            }

            return Math.Abs(hash);
        }
    }
}
