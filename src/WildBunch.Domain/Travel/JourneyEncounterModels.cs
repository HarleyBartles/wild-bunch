namespace WildBunch.Domain.Travel;

public sealed record JourneyEncounterChoiceState(string Id, string Label);

public sealed record JourneyEncounterState(
    string Kind,
    string Message,
    IReadOnlyList<JourneyEncounterChoiceState> Choices)
{
    public static JourneyEncounterState CreateFoe(string message)
        => new(
            "foe",
            message,
            new[]
            {
                new JourneyEncounterChoiceState("run", "Run"),
                new JourneyEncounterChoiceState("fight", "Fight"),
                new JourneyEncounterChoiceState("bribe", "Bribe")
            });

    public static JourneyEncounterState CreateChoiceEncounter(
        string kind,
        string message,
        IReadOnlyList<JourneyEncounterChoiceState>? choices = null)
        => new(
            kind,
            message,
            choices ?? new[]
            {
                new JourneyEncounterChoiceState("run", "Run"),
                new JourneyEncounterChoiceState("fight", "Fight"),
                new JourneyEncounterChoiceState("bribe", "Bribe")
            });
}
