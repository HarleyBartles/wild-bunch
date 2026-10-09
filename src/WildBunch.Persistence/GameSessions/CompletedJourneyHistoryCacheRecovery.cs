using WildBunch.Domain.Events;
using WildBunch.Domain.Travel;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

internal static class CompletedJourneyHistoryCacheRecovery
{
    internal static bool HasAcknowledgedHistory(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        return events.Any(domainEvent => domainEvent is JourneyArrivalAcknowledged);
    }

    internal static bool MatchesAcknowledgedHistory(
        IReadOnlyList<IDomainEvent> events,
        IReadOnlyList<TravelJourneySnapshot> cachedHistory,
        GameSessionJsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(cachedHistory);
        ArgumentNullException.ThrowIfNull(serializer);

        var acknowledgedHistory = events
            .OfType<JourneyArrivalAcknowledged>()
            .Select(domainEvent => domainEvent.JourneySnapshot)
            .ToArray();

        return acknowledgedHistory.Length == 0
            || string.Equals(
                serializer.SerializeCompletedJourneyHistory(acknowledgedHistory),
                serializer.SerializeCompletedJourneyHistory(cachedHistory),
                StringComparison.Ordinal);
    }
}
