using WildBunch.Domain.Game;

namespace WildBunch.Persistence.GameSessions;

/// <summary>
/// Legacy projection of game log entries. This table is a pre-event-sourcing
/// projection that is being demoted to projection-legacy status per ADR-0028.
/// The authoritative source of game history is the event stream
/// (StoredEventEntity). New projection consumers should derive from the event
/// stream via IDomainEventProjector, not from this table.
/// This table is retained for backward compatibility during the migration.
/// </summary>
public sealed class GameSessionLogEntryEntity
{
    public Guid SessionId { get; set; }

    public int Sequence { get; set; }

    public GameLogEntryKind Kind { get; set; }

    public string Message { get; set; } = string.Empty;

    public int Day { get; set; }

    public int Turn { get; set; }

    public GameSessionEntity Session { get; set; } = null!;
}
