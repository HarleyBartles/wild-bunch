using System.Collections.Concurrent;
using WildBunch.Application.Abstractions;
using WildBunch.Domain.Game;

namespace WildBunch.Api.Infrastructure;

public sealed class InMemoryGameSessionRepository : IGameSessionRepository
{
    private readonly ConcurrentDictionary<GameSessionId, GameSession> _sessions = new();

    public Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task SaveAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
