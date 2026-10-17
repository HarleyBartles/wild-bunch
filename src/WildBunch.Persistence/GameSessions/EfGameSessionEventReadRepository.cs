using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Abstractions;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameSessionEventReadRepository : IGameSessionEventReadRepository
{
    private readonly WildBunchDbContext _dbContext;
    private readonly PersistedPayloadLoader _payloadLoader;

    public EfGameSessionEventReadRepository(
        WildBunchDbContext dbContext,
        PersistedPayloadLoader payloadLoader)
    {
        _dbContext = dbContext;
        _payloadLoader = payloadLoader;
    }

    public async Task<IReadOnlyList<IDomainEvent>?> GetEventStreamAsync(
        GameSessionId id,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken)
            .ConfigureAwait(false);

        var exists = await _dbContext.GameSessions.AsNoTracking()
            .AnyAsync(session => session.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        var storedEvents = await _dbContext.StoredEvents.AsNoTracking()
            .Where(domainEvent => domainEvent.StreamId == id.Value)
            .OrderBy(domainEvent => domainEvent.Sequence)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var events = _payloadLoader.LoadEvents(storedEvents);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return events;
    }
}
