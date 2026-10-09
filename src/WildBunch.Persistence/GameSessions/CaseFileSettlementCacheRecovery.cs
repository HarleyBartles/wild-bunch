using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class CaseFileSettlementCacheRecovery
{
    internal static bool MatchesEventSettlements(
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

        var expectedSettlements = events
            .Skip(generatedCaseFileIndex + 1)
            .OfType<SheriffTurnInSettled>()
            .Select(turnIn => new SheriffTurnInSettlementState(
                turnIn.TargetSuspectId,
                turnIn.TargetName,
                turnIn.Disposition,
                turnIn.IsAlive,
                turnIn.BountyAmount,
                turnIn.Day,
                turnIn.Turn))
            .ToArray();

        return expectedSettlements.SequenceEqual(cachedCaseFile.SheriffTurnInSettlements);
    }
}
