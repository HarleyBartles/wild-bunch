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

        CaseFileSnapshot? generatedCaseFile = null;
        var generatedCaseFileIndex = -1;
        for (var index = 0; index < events.Count; index++)
        {
            if (events[index] is CaseFileGenerated generated)
            {
                generatedCaseFile = generated.CaseFile;
                generatedCaseFileIndex = index;
            }
        }
        if (generatedCaseFile is null)
        {
            return true;
        }

        var capturedSuspectIds = events.Skip(generatedCaseFileIndex + 1)
            .OfType<SheriffTurnInSettled>()
            .Where(settlement => settlement.IsAlive)
            .Select(settlement => settlement.TargetSuspectId)
            .ToHashSet();

        return generatedCaseFile.Suspects is not null
            && generatedCaseFile.SuspectTurfAssignments is not null
            && generatedCaseFile.OpeningLead is not null
            && string.Equals(generatedCaseFile.TrueCulpritId, cachedCaseFile.TrueCulpritId.Value, StringComparison.Ordinal)
            && MatchesSuspects(generatedCaseFile.Suspects, cachedCaseFile.Suspects, capturedSuspectIds)
            && string.Equals(
                generatedCaseFile.OpeningLead.Description,
                cachedCaseFile.OpeningLead.Description,
                StringComparison.Ordinal)
            && generatedCaseFile.KillerReleaseThreshold == cachedCaseFile.KillerReleaseThreshold
            && string.Equals(generatedCaseFile.AccusationId, cachedCaseFile.Accusation?.Value, StringComparison.Ordinal)
            && MatchesTurfAssignments(generatedCaseFile.SuspectTurfAssignments, cachedCaseFile.SuspectTurfAssignments);
    }

    private static bool MatchesSuspects(IReadOnlyList<SuspectSnapshot> expected, IReadOnlyList<Suspect> actual, IReadOnlySet<SuspectId> capturedSuspectIds)
        => expected.Count == actual.Count
            && expected.Zip(actual).All(pair => MatchesSuspect(pair.First, pair.Second, capturedSuspectIds.Contains(pair.Second.Id)));

    private static bool MatchesSuspect(SuspectSnapshot expected, Suspect actual, bool captured)
        => string.Equals(expected.Id, actual.Id.Value, StringComparison.Ordinal)
            && string.Equals(expected.Name, actual.Name, StringComparison.Ordinal)
            && string.Equals(captured ? nameof(SuspectStatus.Captured) : expected.Status, actual.Status.ToString(), StringComparison.Ordinal)
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
