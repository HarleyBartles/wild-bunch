using WildBunch.Domain.Game;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player entered a new action context within the current town, advancing the turn.
/// Carries the resulting context and clock state so that replay can reconstruct both
/// <see cref="GameSession.CurrentActionContext"/> and <see cref="GameSession.Clock"/> without divergence.
/// This is the replayable clock/context mutation — no gameplay event carries an AdvanceClock hint.
/// See ADR-0028 and BUNCH-80 clock/turn correction.
/// </summary>
public sealed record TownActionContextEntered : IDomainEvent
{
    public required TownActionContext Context { get; init; }
    public required int Day { get; init; }
    public required int Turn { get; init; }
    public required TimeOfDay TimeOfDay { get; init; }
}
