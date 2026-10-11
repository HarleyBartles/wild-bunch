namespace WildBunch.Domain.Cases;

public sealed record CaseOpeningLead(string Description)
{
    public static CaseOpeningLead Create(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return new CaseOpeningLead(description.Trim());
    }
}

public sealed record KillerReleaseState(int Progress, int RequiredGangSettlements)
{
    public bool IsReleased => Progress >= RequiredGangSettlements;

    public int RemainingGangSettlements => Math.Max(0, RequiredGangSettlements - Progress);

    public string StatusText => IsReleased
        ? "The killer trail is released."
        : $"The killer trail is locked until {RemainingGangSettlements} more gang member{(RemainingGangSettlements == 1 ? " is" : "s are")} brought to justice.";
}
