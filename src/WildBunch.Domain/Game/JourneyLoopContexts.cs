using WildBunch.Domain.Events;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Game;

/// <summary>Read-only inputs for starting a journey.</summary>
internal sealed record StartJourneyContext(
    TravelPreview Preview,
    int NextJourneySequence,
    TravelRulesProfile TravelRules);

/// <summary>Read-only inputs for advancing a travel day.</summary>
internal record AdvanceJourneyDayContext(
    TravelRulesProfile TravelRules,
    string Salt,
    SaltSourceMode SaltMode,
    GameEntropy GameEntropy,
    int ClockDay,
    int CurrentHeat,
    PlayerCapabilities Capabilities,
    int AvailableFood,
    int AvailableHorseFeed,
    CanteenState? CanteenState,
    HorseTravelState? HorseState,
    decimal PlayerCash,
    int PlayerHealth,
    int AvailableAmmo);

/// <summary>Read-only inputs for resolving a journey encounter.</summary>
internal sealed record ResolveJourneyEncounterContext(
    TravelRulesProfile TravelRules,
    string Salt,
    SaltSourceMode SaltMode,
    GameEntropy GameEntropy,
    int ClockDay,
    int CurrentHeat,
    PlayerCapabilities Capabilities,
    int AvailableFood,
    int AvailableHorseFeed,
    CanteenState? CanteenState,
    HorseTravelState? HorseState,
    decimal PlayerCash,
    int PlayerHealth,
    string ChoiceId,
    int? BulletSpend,
    decimal? BribeAmount,
    ulong? ForcedRoll,
    int AvailableRevolverAmmo,
    int AvailableRifleAmmo,
    bool HasKnife) : AdvanceJourneyDayContext(
        TravelRules, Salt, SaltMode, GameEntropy, ClockDay, CurrentHeat, Capabilities,
        AvailableFood, AvailableHorseFeed, CanteenState, HorseState,
        PlayerCash, PlayerHealth, AvailableRevolverAmmo + AvailableRifleAmmo);

/// <summary>Read-only inputs for acknowledging journey arrival.</summary>
internal sealed record AcknowledgeJourneyArrivalContext(
    TravelRulesProfile TravelRules);

/// <summary>Read-only inputs for forcing a dev travel override.</summary>
internal sealed record ForceDevTravelOverrideContext(
    DevTravelOverride Override);

/// <summary>
/// Player capabilities snapshot for travel decisions. Computed by
/// the parent aggregate from Player state and passed to JourneyLoop as read-only context.
/// </summary>
internal sealed record PlayerCapabilities(
    bool MountedTravelAvailable,
    bool FirearmThreatAvailable);

/// <summary>
/// Result from a JourneyLoop command method. Carries the public result object
/// plus events that the parent aggregate must produce. JourneyLoop does not produce events.
/// </summary>
internal sealed record JourneyLoopResult<TResult>(TResult Result, IReadOnlyList<IDomainEvent> Events);
