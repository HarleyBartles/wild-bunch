using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a dev command forced a pending saloon override.
/// This is a dev-only event - it records dev intent, not a gameplay outcome.
/// The override is consumed by the next DevSaloonOverrideConsumed +
/// SaloonPersonOfInterestSpotted pair.
/// See BUNCH-90 and ADR-0030.
/// </summary>
public sealed record DevSaloonOverrideForced : IDomainEvent
{
    public required DevSaloonPoiKind ForcedKind { get; init; }
    public SuspectId? ForcedSuspectId { get; init; }
    public string? ForcedCitizenRoleKey { get; init; }
}
