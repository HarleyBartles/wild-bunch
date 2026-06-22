using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: an investigation source was checked, possibly revealing a public clue and/or warrant.
/// Carries only public data — hidden culprit truth is never in this event.
/// See ADR-0028.
/// </summary>
public sealed record InvestigationPerformed : IDomainEvent
{
    public required InvestigationSourceKind SourceKind { get; init; }
    public required TownId TownId { get; init; }
    public required string Message { get; init; }
    public Clue? Clue { get; init; }
    public Warrant? Warrant { get; init; }
    public bool AdvanceClock { get; init; } = true;
}
