using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using Xunit;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Roll-up preservation tests proving that the beat slot naming layer (Task 4)
/// does not affect daily roll-up, day advancement, or travel diary state.
/// These are characterization tests — the production code already supports these behaviors.
/// </summary>
public class BeatModelRollupPreservationTests
{
    [Fact]
    public void AdvanceJourneyDay_StillRollsUpOncePerDay()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.MarkEventsCommitted();
        session.StartJourney(preview);

        var dayBefore = session.Clock.Day;

        session.AdvanceJourneyDay();

        // Daily roll-up: day advanced once
        Assert.Equal(dayBefore + 1, session.Clock.Day);
    }

    [Fact]
    public void AdvanceJourneyDay_PreservesClockTurnReset()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.MarkEventsCommitted();
        session.StartJourney(preview);

        session.AdvanceJourneyDay();

        // Travel day advancement resets turn to 0 (Morning)
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(TimeOfDay.Morning, session.Clock.TimeOfDay);
    }

    [Fact]
    public void TravelDiaryDayState_HasNoBeatSlotsField()
    {
        // Falsification: proves BeatSlots is NOT on the domain state (mapper-only projection)
        var properties = typeof(TravelDiaryDayState).GetProperties();
        Assert.DoesNotContain(properties, p => p.Name.Contains("BeatSlot"));
    }
}
