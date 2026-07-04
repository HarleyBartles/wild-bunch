using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed class ArchivePlaythroughHandler : GameSessionCommandHandler
{
    public ArchivePlaythroughHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    // Lifecycle handler: archiving is valid in any phase, including setup.
    protected override bool RequiresGameStarted => false;

    public async Task<ArchivePlaythroughResultDto> HandleAsync(
        ArchivePlaythroughCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await ExecuteWithRetryAsync(command.SessionId, async (session, ct) =>
        {
            session.ArchivePlaythrough(command.ArchiveReason);

            return BuildResultDto(session);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static ArchivePlaythroughResultDto BuildResultDto(GameSession session)
        => new(
            session.Id.Value,
            session.Status,
            session.Player.Name,
            session.IsSetupPhase
                ? null
                : session.CurrentTown.TownId.Value,
            session.IsSetupPhase
                ? null
                : session.CurrentTown.TownName,
            session.Clock.Day,
            session.Clock.Turn.ToString());
}
