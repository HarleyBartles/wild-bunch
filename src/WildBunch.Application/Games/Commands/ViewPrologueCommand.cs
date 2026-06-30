namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Command to record that the player has viewed the prologue and the starting clue was revealed.
/// This emits a PrologueViewed event and advances the start flow phase.
/// </summary>
public sealed record ViewPrologueCommand
{
    public required string RevealedSuspectIdentifier { get; init; }
}