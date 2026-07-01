using WildBunch.Domain.Travel;
using Xunit;

namespace WildBunch.Domain.Tests;

public class TrailBeatSlotMappingTests
{
    [Theory]
    [InlineData(TravelDayEncounterCategory.Quiet, TrailBeatSlotType.Quiet)]
    [InlineData(TravelDayEncounterCategory.Lucky, TrailBeatSlotType.Minor)]
    [InlineData(TravelDayEncounterCategory.Unlucky, TrailBeatSlotType.Minor)]
    [InlineData(TravelDayEncounterCategory.Resource, TrailBeatSlotType.Minor)]
    [InlineData(TravelDayEncounterCategory.HorseTrouble, TrailBeatSlotType.Minor)]
    [InlineData(TravelDayEncounterCategory.Foe, TrailBeatSlotType.Eventful)]
    [InlineData(TravelDayEncounterCategory.Npc, TrailBeatSlotType.Eventful)]
    [InlineData(TravelDayEncounterCategory.Environmental, TrailBeatSlotType.Eventful)]
    public void ToSlotType_MapsCategoryToBeatSlot(TravelDayEncounterCategory category, TrailBeatSlotType expected)
    {
        var result = TrailBeatSlotMapper.ToSlotType(category, requiresChoice: false);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToSlotType_RequiresChoiceOverridesToInterrupting()
    {
        var result = TrailBeatSlotMapper.ToSlotType(TravelDayEncounterCategory.Foe, requiresChoice: true);
        Assert.Equal(TrailBeatSlotType.Interrupting, result);
    }

    [Fact]
    public void ToSlotType_QuietWithChoiceStillInterrupting()
    {
        var result = TrailBeatSlotMapper.ToSlotType(TravelDayEncounterCategory.Quiet, requiresChoice: true);
        Assert.Equal(TrailBeatSlotType.Interrupting, result);
    }
}
