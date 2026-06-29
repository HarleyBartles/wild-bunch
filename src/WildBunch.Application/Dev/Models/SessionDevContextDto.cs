namespace WildBunch.Application.Dev.Models;

/// <summary>
/// Dev-only DTO exposing session-level setup and control state: identity,
/// status, phase/clock, current town, difficulty, entropy, salt posture,
/// and derived travel-rule facts. Guarded by DevRoleGuard and separated
/// from player DTOs. Per ADR-0030 §7 and the dev-overlay doctrine.
/// See BUNCH-101, BUNCH-94.
/// </summary>
public sealed record SessionDevContextDto(
    Guid SessionId,
    string Status,
    string GameDifficulty,
    string GameEntropy,
    SaltPostureDevDto SaltPosture,
    ClockDevDto Clock,
    string? CurrentTownId,
    string? CurrentTownName,
    string CurrentActionContext,
    bool HasActiveJourney,
    bool SeedCodeRetained,
    string? SeedCodeText,
    TravelRulesDevDto? TravelRules);

/// <summary>
/// Dev-only DTO exposing the current RNG salt posture.
/// Mode is "Runtime" or "Fixed". Salt is the current salt token.
/// </summary>
public sealed record SaltPostureDevDto(
    string Mode,
    string? Salt);

/// <summary>
/// Dev-only DTO exposing the session clock (day, turn, time of day).
/// </summary>
public sealed record ClockDevDto(
    int Day,
    int Turn,
    string TimeOfDay);

/// <summary>
/// Dev-only DTO exposing derived travel-rule facts from the session's
/// TravelRulesProfile. These facts change when difficulty changes, so the
/// dev panel can show the difficulty envelope shift, not just a label swap.
/// See BUNCH-94.
/// </summary>
public sealed record TravelRulesDevDto(
    int CanteenCapacity,
    decimal MountedRideDayProgress,
    decimal FootRideDayProgress,
    int EncounterFightAmmoHealthLoss,
    int EncounterFightUnarmedHealthLoss,
    int EncounterRunFootHealthLoss);
