using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameSessionReadRepository : IGameSessionReadRepository
{
    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionJsonSerializer _serializer;

    public EfGameSessionReadRepository(WildBunchDbContext dbContext, GameSessionJsonSerializer serializer)
    {
        _dbContext = dbContext;
        _serializer = serializer;
    }

    public Task<GameSessionReadModel?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
        => GameSessionReadStoreLoader.LoadGameSessionReadModelAsync(_dbContext, _serializer, id, cancellationToken);
}
