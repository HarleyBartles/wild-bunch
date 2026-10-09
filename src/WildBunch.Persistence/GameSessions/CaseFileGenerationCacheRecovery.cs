using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class CaseFileGenerationCacheRecovery
{
    internal static bool MatchesEventGeneratedFacts(
        IReadOnlyList<IDomainEvent> events,
        CaseFile cachedCaseFile)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(cachedCaseFile);

        var generatedCaseFile = events.OfType<CaseFileGenerated>().LastOrDefault()?.CaseFile;
        if (generatedCaseFile is null)
        {
            return true;
        }

        return generatedCaseFile.Suspects is not null
            && generatedCaseFile.SuspectTurfAssignments is not null
            && generatedCaseFile.OpeningLead is not null
            && string.Equals(generatedCaseFile.TrueCulpritId, cachedCaseFile.TrueCulpritId.Value, StringComparison.Ordinal)
            && MatchesSuspects(generatedCaseFile.Suspects, cachedCaseFile.Suspects)
            && string.Equals(
                generatedCaseFile.OpeningLead.Description,
                cachedCaseFile.OpeningLead.Description,
                StringComparison.Ordinal)
            && generatedCaseFile.KillerReleaseThreshold == cachedCaseFile.KillerReleaseThreshold
            && MatchesTurfAssignments(generatedCaseFile.SuspectTurfAssignments, cachedCaseFile.SuspectTurfAssignments);
    }

    private static bool MatchesSuspects(IReadOnlyList<SuspectSnapshot> expected, IReadOnlyList<Suspect> actual)
        => expected.Count == actual.Count
            && expected.Zip(actual).All(pair => MatchesSuspect(pair.First, pair.Second));

    private static bool MatchesSuspect(SuspectSnapshot expected, Suspect actual)
        => string.Equals(expected.Id, actual.Id.Value, StringComparison.Ordinal)
            && string.Equals(expected.Name, actual.Name, StringComparison.Ordinal)
            && string.Equals(expected.Status, actual.Status.ToString(), StringComparison.Ordinal)
            && expected.TraitsTags is not null
            && expected.TraitsTags.SequenceEqual(actual.Traits.Tags.Select(tag => tag.Value), StringComparer.Ordinal)
            && MatchesAliases(expected.Profile?.Aliases, actual.Profile.Aliases)
            && MatchesIdentityFacts(expected.Profile?.IdentifyingFacts, actual.Profile.IdentifyingFacts);

    private static bool MatchesAliases(
        IReadOnlyList<SuspectAliasSnapshot>? expected,
        IReadOnlyList<SuspectAlias> actual)
        => expected is not null
            && expected.Count == actual.Count
            && expected.Zip(actual).All(pair =>
                string.Equals(pair.First.Name, pair.Second.Name, StringComparison.Ordinal)
                && string.Equals(pair.First.AliasKind, pair.Second.Kind.ToString(), StringComparison.Ordinal));

    private static bool MatchesIdentityFacts(
        IReadOnlyList<SuspectIdentityFactSnapshot>? expected,
        IReadOnlyList<SuspectIdentityFact> actual)
        => expected is not null
            && expected.Count == actual.Count
            && expected.Zip(actual).All(pair =>
                string.Equals(pair.First.Raw, pair.Second.Language.HasForm, StringComparison.Ordinal)
                && string.Equals(pair.First.ThirdPerson, pair.Second.Language.WithForm, StringComparison.Ordinal)
                && string.Equals(pair.First.FirstPerson, pair.Second.Language.WhoForm, StringComparison.Ordinal)
                && pair.First.IsPrimary == pair.Second.IsPrimary);

    private static bool MatchesTurfAssignments(
        IReadOnlyList<SuspectTurfAssignmentSnapshot> expected,
        IReadOnlyList<SuspectTurfAssignment> actual)
        => expected.Count == actual.Count
            && expected.Zip(actual).All(pair =>
                string.Equals(pair.First.SuspectId, pair.Second.SuspectId.Value, StringComparison.Ordinal)
                && string.Equals(pair.First.TurfTownId, pair.Second.TurfTownId.Value, StringComparison.Ordinal));
}
