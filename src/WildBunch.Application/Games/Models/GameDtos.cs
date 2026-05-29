using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

public sealed record GameSessionDto(
    Guid Id,
    GameStatus Status,
    PlayerDto Player,
    WorldDto World,
    CaseFileDto CaseFile,
    InventoryDto Inventory,
    GameClockDto Clock,
    PursuitStateDto PursuitState,
    IReadOnlyList<GameLogEntryDto> LogEntries);

public sealed record PlayerDto(
    string Name,
    string CurrentTownId,
    int Health);

public sealed record InventoryDto(
    WalletDto Wallet,
    IReadOnlyList<InventoryItemDto> Items,
    HorseCondition? HorseCondition,
    InventoryCapabilitiesDto Capabilities);

public sealed record WalletDto(decimal Cash);

public sealed record InventoryItemDto(
    ItemKind Kind,
    int Quantity,
    HorseCondition? HorseCondition);

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
    TrailRisk Risk);

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
    GameSessionDto CurrentSession);

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
