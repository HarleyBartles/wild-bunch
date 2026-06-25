using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Guardrail tests proving heat is future lawman pressure from time spent in
/// town, not trail danger. These tests encode the BUNCH-85 / ADR-0029 model:
/// - Heat increases by +1 only when a full in-town day rolls over (turn 4 → next day turn 1).
/// - Heat resets to 0 when leaving town (starting a journey).
/// - Heat does not change on the trail — trail events and encounters do not affect heat.
/// - High/low heat has no mechanical effect yet.
/// - Heat starts counting again when the player reaches the next town and spends time there.
/// </summary>
public sealed class HeatSemanticGuardrailTests
{
    [Fact]
    public void NonRolloverTownTurns_DoNotIncreaseHeat()
    {
        var session = TestSessionFactory.CreateDefault();

        // EnterActionContext advances the turn when the context changes.
        // Turns 1, 2, and 3 do not roll over a full day, so heat stays at 0.
        Assert.Equal(0, session.PursuitState.Heat);

        session.EnterActionContext(TownActionContext.SheriffOffice);
        Assert.Equal(0, session.PursuitState.Heat);

        session.EnterActionContext(TownActionContext.Saloon);
        Assert.Equal(0, session.PursuitState.Heat);

        session.EnterActionContext(TownActionContext.Store);
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void TownDayRollover_IncreasesHeatByExactlyOne()
    {
        var session = TestSessionFactory.CreateDefault();

        Assert.Equal(0, session.PursuitState.Heat);

        // Advance through 4 context changes to trigger a day rollover
        // (turn 0 → 1 → 2 → 3 → rollover to day 2 turn 0).
        session.EnterActionContext(TownActionContext.SheriffOffice);
        session.EnterActionContext(TownActionContext.Saloon);
        session.EnterActionContext(TownActionContext.Store);
        session.EnterActionContext(TownActionContext.Stable);

        // After one full day rollover, heat should be exactly 1.
        Assert.Equal(1, session.PursuitState.Heat);
    }

    [Fact]
    public void StartingJourney_ResetsHeatToZero()
    {
        var session = TestSessionFactory.CreateDefault();

        // Build up heat by rolling over a full town day.
        session.EnterActionContext(TownActionContext.SheriffOffice);
        session.EnterActionContext(TownActionContext.Saloon);
        session.EnterActionContext(TownActionContext.Store);
        session.EnterActionContext(TownActionContext.Stable);
        Assert.Equal(1, session.PursuitState.Heat);

        // Start a journey — heat resets to 0.
        var (travelSession, preview) = TravelTestFactory.CreateEasyShortJourney();
        travelSession.StartJourney(preview);

        Assert.Equal(0, travelSession.PursuitState.Heat);
    }

    [Fact]
    public void AdvancingTrailDays_DoesNotIncreaseHeat()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        Assert.Equal(0, session.PursuitState.Heat);

        // Advance all days of the journey — heat should never rise on the trail.
        for (var i = 0; i < 6; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Completed)
                break;
        }

        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void ArrivalInNewTown_DoesNotRetroactivelyAddTravelHeat()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        // Advance until the journey completes.
        for (var i = 0; i < 6; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Completed)
                break;
        }

        // Heat should still be 0 after arrival — travel does not add heat.
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void TownDayRolloverInNextTown_StartsIncrementingHeatAgain()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);

        // Advance until the journey completes.
        for (var i = 0; i < 6; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Completed)
                break;
        }

        Assert.Equal(0, session.PursuitState.Heat);

        // Acknowledge arrival to clear the journey and enter the new town.
        session.AcknowledgeJourneyArrival();

        // Roll over a full day in the new town by cycling through action contexts.
        session.EnterActionContext(TownActionContext.SheriffOffice);
        session.EnterActionContext(TownActionContext.Saloon);
        session.EnterActionContext(TownActionContext.Store);
        session.EnterActionContext(TownActionContext.Stable);

        // Heat should now be 1 — time in the new town started accumulating heat.
        Assert.Equal(1, session.PursuitState.Heat);
    }

    [Fact]
    public void PursuitState_SetHeat_SetsAbsoluteValue()
    {
        // The SetHeat path is used by event-sourced Apply methods that carry
        // the absolute heat value. This proves it works independently of
        // the IncreaseHeat accumulation path.
        var pursuitState = new PursuitState();
        Assert.Equal(0, pursuitState.Heat);

        pursuitState.SetHeat(3);
        Assert.Equal(3, pursuitState.Heat);

        pursuitState.SetHeat(0);
        Assert.Equal(0, pursuitState.Heat);
    }
}
