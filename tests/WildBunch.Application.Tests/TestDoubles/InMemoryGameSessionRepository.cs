using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Tests.TestDoubles;

public sealed class InMemoryGameSessionRepository : IGameSessionRepository, IGameSessionReadRepository, IGameJournalReadRepository, IGameSessionUnitOfWork
{
    private readonly Dictionary<GameSessionId, GameSession> _sessions = new();
    private readonly Dictionary<GameSessionId, GameSession> _pendingSessions = new();

    public int StoreCalls { get; private set; }

    public int CommitCalls { get; private set; }

    public int SaveCalls => StoreCalls;

    public int? LastJournalSkip { get; private set; }

    public int? LastJournalTake { get; private set; }

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
            session.Entropy,
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
        LastJournalSkip = skip;
        LastJournalTake = take;
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

    public Task StoreAsync(GameSession session, CancellationToken cancellationToken = default)
    {
        StoreCalls++;
        _pendingSessions[session.Id] = session;
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        CommitCalls++;
        foreach (var pending in _pendingSessions)
        {
            _sessions[pending.Key] = pending.Value;
        }

        _pendingSessions.Clear();
        return Task.CompletedTask;
    }

}
