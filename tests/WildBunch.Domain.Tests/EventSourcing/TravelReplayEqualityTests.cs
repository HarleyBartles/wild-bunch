using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests.EventSourcing;

/// <summary>
/// Proves that command-path state == replay-path state for travel events.
/// Following the BountySaloonEventSourcingTests pattern from BUNCH-80:
/// collect ALL events from the command path (GameStarted + travel events),
/// replay them through RehydrateFromEvents, and assert exact field equality.
/// </summary>
public sealed class TravelReplayEqualityTests
{
    [Fact]
    public void Replay_JourneyStarted_MatchesCommandPath_ExactState()
    {
        var (commandSession, preview) = TravelTestFactory.CreateEasyShortJourney();
        commandSession.StartJourney(preview);
        var events = commandSession.AllEvents.ToArray();

        var replayed = GameSession.RehydrateFromEvents(
            commandSession.Id, commandSession.World,
            events);

        Assert.NotNull(replayed.Journey);
        Assert.NotNull(commandSession.Journey);
        Assert.Equal(commandSession.Journey!.JourneySequence, replayed.Journey!.JourneySequence);
        Assert.Equal(commandSession.Journey.Status, replayed.Journey.Status);
        Assert.Equal(commandSession.Journey.RemainingDays, replayed.Journey.RemainingDays);
        Assert.Equal(commandSession.Journey.FoodRemaining, replayed.Journey.FoodRemaining);
        Assert.Equal(commandSession.Journey.HorseFeedRemaining, replayed.Journey.HorseFeedRemaining);
        Assert.Equal(commandSession.Journey.AvailableCanteenCharges, replayed.Journey.AvailableCanteenCharges);
        Assert.Equal(commandSession.Version, replayed.Version);
    }

    [Fact]
    public void Replay_AdvanceJourneyDay_MatchesCommandPath_ExactState()
    {
        var (commandSession, preview) = TravelTestFactory.CreateEasyShortJourney();
        commandSession.StartJourney(preview);
        commandSession.AdvanceJourneyDay();
        var events = commandSession.AllEvents.ToArray();

        var replayed = GameSession.RehydrateFromEvents(
            commandSession.Id, commandSession.World,
            events);

        Assert.Equal(commandSession.Player.Health, replayed.Player.Health);
        Assert.Equal(commandSession.Player.Wallet.Cash, replayed.Player.Wallet.Cash);
        Assert.Equal(commandSession.Player.GetQuantity(ItemKind.Food),
            replayed.Player.GetQuantity(ItemKind.Food));
        Assert.Equal(commandSession.Clock.Day, replayed.Clock.Day);
        Assert.Equal(commandSession.PursuitState.Heat, replayed.PursuitState.Heat);
        Assert.Equal(commandSession.Version, replayed.Version);
    }

    [Fact]
    public void Replay_FullQuietJourneyCycle_ReconstructsCompletedArrival()
    {
        var (commandSession, preview) = TravelTestFactory.CreateFourDayQuietJourney();
        var originTownId = commandSession.Player.CurrentTownId;
        var destinationTownId = preview.DestinationTownId;
        var start = commandSession.StartJourney(preview);
        Assert.True(start.Success, start.Message);

        for (var day = 1; day <= 4; day++)
        {
            commandSession.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            var result = commandSession.AdvanceJourneyDay();

            Assert.True(result.Success, result.Message);
            Assert.NotNull(result.Journey);
            var expectedStatus = day == 4 ? JourneyStatus.Completed : JourneyStatus.Active;
            Assert.Equal(expectedStatus, result.Status);
            Assert.Equal(day, result.Journey!.DaysTravelled);

            if (day < 4)
            {
                Assert.Equal(originTownId, commandSession.Player.CurrentTownId);
            }
            else
            {
                Assert.Equal(destinationTownId, commandSession.Player.CurrentTownId);
            }
        }

        Assert.NotNull(commandSession.Journey);
        Assert.Equal(JourneyStatus.Completed, commandSession.Journey!.Status);
        Assert.Equal(4, commandSession.Journey.DaysTravelled);
        Assert.Equal(0m, commandSession.Journey.RemainingRideDayDistance);
        Assert.Equal(5, commandSession.Clock.Day);
        Assert.Equal(0, commandSession.Clock.Turn);

        var completed = Assert.Single(commandSession.AllEvents.OfType<JourneyCompleted>());
        Assert.Equal(destinationTownId, completed.DestinationTownId);
        Assert.Equal(JourneyStatus.Completed, completed.JourneySnapshot.Status);
        Assert.Equal(0m, completed.JourneySnapshot.RemainingRideDayDistance);

        var acknowledgment = commandSession.AcknowledgeJourneyArrival();
        Assert.True(acknowledgment.Success, acknowledgment.Message);
        Assert.Null(commandSession.Journey);

        var acknowledged = Assert.Single(commandSession.AllEvents.OfType<JourneyArrivalAcknowledged>());
        var completedHistory = Assert.Single(commandSession.CompletedJourneyHistory);
        Assert.Equal(destinationTownId, completedHistory.DestinationTownId);
        Assert.Equal(TravelMode.Foot, completedHistory.TravelMode);
        Assert.Equal(4, completedHistory.DaysTravelled);
        Assert.Equal(completedHistory, acknowledged.JourneySnapshot);

        var events = commandSession.AllEvents.ToArray();
        var replayed = GameSession.RehydrateFromEvents(
            commandSession.Id, commandSession.World,
            events);

        Assert.Equal(destinationTownId, replayed.Player.CurrentTownId);
        Assert.Equal(commandSession.Player.Health, replayed.Player.Health);
        Assert.Equal(commandSession.Player.Wallet.Cash, replayed.Player.Wallet.Cash);
        Assert.Equal(commandSession.Player.GetQuantity(ItemKind.Food),
            replayed.Player.GetQuantity(ItemKind.Food));
        Assert.Equal(commandSession.Clock.Day, replayed.Clock.Day);
        Assert.Equal(commandSession.PursuitState.Heat, replayed.PursuitState.Heat);
        Assert.Null(replayed.Journey);
        Assert.Equal(completedHistory, Assert.Single(replayed.CompletedJourneyHistory));
        Assert.Equal(commandSession.Version, replayed.Version);
    }

    [Fact]
    public void Replay_HorseLossFootTravel_PreservesDistanceUntilArrival()
    {
        var (commandSession, preview) = TravelTestFactory.CreateThreeUnitJourneyWithCriticalHorse();
        var originTownId = commandSession.Player.CurrentTownId;
        var destinationTownId = preview.DestinationTownId;
        var start = commandSession.StartJourney(preview);
        Assert.True(start.Success, start.Message);
        Assert.Equal(TravelMode.Mounted, preview.TravelMode);

        var expectedDistances = new[] { 2.5m, 2m, 1.5m, 1m, 0.5m, 0m };
        var expectedRemainingDays = new[] { 5, 4, 3, 2, 1, 0 };
        for (var dayIndex = 0; dayIndex < expectedDistances.Length; dayIndex++)
        {
            commandSession.ForceDevTravelOverride(
                DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            var step = commandSession.AdvanceJourneyDay();
            Assert.True(step.Success, step.Message);
            Assert.NotNull(step.Journey);
            Assert.Equal(expectedDistances[dayIndex], step.Journey.RemainingRideDayDistance);
            Assert.Equal(expectedRemainingDays[dayIndex], step.Journey.RemainingDays);

            if (dayIndex < expectedDistances.Length - 1)
            {
                Assert.Equal(JourneyStatus.Active, step.Status);
                Assert.Equal(originTownId, commandSession.Player.CurrentTownId);
                Assert.DoesNotContain(
                    commandSession.UncommittedEvents.OfType<JourneyCompleted>(),
                    completed => completed.JourneySnapshot.RemainingRideDayDistance > 0);
            }
            else
            {
                Assert.Equal(JourneyStatus.Completed, step.Status);
                Assert.Equal(destinationTownId, commandSession.Player.CurrentTownId);
                Assert.Equal(0m, step.Journey.RemainingRideDayDistance);
            }
        }

        Assert.Equal(6, commandSession.Journey!.DaysTravelled);
        Assert.Single(commandSession.UncommittedEvents.OfType<JourneyCompleted>());
        Assert.True(commandSession.AcknowledgeJourneyArrival().Success);
        Assert.Null(commandSession.Journey);

        var events = commandSession.AllEvents.ToArray();
        var replayed = GameSession.RehydrateFromEvents(commandSession.Id, commandSession.World, events);

        Assert.Equal(commandSession.Player.CurrentTownId, replayed.Player.CurrentTownId);
        Assert.Null(replayed.Journey);
        var completedJourney = Assert.Single(replayed.CompletedJourneyHistory);
        Assert.Equal(destinationTownId, completedJourney.DestinationTownId);
        Assert.Equal(TravelMode.Foot, completedJourney.TravelMode);
        Assert.Equal(6, completedJourney.DaysTravelled);
        Assert.Equal(0m, completedJourney.RemainingRideDayDistance);
        Assert.Equal(0, completedJourney.RemainingDays);
        Assert.Equal(commandSession.Version, replayed.Version);
    }

    [Fact]
    public void Replay_ResolveJourneyEncounter_MatchesCommandPath_ExactState()
    {
        var (commandSession, preview) = TravelTestFactory.CreateHighRiskJourney();
        commandSession.StartJourney(preview);

        // Force a foe encounter through the dev-travel override seam instead of
        // looping until the seed produces one. See BUNCH-87.
        commandSession.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        var step = commandSession.AdvanceJourneyDay();
        Assert.Equal(JourneyStatus.Interrupted, step.Status);

        var resolved = commandSession.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0);
        Assert.True(resolved.Success);
        var events = commandSession.AllEvents.ToArray();

        var replayed = GameSession.RehydrateFromEvents(
            commandSession.Id, commandSession.World,
            events);

        Assert.Equal(commandSession.Player.Health, replayed.Player.Health);
        Assert.Equal(commandSession.Player.Wallet.Cash, replayed.Player.Wallet.Cash);
        Assert.Equal(commandSession.Clock.Day, replayed.Clock.Day);
        Assert.Equal(commandSession.PursuitState.Heat, replayed.PursuitState.Heat);
        Assert.Equal(commandSession.Version, replayed.Version);
    }
}
