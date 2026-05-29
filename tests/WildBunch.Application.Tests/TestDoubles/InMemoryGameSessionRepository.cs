using WildBunch.Application.Abstractions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Tests.TestDoubles;

public sealed class InMemoryGameSessionRepository : IGameSessionRepository
{
    private readonly Dictionary<GameSessionId, GameSession> _sessions = new();

    public int SaveCalls { get; private set; }

    public IReadOnlyCollection<GameSession> Sessions => _sessions.Values.ToArray();

    public void Seed(GameSession session)
    {
        _sessions[session.Id] = session;
    }

    public Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken cancellationToken = default)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session);
    }

    public Task SaveAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
