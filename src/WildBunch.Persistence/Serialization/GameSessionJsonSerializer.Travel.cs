using System.Reflection;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private sealed class JourneySnapshot
    {
        public string OriginTownId { get; set; } = string.Empty;
        public string DestinationTownId { get; set; } = string.Empty;
        public string OriginTownName { get; set; } = string.Empty;
        public string DestinationTownName { get; set; } = string.Empty;
        public TravelRouteProfileSnapshot RouteProfile { get; set; } = new();
        public TravelMode TravelMode { get; set; }
        public JourneyStatus Status { get; set; }
        public bool MountedTravelAvailable { get; set; }
        public bool WaterSecure { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal RemainingDistance { get; set; }
        public int ExpectedDays { get; set; }
        public int RemainingDays { get; set; }
        public int CanteenChargesPerDay { get; set; }
        public int RequiredCanteenCharges { get; set; }
        public int AvailableCanteenCharges { get; set; }
        public int CanteenReserveCharges { get; set; }
        public int DelayMarginDays { get; set; }
        public bool DelayRisk { get; set; }
        public int RequiredFood { get; set; }
        public int AvailableFood { get; set; }
        public int RequiredHorseFeed { get; set; }
        public int AvailableHorseFeed { get; set; }
        public DomainHorseTravelState? HorseState { get; set; }
        public string? OpeningNarration { get; set; }
        public int DaysTravelled { get; set; }
        public int DelayDays { get; set; }
        public TravelDayPlanSnapshot? CurrentDayPlan { get; set; }
        public JourneyEncounterSnapshot? PendingEncounter { get; set; }
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public static JourneySnapshot FromDomain(TravelJourneySnapshot snapshot)
            => new()
            {
                OriginTownId = snapshot.OriginTownId.Value,
                DestinationTownId = snapshot.DestinationTownId.Value,
                OriginTownName = snapshot.OriginTownName,
                DestinationTownName = snapshot.DestinationTownName,
                RouteProfile = TravelRouteProfileSnapshot.FromDomain(snapshot.RouteProfile),
                TravelMode = snapshot.TravelMode,
                Status = snapshot.Status,
                MountedTravelAvailable = snapshot.MountedTravelAvailable,
                WaterSecure = snapshot.WaterSecure,
                TotalDistance = snapshot.RideDayDistance,
                RemainingDistance = snapshot.RemainingRideDayDistance,
                ExpectedDays = snapshot.ExpectedDays,
                RemainingDays = snapshot.RemainingDays,
                CanteenChargesPerDay = snapshot.CanteenChargesPerDay,
                RequiredCanteenCharges = snapshot.RequiredCanteenCharges,
                AvailableCanteenCharges = snapshot.AvailableCanteenCharges,
                CanteenReserveCharges = snapshot.CanteenReserveCharges,
                DelayMarginDays = snapshot.DelayMarginDays,
                DelayRisk = snapshot.DelayRisk,
                RequiredFood = snapshot.RequiredFood,
                AvailableFood = snapshot.AvailableFood,
                RequiredHorseFeed = snapshot.RequiredHorseFeed,
                AvailableHorseFeed = snapshot.AvailableHorseFeed,
                HorseState = snapshot.HorseState,
                OpeningNarration = snapshot.OpeningNarration,
                DaysTravelled = snapshot.DaysTravelled,
                DelayDays = snapshot.DelayDays,
                CurrentDayPlan = snapshot.CurrentDayPlan is null ? null : TravelDayPlanSnapshot.FromDomain(snapshot.CurrentDayPlan),
                PendingEncounter = snapshot.PendingEncounter is null ? null : JourneyEncounterSnapshot.FromDomain(snapshot.PendingEncounter),
                Warnings = snapshot.Warnings.ToArray()
            };

        public TravelJourneySnapshot ToDomain()
            => new(
                new TownId(OriginTownId),
                new TownId(DestinationTownId),
                OriginTownName,
                DestinationTownName,
                RouteProfile.ToDomain(),
                TravelMode,
                Status,
                MountedTravelAvailable,
                WaterSecure,
                TotalDistance,
                RemainingDistance,
                ExpectedDays,
                RemainingDays,
                CanteenChargesPerDay,
                RequiredCanteenCharges,
                AvailableCanteenCharges,
                CanteenReserveCharges,
                DelayMarginDays,
                DelayRisk,
                RequiredFood,
                AvailableFood,
                RequiredHorseFeed,
                AvailableHorseFeed,
                HorseState,
                OpeningNarration,
                DaysTravelled,
                DelayDays,
                CurrentDayPlan?.ToDomain(),
                PendingEncounter?.ToDomain(),
                Warnings.ToArray());
    }

    private sealed class TravelDayPlanSnapshot
    {
        public int DayNumber { get; set; }
        public IReadOnlyList<TravelDayEncounterSnapshot> Encounters { get; set; } = Array.Empty<TravelDayEncounterSnapshot>();
        public int CurrentEncounterIndex { get; set; }
        public bool IsComplete { get; set; }

        public static TravelDayPlanSnapshot FromDomain(TravelDayPlanState dayPlan)
            => new()
            {
                DayNumber = dayPlan.DayNumber,
                Encounters = dayPlan.Encounters.Select(TravelDayEncounterSnapshot.FromDomain).ToArray(),
                CurrentEncounterIndex = dayPlan.CurrentEncounterIndex,
                IsComplete = dayPlan.IsComplete
            };

        public TravelDayPlanState ToDomain()
            => new(
                DayNumber,
                Encounters.Select(encounter => encounter.ToDomain()).ToArray(),
                CurrentEncounterIndex,
                IsComplete);
    }

    private sealed class TravelDayEncounterSnapshot
    {
        public int EncounterIndex { get; set; }
        public TravelDayEncounterCategory Category { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public JourneyTrailEventSnapshot? TrailEvent { get; set; }
        public JourneyEncounterSnapshot? PendingEncounter { get; set; }
        public TravelDiaryEncounterResolutionSnapshot? Resolution { get; set; }

        public static TravelDayEncounterSnapshot FromDomain(TravelDayEncounterState encounter)
            => new()
            {
                EncounterIndex = encounter.EncounterIndex,
                Category = encounter.Category,
                Title = encounter.Title,
                Message = encounter.Message,
                TrailEvent = encounter.TrailEvent is null ? null : JourneyTrailEventSnapshot.FromDomain(encounter.TrailEvent),
                PendingEncounter = encounter.PendingEncounter is null ? null : JourneyEncounterSnapshot.FromDomain(encounter.PendingEncounter),
                Resolution = encounter.Resolution is null ? null : TravelDiaryEncounterResolutionSnapshot.FromDomain(encounter.Resolution)
            };

        public TravelDayEncounterState ToDomain()
            => new(
                EncounterIndex,
                Category,
                Title,
                Message,
                TrailEvent?.ToDomain(),
                PendingEncounter?.ToDomain(),
                Resolution?.ToDomain());
    }

    private sealed class JourneyEncounterSnapshot
    {
        public string Kind { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<JourneyEncounterChoiceSnapshot> Choices { get; set; } = Array.Empty<JourneyEncounterChoiceSnapshot>();
        public JourneyFoeProfileSnapshot? FoeProfile { get; set; }
        public int ResolutionAttempts { get; set; }
        public JourneyEncounterHiddenStateSnapshot? HiddenState { get; set; }

        public static JourneyEncounterSnapshot FromDomain(JourneyEncounterState encounter)
            => new()
            {
                Kind = encounter.Kind,
                Message = encounter.Message,
                Choices = encounter.Choices.Select(JourneyEncounterChoiceSnapshot.FromDomain).ToArray(),
                FoeProfile = encounter.FoeProfile is null ? null : JourneyFoeProfileSnapshot.FromDomain(encounter.FoeProfile),
                ResolutionAttempts = encounter.ResolutionAttempts,
                HiddenState = encounter.HiddenState is null ? null : JourneyEncounterHiddenStateSnapshot.FromDomain(encounter.HiddenState)
            };

        public JourneyEncounterState ToDomain()
            => new(
                Kind,
                Message,
                Choices.Select(choice => choice.ToDomain()).ToArray(),
                FoeProfile?.ToDomain(),
                ResolutionAttempts,
                HiddenState?.ToDomain());
    }

    public sealed class JourneyFoeProfileSnapshot
    {
        public int Speed { get; set; }
        public int FightStrength { get; set; }
        public decimal MinimumBribe { get; set; }

        public static JourneyFoeProfileSnapshot FromDomain(JourneyFoeProfile foeProfile)
            => new()
            {
                Speed = foeProfile.Speed,
                FightStrength = foeProfile.FightStrength,
                MinimumBribe = foeProfile.MinimumBribe
            };

        public JourneyFoeProfile ToDomain()
            => new(Speed, FightStrength, MinimumBribe);
    }

    private sealed class JourneyEncounterHiddenStateSnapshot
    {
        public int BribeOffersMade { get; set; }
        public decimal CumulativeBribePaid { get; set; }
        public bool BribeLockedOut { get; set; }
        public int ChaseFatigue { get; set; }
        public int Annoyance { get; set; }
        public bool Shaken { get; set; }

        public static JourneyEncounterHiddenStateSnapshot FromDomain(JourneyEncounterHiddenState hiddenState)
            => new()
            {
                BribeOffersMade = hiddenState.BribeOffersMade,
                CumulativeBribePaid = hiddenState.CumulativeBribePaid,
                BribeLockedOut = hiddenState.BribeLockedOut,
                ChaseFatigue = hiddenState.ChaseFatigue,
                Annoyance = hiddenState.Annoyance,
                Shaken = hiddenState.Shaken
            };

        public JourneyEncounterHiddenState ToDomain()
            => new(
                BribeOffersMade,
                CumulativeBribePaid,
                BribeLockedOut,
                ChaseFatigue,
                Annoyance,
                Shaken);
    }

    private sealed class JourneyTrailEventSnapshot
    {
        public JourneyTrailEventId Id { get; set; }
        public JourneyTrailEventKind Kind { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal WalletDelta { get; set; }
        public int FoodDelta { get; set; }
        public int CanteenChargeDelta { get; set; }
        public int HorseHungerDelta { get; set; }
        public int HorseThirstDelta { get; set; }
        public int HorseExhaustionDelta { get; set; }
        public int DelayDays { get; set; }
        public int HeatIncrease { get; set; }

        public static JourneyTrailEventSnapshot FromDomain(JourneyTrailEventState trailEvent)
            => new()
            {
                Id = trailEvent.Id,
                Kind = trailEvent.Kind,
                Title = trailEvent.Title,
                Message = trailEvent.Message,
                WalletDelta = trailEvent.WalletDelta,
                FoodDelta = trailEvent.FoodDelta,
                CanteenChargeDelta = trailEvent.CanteenChargeDelta,
                HorseHungerDelta = trailEvent.HorseHungerDelta,
                HorseThirstDelta = trailEvent.HorseThirstDelta,
                HorseExhaustionDelta = trailEvent.HorseExhaustionDelta,
                DelayDays = trailEvent.DelayDays,
                HeatIncrease = trailEvent.HeatIncrease
            };

        public JourneyTrailEventState ToDomain()
            => new(
                Id,
                Kind,
                Title,
                Message,
                WalletDelta,
                FoodDelta,
                CanteenChargeDelta,
                HorseHungerDelta,
                HorseThirstDelta,
                HorseExhaustionDelta,
                DelayDays,
                HeatIncrease);
    }

    private sealed class TravelDiaryEncounterResolutionSnapshot
    {
        public string ChoiceId { get; set; } = string.Empty;
        public string ChoiceLabel { get; set; } = string.Empty;
        public int HealthDelta { get; set; }
        public decimal WalletDelta { get; set; }
        public int AmmoSpent { get; set; }
        public int HeatIncrease { get; set; }
        public int HorseExhaustionDelta { get; set; }
        public bool ContinuedOnFoot { get; set; }

        public static TravelDiaryEncounterResolutionSnapshot FromDomain(TravelDiaryEncounterResolutionState resolution)
            => new()
            {
                ChoiceId = resolution.ChoiceId,
                ChoiceLabel = resolution.ChoiceLabel,
                HealthDelta = resolution.HealthDelta,
                WalletDelta = resolution.WalletDelta,
                AmmoSpent = resolution.AmmoSpent,
                HeatIncrease = resolution.HeatIncrease,
                HorseExhaustionDelta = resolution.HorseExhaustionDelta,
                ContinuedOnFoot = resolution.ContinuedOnFoot
            };

        public TravelDiaryEncounterResolutionState ToDomain()
            => new(
                ChoiceId,
                ChoiceLabel,
                HealthDelta,
                WalletDelta,
                AmmoSpent,
                HeatIncrease,
                HorseExhaustionDelta,
                ContinuedOnFoot);
    }

    private sealed class JourneyEncounterChoiceSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        public static JourneyEncounterChoiceSnapshot FromDomain(JourneyEncounterChoiceState choice)
            => new()
            {
                Id = choice.Id,
                Label = choice.Label
            };

        public JourneyEncounterChoiceState ToDomain()
            => new(Id, Label);
    }

    private sealed class TravelRouteProfileSnapshot
    {
        public string TrailId { get; set; } = string.Empty;
        public TrailRisk Risk { get; set; }
        public TrailTerrain Terrain { get; set; }
        public WaterFeature WaterFeature { get; set; }
        public decimal TotalDistance { get; set; }
        public decimal MountedDailyProgress { get; set; }
        public decimal FootDailyProgress { get; set; }
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public static TravelRouteProfileSnapshot FromDomain(TravelRouteProfile routeProfile)
            => new()
            {
                TrailId = routeProfile.TrailId,
                Risk = routeProfile.Risk,
                Terrain = routeProfile.Terrain,
                WaterFeature = routeProfile.WaterFeature,
                TotalDistance = routeProfile.RideDayDistance,
                MountedDailyProgress = routeProfile.MountedRideDayProgress,
                FootDailyProgress = routeProfile.FootRideDayProgress,
                Warnings = routeProfile.Warnings.ToArray()
            };

        public TravelRouteProfile ToDomain()
            => new(
                TrailId,
                Risk,
                Terrain,
                WaterFeature,
                TotalDistance,
                MountedDailyProgress,
                FootDailyProgress,
                Warnings.ToArray());
    }

    private static class GameSessionRehydrator
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
}
