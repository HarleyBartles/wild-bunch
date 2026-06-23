namespace WildBunch.Domain.Game;

/// <summary>
/// The action context the player is currently in within a town. Entering a NEW context
/// (different from the current one) advances the turn via <see cref="GameSession.EnterActionContext"/>
/// and produces a replayable <see cref="WildBunch.Domain.Events.TownActionContextEntered"/> event.
/// Staying in the same context does NOT advance the turn. <see cref="None"/> never produces an event.
/// This is a simple context tracker — no complex location model. See ADR-0028 and BUNCH-80.
/// </summary>
public enum TownActionContext
{
    None = 0,
    SheriffOffice = 1,
    Saloon = 2,
    Store = 3,
    Stable = 4,
    Jail = 5,
    TelegraphOffice = 6,
    TownSquare = 7
}
