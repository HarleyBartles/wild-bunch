using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;

namespace WildBunch.Application.Games.Commands;

public sealed class StartNewGameHandler
{
    private readonly INewGameFactory _newGameFactory;
    private readonly IGameSessionRepository _gameSessionRepository;

    public StartNewGameHandler(INewGameFactory newGameFactory, IGameSessionRepository gameSessionRepository)
    {
        _newGameFactory = newGameFactory;
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<GameSessionDto> HandleAsync(StartNewGameCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var session = _newGameFactory.Create(command.PlayerName, command.TravelDifficulty);
        await _gameSessionRepository.SaveAsync(session, cancellationToken).ConfigureAwait(false);

        return GameSessionMapper.ToDto(session);
    }
}
