namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Command to record that the player has viewed the prologue and the starting clue was revealed.
/// This emits a PrologueViewed event and advances the start flow phase.
/// The true culprit descriptor is resolved from the session's seed/difficulty/entropy
/// by the handler — it is not caller-supplied.
/// </summary>
public sealed record ViewPrologueCommand;
