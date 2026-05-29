using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Abstractions;
using WildBunch.Domain.Game;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

public sealed class EfGameSessionRepository : IGameSessionRepository
{
    private readonly WildBunchDbContext _dbContext;
    private readonly GameSessionJsonSerializer _serializer;

    public EfGameSessionRepository(WildBunchDbContext dbContext, GameSessionJsonSerializer serializer)
    {
        _dbContext = dbContext;
        _serializer = serializer;
    }

    public async Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.GameSessions.AsNoTracking().SingleOrDefaultAsync(session => session.Id == id.Value, cancellationToken);
        return entity is null ? null : _serializer.Deserialize(entity.StateJson);
    }

    public async Task SaveAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = await _dbContext.GameSessions.SingleOrDefaultAsync(existing => existing.Id == session.Id.Value, cancellationToken);
        var stateJson = _serializer.Serialize(session);

        if (entity is null)
        {
            _dbContext.GameSessions.Add(new GameSessionEntity
            {
                Id = session.Id.Value,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Status = session.Status.ToString(),
                StateJson = stateJson
            });
        }
        else
        {
            entity.UpdatedAtUtc = now;
            entity.Status = session.Status.ToString();
            entity.StateJson = stateJson;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
