using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

public sealed record GameSessionDto(
    Guid Id,
    GameStatus Status,
    PlayerDto Player,
    WorldDto World,
    CaseFileDto CaseFile,
    GameClockDto Clock,
    PursuitStateDto PursuitState,
    IReadOnlyList<GameLogEntryDto> LogEntries);

public sealed record PlayerDto(
    string Name,
    string CurrentTownId,
    int Health,
    decimal Money,
    int Supplies);

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
    int SupplyCost,
    TrailRisk Risk);

public sealed record CaseFileDto(
    string? AccusationId,
    IReadOnlyList<SuspectDto> Suspects,
    IReadOnlyList<ClueDto> KnownClues);

public sealed record SuspectDto(
    string Id,
    string Name,
    SuspectTraitsDto Traits,
    SuspectStatus Status);

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

public sealed record GameLogEntryDto(
    GameLogEntryKind Kind,
    string Message,
    int Day,
    int Turn);
