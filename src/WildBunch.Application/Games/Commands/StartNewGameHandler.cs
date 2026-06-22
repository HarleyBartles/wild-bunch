using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Games.Commands;

public sealed class StartNewGameHandler : GameSessionCommandHandler
{
    private readonly INewGameFactory _newGameFactory;

    public StartNewGameHandler(
        INewGameFactory newGameFactory,
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
        _newGameFactory = newGameFactory;
    }

    public async Task<GameSessionDto> HandleAsync(StartNewGameCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await ExecuteNewSessionAsync(async ct =>
        {
            var session = _newGameFactory.Create(command.PlayerName, command.TravelDifficulty, command.SetupSeedCode, command.Entropy);
            return (session, GameSessionMapper.ToDto(session));
        }, cancellationToken).ConfigureAwait(false);
    }
}
