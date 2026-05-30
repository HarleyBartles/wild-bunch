namespace WildBunch.Domain.Travel;

public sealed record JourneyEncounterChoiceState(string Id, string Label);

public sealed record JourneyFoeProfile(
    int Speed,
    int FightStrength,
    decimal MinimumBribe)
{
    public string DescribeSpeedBand()
        => Speed switch
        {
            <= 2 => "slow",
            3 or 4 => "steady",
            5 or 6 => "quick",
            7 or 8 => "fast",
            _ => "hard to shake"
        };

    public string DescribeFightBand()
        => FightStrength switch
        {
            <= 2 => "light-handed",
            3 or 4 => "hard-handed",
            5 or 6 => "dangerous",
            7 or 8 => "mean",
            _ => "brutal"
        };

    public string DescribeBribeBand()
        => MinimumBribe switch
        {
            <= 3m => "cheap",
            <= 5m => "reasonable",
            <= 8m => "pricey",
            <= 12m => "greedy",
            _ => "very greedy"
        };
}

public sealed record JourneyEncounterState(
    string Kind,
    string Message,
    IReadOnlyList<JourneyEncounterChoiceState> Choices,
    JourneyFoeProfile? FoeProfile = null,
    int ResolutionAttempts = 0)
{
    public static JourneyEncounterState CreateFoe(string message, JourneyFoeProfile foeProfile)
        => new(
            "foe",
            message,
            new[]
            {
                new JourneyEncounterChoiceState("run", "Run"),
                new JourneyEncounterChoiceState("fight", "Fight"),
                new JourneyEncounterChoiceState("bribe", "Bribe")
            },
            foeProfile);

    public static JourneyEncounterState CreateChoiceEncounter(
        string kind,
        string message,
        IReadOnlyList<JourneyEncounterChoiceState>? choices = null,
        JourneyFoeProfile? foeProfile = null)
        => new(
            kind,
            message,
            choices ?? new[]
            {
                new JourneyEncounterChoiceState("run", "Run"),
                new JourneyEncounterChoiceState("fight", "Fight"),
                new JourneyEncounterChoiceState("bribe", "Bribe")
            },
            foeProfile);

    public JourneyEncounterState IncrementResolutionAttempts()
        => this with { ResolutionAttempts = ResolutionAttempts + 1 };
}
