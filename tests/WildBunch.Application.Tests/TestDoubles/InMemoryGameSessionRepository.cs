using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Tests.TestDoubles;

public sealed class InMemoryGameSessionRepository : IGameSessionRepository, IGameSessionReadRepository, IGameJournalReadRepository, IGameSessionUnitOfWork
{
    private readonly Dictionary<GameSessionId, GameSession> _sessions = new();
    private readonly Dictionary<GameSessionId, GameSession> _pendingSessions = new();
    private readonly Dictionary<GameSessionId, List<IDomainEvent>> _eventStreams = new();

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
        if (session is not null && _eventStreams.TryGetValue(id, out var stream))
        {
            session.SetCommittedEvents(stream);
        }
        return Task.FromResult(session);
    }

    public Task<IReadOnlyList<GameSession>> GetByStatusAsync(GameStatus status, CancellationToken cancellationToken = default)
    {
        var matching = _sessions.Values.Where(s => s.Status == status).ToArray();
        return Task.FromResult<IReadOnlyList<GameSession>>(matching);
    }

    Task<GameSessionReadModel?> IGameSessionReadRepository.GetByIdAsync(GameSessionId id, CancellationToken cancellationToken)
    {
        _sessions.TryGetValue(id, out var session);
        return Task.FromResult(session is null ? null : new GameSessionReadModel(
            session.Id.Value,
            session.Status,
            session.GameDifficulty,
            session.GameEntropy,
            session.StartFlowPhase,
            session.Player,
            session.World,
            session.CaseFile,
            session.Clock,
            session.PursuitState,
            session.CurrentTownVisit,
            session.Journey is null ? null : session.Journey.ToSnapshot(session.TravelRules),
            session.TravelDiaryDays.ToArray(),
            GameSessionLogProjection.Project(session).ToArray()));
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

        var snapshot = new JournalResolver().Resolve(session, new JournalLogProjector().Project(session.AllEvents));
        var logEntries = snapshot.LogEntries.Skip(Math.Max(0, skip));
        var slicedEntries = take.HasValue ? logEntries.Take(Math.Max(0, take.Value)).ToArray() : logEntries.ToArray();
        return Task.FromResult<JournalSnapshot?>(snapshot with { LogEntries = slicedEntries });
    }

    public Task StoreAsync(GameSession session, Guid? correlationId = null, CancellationToken cancellationToken = default)
    {
        StoreCalls++;
        _pendingSessions[session.Id] = session;
        if (session.UncommittedEvents.Count > 0)
        {
            if (!_eventStreams.TryGetValue(session.Id, out var stream))
            {
                stream = [];
                _eventStreams[session.Id] = stream;
            }
            stream.AddRange(session.UncommittedEvents);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IDomainEvent>> GetEventStreamAsync(GameSessionId id, long fromVersion = 0, CancellationToken cancellationToken = default)
    {
        if (!_eventStreams.TryGetValue(id, out var stream))
        {
            return Task.FromResult<IReadOnlyList<IDomainEvent>>(Array.Empty<IDomainEvent>());
        }
        return Task.FromResult<IReadOnlyList<IDomainEvent>>(stream.Skip((int)fromVersion).ToArray());
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
