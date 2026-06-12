using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Models;

public sealed record SheriffTurnInResultDto(
    bool Success,
    string Message,
    SheriffTurnInOutcome Outcome,
    GameSessionDto CurrentSession,
    string? TargetName,
    WarrantDisposition? Disposition,
    decimal? BountyAmount);
