using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class TownVisitCacheRecovery
{
    internal static bool HasStartedGame(IReadOnlyList<IDomainEvent> events)
        => events.Any(domainEvent => domainEvent is GameStarted);
}
