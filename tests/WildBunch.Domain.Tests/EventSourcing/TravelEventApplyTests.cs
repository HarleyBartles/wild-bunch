using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests.EventSourcing;

/// <summary>
/// Isolated aggregate Apply tests for selected travel event payload effects.
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
            DiaryMessage = "You head out at dawn.",
            PursuitHeat = 0
        });

        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Equal(snapshot.JourneySequence, session.Journey.JourneySequence);
        Assert.Equal(snapshot.RemainingDays, session.Journey.RemainingDays);
        Assert.Equal(snapshot.AvailableFood, session.Journey.FoodRemaining);
    }

    [Fact]
    public void Apply_TravelDayAdvanced_SetsSnapshotClockAndHealthHeatEffects()
    {
        var (setup, preview) = TravelTestFactory.CreateEasyShortJourney();
        Assert.True(setup.StartJourney(preview).Success);
        setup.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        var startSnapshot = setup.Journey!.ToSnapshot(setup.TravelRules);

        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();
        session.Apply(new JourneyStarted
        {
            JourneySnapshot = startSnapshot,
            DiaryMessage = "You head out at dawn.",
            PursuitHeat = 0
        });

        var advanceResult = setup.AdvanceJourneyDay();
        Assert.True(advanceResult.Success, advanceResult.Message);
        var advancedSnapshot = setup.Journey!.ToSnapshot(setup.TravelRules);
        var healthBefore = session.Player.Health;
        var journeyDaysBefore = session.Journey!.DaysTravelled;

        session.Apply(new TravelDayAdvanced
        {
            Day = 2,
            JourneySnapshot = advancedSnapshot,
            HealthDelta = -2,
            PursuitHeat = 3,
            DayOutcome = TravelDayOutcome.Ongoing,
            DiaryMessage = "The trail exacts a toll.",
            HorseLostMessage = string.Empty
        });

        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.NotEqual(journeyDaysBefore, advancedSnapshot.DaysTravelled);
        Assert.Equal(advancedSnapshot.DaysTravelled, session.Journey!.DaysTravelled);
        Assert.Equal(advancedSnapshot.AvailableFood, session.Journey.FoodRemaining);
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
            DiaryMessage = "You head out at dawn.",
            PursuitHeat = 0
        });

        var walletBefore = session.Player.Wallet.Cash;
        var foodBefore = session.Player.GetQuantity(ItemKind.Food);

        session.Apply(new TrailEventApplied
        {
            JourneySnapshot = snapshot,
            TrailEventKind = JourneyTrailEventKind.Lucky,
            TrailEventId = JourneyTrailEventId.LuckyCoinCache,
            Title = "Hidden coin cache",
            Message = "I uncovered a hidden cache of trail coins and pocketed $4.00.",
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
    public void Apply_JourneyEncounterResolved_SetsAbsoluteFieldsAndSpendsAmmo()
    {
        var (session, progressedSnapshot) = CreateSessionWithProgressedJourney();
        session.Player.AddItem(ItemKind.RevolverAmmo, 5);
        var journeyDaysBefore = session.Journey!.DaysTravelled;

        session.Apply(new JourneyEncounterResolved
        {
            ChoiceId = "run",
            ChoiceLabel = "Run for it",
            Resolved = false,
            PlayerHealth = 18,
            WalletCash = 16m,
            AmmoSpent = 2,
            StolenItemKind = null,
            StolenItemQuantity = 0,
            PursuitHeat = 4,
            HorseExhaustionDelta = 0,
            ContinuedOnFoot = false,
            JourneySnapshot = progressedSnapshot,
            DiaryMessage = "The rider cuts across the trail.",
            DayCompleted = false,
            JourneyCompleted = false
        });

        Assert.Equal(18, session.Player.Health);
        Assert.Equal(16m, session.Player.Wallet.Cash);
        Assert.Equal(3, session.Player.GetQuantity(ItemKind.RevolverAmmo));
        Assert.Equal(4, session.PursuitState.Heat);
        Assert.NotEqual(journeyDaysBefore, progressedSnapshot.DaysTravelled);
        Assert.Equal(progressedSnapshot.DaysTravelled, session.Journey!.DaysTravelled);
    }

    [Fact]
    public void Apply_JourneyEncounterResolved_RemovesStolenRevolverAmmo()
    {
        var (session, progressedSnapshot) = CreateSessionWithProgressedJourney();
        session.Player.AddItem(ItemKind.RevolverAmmo, 5);

        session.Apply(new JourneyEncounterResolved
        {
            ChoiceId = "hide",
            ChoiceLabel = "Hide",
            Resolved = false,
            PlayerHealth = session.Player.Health,
            WalletCash = session.Player.Wallet.Cash,
            AmmoSpent = 0,
            StolenItemKind = ItemKind.RevolverAmmo,
            StolenItemQuantity = 2,
            PursuitHeat = session.PursuitState.Heat,
            HorseExhaustionDelta = 0,
            ContinuedOnFoot = false,
            JourneySnapshot = progressedSnapshot,
            DiaryMessage = "Someone slips away with your cartridges.",
            DayCompleted = false,
            JourneyCompleted = false
        });

        Assert.Equal(3, session.Player.GetQuantity(ItemKind.RevolverAmmo));
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
        Assert.Single(session.CompletedJourneyHistory);
        Assert.Equal(sequence, session.CompletedJourneyHistory[0].JourneySequence);
    }

    private static (GameSession Session, TravelJourneySnapshot ProgressedSnapshot)
        CreateSessionWithProgressedJourney()
    {
        var (setup, preview) = TravelTestFactory.CreateEasyShortJourney();
        Assert.True(setup.StartJourney(preview).Success);
        setup.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        var startingSnapshot = setup.Journey!.ToSnapshot(setup.TravelRules);
        var advanceResult = setup.AdvanceJourneyDay();
        Assert.True(advanceResult.Success, advanceResult.Message);
        var progressedSnapshot = setup.Journey!.ToSnapshot(setup.TravelRules);

        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();
        session.Apply(new JourneyStarted
        {
            JourneySnapshot = startingSnapshot,
            DiaryMessage = "You head out at dawn.",
            PursuitHeat = 0
        });

        return (session, progressedSnapshot);
    }
}
