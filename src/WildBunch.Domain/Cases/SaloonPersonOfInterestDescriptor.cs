using System.Globalization;

namespace WildBunch.Domain.Cases;

public static class SaloonPersonOfInterestDescriptor
{
    public static string Describe(Suspect suspect, CaseFile caseFile)
    {
        ArgumentNullException.ThrowIfNull(suspect);
        ArgumentNullException.ThrowIfNull(caseFile);

        return Describe(suspect, caseFile.KnownWarrants);
    }

    /// <summary>
    /// Describes a suspect using the known warrants list directly.
    /// Used by BountyLoop which receives warrants via context records.
    /// </summary>
    public static string Describe(Suspect suspect, IReadOnlyList<Warrant> knownWarrants)
    {
        ArgumentNullException.ThrowIfNull(suspect);
        ArgumentNullException.ThrowIfNull(knownWarrants);

        var warrantDescriptor = knownWarrants.FirstOrDefault(warrant => MatchesKnownWarrant(warrant, suspect));
        if (warrantDescriptor is not null)
        {
            var feature = warrantDescriptor.Terms.KnownFeatures.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(feature))
            {
                return $"a stranger with {TrimFeature(feature)}";
            }
        }

        var primaryFact = suspect.Profile.IdentifyingFacts.FirstOrDefault();
        var profileDescriptor = primaryFact.Language?.WithForm;
        if (!string.IsNullOrWhiteSpace(profileDescriptor))
        {
            return $"a stranger with {profileDescriptor}";
        }

        var traitDescriptor = suspect.Traits.Tags.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(traitDescriptor.Value))
        {
            return FormatPublicTraitDescriptor(FormatTraitDescriptor(traitDescriptor.Value));
        }

        return "an unfamiliar person";
    }

    /// <summary>
    /// Trims trailing punctuation from warrant feature strings (noun phrases like
    /// "Raven-feather pin"). Warrant <see cref="WarrantTerms.KnownFeatures"/> are plain
    /// noun-phrase strings from the warrant pool, not structured
    /// <see cref="FeatureLanguage"/>; no prefix normalization is applied.
    /// </summary>
    private static string TrimFeature(string feature)
        => feature.Trim().TrimEnd('.', '!', '?');

    private static string FormatTraitDescriptor(string traitTag)
        => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(traitTag.Trim().Replace('-', ' '));

    private static string FormatPublicTraitDescriptor(string descriptor)
        => $"a stranger who is {descriptor.ToLowerInvariant()}";

    private static bool MatchesKnownWarrant(Warrant warrant, Suspect targetSuspect)
    {
        ArgumentNullException.ThrowIfNull(warrant);
        ArgumentNullException.ThrowIfNull(targetSuspect);

        if (string.Equals(warrant.TargetName, targetSuspect.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return warrant.Terms.KnownAliases.Any(alias => string.Equals(alias, targetSuspect.Name, StringComparison.OrdinalIgnoreCase));
    }
}
