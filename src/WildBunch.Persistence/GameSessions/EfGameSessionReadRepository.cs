using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameSessionReadRepository : IGameSessionReadRepository
{
    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionReadStoreLoader _readStoreLoader;

    public EfGameSessionReadRepository(WildBunchDbContext dbContext, GameSessionReadStoreLoader readStoreLoader)
    {
        _dbContext = dbContext;
        _readStoreLoader = readStoreLoader;
    }

    public Task<GameSessionReadModel?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
        => _readStoreLoader.LoadGameSessionReadModelAsync(_dbContext, id, cancellationToken);
}
