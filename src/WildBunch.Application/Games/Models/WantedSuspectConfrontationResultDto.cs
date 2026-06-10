using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Models;

public sealed record WantedSuspectConfrontationResultDto(
    bool Success,
    string Message,
    WantedSuspectConfrontationOutcome Outcome,
    GameSessionDto CurrentSession,
    string? TargetName,
    WarrantDisposition? Disposition,
    bool? IsAlive,
    bool? IsSecured,
    bool SessionChanged);
