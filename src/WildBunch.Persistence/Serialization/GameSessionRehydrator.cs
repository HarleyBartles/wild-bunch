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
            typeof(GameDifficulty),
            typeof(SaltSource),
            typeof(GameEntropy),
            typeof(TownVisitState),
            typeof(IReadOnlyList<TravelJourneySnapshot>),
            typeof(IReadOnlyList<WantedSuspectPresenceEntry>)
        },
        modifiers: null);

    public static GameSession Create(
        GameSessionId id,
        Player player,
        DomainWorld world,
        CaseFile caseFile,
        PursuitState pursuitState,
        GameClock clock,
        GameStatus status,
        TravelJourney? journey,
        GameDifficulty gameDifficulty,
        SaltSource saltSource,
        GameEntropy entropy,
        TownVisitState? townVisitState,
        IReadOnlyList<TravelJourneySnapshot>? completedJourneyHistory,
        IReadOnlyList<WantedSuspectPresenceEntry>? wantedSuspectPresenceEntries)
    {
        if (Constructor is null)
        {
            throw new InvalidOperationException("Unable to locate the GameSession persistence constructor.");
        }

        return (GameSession)Constructor.Invoke(new object?[] { id, player, world, caseFile, pursuitState, clock, status, journey, gameDifficulty, saltSource, entropy, townVisitState, completedJourneyHistory ?? Array.Empty<TravelJourneySnapshot>(), wantedSuspectPresenceEntries ?? Array.Empty<WantedSuspectPresenceEntry>() });
    }

    public static void ReplaceTravelDiaryDays(GameSession session, IReadOnlyList<TravelDiaryDayState> travelDiaryDays)
    {
        session.ReplaceTravelDiaryDays(travelDiaryDays);
    }

    public static void SetBackingField<T>(object target, string fieldName, T value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Unable to access field {fieldName} on {target.GetType().Name}.");

        field.SetValue(target, value);
    }

    /// <summary>
    /// Sets the session's stream version when loading from snapshot.
    /// The snapshot is at version N; the session must know its version
    /// so the next command's optimistic concurrency check works.
    /// See ADR-0028.
    /// </summary>
    public static void SetVersion(GameSession session, int version)
    {
        SetBackingField(session, "_version", version);
    }

    /// <summary>
    /// Restores the session's ActionContextTracker-owned state (CurrentActionContext and
    /// the town it was entered in) when loading from snapshot. These are also reconstructed
    /// from event replay via Apply(TownActionContextEntered). Both paths (snapshot load +
    /// event replay) must produce the same values. See ADR-0028, BUNCH-80, and BUNCH-120.
    /// </summary>
    public static void RestoreActionContextState(GameSession session, TownActionContext context, TownId? townId)
    {
        session.RestoreActionContextState(context, townId);
    }
}
