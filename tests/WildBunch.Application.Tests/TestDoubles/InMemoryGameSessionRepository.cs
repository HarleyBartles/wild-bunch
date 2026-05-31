using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Tests.TestDoubles;

public sealed class InMemoryGameSessionRepository : IGameSessionRepository, IGameSessionReadRepository, IGameJournalReadRepository
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

    Task<GameSessionReadModel?> IGameSessionReadRepository.GetByIdAsync(GameSessionId id, CancellationToken cancellationToken)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session is null ? null : new GameSessionReadModel(
            session.Id.Value,
            session.Status,
            session.TravelDifficulty,
            session.Player,
            session.World,
            session.CaseFile,
            session.Clock,
            session.PursuitState,
            session.Journey is null ? null : session.Journey.ToSnapshot(session.TravelRules),
            session.TravelDiaryDays.ToArray(),
            session.LogEntries.ToArray()));
    }

    Task<JournalSnapshot?> IGameJournalReadRepository.GetByIdAsync(
        GameSessionId id,
        int skip,
        int? take,
        CancellationToken cancellationToken)
    {
        _sessions.TryGetValue(id, out var session);
        if (session is null)
        {
            return Task.FromResult<JournalSnapshot?>(null);
        }

        var snapshot = new JournalResolver().Resolve(session);
        var logEntries = snapshot.LogEntries.Skip(Math.Max(0, skip));
        var slicedEntries = take.HasValue ? logEntries.Take(Math.Max(0, take.Value)).ToArray() : logEntries.ToArray();
        return Task.FromResult<JournalSnapshot?>(snapshot with { LogEntries = slicedEntries });
    }

    public Task SaveAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
