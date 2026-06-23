using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a wanted suspect was confronted with a specific outcome.
/// Carries only public data — TrueCulpritId is never in this event.
/// Clock advancement is handled by EnterActionContext, not this event.
/// See ADR-0028 and BUNCH-80.
/// </summary>
public sealed record WantedSuspectConfronted : IDomainEvent
{
    public required SuspectId TargetSuspectId { get; init; }
    public required string TargetName { get; init; }
    public required WarrantDisposition Disposition { get; init; }
    public required WantedSuspectConfrontationChoice Choice { get; init; }
    public required WantedSuspectConfrontationOutcome Outcome { get; init; }
    public required bool IsAlive { get; init; }
    public required bool IsSecured { get; init; }
    public required string Message { get; init; }
    public string? DeclaredWantedIdentityHandle { get; init; }
}
