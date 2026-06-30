using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Command to complete the game start by selecting a starting town.
/// This appends a GameStarted event to the existing setup-phase session.
/// </summary>
public sealed record CompleteGameStartCommand
{
    public required GameSessionId SessionId { get; init; }
    public required string StartingTownId { get; init; }
}