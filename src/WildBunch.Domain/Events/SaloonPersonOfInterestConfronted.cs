using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a saloon person of interest was confronted. Covers citizen/wrong-declaration
/// paths and rejection paths that clear the active saloon person. When the confrontation
/// delegates to ResolveWantedSuspectConfrontation or SettleSheriffTurnIn, those produce
/// their own events; this event covers only the saloon-person-level outcome.
/// Carries only public data. See ADR-0028 and BUNCH-80.
/// </summary>
public sealed record SaloonPersonOfInterestConfronted : IDomainEvent
{
    public required string Message { get; init; }
    public SuspectId? TargetSuspectId { get; init; }
    public required string TargetName { get; init; }
    public required SaloonPersonOfInterestKind PersonOfInterestKind { get; init; }
    public required SaloonPersonOfInterestConfrontationOutcome Outcome { get; init; }
    public bool? IsAlive { get; init; }
    public bool? IsSecured { get; init; }
    public decimal? FineAmount { get; init; }
    public decimal? WalletBefore { get; init; }
    public decimal? WalletAfter { get; init; }
    public string? DeclaredWantedIdentityHandle { get; init; }
    public bool IsCitizen { get; init; }
    /// <summary>
    /// The revealed citizen role key (e.g. "butcher"), null for suspect confrontations.
    /// The display name is resolved via CitizenCast.GetRoleByKey at narration build time.
    /// </summary>
    public string? CitizenRole { get; init; }
}
