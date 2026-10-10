using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Events;

/// <summary>
/// Fact: a saloon person of interest was challenged. Covers citizen release, wrong-name
/// release, wanted-person refusal, and rejection paths that clear the active saloon person.
/// Successful wanted settlement is represented by its confrontation and sheriff-settlement
/// events, with this event recording the saloon-level result.
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
