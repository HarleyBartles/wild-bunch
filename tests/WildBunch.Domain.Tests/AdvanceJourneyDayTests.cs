using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using Xunit;

namespace WildBunch.Domain.Tests;

/// <summary>Travel-day advancement preserves the daily clock invariants.</summary>
public class AdvanceJourneyDayTests
{
    [Fact]
    public void AdvanceJourneyDay_AdvancesOneDayAndResetsClockToMorning()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.MarkEventsCommitted();
        session.StartJourney(preview);

        var dayBefore = session.Clock.Day;
        session.AdvanceJourneyDay();

        Assert.Equal(dayBefore + 1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(TimeOfDay.Morning, session.Clock.TimeOfDay);
    }
}
