using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Characterization tests for encounter resolution with deterministic ForcedRoll.
/// Forces specific outcomes (0=success, 99=failure) and asserts exact state.
/// These tests MUST pass before and after the Phase 2 event-sourcing migration.
/// All values are captured from deterministic scenarios using
/// TravelRandomnessState.CreateDeterministic(string.Empty) and ForcedRoll.
/// </summary>
public sealed class TravelEncounterResolutionCharacterizationTests
{
    /// <summary>
    /// Helper: advance journey until interrupted by encounter.
    /// Fails the test if the journey doesn't interrupt within 10 days.
    /// </summary>
    private static void AdvanceUntilInterrupted(GameSession session)
    {
        for (var i = 0; i < 10; i++)
        {
            var result = session.AdvanceJourneyDay();
            if (result.Status == JourneyStatus.Interrupted)
                return;
            if (result.Status == JourneyStatus.Completed || !result.Success)
                Assert.Fail($"Journey did not interrupt — it {result.Status} on day {i + 1}. Adjust TravelTestFactory.CreateHighRiskJourney().");
        }
        Assert.Fail("Journey did not interrupt within 10 days. Adjust TravelTestFactory.CreateHighRiskJourney().");
    }

    [Fact]
    public void ResolveJourneyEncounter_Run_Success_ExactState()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        var result = session.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0UL);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.Equal("You push the rider behind you and keep moving.", result.Message);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(1250, session.Player.Health);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(2, session.Player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void ResolveJourneyEncounter_Run_Failure_KeepsEncounterPending()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        // The High-risk Badlands route produces a Foe encounter from route risk
        // and terrain alone; heat no longer affects trail encounters (BUNCH-85 / ADR-0029).
        AdvanceUntilInterrupted(session);

        var result = session.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 99UL);

        Assert.False(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.Equal("I tried to outrun the rider, but the horse still had to work for it.", result.Message);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);
        Assert.NotNull(session.Journey.PendingEncounter);
        Assert.Equal("foe", session.Journey.PendingEncounter!.Kind);
        Assert.Equal(1, session.Journey.PendingEncounter.HiddenState!.ChaseFatigue);
        Assert.Equal(0, session.Journey.PendingEncounter.HiddenState!.Annoyance);
        Assert.Equal(1250, session.Player.Health);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        // Heat no longer increases on the trail (BUNCH-85 / ADR-0029). The failed
        // mounted run still costs horse exhaustion but adds no pursuit heat.
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void ResolveJourneyEncounter_Bribe_Success_ExactWalletDelta()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        const decimal bribeAmount = 14m;

        var result = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: bribeAmount, forcedRoll: 0UL);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.Equal("You push the rider behind you and keep moving.", result.Message);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(11m, session.Player.Wallet.Cash);
        Assert.Equal(1250, session.Player.Health);
        Assert.Equal(0, session.PursuitState.Heat);
    }

    [Fact]
    public void ResolveJourneyEncounter_Bribe_Failure_LocksOutAfterTwoOffers()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        // The High-risk Badlands route produces a Foe encounter with a
        // deterministic MinimumBribe of $9.00 from route risk, terrain, wallet
        // band, and difficulty alone. Heat no longer affects foe profiles or
        // bribe costs (BUNCH-85 / ADR-0029).
        AdvanceUntilInterrupted(session);

        // First bribe attempt — non-insulting amount (4m > 3.15m insult threshold,
        // i.e. MinimumBribe * 0.35) keeps the encounter pending without retaliation.
        var firstBribe = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 4m, forcedRoll: 0UL);
        Assert.False(firstBribe.Success);
        Assert.True(firstBribe.SessionChanged);
        Assert.Equal(JourneyStatus.Interrupted, firstBribe.Status);
        Assert.Equal("I offered $4.00, and the rider pocketed it without moving aside.", firstBribe.Message);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);
        Assert.NotNull(session.Journey.PendingEncounter);
        Assert.Equal(1, session.Journey.PendingEncounter!.HiddenState!.BribeOffersMade);
        Assert.False(session.Journey.PendingEncounter.HiddenState!.BribeLockedOut);

        // Second bribe attempt — still non-insulting, but cumulative (8m) is below
        // the minimum bribe (9m), so the lockout flag is set after this offer.
        var secondBribe = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 4m, forcedRoll: 0UL);
        Assert.False(secondBribe.Success);
        Assert.True(secondBribe.SessionChanged);
        Assert.Equal(JourneyStatus.Interrupted, secondBribe.Status);
        Assert.Equal("I offered $4.00, and the rider pocketed it without moving aside.", secondBribe.Message);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);
        Assert.NotNull(session.Journey.PendingEncounter);
        Assert.Equal(2, session.Journey.PendingEncounter!.HiddenState!.BribeOffersMade);
        Assert.True(session.Journey.PendingEncounter.HiddenState!.BribeLockedOut);

        // Third bribe attempt — locked out, the rider refuses any more money.
        var thirdBribe = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 4m, forcedRoll: 0UL);
        Assert.False(thirdBribe.Success);
        Assert.False(thirdBribe.SessionChanged);
        Assert.Equal(JourneyStatus.Interrupted, thirdBribe.Status);
        Assert.Contains("not take any more money", thirdBribe.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounter_InvalidChoice_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        var result = session.ResolveJourneyEncounter("dance");

        Assert.False(result.Success);
        Assert.Contains("not a lawful way", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounter_EmptyChoice_Fails()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        session.StartJourney(preview);
        AdvanceUntilInterrupted(session);

        var result = session.ResolveJourneyEncounter("");

        Assert.False(result.Success);
        Assert.Contains("Choose how", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
