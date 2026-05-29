namespace WildBunch.Domain.Cases;

public sealed record CaseOpeningLead(string Description)
{
    public static CaseOpeningLead Create(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return new CaseOpeningLead(description.Trim());
    }
}

public sealed record KillerReleaseState(int Progress, int RequiredPublicClues)
{
    public bool IsReleased => Progress >= RequiredPublicClues;

    public string StatusText => IsReleased
        ? "The killer trail is released."
        : $"The killer trail is locked until {RequiredPublicClues - Progress} more public clue(s) surface.";
}
