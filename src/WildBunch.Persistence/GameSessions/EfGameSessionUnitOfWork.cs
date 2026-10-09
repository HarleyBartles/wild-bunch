using Microsoft.EntityFrameworkCore;
using Npgsql;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameSessionUnitOfWork : IGameSessionUnitOfWork
{
    private readonly WildBunchDbContext _dbContext;

    public EfGameSessionUnitOfWork(WildBunchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsEventSequenceConflict(ex))
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            // Clear tracked entities so a retry can re-load and re-stage cleanly.
            _dbContext.ChangeTracker.Clear();
            throw new ConcurrencyException(
                "Concurrency conflict: a duplicate event sequence was detected by the database unique index. " +
                "This indicates a race between concurrent command handlers. Reload and retry.");
        }
    }

    /// <summary>
    /// Checks whether PostgreSQL rejected the append on the stored-event stream-position key.
    /// </summary>
    private static bool IsEventSequenceConflict(DbUpdateException ex)
    {
        return ex.GetBaseException() is PostgresException providerException &&
            providerException.SqlState == PostgresErrorCodes.UniqueViolation &&
            providerException.ConstraintName == "PK_GameSessionStoredEvents";
    }
}
