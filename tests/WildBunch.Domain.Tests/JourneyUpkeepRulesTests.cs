using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class JourneyUpkeepRulesTests
{
    [Fact]
    public void ApplyDailyUpkeepOnDryTrailAccumulatesPressureAndLetsThirstKillBeforeHunger()
    {
        var dayOne = JourneyUpkeepRules.ApplyDailyUpkeep(
            TrailTerrain.Badlands,
            WaterFeature.None,
            HorseTravelState.Healthy,
            canteenState: null,
            horseFeedAvailable: 0);

        Assert.Equal(new HorseTravelState(1, 1, 1), dayOne.HorseState);
        Assert.False(dayOne.MountedTravelLost);

        var dayTwo = JourneyUpkeepRules.ApplyDailyUpkeep(
            TrailTerrain.Badlands,
            WaterFeature.None,
            dayOne.HorseState,
            dayOne.CanteenState,
            horseFeedAvailable: 0);

        Assert.Equal(new HorseTravelState(2, 2, 2), dayTwo.HorseState);
        Assert.True(dayTwo.MountedTravelLost);
        Assert.True(dayTwo.HorseState!.IsDead);
        Assert.False(dayTwo.HorseState.Hunger >= 3);
    }

    [Fact]
    public void ApplyDailyUpkeepRecoversHungerOnGrazingTerrain()
    {
        var result = JourneyUpkeepRules.ApplyDailyUpkeep(
            TrailTerrain.OpenRange,
            WaterFeature.None,
            new HorseTravelState(1, 0, 0),
            canteenState: null,
            horseFeedAvailable: 0);

        Assert.Equal(new HorseTravelState(0, 1, 0), result.HorseState);
        Assert.False(result.MountedTravelLost);
        Assert.Equal(0, result.HorseFeedConsumed);
    }

    [Fact]
    public void ApplyDailyUpkeepRecoversThirstOnRouteWater()
    {
        var result = JourneyUpkeepRules.ApplyDailyUpkeep(
            TrailTerrain.Badlands,
            WaterFeature.River,
            new HorseTravelState(0, 1, 0),
            canteenState: null,
            horseFeedAvailable: 0);

        Assert.Equal(new HorseTravelState(1, 0, 1), result.HorseState);
        Assert.False(result.MountedTravelLost);
        Assert.Equal(0, result.HorseFeedConsumed);
    }

    [Fact]
    public void ThirstKillsBeforeHungerDoes()
    {
        var thirstyHorse = new HorseTravelState(2, 1, 0).IncreaseThirst();

        Assert.True(thirstyHorse.IsDead);
        Assert.Equal(2, thirstyHorse.Hunger);
        Assert.Equal(2, thirstyHorse.Thirst);
        Assert.False(thirstyHorse.Hunger >= 3);
    }

    [Fact]
    public void ExhaustionMakesHorseLameBeforeItDies()
    {
        var lameHorse = new HorseTravelState(0, 0, 2).IncreaseExhaustion();

        Assert.True(lameHorse.IsLame);
        Assert.False(lameHorse.IsDead);
        Assert.False(lameHorse.CanProvideMountedTravel);

        var deadHorse = lameHorse.IncreaseExhaustion(2);

        Assert.True(deadHorse.IsDead);
        Assert.False(deadHorse.IsLame);
        Assert.False(deadHorse.CanProvideMountedTravel);
    }
}
