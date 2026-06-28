using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Games.Commands;

public sealed class StartNewGameHandler : GameSessionCommandHandler
{
    private const string SupersededByNewPlaythrough = "superseded-by-new-playthrough";

    private readonly INewGameFactory _newGameFactory;
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public StartNewGameHandler(
        INewGameFactory newGameFactory,
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _newGameFactory = newGameFactory;
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    /// <summary>
    /// Creates a new game session while enforcing the one-active-playthrough invariant:
    /// any pre-existing <see cref="GameStatus.Active"/> sessions are archived
    /// (<see cref="PlaythroughArchived"/> with reason <c>superseded-by-new-playthrough</c>)
    /// in the SAME correlation id and SAME unit-of-work commit as the new session create.
    /// This guarantees that after any successful call, at most one Active session exists
    /// in the persisted store. See BUNCH-102.
    /// </summary>
    public async Task<GameSessionDto> HandleAsync(StartNewGameCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // One correlation id for the entire archive-old + create-new flow.
        var correlationId = Guid.NewGuid();

        // 1. Archive all pre-existing Active sessions (one-active-playthrough invariant).
        var activeSessions = await GameSessionRepository.GetByStatusAsync(
            GameStatus.Active, cancellationToken).ConfigureAwait(false);
        foreach (var activeSession in activeSessions)
        {
            activeSession.ArchivePlaythrough(SupersededByNewPlaythrough);
            await GameSessionRepository.StoreAsync(
                activeSession, correlationId, cancellationToken).ConfigureAwait(false);
        }

        // 2. Create the new session and stage it on the same DbContext.
        var newSession = _newGameFactory.Create(
            command.PlayerName, command.GameDifficulty, command.SetupSeedCode, command.Entropy, command.StartingTownId);
        await GameSessionRepository.StoreAsync(
            newSession, correlationId, cancellationToken).ConfigureAwait(false);

        // 3. Commit everything in one transaction (single EF SaveChanges + commit).
        await GameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        // 4. Mark events committed on all touched sessions.
        foreach (var archived in activeSessions)
        {
            archived.MarkEventsCommitted();
        }

        newSession.MarkEventsCommitted();

        // 5. Project HUD/diary for the new session (same as before).
        var dto = GameSessionMapper.ToDto(newSession);
        var events = await GameSessionRepository.GetEventStreamAsync(
            newSession.Id, 0, cancellationToken).ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = dto.Id };
        var diary = _diaryProjector.Project(events) with { SessionId = dto.Id };

        return dto with { HudProjection = hud, DiaryProjection = diary };
    }
}
