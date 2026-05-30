using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests;

public sealed class TravelDiaryMapperTests
{
    [Fact]
    public void ToDtoUsesRendererOwnedDiaryProse()
    {
        var day = new TravelDiaryDayState(
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
            null,
            null,
            null,
            null,
            null,
            null,
            Entries: new[] { "I found a cache of jerky and trail biscuits and picked up 2 food." },
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

        var dto = TravelDiaryMapper.ToDto(new[] { day });

        Assert.NotNull(dto);
        var mappedDay = Assert.Single(dto!.Days);
        Assert.Equal("I cross open range with the horse moving steady under me.", mappedDay.JourneyBeat);
        Assert.Null(mappedDay.ResourceBeat);
        Assert.Equal(new[] { "I cross open range with the horse moving steady under me.", "I found a cache of jerky and trail biscuits and picked up 2 food.", "I keep moving and let the trail stretch ahead." }, mappedDay.Entries);
    }
}
