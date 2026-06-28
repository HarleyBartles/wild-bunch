using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a saloon look-around revealed a person of interest (wanted suspect or citizen),
/// or found nobody of interest on a repeat visit. Carries only public data.
/// See ADR-0028. Clock advancement is handled by EnterActionContext, not this event.
/// </summary>
public sealed record SaloonPersonOfInterestSpotted : IDomainEvent
{
    public required InvestigationSourceKind SourceKind { get; init; }
    public required TownId TownId { get; init; }
    public required string Message { get; init; }
    public SuspectId? SuspectId { get; init; }
    public string? Descriptor { get; init; }
    public SaloonPersonOfInterestKind? PersonOfInterestKind { get; init; }
    /// <summary>
    /// The citizen role key (e.g. "butcher"), null for suspect/repeat POIs.
    /// Carries only the role key — the display name is resolved via CitizenCast.GetRoleByKey
    /// at reveal time. Not shown in player-facing DTOs during lookaround (concealment).
    /// </summary>
    public string? CitizenRole { get; init; }
    /// <summary>
    /// Whether to append a case-update log entry. The repeat path and suspect path
    /// log a message; the citizen path does not (preserving existing behavior).
    /// This does NOT advance the clock — clock advance comes from EnterActionContext.
    /// </summary>
    public bool RecordLog { get; init; } = true;
}
