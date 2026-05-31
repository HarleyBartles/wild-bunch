using System.Text.Json;
using System.Reflection;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
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

    public string SerializeCompletedJourneyHistory(IReadOnlyList<TravelJourneySnapshot> completedJourneys)
    {
        ArgumentNullException.ThrowIfNull(completedJourneys);
        return JsonSerializer.Serialize(completedJourneys.Select(JourneySnapshot.FromDomain).ToArray(), Options);
    }

    public IReadOnlyList<TravelJourneySnapshot> DeserializeCompletedJourneyHistory(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<TravelJourneySnapshot>();
        }

        return Deserialize<JourneySnapshot[]>(json).Select(snapshot => snapshot.ToDomain()).ToArray();
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

    private sealed class JourneySnapshot
    {
        public int JourneySequence { get; set; }
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
                JourneySequence = snapshot.JourneySequence,
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
                Math.Max(1, JourneySequence),
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
        public JourneyTrailEventState? TrailEvent { get; set; }
        public JourneyEncounterState? PendingEncounter { get; set; }
        public TravelDiaryEncounterResolutionState? Resolution { get; set; }

        public static TravelDayEncounterSnapshot FromDomain(TravelDayEncounterState encounter)
            => new()
            {
                EncounterIndex = encounter.EncounterIndex,
                Category = encounter.Category,
                Title = encounter.Title,
                Message = encounter.Message,
                TrailEvent = encounter.TrailEvent,
                PendingEncounter = encounter.PendingEncounter,
                Resolution = encounter.Resolution
            };

        public TravelDayEncounterState ToDomain()
            => new(
                EncounterIndex,
                Category,
                Title,
                Message,
                TrailEvent,
                PendingEncounter,
                Resolution);
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

    private sealed class TravelDiaryDaySnapshot
    {
        public int DayNumber { get; set; }
        public string OriginTownName { get; set; } = string.Empty;
        public string DestinationTownName { get; set; } = string.Empty;
        public TravelMode StartingTravelMode { get; set; }
        public TravelMode EndingTravelMode { get; set; }
        public JourneyStatus Status { get; set; }
        public decimal StartingRideDayDistance { get; set; }
        public decimal RemainingRideDayDistance { get; set; }
        public int StartingDaysRemaining { get; set; }
        public int RemainingDays { get; set; }
        public DomainHorseTravelState? HorseStateBefore { get; set; }
        public DomainHorseTravelState? HorseStateAfter { get; set; }
        public JourneyTrailEventState? TrailEvent { get; set; }
        public JourneyEncounterState? PendingEncounter { get; set; }
        public TravelDiaryEncounterResolutionState? EncounterResolution { get; set; }
        public string? OpeningNarration { get; set; }
        public string? JourneyBeat { get; set; }
        public string? ResourceBeat { get; set; }
        public IReadOnlyList<string> Entries { get; set; } = Array.Empty<string>();
        public int HealthDelta { get; set; }
        public decimal WalletDelta { get; set; }
        public int FoodDelta { get; set; }
        public int HorseFeedDelta { get; set; }
        public int CanteenChargeDelta { get; set; }
        public int AmmoSpent { get; set; }
        public int HorseHungerDelta { get; set; }
        public int HorseThirstDelta { get; set; }
        public int HorseExhaustionDelta { get; set; }
        public int DelayDays { get; set; }
        public int HeatIncrease { get; set; }
        public int CurrentHealth { get; set; }
        public decimal CurrentWallet { get; set; }
        public int CurrentFood { get; set; }
        public int CurrentHorseFeed { get; set; }
        public int CurrentCanteenCharges { get; set; }
        public int CurrentAmmo { get; set; }
        public int CurrentHeat { get; set; }
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
        public TrailTerrain Terrain { get; set; }
        public bool RouteWaterSecure { get; set; }
        public int CanteenChargesPerDay { get; set; }

        public static TravelDiaryDaySnapshot FromDomain(TravelDiaryDayState day)
            => new()
            {
                DayNumber = day.DayNumber,
                OriginTownName = day.OriginTownName,
                DestinationTownName = day.DestinationTownName,
                StartingTravelMode = day.StartingTravelMode,
                EndingTravelMode = day.EndingTravelMode,
                Status = day.Status,
                StartingRideDayDistance = day.StartingRideDayDistance,
                RemainingRideDayDistance = day.RemainingRideDayDistance,
                StartingDaysRemaining = day.StartingDaysRemaining,
                RemainingDays = day.RemainingDays,
                HorseStateBefore = day.HorseStateBefore,
                HorseStateAfter = day.HorseStateAfter,
                TrailEvent = day.TrailEvent,
                PendingEncounter = day.PendingEncounter,
                EncounterResolution = day.EncounterResolution,
                OpeningNarration = day.OpeningNarration,
                JourneyBeat = day.JourneyBeat,
                ResourceBeat = day.ResourceBeat,
                Entries = day.Entries.ToArray(),
                HealthDelta = day.HealthDelta,
                WalletDelta = day.WalletDelta,
                FoodDelta = day.FoodDelta,
                HorseFeedDelta = day.HorseFeedDelta,
                CanteenChargeDelta = day.CanteenChargeDelta,
                AmmoSpent = day.AmmoSpent,
                HorseHungerDelta = day.HorseHungerDelta,
                HorseThirstDelta = day.HorseThirstDelta,
                HorseExhaustionDelta = day.HorseExhaustionDelta,
                DelayDays = day.DelayDays,
                HeatIncrease = day.HeatIncrease,
                CurrentHealth = day.CurrentHealth,
                CurrentWallet = day.CurrentWallet,
                CurrentFood = day.CurrentFood,
                CurrentHorseFeed = day.CurrentHorseFeed,
                CurrentCanteenCharges = day.CurrentCanteenCharges,
                CurrentAmmo = day.CurrentAmmo,
                CurrentHeat = day.CurrentHeat,
                Warnings = day.Warnings.ToArray(),
                Terrain = GetInternalProperty<TrailTerrain>(day, "Terrain"),
                RouteWaterSecure = GetInternalProperty<bool>(day, "RouteWaterSecure"),
                CanteenChargesPerDay = GetInternalProperty<int>(day, "CanteenChargesPerDay")
            };

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
}
