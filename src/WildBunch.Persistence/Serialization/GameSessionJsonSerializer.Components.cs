using System.Reflection;
using System.Text.Json;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    public string SerializePlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return JsonSerializer.Serialize(PlayerSnapshot.FromDomain(player), Options);
    }

    public Player DeserializePlayer(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<PlayerSnapshot>(json);
        return PlayerSnapshot.ToDomain(snapshot);
    }

    public string SerializeWorld(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return JsonSerializer.Serialize(WorldSnapshot.FromDomain(world), Options);
    }

    public World DeserializeWorld(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<WorldSnapshot>(json);
        return WorldSnapshot.ToDomain(snapshot);
    }

    public string SerializeCaseFile(CaseFile caseFile)
    {
        ArgumentNullException.ThrowIfNull(caseFile);
        return JsonSerializer.Serialize(CaseFileSnapshot.FromDomain(caseFile), Options);
    }

    public CaseFile DeserializeCaseFile(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<CaseFileSnapshot>(json);
        return CaseFileSnapshot.ToDomain(snapshot);
    }

    public string SerializeClock(GameClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return JsonSerializer.Serialize(GameClockSnapshot.FromDomain(clock), Options);
    }

    public GameClock DeserializeClock(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<GameClockSnapshot>(json);
        return GameClockSnapshot.ToDomain(snapshot);
    }

    public string SerializePursuitState(PursuitState pursuitState)
    {
        ArgumentNullException.ThrowIfNull(pursuitState);
        return JsonSerializer.Serialize(PursuitStateSnapshot.FromDomain(pursuitState), Options);
    }

    public PursuitState DeserializePursuitState(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<PursuitStateSnapshot>(json);
        return PursuitStateSnapshot.ToDomain(snapshot);
    }

    public string SerializeTravelRandomness(TravelRandomnessState travelRandomness)
    {
        ArgumentNullException.ThrowIfNull(travelRandomness);
        return JsonSerializer.Serialize(TravelRandomnessSnapshot.FromDomain(travelRandomness), Options);
    }

    public TravelRandomnessState DeserializeTravelRandomness(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var snapshot = Deserialize<TravelRandomnessSnapshot>(json);
        return snapshot.ToDomain();
    }

    public string SerializeJourneySnapshot(TravelJourneySnapshot? journey)
        => journey is null ? string.Empty : JsonSerializer.Serialize(JourneySnapshot.FromDomain(journey), Options);

    public TravelJourneySnapshot? DeserializeJourneySnapshot(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return Deserialize<JourneySnapshot>(json).ToDomain();
    }

    public GameSession RehydrateGameSession(
        Guid id,
        GameStatus status,
        TravelDifficulty travelDifficulty,
        Player player,
        World world,
        CaseFile caseFile,
        GameClock clock,
        PursuitState pursuitState,
        TravelRandomnessState travelRandomness,
        TravelJourneySnapshot? journey,
        IReadOnlyList<TravelDiaryDayState> travelDiaryDays,
        IReadOnlyList<GameLogEntry> logEntries)
    {
        var session = GameSessionRehydrator.Create(
            new GameSessionId(id),
            player,
            world,
            caseFile,
            pursuitState,
            clock,
            status,
            journey is null ? null : TravelJourney.FromSnapshot(journey),
            travelDifficulty,
            travelRandomness);

        GameSessionRehydrator.ReplaceTravelDiaryDays(session, travelDiaryDays);
        GameSessionRehydrator.ReplaceLogEntries(session, logEntries);
        return session;
    }

    public string SerializeTravelDiaryDay(TravelDiaryDayState day)
    {
        ArgumentNullException.ThrowIfNull(day);
        return JsonSerializer.Serialize(TravelDiaryDaySnapshot.FromDomain(day), Options);
    }

    public TravelDiaryDayState DeserializeTravelDiaryDay(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return Deserialize<TravelDiaryDaySnapshot>(json).ToDomain();
    }

    private static T Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new InvalidOperationException($"Unable to deserialize {typeof(T).Name}.");

    private static TProperty GetInternalProperty<TProperty>(object target, string propertyName)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Unable to access property {propertyName} on {target.GetType().Name}.");

        return (TProperty)(property.GetValue(target) ?? throw new InvalidOperationException($"Property {propertyName} on {target.GetType().Name} was null."));
    }

    private static void SetInternalProperty<TProperty>(object target, string propertyName, TProperty value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Unable to access property {propertyName} on {target.GetType().Name}.");

        property.SetValue(target, value);
    }

    private sealed record TravelDiaryDaySnapshot(
        int DayNumber,
        string OriginTownName,
        string DestinationTownName,
        TravelMode StartingTravelMode,
        TravelMode EndingTravelMode,
        JourneyStatus Status,
        decimal StartingRideDayDistance,
        decimal RemainingRideDayDistance,
        int StartingDaysRemaining,
        int RemainingDays,
        HorseTravelState? HorseStateBefore,
        HorseTravelState? HorseStateAfter,
        JourneyTrailEventState? TrailEvent,
        JourneyEncounterState? PendingEncounter,
        TravelDiaryEncounterResolutionState? EncounterResolution,
        string? OpeningNarration,
        string? JourneyBeat,
        string? ResourceBeat,
        IReadOnlyList<string> Entries,
        int HealthDelta,
        decimal WalletDelta,
        int FoodDelta,
        int HorseFeedDelta,
        int CanteenChargeDelta,
        int AmmoSpent,
        int HorseHungerDelta,
        int HorseThirstDelta,
        int HorseExhaustionDelta,
        int DelayDays,
        int HeatIncrease,
        int CurrentHealth,
        decimal CurrentWallet,
        int CurrentFood,
        int CurrentHorseFeed,
        int CurrentCanteenCharges,
        int CurrentAmmo,
        int CurrentHeat,
        IReadOnlyList<string> Warnings,
        TrailTerrain Terrain,
        bool RouteWaterSecure,
        int CanteenChargesPerDay)
    {
        public static TravelDiaryDaySnapshot FromDomain(TravelDiaryDayState day)
            => new(
                day.DayNumber,
                day.OriginTownName,
                day.DestinationTownName,
                day.StartingTravelMode,
                day.EndingTravelMode,
                day.Status,
                day.StartingRideDayDistance,
                day.RemainingRideDayDistance,
                day.StartingDaysRemaining,
                day.RemainingDays,
                day.HorseStateBefore,
                day.HorseStateAfter,
                day.TrailEvent,
                day.PendingEncounter,
                day.EncounterResolution,
                day.OpeningNarration,
                day.JourneyBeat,
                day.ResourceBeat,
                day.Entries.ToArray(),
                day.HealthDelta,
                day.WalletDelta,
                day.FoodDelta,
                day.HorseFeedDelta,
                day.CanteenChargeDelta,
                day.AmmoSpent,
                day.HorseHungerDelta,
                day.HorseThirstDelta,
                day.HorseExhaustionDelta,
                day.DelayDays,
                day.HeatIncrease,
                day.CurrentHealth,
                day.CurrentWallet,
                day.CurrentFood,
                day.CurrentHorseFeed,
                day.CurrentCanteenCharges,
                day.CurrentAmmo,
                day.CurrentHeat,
                day.Warnings.ToArray(),
                GetInternalProperty<TrailTerrain>(day, "Terrain"),
                GetInternalProperty<bool>(day, "RouteWaterSecure"),
                GetInternalProperty<int>(day, "CanteenChargesPerDay"));

        public TravelDiaryDayState ToDomain()
        {
            var day = new TravelDiaryDayState(
                DayNumber,
                OriginTownName,
                DestinationTownName,
                StartingTravelMode,
                EndingTravelMode,
                Status,
                StartingRideDayDistance,
                RemainingRideDayDistance,
                StartingDaysRemaining,
                RemainingDays,
                HorseStateBefore,
                HorseStateAfter,
                TrailEvent,
                PendingEncounter,
                EncounterResolution,
                OpeningNarration,
                JourneyBeat,
                ResourceBeat,
                Entries.ToArray(),
                HealthDelta,
                WalletDelta,
                FoodDelta,
                HorseFeedDelta,
                CanteenChargeDelta,
                AmmoSpent,
                HorseHungerDelta,
                HorseThirstDelta,
                HorseExhaustionDelta,
                DelayDays,
                HeatIncrease,
                CurrentHealth,
                CurrentWallet,
                CurrentFood,
                CurrentHorseFeed,
                CurrentCanteenCharges,
                CurrentAmmo,
                CurrentHeat,
                Warnings.ToArray());

            SetInternalProperty(day, "Terrain", Terrain);
            SetInternalProperty(day, "RouteWaterSecure", RouteWaterSecure);
            SetInternalProperty(day, "CanteenChargesPerDay", CanteenChargesPerDay);
            return day;
        }
    }
}
