using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.GameContent.Prologue;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Records that the player has viewed the prologue and the starting clue was revealed.
/// Appends a PrologueViewed event to the session's event stream.
/// </summary>
public sealed class ViewPrologueHandler : GameSessionCommandHandler
{
    public ViewPrologueHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    // Setup-flow handler: views prologue before GameStarted.
    protected override bool RequiresGameStarted => false;

    public async Task<GameSessionDto> HandleAsync(ViewPrologueCommand command, GameSessionId sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await ExecuteWithRetryAsync(sessionId, async (session, ct) =>
        {
            // Resolve the true culprit descriptor for the prologue reveal.
            var trueCulpritDescriptor = PrologueDescriptorResolver.ResolveTrueCulpritDescriptor(
                session.GameDifficulty, session.SeedCode, session.GameEntropy);

            session.ViewPrologue(trueCulpritDescriptor);

            return GameSessionMapper.ToDto(session);
        }, cancellationToken).ConfigureAwait(false);
    }
}
