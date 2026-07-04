using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;
using WildBunch.GameContent.Prologue;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Records that the player has viewed the prologue and the starting clue was revealed.
/// Appends a PrologueViewed event to the session's event stream.
/// </summary>
public sealed class ViewPrologueHandler : GameSessionCommandHandler
{
    private readonly HudProjector _hudProjector;
    private readonly DiaryProjector _diaryProjector;

    public ViewPrologueHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork,
        HudProjector hudProjector,
        DiaryProjector diaryProjector)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _hudProjector = hudProjector;
        _diaryProjector = diaryProjector;
    }

    // Setup-flow handler: views prologue before GameStarted.
    protected override bool RequiresGameStarted => false;

    public async Task<GameSessionDto> HandleAsync(ViewPrologueCommand command, GameSessionId sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dto = await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            // Resolve the true culprit descriptor for the prologue reveal.
            var trueCulpritDescriptor = PrologueDescriptorResolver.ResolveTrueCulpritDescriptor(
                session.GameDifficulty, session.SeedCode, session.GameEntropy);

            session.ViewPrologue(trueCulpritDescriptor);

            return GameSessionMapper.ToDto(session);
        }, cancellationToken).ConfigureAwait(false);

        // Project from the event stream AFTER the base pipeline has stored and
        // committed the new events. Fetching inside the lambda would project
        // from a stream that does not yet include the just-emitted event.
        // See TravelToTownHandler for the same pattern.
        var events = await GameSessionRepository.GetEventStreamAsync(
            sessionId, 0, cancellationToken).ConfigureAwait(false);
        var hud = _hudProjector.Project(events) with { SessionId = dto.Id };
        var diary = _diaryProjector.Project(events) with { SessionId = dto.Id };

        return dto with { HudProjection = hud, DiaryProjection = diary };
    }
}