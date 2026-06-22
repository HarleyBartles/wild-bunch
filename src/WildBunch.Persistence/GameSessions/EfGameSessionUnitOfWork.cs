using Microsoft.EntityFrameworkCore;
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
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
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
    /// Checks whether a <see cref="DbUpdateException"/> represents a unique constraint violation.
    /// PostgreSQL wraps these as <c>23505</c> (unique_violation).
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Npgsql surfaces the SQLSTATE via the inner exception's Data or message.
        // The most reliable check is the PostgreSQL SQLSTATE code 23505.
        var inner = ex.InnerException;
        if (inner is not null)
        {
            var message = inner.Message;
            if (message.Contains("23505", StringComparison.Ordinal) ||
                message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Fallback: check the message itself
        if (ex.Message.Contains("23505", StringComparison.Ordinal) ||
            ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
