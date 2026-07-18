using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Tests;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests.Projections;

/// <summary>
/// Proves that TravelDiaryDayProjector reconstructs the exact same
/// TravelDiaryDayState records as the command path produces.
/// This is the parity test that proves diary days are derived state
/// rebuildable from the event stream alone.
/// </summary>
public sealed class TravelDiaryDayProjectorParityTests
{
    [Fact]
    public void Projector_FullJourneyCycle_MatchesCommandPathDiaryDays()
    {
        var (commandSession, preview, setupEvents) =
            TravelTestFactory.CreateSixDayQuietJourneyWithSetupEvents();
        commandSession.StartJourney(preview);

        // Force quiet days through the dev-travel override seam so the journey
        // completes without seed-dependent encounter interruptions.
        TravelJourneyStepResult result;
        do
        {
            commandSession.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            result = commandSession.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        commandSession.AcknowledgeJourneyArrival();

        var events = setupEvents.Concat(commandSession.UncommittedEvents).ToList();
        var projector = new TravelDiaryDayProjector();
        var projection = projector.Project(events);

        var commandDiaryDays = commandSession.TravelDiaryDays;
        Assert.Equal(commandDiaryDays, projection.Days);
    }

    [Fact]
    public void Projector_ShortJourney_MatchesCommandPathDiaryDays()
    {
        var (commandSession, preview, setupEvents) =
            TravelTestFactory.CreateEasyShortJourneyWithSetupEvents();
        commandSession.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            commandSession.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            result = commandSession.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        commandSession.AcknowledgeJourneyArrival();

        var events = setupEvents.Concat(commandSession.UncommittedEvents).ToList();
        var projector = new TravelDiaryDayProjector();
        var projection = projector.Project(events);

        var commandDiaryDays = commandSession.TravelDiaryDays;
        Assert.Equal(commandDiaryDays, projection.Days);
    }
}
