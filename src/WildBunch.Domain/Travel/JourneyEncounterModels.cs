namespace WildBunch.Domain.Travel;

public sealed record JourneyEncounterChoiceState(string Id, string Label);

public sealed record JourneyEncounterHiddenState(
    int BribeOffersMade = 0,
    decimal CumulativeBribePaid = 0m,
    bool BribeLockedOut = false,
    int ChaseFatigue = 0,
    int Annoyance = 0,
    bool Shaken = false)
{
    public JourneyEncounterHiddenState RecordBribeOffer(decimal cumulativeBribePaid, bool bribeLockedOut)
        => this with
        {
            BribeOffersMade = BribeOffersMade + 1,
            CumulativeBribePaid = cumulativeBribePaid,
            BribeLockedOut = bribeLockedOut
        };

    public JourneyEncounterHiddenState RecordFailedRun()
        => this with
        {
            ChaseFatigue = Math.Min(3, ChaseFatigue + 1)
        };

    public JourneyEncounterHiddenState RecordFightPressure(bool shookTheFoe, bool annoyedTheFoe)
        => this with
        {
            Shaken = Shaken || shookTheFoe,
            Annoyance = annoyedTheFoe ? Math.Min(3, Annoyance + 1) : Annoyance
        };
}

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
    int ResolutionAttempts = 0,
    JourneyEncounterHiddenState? HiddenState = null)
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
            foeProfile,
            HiddenState: new JourneyEncounterHiddenState());

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

    public JourneyEncounterState WithHiddenState(JourneyEncounterHiddenState hiddenState)
        => this with { HiddenState = hiddenState };

    public JourneyEncounterState WithoutChoice(string choiceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(choiceId);

        var filteredChoices = Choices
            .Where(choice => !string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return filteredChoices.Length == Choices.Count ? this : this with { Choices = filteredChoices };
    }

    public bool HasChoice(string choiceId)
        => Choices.Any(choice => string.Equals(choice.Id, choiceId, StringComparison.OrdinalIgnoreCase));
}
