using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Models;

public sealed record ArchivePlaythroughResultDto(
    Guid SessionId,
    GameStatus Status,
    string PlayerName,
    string? LastTownId,
    string? LastTownName,
    int Day,
    string Turn);
