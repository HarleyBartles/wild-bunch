namespace WildBunch.Domain.Game;

/// <summary>
/// Tracks progress through the start game flow (setup → prologue → map selection → game started).
/// This is persisted via domain events and allows the frontend to resume from the correct step after a refresh.
/// </summary>
public enum StartFlowPhase
{
    /// <summary>
    /// No game setup has been initiated.
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Player has completed initial setup (name, difficulty, entropy, seed selection).
    /// PlayerSetupCompleted event has been emitted.
    /// </summary>
    SetupComplete = 1,

    /// <summary>
    /// Player has viewed the prologue and the starting clue (suspect identifier) was revealed.
    /// PrologueViewed event has been emitted.
    /// </summary>
    PrologueViewed = 2,

    /// <summary>
    /// Player has selected a starting town and the game has started.
    /// GameStarted event has been emitted.
    /// </summary>
    GameStarted = 3
}