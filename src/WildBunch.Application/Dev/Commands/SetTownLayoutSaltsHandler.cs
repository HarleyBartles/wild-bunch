using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Dev.Commands;

/// <summary>
/// Handler for SetTownLayoutSaltsCommand. Sets the layout salts by storing
/// them in the game session for use in layout generation. Follows the same
/// pattern as ForceDevSaltSource - stores dev-controlled values in the session.
/// </summary>
public sealed class SetTownLayoutSaltsHandler : GameSessionCommandHandler
{
    public SetTownLayoutSaltsHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(SetTownLayoutSaltsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameId);
        var layoutSalts = new LayoutSalts(
            command.BuildingsSalt,
            command.RoadsSalt,
            command.DirtSalt,
            command.PropsSalt);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.SetDevLayoutSalts(layoutSalts);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
