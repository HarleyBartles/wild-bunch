using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Guardrail tests proving heat is future lawman pressure, not trail danger.
/// These tests encode the BUNCH-85 / ADR-0029 semantic decision:
/// - Travel no longer raises heat from route risk alone.
/// - Private-hardship trail events (washout, dust storm, spooked horse, hard miles)
///   do not raise heat because they are not noisy/visible/witnessed incidents.
/// - Encounter run/fight/bribe heat is preserved (visible/noisy incidents).
/// </summary>
public sealed class HeatSemanticGuardrailTests
{
    [Fact]
    public void QuietTravelDay_DoesNotRaiseHeat_FromRouteRiskAlone()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        Assert.Equal(0, session.PursuitState.Heat);

        session.AdvanceJourneyDay();

        // Heat stays 0 — travel no longer raises heat from route risk.
        // See ADR-0029.
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void MultipleQuietTravelDays_DoNotRaiseHeat_FromRouteRiskAlone()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        session.StartJourney(preview);
        Assert.Equal(0, session.PursuitState.Heat);

        // Advance all days of the quiet journey — heat should never rise
        // from route risk alone.
        for (var i = 0; i < 6; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Completed)
                break;
        }

        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void BadLuckTrailEvents_DoNotRaiseHeat_BecauseTheyArePrivateHardship()
    {
        // All bad-luck trail events (washout, dust-choked outfit, spooked horse,
        // hard miles) should carry HeatIncrease=0 because they are private hardship
        // or generic route difficulty, not noisy/visible/witnessed incidents.
        // See ADR-0029 and the trail-event heat audit.
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);

        // Advance days until the journey completes or interrupts.
        // Any trail events that fire should not raise heat.
        for (var i = 0; i < 10; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status is JourneyStatus.Completed or JourneyStatus.Interrupted)
                break;
        }

        // Heat should be 0 — no per-day route-risk increase, and any trail events
        // that fired are private hardship with HeatIncrease=0.
        // If the journey interrupted with an encounter, heat may be >0 from the
        // encounter resolution, but we haven't resolved any encounter here.
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void PursuitState_IncreaseHeat_StillWorks_ForVisibleNoisyIncidents()
    {
        // The heat model itself is not disabled — encounter run/fight/bribe
        // still raises heat because those are visible/noisy incidents.
        // This test proves the IncreaseHeat path is intact for future lawman
        // pressure from noisy behavior.
        var pursuitState = new PursuitState();
        Assert.Equal(0, pursuitState.Heat);

        pursuitState.IncreaseHeat(2);

        Assert.Equal(2, pursuitState.Heat);
    }
}
