using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Abstractions;

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
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
