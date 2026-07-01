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
            var descriptor = warrantDescriptor.Terms.KnownFeatures.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(descriptor))
            {
                return FormatPublicDescriptor(descriptor);
            }
        }

        var primaryFact = suspect.Profile.IdentifyingFacts.FirstOrDefault();
        var profileDescriptor = primaryFact.Language?.HasForm;
        if (!string.IsNullOrWhiteSpace(profileDescriptor))
        {
            return FormatPublicDescriptor(profileDescriptor);
        }

        var traitDescriptor = suspect.Traits.Tags.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(traitDescriptor.Value))
        {
            return FormatPublicTraitDescriptor(FormatTraitDescriptor(traitDescriptor.Value));
        }

        return "an unfamiliar person";
    }

    private static string TrimDescriptor(string descriptor)
        => descriptor.Trim().TrimEnd('.', '!', '?');

    private static string FormatPublicDescriptor(string descriptor)
        => $"a stranger with {NormalizeFeatureDescriptor(TrimDescriptor(descriptor))}";

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
        })
        {
            if (descriptor.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return replacement + descriptor[prefix.Length..];
            }
        }

        return descriptor;
    }

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
