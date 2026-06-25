using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: the player entered a new action context within the current town, advancing the turn.
/// Carries the resulting context, the town it was entered in, and the clock state so that replay
/// can reconstruct both <see cref="GameSession.CurrentActionContext"/>,
/// <see cref="GameSession.CurrentActionContextTownId"/>, and <see cref="GameSession.Clock"/>
/// without divergence. The town id scopes the context: entering Saloon in Town A does not
/// suppress time advancement when entering Saloon in Town B.
/// PursuitHeat is ABSOLUTE — Apply sets pursuit heat from it. Heat increases by 1
/// when a full day passes in town (turn wraps from 3 to 0). See ADR-0029.
/// This is the replayable clock/context mutation — no gameplay event carries an AdvanceClock hint.
/// See ADR-0028 and BUNCH-80 clock/turn correction.
/// </summary>
public sealed record TownActionContextEntered : IDomainEvent
{
    public required TownActionContext Context { get; init; }
    public required TownId TownId { get; init; }
    public required int Day { get; init; }
    public required int Turn { get; init; }
    public required TimeOfDay TimeOfDay { get; init; }
    public required int PursuitHeat { get; init; }
}
