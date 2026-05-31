using WildBunch.Application.Abstractions;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameJournalReadRepository : IGameJournalReadRepository
{
    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionJsonSerializer _serializer;

    public EfGameJournalReadRepository(WildBunchDbContext dbContext, GameSessionJsonSerializer serializer)
    {
        _dbContext = dbContext;
        _serializer = serializer;
    }

    public Task<JournalSnapshot?> GetByIdAsync(
        GameSessionId id,
        int skip = 0,
        int? take = null,
        CancellationToken cancellationToken = default)
        => GameSessionReadStoreLoader.LoadJournalSnapshotAsync(_dbContext, _serializer, id, skip, take, cancellationToken);
}
