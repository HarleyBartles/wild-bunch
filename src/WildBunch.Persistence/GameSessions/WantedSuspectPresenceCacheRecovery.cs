using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class WantedSuspectPresenceCacheRecovery
{
    internal static bool HasStateChangingConfrontation(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events.OfType<WantedSuspectConfronted>().Any(confrontation =>
            confrontation.Outcome != WantedSuspectConfrontationOutcome.Abandoned
            && confrontation.Choice is WantedSuspectConfrontationChoice.Surrendered
                or WantedSuspectConfrontationChoice.Fled
                or WantedSuspectConfrontationChoice.Killed);
    }
}
