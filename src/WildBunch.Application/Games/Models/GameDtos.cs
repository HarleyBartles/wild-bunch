using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

public sealed record GameSessionDto(
    Guid Id,
    GameStatus Status,
    TravelDifficulty TravelDifficulty,
    PlayerDto Player,
    WorldDto World,
    CaseFileDto CaseFile,
    InventoryDto Inventory,
    GameClockDto Clock,
    PursuitStateDto PursuitState,
    TravelJourneyDto? Journey,
    IReadOnlyList<GameLogEntryDto> LogEntries);

public sealed record PlayerDto(
    string Name,
    string CurrentTownId,
    int Health);

public sealed record InventoryDto(
    WalletDto Wallet,
    IReadOnlyList<InventoryItemDto> Items,
    HorseTravelStateDto? HorseState,
    CanteenStateDto? CanteenState,
    InventoryCapabilitiesDto Capabilities);

public sealed record WalletDto(decimal Cash);

public sealed record InventoryItemDto(
    ItemKind Kind,
    int Quantity,
    HorseTravelStateDto? HorseState,
    CanteenStateDto? CanteenState);

public sealed record HorseTravelStateDto(
    int Hunger,
    int Thirst,
    int Exhaustion,
    bool IsLame,
    bool IsDead,
    bool CanProvideMountedTravel);

public sealed record CanteenStateDto(
    int Charges,
    int Capacity,
    bool HasWater);

public sealed record InventoryCapabilitiesDto(
    bool MountedTravelAvailable,
    bool HorseUpkeepRequired,
    bool NormalRouteWaterSecure,
    bool TrailUtility,
    bool CloseThreatAvailable,
    bool FirearmThreatAvailable,
    bool GunfightCapable,
    bool RevolverUsable,
    bool RifleUsable);

public sealed record WorldDto(
    IReadOnlyList<TownDto> Towns,
    IReadOnlyList<TrailDto> Trails);

public sealed record TownDto(
    string Id,
    string Name,
    TownServices Services);

public sealed record TrailDto(
    string Id,
    string FromTownId,
    string ToTownId,
    TrailRisk Risk,
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    decimal RideDayDistance);

public sealed record CaseFileDto(
    string? AccusationId,
    string OpeningLead,
    KillerReleaseStateDto KillerReleaseState,
    IReadOnlyList<SuspectDto> Suspects,
    IReadOnlyList<ClueDto> KnownClues);

public sealed record SuspectDto(
    string Id,
    string Name,
    SuspectProfileDto Profile,
    SuspectTraitsDto Traits,
    SuspectStatus Status);

public sealed record SuspectProfileDto(
    IReadOnlyList<SuspectAliasDto> Aliases,
    IReadOnlyList<SuspectIdentityFactDto> IdentifyingFacts);

public sealed record SuspectAliasDto(
    string Name,
    AliasKind Kind);

public sealed record SuspectIdentityFactDto(string Description);

public sealed record SuspectTraitsDto(
    bool IsLocal,
    bool IsArmed,
    bool IsDesperate);

public sealed record ClueDto(
    string Id,
    ClueKind Kind,
    string Description);

public sealed record GameTurnResultDto(
    bool Success,
    string Message,
    GameSessionDto CurrentSession,
    JourneyStatus? JourneyStatus = null,
    TravelJourneyDto? Journey = null,
    JourneyTrailEventDto? TrailEvent = null);

public sealed record TravelPreviewDto(
    string OriginTownId,
    string OriginTownName,
    string DestinationTownId,
    string DestinationTownName,
    TravelMode TravelMode,
    bool MountedTravelAvailable,
    bool WaterSecure,
    decimal RideDayDistance,
    decimal RemainingRideDayDistance,
    int ExpectedDays,
    int RemainingDays,
    int CanteenChargesPerDay,
    int RequiredCanteenCharges,
    int AvailableCanteenCharges,
    int CanteenReserveCharges,
    int DelayMarginDays,
    bool DelayRisk,
    int RequiredFood,
    int AvailableFood,
    int RequiredHorseFeed,
    int AvailableHorseFeed,
    HorseTravelStateDto? HorseState,
    IReadOnlyList<string> Warnings,
    TravelRouteProfileDto RouteProfile);

public sealed record TravelPreviewResultDto(
    bool Success,
    string Message,
    TravelPreviewDto? Preview);

public sealed record TravelRouteProfileDto(
    string TrailId,
    TrailRisk Risk,
    TrailTerrain Terrain,
    WaterFeature WaterFeature,
    decimal RideDayDistance,
    decimal MountedRideDayProgress,
    decimal FootRideDayProgress,
    IReadOnlyList<string> Warnings);

public sealed record TravelJourneyDto(
    string OriginTownId,
    string OriginTownName,
    string DestinationTownId,
    string DestinationTownName,
    TravelMode TravelMode,
    JourneyStatus Status,
    bool MountedTravelAvailable,
    bool WaterSecure,
    decimal RideDayDistance,
    decimal RemainingRideDayDistance,
    int ExpectedDays,
    int RemainingDays,
    int CanteenChargesPerDay,
    int RequiredCanteenCharges,
    int AvailableCanteenCharges,
    int CanteenReserveCharges,
    int DelayMarginDays,
    bool DelayRisk,
    int RequiredFood,
    int AvailableFood,
    int RequiredHorseFeed,
    int AvailableHorseFeed,
    HorseTravelStateDto? HorseState,
    int DaysTravelled,
    int DelayDays,
    JourneyEncounterDto? PendingEncounter,
    IReadOnlyList<string> Warnings,
    TravelRouteProfileDto RouteProfile);

public sealed record JourneyEncounterDto(
    string Kind,
    string Message,
    IReadOnlyList<JourneyEncounterChoiceDto> Choices);

public sealed record JourneyEncounterChoiceDto(
    string Id,
    string Label);

public sealed record JourneyTrailEventDto(
    JourneyTrailEventId Id,
    JourneyTrailEventKind Kind,
    string Title,
    string Message,
    decimal WalletDelta,
    int FoodDelta,
    int CanteenChargeDelta,
    int HorseHungerDelta,
    int HorseThirstDelta,
    int HorseExhaustionDelta,
    int DelayDays,
    int HeatIncrease);

public sealed record GameClockDto(int Day, int Turn);

public sealed record PursuitStateDto(int Heat);

public sealed record KillerReleaseStateDto(
    bool IsReleased,
    int Progress,
    int RequiredPublicClues,
    string StatusText);

public sealed record GameLogEntryDto(
    GameLogEntryKind Kind,
    string Message,
    int Day,
    int Turn);
