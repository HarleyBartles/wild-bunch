using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>Read-only inputs for a saloon look-around decision.</summary>
internal sealed record SaloonLookAroundContext(
    TownId TownId,
    int Day,
    int Turn,
    int VisitNumber,
    string Salt,
    IReadOnlyList<Suspect> EligibleSuspects,
    IReadOnlyList<Warrant> KnownWarrants,
    int CitizenRoleCount,
    bool IsSaloonSourceSpent,
    DevSaloonOverride? PendingDevOverride,
    IReadOnlyList<string> SuspectFeatureDescriptions,
    Func<TownId, int, int, int, IReadOnlyList<string>, CitizenEncounter> CitizenSelect,
    Func<string, IReadOnlyList<string>, CitizenEncounter> CitizenSelectByRoleKey,
    Func<CitizenEncounter, string> CitizenDescriptorResolver);

/// <summary>Read-only inputs for a saloon POI confrontation decision.</summary>
internal sealed record SaloonConfrontationContext(
    SuspectId? ActiveSaloonSuspectId,
    string? ActiveSaloonDescriptor,
    SaloonPersonOfInterestKind? ActiveSaloonPOIKind,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Warrant> KnownWarrants,
    IReadOnlyDictionary<SuspectId, WantedSuspectConfrontationState> ConfrontationStates,
    bool FirearmThreatAvailable,
    decimal PlayerCash,
    int ClockDay,
    int ClockTurn,
    string? DeclaredWantedIdentityHandle);

/// <summary>Read-only inputs for a wanted-suspect confrontation decision.</summary>
internal sealed record WantedSuspectConfrontationContext(
    SuspectId TargetSuspectId,
    WantedSuspectConfrontationChoice Choice,
    string? DeclaredWantedIdentityHandle,
    bool CanConfrontInCurrentContext,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Warrant> KnownWarrants,
    IReadOnlyDictionary<SuspectId, WantedSuspectConfrontationState> ConfrontationStates);

/// <summary>Read-only inputs for a sheriff turn-in assess/settle decision.</summary>
internal sealed record SheriffTurnInContext(
    SuspectId TargetSuspectId,
    bool IsAlive,
    bool IsJourneyModal,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Warrant> KnownWarrants,
    IReadOnlyDictionary<SuspectId, WantedSuspectConfrontationState> ConfrontationStates,
    int ClockDay,
    int ClockTurn);

/// <summary>Read-only inputs for an unrelated-criminal turn-in decision.</summary>
internal sealed record UnrelatedCriminalTurnInContext(
    WarrantId WarrantId,
    bool IsAlive,
    IReadOnlyList<Warrant> KnownWarrants,
    int ClockDay,
    int ClockTurn);

/// <summary>
/// Result from a BountyLoop command method. Carries the public result object
/// plus events that GameSession must produce. BountyLoop does not produce events.
/// </summary>
internal sealed record BountyLoopResult<TResult>(TResult Result, IReadOnlyList<IDomainEvent> Events);
