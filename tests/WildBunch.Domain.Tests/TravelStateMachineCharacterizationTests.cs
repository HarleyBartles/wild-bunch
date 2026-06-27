using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Characterization tests pinning exact current travel/journey behavior.
/// These tests MUST pass before and after the Phase 2 event-sourcing migration.
/// All values are captured from deterministic scenarios using
/// TravelRandomnessState.CreateDeterministic(string.Empty) and ForcedRoll.
/// </summary>
public sealed class TravelStateMachineCharacterizationTests
{
    [Fact]
    public void StartJourney_EasyShortJourney_ExactInitialState()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();

        var result = session.StartJourney(preview);

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Equal(1, session.Journey.JourneySequence);
        Assert.Equal(TravelMode.Mounted, session.Journey.TravelMode);
        Assert.Equal(1m, session.Journey.RemainingRideDayDistance);
        Assert.Equal(1, session.Journey.RemainingDays);
        Assert.Equal(0, session.Journey.DaysTravelled);
        Assert.Equal(0, session.Journey.DelayDays);
        Assert.Equal(4, session.Journey.FoodRemaining);
        Assert.Equal(0, session.Journey.HorseFeedRemaining);
        Assert.Equal(10, session.Journey.AvailableCanteenCharges);
        Assert.Equal(HorseTravelState.Healthy, session.Journey.HorseState);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Null(session.Journey.CurrentDayPlan);
        Assert.Equal(1250, session.Player.Health);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(4, session.Player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void StartJourney_WhenAlreadyOnTrail_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        var secondStart = session.StartJourney(preview);

        Assert.False(secondStart.Success);
        Assert.Contains("already on the trail", secondStart.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdvanceJourneyDay_WhenNoJourney_Fails()
    {
        var session = TestSessionFactory.CreateDefault();
        var result = session.AdvanceJourneyDay();

        Assert.False(result.Success);
        Assert.Contains("No active journey", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // Heat no longer affects trail events or encounters, so the deterministic
    // rolls now produce a different outcome for the same route profile: the
    // EasyShortJourney is interrupted by an NPC encounter on day 1 instead of
    // completing quietly. See ADR-0029.
    [Fact]
    public void AdvanceJourneyDay_FirstDay_ExactState()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.False(result.Success);
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.Equal("Your journey is interrupted by a trail encounter.", result.Message);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);
        Assert.Equal(1, session.Journey.DaysTravelled);
        Assert.Equal(0, session.Journey.RemainingDays);
        Assert.Equal(3, session.Journey.FoodRemaining);
        Assert.Equal(0, session.Journey.HorseFeedRemaining);
        Assert.Equal(10, session.Journey.AvailableCanteenCharges);
        Assert.Equal(1250, session.Player.Health);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(3, session.Player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(0, session.PursuitState.Heat);
        Assert.Single(session.TravelDiaryDays);
        Assert.NotNull(session.Journey.PendingEncounter);
    }

    [Fact]
    public void AdvanceJourneyDay_WhenPendingEncounter_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);

        JourneyStatus status;
        do
        {
            var advanceResult = session.AdvanceJourneyDay();
            status = advanceResult.Status;
        } while (status == JourneyStatus.Active);

        Assert.Equal(JourneyStatus.Interrupted, status);

        var blockedAdvance = session.AdvanceJourneyDay();

        Assert.False(blockedAdvance.Success);
        Assert.Contains("pending encounter", blockedAdvance.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounter_WhenNoJourney_Fails()
    {
        var session = TestSessionFactory.CreateDefault();
        var result = session.ResolveJourneyEncounter("run");

        Assert.False(result.Success);
        Assert.Contains("No active journey", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounter_WhenNoPendingEncounter_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        var result = session.ResolveJourneyEncounter("run");

        Assert.False(result.Success);
        Assert.Contains("no pending encounter", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcknowledgeJourneyArrival_WhenNoJourney_Fails()
    {
        var session = TestSessionFactory.CreateDefault();
        var result = session.AcknowledgeJourneyArrival();

        Assert.False(result.Success);
        Assert.Contains("No completed journey", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcknowledgeJourneyArrival_WhenJourneyNotCompleted_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        var result = session.AcknowledgeJourneyArrival();

        Assert.False(result.Success);
        Assert.Contains("not ready to be acknowledged", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
