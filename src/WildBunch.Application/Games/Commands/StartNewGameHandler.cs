using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Games.Commands;

public sealed class StartNewGameHandler
{
    private readonly INewGameFactory _newGameFactory;
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameSessionUnitOfWork _gameSessionUnitOfWork;

    public StartNewGameHandler(
        INewGameFactory newGameFactory,
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
    {
        _newGameFactory = newGameFactory;
        _gameSessionRepository = gameSessionRepository;
        _gameSessionUnitOfWork = gameSessionUnitOfWork;
    }

    public async Task<GameSessionDto> HandleAsync(StartNewGameCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = _newGameFactory.Create(command.PlayerName, command.TravelDifficulty, command.SetupSeedCode, command.Entropy);
        await _gameSessionRepository.StoreAsync(session, cancellationToken).ConfigureAwait(false);
        await _gameSessionUnitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);

        return GameSessionMapper.ToDto(session);
    }
}
