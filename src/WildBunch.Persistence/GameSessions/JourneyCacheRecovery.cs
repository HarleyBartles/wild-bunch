using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class JourneyCacheRecovery
{
    internal static bool HasCurrentJourney(IReadOnlyList<IDomainEvent> events)
    {
        // Process transitions in stream order: completion keeps a Journey current;
        // acknowledgement clears it, and a later start establishes the next one.
        var hasCurrentJourney = false;
        foreach (var domainEvent in events)
        {
            hasCurrentJourney = domainEvent switch
            {
                JourneyStarted => true,
                JourneyArrivalAcknowledged => false,
                _ => hasCurrentJourney
            };
        }

        return hasCurrentJourney;
    }
}
