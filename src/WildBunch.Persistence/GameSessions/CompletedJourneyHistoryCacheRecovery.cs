using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class CompletedJourneyHistoryCacheRecovery
{
    internal static bool HasAcknowledgedHistory(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events.Any(domainEvent => domainEvent is JourneyArrivalAcknowledged);
    }
}
