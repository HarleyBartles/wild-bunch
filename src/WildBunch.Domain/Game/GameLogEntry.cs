namespace WildBunch.Domain.Game;

/// <summary>
/// Legacy projection-only log entry. Demoted to projection-legacy per ADR-0028.
/// The authoritative source of game history is the typed domain event stream.
/// New domain code should not produce GameLogEntry values; use typed domain
/// events and derive projections from them via IDomainEventProjector.
/// This record is retained as the projection output type for JournalLogProjector
/// and read-model DTOs; the authoritative source of game history is the typed
/// domain event stream.
/// </summary>
public sealed record GameLogEntry(GameLogEntryKind Kind, string Message, int Day, int Turn);
