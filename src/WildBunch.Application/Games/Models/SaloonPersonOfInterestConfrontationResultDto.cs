using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Models;

public sealed record SaloonPersonOfInterestConfrontationResultDto(
    bool Success,
    string Message,
    SaloonPersonOfInterestConfrontationOutcome Outcome,
    GameSessionDto CurrentSession,
    string? DeclaredWantedIdentityHandle,
    string? TargetName,
    WarrantDisposition? Disposition,
    bool? IsAlive,
    bool? IsSecured,
    bool? IsCitizen,
    decimal? FineAmount,
    decimal? WalletBefore,
    decimal? WalletAfter,
    bool SessionChanged);
