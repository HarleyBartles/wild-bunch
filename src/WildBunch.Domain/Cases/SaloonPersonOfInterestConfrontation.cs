namespace WildBunch.Domain.Cases;

public enum SaloonPersonOfInterestKind
{
    Citizen = 0,
    WantedSuspect = 1
}

public enum SaloonPersonOfInterestConfrontationOutcome
{
    Surrendered = 0,
    Rejected = 1,
    WrongWantedDeclaration = 2,
    Refused = 3,
    CitizenReleased = 4
}

public sealed record SaloonPersonOfInterestConfrontationResult(
    bool Success,
    string Message,
    SaloonPersonOfInterestConfrontationOutcome Outcome,
    string? DeclaredWantedIdentityHandle,
    string? TargetName,
    WarrantDisposition? Disposition,
    bool? IsAlive,
    bool? IsSecured,
    bool? IsCitizen,
    decimal? FineAmount,
    decimal? WalletBefore,
    decimal? WalletAfter,
    bool SessionChanged,
    SaloonPersonOfInterestKind? PersonOfInterestKind = null)
{
    public static SaloonPersonOfInterestConfrontationResult Surrendered(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Surrendered, declaredWantedIdentityHandle, targetName, disposition, true, true, false, null, null, null, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static SaloonPersonOfInterestConfrontationResult WrongWantedDeclaration(
        string? declaredWantedIdentityHandle,
        string targetName,
        string message,
        decimal fineAmount,
        decimal walletBefore,
        decimal walletAfter)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, declaredWantedIdentityHandle, targetName, null, true, false, false, fineAmount, walletBefore, walletAfter, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static SaloonPersonOfInterestConfrontationResult Refused(
        string? declaredWantedIdentityHandle,
        string targetName,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Refused, declaredWantedIdentityHandle, targetName, null, true, false, false, null, null, null, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static SaloonPersonOfInterestConfrontationResult CitizenReleased(
        string? declaredWantedIdentityHandle,
        string targetName,
        string message,
        decimal fineAmount,
        decimal walletBefore,
        decimal walletAfter)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.CitizenReleased, declaredWantedIdentityHandle, targetName, null, true, true, true, fineAmount, walletBefore, walletAfter, true, SaloonPersonOfInterestKind.Citizen);

    public static SaloonPersonOfInterestConfrontationResult Rejected(
        string message,
        string? declaredWantedIdentityHandle = null,
        string? targetName = null,
        WarrantDisposition? disposition = null,
        bool sessionChanged = false,
        SaloonPersonOfInterestKind? personOfInterestKind = null)
        => new(false, message, SaloonPersonOfInterestConfrontationOutcome.Rejected, declaredWantedIdentityHandle, targetName, disposition, null, null, null, null, null, null, sessionChanged, personOfInterestKind);
}
