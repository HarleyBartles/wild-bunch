using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class CaseFileKnownClueCacheRecovery
{
    internal static bool MatchesEventKnownClues(
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

        var expectedKnownClueIds = generatedCaseFile.Clues
            .Select(clue => clue.Id)
            .ToHashSet(StringComparer.Ordinal);
        var generatedPublicClueIds = generatedCaseFile.PublicClues
            .Select(clue => clue.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var investigation in events
                     .Skip(generatedCaseFileIndex + 1)
                     .OfType<InvestigationPerformed>())
        {
            if (investigation.ClueId is not null
                && generatedPublicClueIds.Contains(investigation.ClueId.Value.Value))
            {
                expectedKnownClueIds.Add(investigation.ClueId.Value.Value);
            }
        }

        return expectedKnownClueIds.SetEquals(cachedCaseFile.KnownClues.Select(clue => clue.Id.Value));
    }
}
