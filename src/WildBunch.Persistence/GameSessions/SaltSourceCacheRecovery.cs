using WildBunch.Domain.Events;
using WildBunch.Domain.Game;

namespace WildBunch.Persistence.GameSessions;

internal static class SaltSourceCacheRecovery
{
    internal static bool MatchesGeneratedSaltSource(IReadOnlyList<IDomainEvent> events, SaltSource saltSource)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(saltSource);

        var generatedSaltSource = events.OfType<WorldGenerated>().Single().SaltSource;
        return generatedSaltSource == saltSource;
    }
}
