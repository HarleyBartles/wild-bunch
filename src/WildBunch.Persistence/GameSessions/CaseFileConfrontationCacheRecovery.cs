using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class CaseFileConfrontationCacheRecovery
{
    internal static bool MatchesEventConfrontations(
        IReadOnlyList<IDomainEvent> events,
        CaseFile cachedCaseFile)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(cachedCaseFile);

        var generatedCaseFileIndex = -1;
        for (var index = 0; index < events.Count; index++)
        {
            if (events[index] is CaseFileGenerated)
            {
                generatedCaseFileIndex = index;
            }
        }

        if (generatedCaseFileIndex < 0)
        {
            return true;
        }

        var day = 1;
        var turn = 0;
        var expectedConfrontations = new List<WantedSuspectConfrontationState>();
        foreach (var domainEvent in events.Skip(generatedCaseFileIndex + 1))
        {
            switch (domainEvent)
            {
                case TownActionContextEntered contextEntered:
                    day = contextEntered.Day;
                    turn = contextEntered.Turn;
                    break;
                case WantedSuspectConfronted { Outcome: not WantedSuspectConfrontationOutcome.Abandoned } confronted:
                    expectedConfrontations.Add(new WantedSuspectConfrontationState(
                        confronted.TargetSuspectId,
                        confronted.TargetName,
                        confronted.Disposition,
                        confronted.Outcome,
                        confronted.IsAlive,
                        confronted.IsSecured,
                        day,
                        turn));
                    break;
            }
        }

        return expectedConfrontations.SequenceEqual(cachedCaseFile.WantedSuspectConfrontations);
    }
}
