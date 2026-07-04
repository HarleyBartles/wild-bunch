using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Tests for BUNCH-89 event-sourced travel dev controls.
/// Proves force, clear, consume-once, replay safety, and no-override unchanged behavior.
/// </summary>
public sealed class DevTravelOverrideTests
{
    private static GameSession CreateSessionWithActiveJourney()
    {
        var (session, preview) = TravelTestFactory.CreateEasyShortJourney();
        session.StartJourney(preview);
        session.MarkEventsCommitted();
        return session;
    }

    [Fact]
    public void ForceDevTravelOverride_ProducesEvent_AndSetsPendingOverride()
    {
        var session = CreateSessionWithActiveJourney();
        var foeProfile = new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m);

        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(foeProfile, "A hard-eyed rider blocks the trail."));

        var forcedEvent = Assert.Single(session.UncommittedEvents.OfType<DevTravelOverrideForced>());
        Assert.Equal(TravelDayEncounterCategory.Foe, forcedEvent.ForcedCategory);
        Assert.NotNull(forcedEvent.FoeProfile);
        Assert.Equal(5, forcedEvent.FoeProfile!.Speed);
        Assert.NotNull(session.PendingDevTravelOverride);
    }

    [Fact]
    public void ClearDevTravelOverride_ProducesEvent_AndClearsPendingOverride()
    {
        var session = CreateSessionWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Lucky));
        session.MarkEventsCommitted();

        session.ClearDevTravelOverride();

        Assert.Single(session.UncommittedEvents.OfType<DevTravelOverrideCleared>());
        Assert.Null(session.PendingDevTravelOverride);
    }

    [Fact]
    public void ClearDevTravelOverride_WithNoOverride_IsNoOp()
    {
        var session = CreateSessionWithActiveJourney();

        session.ClearDevTravelOverride();

        Assert.Empty(session.UncommittedEvents);
        Assert.Null(session.PendingDevTravelOverride);
    }

    [Fact]
    public void ForceDevTravelOverride_WithoutActiveJourney_Throws()
    {
        var (session, _) = TravelTestFactory.CreateEasyShortJourney();

        Assert.Throws<InvalidOperationException>(() =>
            session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe)));
    }

    [Fact]
    public void AdvanceJourneyDay_WithDevOverride_ConsumesOverrideOnce()
    {
        var session = CreateSessionWithActiveJourney();
        var foeProfile = new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m);
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(foeProfile, "A hard-eyed rider blocks the trail."));
        session.MarkEventsCommitted();

        session.AdvanceJourneyDay();

        // DevTravelOverrideConsumed event was emitted
        Assert.Single(session.UncommittedEvents.OfType<DevTravelOverrideConsumed>());
        // Override consumed after advance
        Assert.Null(session.PendingDevTravelOverride);
        // Journey interrupted by the forced foe encounter
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);
        Assert.NotNull(session.Journey.PendingEncounter);
        Assert.Equal("foe", session.Journey.PendingEncounter!.Kind);
        // The forced day plan reflects the override: the pending encounter's
        // foe profile matches the forced values, proving the day plan was
        // built from the captured override before the consumed event cleared it.
        Assert.Equal(5, session.Journey.PendingEncounter!.FoeProfile!.Speed);
        Assert.Equal(4, session.Journey.PendingEncounter.FoeProfile!.FightStrength);
        Assert.Equal(8m, session.Journey.PendingEncounter.FoeProfile!.MinimumBribe);
        Assert.Equal("A hard-eyed rider blocks the trail.", session.Journey.PendingEncounter!.Message);
    }

    [Fact]
    public void AdvanceJourneyDay_AfterConsumedOverride_ResumesNormalGeneration()
    {
        var session = CreateSessionWithActiveJourney();
        var foeProfile = new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m);
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(foeProfile));
        session.MarkEventsCommitted();
        session.AdvanceJourneyDay();
        // Resolve the encounter to continue (forcedRoll 0 = successful run)
        session.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0UL);
        session.MarkEventsCommitted();

        // Next advance should use normal generation (no override)
        session.AdvanceJourneyDay();
        Assert.Null(session.PendingDevTravelOverride);
        // No new DevTravelOverrideConsumed event (override was already consumed)
        Assert.Empty(session.UncommittedEvents.OfType<DevTravelOverrideConsumed>());
    }

    [Fact]
    public void AdvanceJourneyDay_WithNoDevOverride_UsesGeneratorOutput()
    {
        var session = CreateSessionWithActiveJourney();

        session.AdvanceJourneyDay();

        Assert.Null(session.PendingDevTravelOverride);
        // No dev events in the stream
        Assert.Empty(session.UncommittedEvents.OfType<DevTravelOverrideForced>());
        Assert.Empty(session.UncommittedEvents.OfType<DevTravelOverrideConsumed>());
    }

    [Fact]
    public void RehydrateFromEvents_WithDevOverrideForced_ReconstructsOverrideState()
    {
        var session = CreateSessionWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        session.MarkEventsCommitted();

        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        var events = gameStarted.Concat(session.CommittedEvents.OfType<IDomainEvent>()).ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, events);

        Assert.NotNull(rehydrated.PendingDevTravelOverride);
        Assert.Equal(TravelDayEncounterCategory.Foe, rehydrated.PendingDevTravelOverride!.ForcedCategory);
    }

    [Fact]
    public void RehydrateFromEvents_AfterConsumption_HasNoPendingOverride()
    {
        var session = CreateSessionWithActiveJourney();
        session.ForceDevTravelOverride(DevTravelOverride.ForFoe(
            new JourneyFoeProfile(Speed: 5, FightStrength: 4, MinimumBribe: 8m)));
        session.MarkEventsCommitted();
        session.AdvanceJourneyDay();
        session.MarkEventsCommitted();

        // Rehydrate from the full event stream: Forced -> Consumed -> TravelDayAdvanced
        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        var events = gameStarted.Concat(session.CommittedEvents.OfType<IDomainEvent>()).ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, events);

        // Critical replay-safety proof: override is null after replay
        Assert.Null(rehydrated.PendingDevTravelOverride);
    }

    [Fact]
    public void RehydrateFromEvents_WithNoDevOverride_HasNoPendingOverride()
    {
        var session = CreateSessionWithActiveJourney();
        session.AdvanceJourneyDay();
        session.MarkEventsCommitted();

        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        var events = gameStarted.Concat(session.CommittedEvents.OfType<IDomainEvent>()).ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, events);

        Assert.Null(rehydrated.PendingDevTravelOverride);
    }
}
