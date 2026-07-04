using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

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
        var (commandSession, preview, setupEvents) =
            TravelTestFactory.CreateEasyShortJourneyWithGameStarted();
        commandSession.StartJourney(preview);
        var events = setupEvents.Concat(commandSession.UncommittedEvents).ToList();

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
        var (commandSession, preview, setupEvents) =
            TravelTestFactory.CreateEasyShortJourneyWithGameStarted();
        commandSession.StartJourney(preview);
        commandSession.AdvanceJourneyDay();
        var events = setupEvents.Concat(commandSession.UncommittedEvents).ToList();

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
    public void Replay_FullJourneyCycle_MatchesCommandPath_ExactState()
    {
        var (commandSession, preview, setupEvents) =
            TravelTestFactory.CreateSixDayQuietJourneyWithGameStarted();
        commandSession.StartJourney(preview);

        // Force quiet days through the dev-travel override seam so the journey
        // completes without seed-dependent encounter interruptions. See BUNCH-87.
        TravelJourneyStepResult result;
        do
        {
            commandSession.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            result = commandSession.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        commandSession.AcknowledgeJourneyArrival();

        var events = setupEvents.Concat(commandSession.UncommittedEvents).ToList();

        var replayed = GameSession.RehydrateFromEvents(
            commandSession.Id, commandSession.World,
            events);

        Assert.Equal(commandSession.Player.CurrentTownId, replayed.Player.CurrentTownId);
        Assert.Equal(commandSession.Player.Health, replayed.Player.Health);
        Assert.Equal(commandSession.Player.Wallet.Cash, replayed.Player.Wallet.Cash);
        Assert.Equal(commandSession.Player.GetQuantity(ItemKind.Food),
            replayed.Player.GetQuantity(ItemKind.Food));
        Assert.Equal(commandSession.Clock.Day, replayed.Clock.Day);
        Assert.Equal(commandSession.PursuitState.Heat, replayed.PursuitState.Heat);
        Assert.Null(replayed.Journey);
        Assert.Equal(commandSession.CompletedJourneyHistory.Count, replayed.CompletedJourneyHistory.Count);
        Assert.Equal(commandSession.Version, replayed.Version);
    }

    [Fact]
    public void Replay_ResolveJourneyEncounter_MatchesCommandPath_ExactState()
    {
        var (commandSession, preview) = TravelTestFactory.CreateHighRiskJourney();
        var setupEvents = TravelTestFactory.RecaptureGameStartedForReplay(commandSession);
        commandSession.StartJourney(preview);

        // Force a foe encounter through the dev-travel override seam instead of
        // looping until the seed produces one. See BUNCH-87.
        commandSession.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        var step = commandSession.AdvanceJourneyDay();
        Assert.Equal(JourneyStatus.Interrupted, step.Status);

        var resolved = commandSession.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0);
        Assert.True(resolved.Success);
        var events = setupEvents.Concat(commandSession.UncommittedEvents).ToList();

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
