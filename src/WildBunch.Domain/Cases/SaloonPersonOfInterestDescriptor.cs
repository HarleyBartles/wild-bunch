using System.Globalization;

namespace WildBunch.Domain.Cases;

public static class SaloonPersonOfInterestDescriptor
{
    public static string Describe(Suspect suspect, CaseFile caseFile)
    {
        ArgumentNullException.ThrowIfNull(suspect);
        ArgumentNullException.ThrowIfNull(caseFile);

        var warrantDescriptor = caseFile.KnownWarrants.FirstOrDefault(warrant => MatchesKnownWarrant(warrant, suspect));
        if (warrantDescriptor is not null)
        {
            var descriptor = warrantDescriptor.Terms.KnownFeatures.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(descriptor))
            {
                return FormatPublicDescriptor(descriptor);
            }
        }

        var profileDescriptor = suspect.Profile.IdentifyingFacts.FirstOrDefault().Description;
        if (!string.IsNullOrWhiteSpace(profileDescriptor))
        {
            return FormatPublicDescriptor(profileDescriptor);
        }

        var traitDescriptor = suspect.Traits.Tags.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(traitDescriptor.Value))
        {
            return FormatPublicDescriptor(FormatTraitDescriptor(traitDescriptor.Value));
        }

        return "an unfamiliar person";
    }

    private static string TrimDescriptor(string descriptor)
        => descriptor.Trim().TrimEnd('.', '!', '?');

    private static string FormatPublicDescriptor(string descriptor)
        => $"a stranger with {TrimDescriptor(descriptor)}";

    private static string FormatTraitDescriptor(string traitTag)
        => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(traitTag.Trim().Replace('-', ' '));

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
