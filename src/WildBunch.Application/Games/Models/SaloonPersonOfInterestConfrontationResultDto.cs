using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Models;

public sealed record SaloonPersonOfInterestConfrontationResultDto(
    bool Success,
    string Message,
    SaloonPersonOfInterestConfrontationOutcome Outcome,
    GameSessionDto CurrentSession,
    string? TargetName,
    WarrantDisposition? Disposition,
    bool? IsAlive,
    bool? IsSecured,
    bool SessionChanged);
