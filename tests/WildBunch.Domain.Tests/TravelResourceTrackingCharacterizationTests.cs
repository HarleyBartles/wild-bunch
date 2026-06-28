using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Characterization tests pinning exact resource tracking and journey completion
/// behavior. These tests MUST pass before and after the Phase 2 event-sourcing
/// migration. All values are captured from deterministic scenarios using
/// SaltSource.CreateFixed(string.Empty) and ForcedRoll.
/// </summary>
public sealed class TravelResourceTrackingCharacterizationTests
{
    [Fact]
    public void AdvanceJourneyDay_ConsumesExactFood()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        Assert.Equal(4, session.Journey!.FoodRemaining);

        session.AdvanceJourneyDay();

        Assert.Equal(3, session.Journey!.FoodRemaining);
        Assert.Equal(3, session.Player.Inventory.GetQuantity(ItemKind.Food));
    }

    [Fact]
    public void AdvanceJourneyDay_AdvancesClockExactly()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);

        session.AdvanceJourneyDay();

        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void AdvanceJourneyDay_DoesNotRaiseHeatFromRouteRiskAlone()
    {
        // Heat is future lawman pressure, not trail danger — travel no longer
        // raises heat from route risk. See ADR-0029.
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        Assert.Equal(0, session.PursuitState.Heat);

        session.AdvanceJourneyDay();

        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void AdvanceJourneyDay_PreservesHealthExactly()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        Assert.Equal(1250, session.Player.Health);

        session.AdvanceJourneyDay();

        Assert.Equal(1250, session.Player.Health);
    }

    // Heat no longer affects trail events or encounters, so the deterministic
    // rolls now produce a different outcome for the same route profile: the
    // EasyShortJourney is interrupted by an NPC encounter on day 1 instead of
    // completing quietly with a LuckyCoinCache. The wallet is therefore
    // unchanged. See ADR-0029.
    [Fact]
    public void AdvanceJourneyDay_InterruptedByEncounter_LeavesWalletUnchanged()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        Assert.Equal(25m, session.Player.Wallet.Cash);

        var result = session.AdvanceJourneyDay();

        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.Equal(25m, session.Player.Wallet.Cash);
    }

    [Fact]
    public void SixDayQuietJourney_CompletesWithExactState()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Completed, result.Status);
        Assert.Equal("You reach Six Mile.", result.Message);
        Assert.Equal(JourneyStatus.Completed, session.Journey!.Status);
        Assert.Equal(4, session.Journey.DaysTravelled);
        Assert.Equal(1250, session.Player.Health);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(4, session.Journey.FoodRemaining);
        Assert.Equal(4, session.Player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(5, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(0, session.PursuitState.Heat);
        Assert.Equal(4, session.TravelDiaryDays.Count);
        Assert.Equal(new TownId("d2"), session.Player.CurrentTownId);
    }

    [Fact]
    public void AcknowledgeJourneyArrival_ClearsJourneyAndChangesTown()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);
        var destinationTown = preview.DestinationTownId;
        Assert.Equal(new TownId("o2"), session.Player.CurrentTownId);

        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(JourneyStatus.Completed, result.Status);
        Assert.Equal(destinationTown, session.Player.CurrentTownId);
        Assert.NotNull(session.Journey);

        var ackResult = session.AcknowledgeJourneyArrival();

        Assert.True(ackResult.Success);
        Assert.Null(session.Journey);
        Assert.Equal(destinationTown, session.Player.CurrentTownId);
    }

    [Fact]
    public void FullJourneyCycle_ExactStateAtEachStep()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);

        // Per-day captured values for CreateSixDayQuietJourney.
        // Indexed by day number (1-based). The journey completes on day 4.
        var expectedFoodRemaining = new[] { 7, 6, 5, 4 };
        var expectedHealth = new[] { 1250, 1250, 1250, 1250 };
        // Heat stays 0 — travel no longer raises heat from route risk. See ADR-0029.
        var expectedHeat = new[] { 0, 0, 0, 0 };
        var expectedClockDay = new[] { 2, 3, 4, 5 };
        var expectedCash = new[] { 25m, 25m, 25m, 25m };
        var expectedTravelDiaryDays = new[] { 1, 2, 3, 4 };
        var expectedStatus = new[]
        {
            JourneyStatus.Active,
            JourneyStatus.Active,
            JourneyStatus.Active,
            JourneyStatus.Completed,
        };

        var dayCount = 0;
        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
            dayCount++;

            Assert.Equal(dayCount, session.Journey!.DaysTravelled);
            Assert.Equal(expectedFoodRemaining[dayCount - 1], session.Journey.FoodRemaining);
            Assert.Equal(expectedHealth[dayCount - 1], session.Player.Health);
            Assert.Equal(expectedHeat[dayCount - 1], session.PursuitState.Heat);
            Assert.Equal(expectedClockDay[dayCount - 1], session.Clock.Day);
            Assert.Equal(0, session.Clock.Turn);
            Assert.Equal(expectedCash[dayCount - 1], session.Player.Wallet.Cash);
            Assert.Equal(expectedTravelDiaryDays[dayCount - 1], session.TravelDiaryDays.Count);
            Assert.Equal(expectedStatus[dayCount - 1], result.Status);
        } while (result.Status == JourneyStatus.Active && result.Success);

        Assert.Equal(JourneyStatus.Completed, result.Status);
        Assert.Equal(4, dayCount);
        Assert.Equal(4, session.Journey!.DaysTravelled);
        Assert.Equal(4, session.Journey.FoodRemaining);
        Assert.Equal(1250, session.Player.Health);
        Assert.Equal(0, session.PursuitState.Heat);
        Assert.Equal(5, session.Clock.Day);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(4, session.TravelDiaryDays.Count);

        var ackResult = session.AcknowledgeJourneyArrival();

        Assert.True(ackResult.Success);
        Assert.Null(session.Journey);
        Assert.Equal(new TownId("d2"), session.Player.CurrentTownId);
    }
}
