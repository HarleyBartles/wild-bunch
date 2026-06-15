namespace WildBunch.Domain.Cases;

public enum SaloonPersonOfInterestKind
{
    Citizen = 0,
    WantedSuspect = 1
}

public enum SaloonPersonOfInterestConfrontationOutcome
{
    Surrendered = 0,
    Fled = 1,
    Killed = 2,
    Abandoned = 3,
    Rejected = 4,
    WrongWantedDeclaration = 5
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

    public static SaloonPersonOfInterestConfrontationResult Fled(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Fled, declaredWantedIdentityHandle, targetName, disposition, true, false, false, null, null, null, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static SaloonPersonOfInterestConfrontationResult Killed(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Killed, declaredWantedIdentityHandle, targetName, disposition, false, true, false, null, null, null, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static SaloonPersonOfInterestConfrontationResult Abandoned(
        string? declaredWantedIdentityHandle,
        string targetName,
        WarrantDisposition? disposition,
        string message)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.Abandoned, declaredWantedIdentityHandle, targetName, disposition, null, null, null, null, null, null, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static SaloonPersonOfInterestConfrontationResult WrongWantedDeclaration(
        string? declaredWantedIdentityHandle,
        string targetName,
        string message,
        decimal fineAmount,
        decimal walletBefore,
        decimal walletAfter)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, declaredWantedIdentityHandle, targetName, null, null, null, true, fineAmount, walletBefore, walletAfter, true, SaloonPersonOfInterestKind.WantedSuspect);

    public static SaloonPersonOfInterestConfrontationResult WrongWantedDeclaration(
        string? declaredWantedIdentityHandle,
        string targetName,
        string message,
        decimal fineAmount,
        decimal walletBefore,
        decimal walletAfter,
        bool isCitizen,
        bool? isAlive,
        bool? isSecured)
        => new(true, message, SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, declaredWantedIdentityHandle, targetName, null, isAlive, isSecured, isCitizen, fineAmount, walletBefore, walletAfter, true, isCitizen ? SaloonPersonOfInterestKind.Citizen : SaloonPersonOfInterestKind.WantedSuspect);

    public static SaloonPersonOfInterestConfrontationResult Rejected(
        string message,
        string? declaredWantedIdentityHandle = null,
        string? targetName = null,
        WarrantDisposition? disposition = null,
        bool sessionChanged = false,
        SaloonPersonOfInterestKind? personOfInterestKind = null)
        => new(false, message, SaloonPersonOfInterestConfrontationOutcome.Rejected, declaredWantedIdentityHandle, targetName, disposition, null, null, null, null, null, null, sessionChanged, personOfInterestKind);

    public static SaloonPersonOfInterestConfrontationResult FromWantedSuspectResult(WantedSuspectConfrontationResult result)
        => new(
            result.Success,
            result.Message,
            (SaloonPersonOfInterestConfrontationOutcome)result.Outcome,
            result.DeclaredWantedIdentityHandle,
            result.TargetName,
            result.Disposition,
            result.IsAlive,
            result.IsSecured,
            false,
            null,
            null,
            null,
            result.SessionChanged,
            SaloonPersonOfInterestKind.WantedSuspect);

    public WantedSuspectConfrontationResult ToWantedSuspectResult()
        => new(
            Success,
            Message,
            (WantedSuspectConfrontationOutcome)Outcome,
            DeclaredWantedIdentityHandle,
            TargetName,
            Disposition,
            IsAlive,
            IsSecured,
            SessionChanged);
}
