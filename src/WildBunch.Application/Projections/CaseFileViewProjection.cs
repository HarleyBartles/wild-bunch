using WildBunch.Domain.Cases;

namespace WildBunch.Application.Projections;

/// <summary>
/// Case file view projection: the detective's case file derived from domain events.
/// This is a read-only projection — it does not mutate aggregate state.
/// See ADR-0028.
/// </summary>
public sealed record CaseFileViewProjection(
    Guid SessionId,
    string? AccusationId,
    string CaseSummary,
    IReadOnlyList<Suspect> DiscoveredSuspects,
    IReadOnlyList<Clue> KnownClues,
    IReadOnlyList<Warrant> KnownWarrants,
    IReadOnlyList<WantedSuspectConfrontationState> Confrontations,
    IReadOnlyList<SheriffTurnInSettlementState> Settlements) : IProjectionResult;
