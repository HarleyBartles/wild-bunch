using WildBunch.Application.Abstractions;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameJournalReadRepository : IGameJournalReadRepository
{
    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionReadStoreLoader _readStoreLoader;

    public EfGameJournalReadRepository(WildBunchDbContext dbContext, GameSessionReadStoreLoader readStoreLoader)
    {
        _dbContext = dbContext;
        _readStoreLoader = readStoreLoader;
    }

    public Task<JournalSnapshot?> GetByIdAsync(
        GameSessionId id,
        int skip = 0,
        int? take = null,
        CancellationToken cancellationToken = default)
        => _readStoreLoader.LoadJournalSnapshotAsync(_dbContext, id, skip, take, cancellationToken);
}
