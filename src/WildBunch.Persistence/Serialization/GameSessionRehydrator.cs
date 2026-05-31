using System.Reflection;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;

namespace WildBunch.Persistence.Serialization;

internal static class GameSessionRehydrator
{
    private static readonly ConstructorInfo? Constructor = typeof(GameSession).GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic,
        binder: null,
        new[]
        {
            typeof(GameSessionId),
            typeof(Player),
            typeof(DomainWorld),
            typeof(CaseFile),
            typeof(PursuitState),
            typeof(GameClock),
            typeof(GameStatus),
            typeof(TravelJourney),
            typeof(TravelDifficulty),
            typeof(TravelRandomnessState)
        },
        modifiers: null);

    private static readonly FieldInfo? LogEntriesField = typeof(GameSession).GetField("_logEntries", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo? TravelDiaryDaysField = typeof(GameSession).GetField("_travelDiaryDays", BindingFlags.Instance | BindingFlags.NonPublic);

    public static GameSession Create(
        GameSessionId id,
        Player player,
        DomainWorld world,
        CaseFile caseFile,
        PursuitState pursuitState,
        GameClock clock,
        GameStatus status,
        TravelJourney? journey,
        TravelDifficulty travelDifficulty,
        TravelRandomnessState travelRandomness)
    {
        if (Constructor is null)
        {
            throw new InvalidOperationException("Unable to locate the GameSession persistence constructor.");
        }

        return (GameSession)Constructor.Invoke(new object?[] { id, player, world, caseFile, pursuitState, clock, status, journey, travelDifficulty, travelRandomness });
    }

    public static void ReplaceLogEntries(GameSession session, IReadOnlyList<GameLogEntry> logEntries)
    {
        if (LogEntriesField?.GetValue(session) is not List<GameLogEntry> entries)
        {
            throw new InvalidOperationException("Unable to access game log entries for rehydration.");
        }

        entries.Clear();
        entries.AddRange(logEntries);
    }

    public static void ReplaceTravelDiaryDays(GameSession session, IReadOnlyList<TravelDiaryDayState> travelDiaryDays)
    {
        if (TravelDiaryDaysField?.GetValue(session) is not List<TravelDiaryDayState> entries)
        {
            throw new InvalidOperationException("Unable to access travel diary entries for rehydration.");
        }

        entries.Clear();
        entries.AddRange(travelDiaryDays);
    }

    public static void SetBackingField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Unable to access field {fieldName} on {target.GetType().Name}.");

        field.SetValue(target, value);
    }
}
