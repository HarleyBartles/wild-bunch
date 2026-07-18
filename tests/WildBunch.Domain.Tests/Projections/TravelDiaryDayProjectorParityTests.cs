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
        Assert.Equal(commandDiaryDays.Count, projection.Days.Count);

        for (var i = 0; i < commandDiaryDays.Count; i++)
        {
            var expected = commandDiaryDays[i];
            var actual = projection.Days[i];

            Assert.Equal(expected.DayNumber, actual.DayNumber);
            Assert.Equal(expected.OriginTownName, actual.OriginTownName);
            Assert.Equal(expected.DestinationTownName, actual.DestinationTownName);
            Assert.Equal(expected.StartingTravelMode, actual.StartingTravelMode);
            Assert.Equal(expected.EndingTravelMode, actual.EndingTravelMode);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.StartingRideDayDistance, actual.StartingRideDayDistance);
            Assert.Equal(expected.RemainingRideDayDistance, actual.RemainingRideDayDistance);
            Assert.Equal(expected.StartingDaysRemaining, actual.StartingDaysRemaining);
            Assert.Equal(expected.RemainingDays, actual.RemainingDays);
            Assert.Equal(expected.HealthDelta, actual.HealthDelta);
            Assert.Equal(expected.WalletDelta, actual.WalletDelta);
            Assert.Equal(expected.FoodDelta, actual.FoodDelta);
            Assert.Equal(expected.HorseFeedDelta, actual.HorseFeedDelta);
            Assert.Equal(expected.CanteenChargeDelta, actual.CanteenChargeDelta);
            Assert.Equal(expected.AmmoSpent, actual.AmmoSpent);
            Assert.Equal(expected.DelayDays, actual.DelayDays);
            Assert.Equal(expected.HeatIncrease, actual.HeatIncrease);
            Assert.Equal(expected.CurrentHealth, actual.CurrentHealth);
            Assert.Equal(expected.CurrentWallet, actual.CurrentWallet);
            Assert.Equal(expected.CurrentFood, actual.CurrentFood);
            Assert.Equal(expected.CurrentHorseFeed, actual.CurrentHorseFeed);
            Assert.Equal(expected.CurrentCanteenCharges, actual.CurrentCanteenCharges);
            Assert.Equal(expected.CurrentAmmo, actual.CurrentAmmo);
            Assert.Equal(expected.CurrentHeat, actual.CurrentHeat);
            Assert.Equal(expected.OpeningNarration, actual.OpeningNarration);
            Assert.Equal(expected.Entries, actual.Entries);
            Assert.Equal(expected.Warnings, actual.Warnings);

            // TrailEvent comparison (may be null)
            if (expected.TrailEvent is null)
            {
                Assert.Null(actual.TrailEvent);
            }
            else
            {
                Assert.NotNull(actual.TrailEvent);
                Assert.Equal(expected.TrailEvent.Id, actual.TrailEvent.Id);
                Assert.Equal(expected.TrailEvent.Kind, actual.TrailEvent.Kind);
                Assert.Equal(expected.TrailEvent.Title, actual.TrailEvent.Title);
                Assert.Equal(expected.TrailEvent.Message, actual.TrailEvent.Message);
                Assert.Equal(expected.TrailEvent.WalletDelta, actual.TrailEvent.WalletDelta);
                Assert.Equal(expected.TrailEvent.FoodDelta, actual.TrailEvent.FoodDelta);
                Assert.Equal(expected.TrailEvent.CanteenChargeDelta, actual.TrailEvent.CanteenChargeDelta);
                Assert.Equal(expected.TrailEvent.DelayDays, actual.TrailEvent.DelayDays);
            }

            // EncounterResolution comparison (may be null)
            if (expected.EncounterResolution is null)
            {
                Assert.Null(actual.EncounterResolution);
            }
            else
            {
                Assert.NotNull(actual.EncounterResolution);
                Assert.Equal(expected.EncounterResolution.ChoiceId, actual.EncounterResolution.ChoiceId);
                Assert.Equal(expected.EncounterResolution.ChoiceLabel, actual.EncounterResolution.ChoiceLabel);
                Assert.Equal(expected.EncounterResolution.HealthDelta, actual.EncounterResolution.HealthDelta);
                Assert.Equal(expected.EncounterResolution.WalletDelta, actual.EncounterResolution.WalletDelta);
                Assert.Equal(expected.EncounterResolution.AmmoSpent, actual.EncounterResolution.AmmoSpent);
                Assert.Equal(expected.EncounterResolution.HeatIncrease, actual.EncounterResolution.HeatIncrease);
                Assert.Equal(expected.EncounterResolution.HorseExhaustionDelta, actual.EncounterResolution.HorseExhaustionDelta);
                Assert.Equal(expected.EncounterResolution.ContinuedOnFoot, actual.EncounterResolution.ContinuedOnFoot);
            }
        }
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
        Assert.Equal(commandDiaryDays.Count, projection.Days.Count);
    }
}
