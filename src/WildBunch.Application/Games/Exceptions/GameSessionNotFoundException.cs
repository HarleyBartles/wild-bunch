using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Exceptions;

public sealed class GameSessionNotFoundException : InvalidOperationException
{
    public GameSessionNotFoundException(GameSessionId gameSessionId)
        : base($"Game session {gameSessionId.Value} was not found.")
    {
        GameSessionId = gameSessionId;
    }

    public GameSessionId GameSessionId { get; }
}
