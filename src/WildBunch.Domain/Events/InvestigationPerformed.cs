using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: an investigation source was checked, possibly revealing a public clue and/or warrant.
/// Carries only public data — hidden culprit truth is never in this event.
/// The clue and warrant are referenced by ID (not full domain objects) so the event
/// payload is minimal and serializable without coupling domain models to JSON.
/// The <see cref="GameSession.Apply(InvestigationPerformed)"/> method looks up the
/// clue/warrant from the <see cref="CaseFile"/> public pool by ID during replay.
/// See ADR-0028.
/// </summary>
public sealed record InvestigationPerformed : IDomainEvent
{
    public required InvestigationSourceKind SourceKind { get; init; }
    public required TownId TownId { get; init; }
    public required string Message { get; init; }
    public ClueId? ClueId { get; init; }
    public WarrantId? WarrantId { get; init; }
}
