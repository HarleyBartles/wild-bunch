using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class CaseFileClueCacheRecovery
{
    internal static bool MatchesEventClueCollections(
        IReadOnlyList<IDomainEvent> events,
        CaseFile cachedCaseFile)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(cachedCaseFile);

        CaseFileSnapshot? generatedCaseFile = null;
        var generatedCaseFileIndex = -1;
        for (var index = 0; index < events.Count; index++)
        {
            if (events[index] is CaseFileGenerated caseFileGenerated)
            {
                generatedCaseFile = caseFileGenerated.CaseFile;
                generatedCaseFileIndex = index;
            }
        }

        if (generatedCaseFile is null
            || generatedCaseFile.Clues is null
            || generatedCaseFile.PublicClues is null)
        {
            return true;
        }

        var expectedKnownClues = generatedCaseFile.Clues.ToList();
        var expectedKnownClueIds = expectedKnownClues
            .Select(clue => clue.Id)
            .ToHashSet(StringComparer.Ordinal);
        var generatedPublicClues = generatedCaseFile.PublicClues
            .GroupBy(clue => clue.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var expectedPublicClues = generatedCaseFile.PublicClues
            .GroupBy(clue => clue.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .Where(clue => !expectedKnownClueIds.Contains(clue.Id))
            .ToList();

        foreach (var investigation in events
                     .Skip(generatedCaseFileIndex + 1)
                     .OfType<InvestigationPerformed>())
        {
            if (investigation.ClueId is not null
                && generatedPublicClues.TryGetValue(investigation.ClueId.Value.Value, out var clue)
                && expectedKnownClueIds.Add(clue.Id))
            {
                expectedKnownClues.Add(clue);
                expectedPublicClues.RemoveAll(publicClue => publicClue.Id == clue.Id);
            }
        }

        var actualKnownClues = cachedCaseFile.KnownClues
            .Select(ClueSnapshot.FromDomain)
            .ToArray();
        var actualPublicClues = cachedCaseFile.PublicClues
            .Select(ClueSnapshot.FromDomain)
            .ToArray();
        var expectedKnownClueSnapshots = expectedKnownClues.ToArray();
        var expectedPublicClueSnapshots = expectedPublicClues.ToArray();

        return SameClueSequence(expectedKnownClueSnapshots, actualKnownClues)
            && SameClueSequence(expectedPublicClueSnapshots, actualPublicClues);
    }

    private static bool SameClueSequence(IReadOnlyList<ClueSnapshot> expected, IReadOnlyList<ClueSnapshot> actual)
        => expected.Count == actual.Count
            && expected.Zip(actual).All(pair => SameCluePayload(pair.First, pair.Second));

    private static bool SameCluePayload(ClueSnapshot expected, ClueSnapshot actual)
        => expected.Id == actual.Id
            && expected.Kind == actual.Kind
            && expected.Description == actual.Description
            && expected.LinkedSuspectIds.SequenceEqual(actual.LinkedSuspectIds, StringComparer.Ordinal)
            && expected.TargetKind == actual.TargetKind
            && expected.SourceKind == actual.SourceKind
            && expected.Source == actual.Source
            && expected.Context == actual.Context
            && expected.Anchors.Subjects.SequenceEqual(actual.Anchors.Subjects)
            && expected.Anchors.Locations.SequenceEqual(actual.Anchors.Locations)
            && expected.Anchors.Times.SequenceEqual(actual.Anchors.Times)
            && expected.Anchors.Directions.SequenceEqual(actual.Anchors.Directions);
}
