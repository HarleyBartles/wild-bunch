using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Apply tests for the 6 typed travel domain events. Each test drives a single Apply
/// method in isolation and asserts exact state. These prove the event-sourced mutation
/// path matches the direct-mutation path field-for-field.
/// </summary>
public sealed class TravelEventApplyTests
{
    [Fact]
    public void Apply_JourneyStarted_SetsJourneyFromSnapshot()
    {
        var (setup, preview) = TravelTestFactory.CreateEasyShortJourney();
        setup.StartJourney(preview);
        var snapshot = setup.Journey!.ToSnapshot(setup.TravelRules);

        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();

        session.Apply(new JourneyStarted
        {
            JourneySnapshot = snapshot,
            DiaryMessage = "You head out at dawn."
        });

        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Equal(snapshot.JourneySequence, session.Journey.JourneySequence);
        Assert.Equal(snapshot.RemainingDays, session.Journey.RemainingDays);
        Assert.Equal(snapshot.AvailableFood, session.Journey.FoodRemaining);
    }

    [Fact]
    public void Apply_TravelDayAdvanced_SetsClockAndJourneyFromSnapshot()
    {
        var (setup, preview) = TravelTestFactory.CreateEasyShortJourney();
        setup.StartJourney(preview);
        var startSnapshot = setup.Journey!.ToSnapshot(setup.TravelRules);
        setup.MarkEventsCommitted();

        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();
        session.Apply(new JourneyStarted
        {
            JourneySnapshot = startSnapshot,
            DiaryMessage = "You head out at dawn."
        });

        setup.AdvanceJourneyDay();
        var advancedSnapshot = setup.Journey!.ToSnapshot(setup.TravelRules);

        session.Apply(new TravelDayAdvanced
        {
            Day = 2,
            JourneySnapshot = advancedSnapshot,
            HealthDelta = 0,
            PursuitHeat = 1,
            DayOutcome = TravelDayOutcome.Completed,
            DiaryMessage = "Day passes.",
            HorseLostMessage = ""
        });

        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(advancedSnapshot.DaysTravelled, session.Journey!.DaysTravelled);
        Assert.Equal(advancedSnapshot.AvailableFood, session.Journey.FoodRemaining);
    }

    [Fact]
    public void Apply_TravelDayAdvanced_AddsHealthAndHeatDeltas()
    {
        var (setup, preview) = TravelTestFactory.CreateEasyShortJourney();
        setup.StartJourney(preview);
        var startSnapshot = setup.Journey!.ToSnapshot(setup.TravelRules);

        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();
        session.Apply(new JourneyStarted
        {
            JourneySnapshot = startSnapshot,
            DiaryMessage = "You head out at dawn."
        });

        var healthBefore = session.Player.Health;
        var heatBefore = session.PursuitState.Heat;

        session.Apply(new TravelDayAdvanced
        {
            Day = 2,
            JourneySnapshot = startSnapshot,
            HealthDelta = -2,
            PursuitHeat = 3,
            DayOutcome = TravelDayOutcome.Ongoing,
            DiaryMessage = "Day passes.",
            HorseLostMessage = ""
        });

        Assert.Equal(healthBefore - 2, session.Player.Health);
        Assert.Equal(3, session.PursuitState.Heat);
    }

    [Fact]
    public void Apply_TrailEventApplied_AddsWalletFoodCanteenDeltasAndSetsJourney()
    {
        var (setup, preview) = TravelTestFactory.CreateEasyShortJourney();
        setup.StartJourney(preview);
        var startSnapshot = setup.Journey!.ToSnapshot(setup.TravelRules);
        setup.AdvanceJourneyDay();
        var snapshot = setup.Journey!.ToSnapshot(setup.TravelRules);

        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();
        session.Apply(new JourneyStarted
        {
            JourneySnapshot = startSnapshot,
            DiaryMessage = "You head out at dawn."
        });

        var walletBefore = session.Player.Wallet.Cash;
        var foodBefore = session.Player.GetQuantity(ItemKind.Food);

        session.Apply(new TrailEventApplied
        {
            JourneySnapshot = snapshot,
            TrailEventKind = JourneyTrailEventKind.Lucky,
            TrailEventId = JourneyTrailEventId.LuckyCoinCache,
            WalletDelta = 4m,
            WalletCash = walletBefore + 4m,
            FoodDelta = -1,
            CanteenChargeDelta = 0,
            HorseHungerDelta = 0,
            HorseThirstDelta = 0,
            HorseExhaustionDelta = 0,
            DelayDays = 0,
            HeatIncrease = 1,
            PursuitHeat = session.PursuitState.Heat + 1,
            TravelModeChangedTo = null,
            DiaryMessage = "I uncovered a hidden cache of trail coins and pocketed $4.00.",
            HorseLostMessage = ""
        });

        Assert.Equal(walletBefore + 4m, session.Player.Wallet.Cash);
        Assert.Equal(foodBefore - 1, session.Player.GetQuantity(ItemKind.Food));
        Assert.Equal(snapshot.DaysTravelled, session.Journey!.DaysTravelled);
    }

    [Fact]
    public void Apply_JourneyCompleted_SetsPlayerTownAndJourneyFromSnapshot()
    {
        var (setup, preview) = TravelTestFactory.CreateEasyShortJourney();
        setup.StartJourney(preview);
        setup.AdvanceJourneyDay();
        Assert.NotNull(setup.Journey);
        var snapshot = setup.Journey.ToSnapshot(setup.TravelRules);
        var destinationId = setup.Journey.Preview.DestinationTownId;

        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();

        session.Apply(new JourneyCompleted
        {
            DestinationTownId = destinationId,
            DestinationTownName = "Connected Town",
            JourneySnapshot = snapshot,
            DiaryMessage = "You reach Connected Town after 1 trail day(s)."
        });

        Assert.Equal(destinationId, session.Player.CurrentTownId);
        Assert.Equal(snapshot.DaysTravelled, session.Journey!.DaysTravelled);
    }

    [Fact]
    public void Apply_JourneyArrivalAcknowledged_ArchivesJourneyAndClearsActive()
    {
        var (setup, preview) = TravelTestFactory.CreateEasyShortJourney();
        setup.StartJourney(preview);
        setup.AdvanceJourneyDay();
        var snapshot = setup.Journey!.ToSnapshot(setup.TravelRules);
        var sequence = setup.Journey.JourneySequence;

        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();

        session.Apply(new JourneyArrivalAcknowledged
        {
            JourneySequence = sequence,
            JourneySnapshot = snapshot,
            DiaryMessage = "You reach Connected Town after 1 trail day(s)."
        });

        Assert.Null(session.Journey);
        Assert.Equal(1, session.CompletedJourneyHistory.Count);
        Assert.Equal(sequence, session.CompletedJourneyHistory[0].JourneySequence);
    }
}
