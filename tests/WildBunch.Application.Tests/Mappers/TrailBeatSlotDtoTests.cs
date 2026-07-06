using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Application.Tests.Mappers;

public class TrailBeatSlotDtoTests
{
    [Fact]
    public void QuietDay_ProducesSingleQuietSlot()
    {
        var day = CreateDay(trailEvent: null, pendingEncounter: null, encounterResolution: null);
        var dto = TravelDiaryMapper.ToDto(new[] { day });
        Assert.NotNull(dto);
        var mappedDay = Assert.Single(dto!.Days);
        Assert.Single(mappedDay.BeatSlots);
        Assert.Equal(TrailBeatSlotType.Quiet, mappedDay.BeatSlots[0].SlotType);
    }

    [Fact]
    public void InterruptedDay_ProducesInterruptingSlot()
    {
        var encounter = JourneyEncounterState.CreateFoe("A bandit blocks the trail.", TestFoeProfile);
        var day = CreateDay(trailEvent: null, pendingEncounter: encounter, encounterResolution: null);
        var dto = TravelDiaryMapper.ToDto(new[] { day });
        Assert.NotNull(dto);
        var mappedDay = Assert.Single(dto!.Days);
        Assert.Contains(mappedDay.BeatSlots, s => s.SlotType == TrailBeatSlotType.Interrupting);
    }

    [Fact]
    public void ResolvedEncounterDay_ProducesEventfulSlot()
    {
        var encounter = JourneyEncounterState.CreateFoe("A bandit blocks the trail.", TestFoeProfile);
        var resolution = new TravelDiaryEncounterResolutionState("run", "Run", 0, 0, 0, 0, 0, false);
        var day = CreateDay(trailEvent: null, pendingEncounter: encounter, encounterResolution: resolution);
        var dto = TravelDiaryMapper.ToDto(new[] { day });
        Assert.NotNull(dto);
        var mappedDay = Assert.Single(dto!.Days);
        Assert.Contains(mappedDay.BeatSlots, s => s.SlotType == TrailBeatSlotType.Eventful);
    }

    [Fact]
    public void TrailEventDay_ProducesMinorSlot()
    {
        var trailEvent = JourneyTrailEventState.CreateLucky(JourneyTrailEventId.LuckyFoodCache, "A lucky find", "You find a cache of supplies.", 0m, 2, 0);
        var day = CreateDay(trailEvent: trailEvent, pendingEncounter: null, encounterResolution: null);
        var dto = TravelDiaryMapper.ToDto(new[] { day });
        Assert.NotNull(dto);
        var mappedDay = Assert.Single(dto!.Days);
        Assert.Contains(mappedDay.BeatSlots, s => s.SlotType == TrailBeatSlotType.Minor);
    }

    private static readonly JourneyFoeProfile TestFoeProfile = new(Speed: 5, FightStrength: 10, MinimumBribe: 5m);

    private static TravelDiaryDayState CreateDay(
        JourneyTrailEventState? trailEvent,
        JourneyEncounterState? pendingEncounter,
        TravelDiaryEncounterResolutionState? encounterResolution)
        => new(
            1,
            "Pinecross",
            "Dry Fork",
            TravelMode.Mounted,
            TravelMode.Mounted,
            JourneyStatus.Active,
            3m,
            3m,
            4,
            4,
            null,
            null,
            trailEvent,
            pendingEncounter,
            encounterResolution,
            null,
            null,
            null,
            Array.Empty<string>(),
            HealthDelta: 0,
            WalletDelta: 0m,
            FoodDelta: 0,
            HorseFeedDelta: 0,
            CanteenChargeDelta: 0,
            AmmoSpent: 0,
            HorseHungerDelta: 0,
            HorseThirstDelta: 0,
            HorseExhaustionDelta: 0,
            DelayDays: 0,
            HeatIncrease: 0,
            CurrentHealth: 1000,
            CurrentWallet: 25m,
            CurrentFood: 3,
            CurrentHorseFeed: 0,
            CurrentCanteenCharges: 2,
            CurrentAmmo: 0,
            CurrentHeat: 0,
            Warnings: Array.Empty<string>())
        {
            Terrain = TrailTerrain.OpenRange,
            RouteWaterSecure = true,
            CanteenChargesPerDay = 0
        };
}
