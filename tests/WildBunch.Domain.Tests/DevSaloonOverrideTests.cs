using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Tests for BUNCH-90 event-sourced saloon dev controls.
/// Proves force, clear, consume-once, replay safety, and no-override unchanged behavior.
/// </summary>
public sealed class DevSaloonOverrideTests
{
    [Fact]
    public void ForceDevSaloonOverride_ProducesForcedEvent_AndSetsPendingOverride()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var suspectId = new SuspectId("suspect-1");

        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));

        var forcedEvent = session.UncommittedEvents.OfType<DevSaloonOverrideForced>().Single();
        Assert.Equal(DevSaloonPoiKind.Suspect, forcedEvent.ForcedKind);
        Assert.Equal(suspectId, forcedEvent.ForcedSuspectId);
        Assert.NotNull(session.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Suspect, session.PendingDevSaloonOverride!.ForcedKind);
        Assert.Equal(suspectId, session.PendingDevSaloonOverride.ForcedSuspectId);
    }

    [Fact]
    public void ClearDevSaloonOverride_ProducesClearedEvent_AndClearsPendingOverride()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();

        session.ClearDevSaloonOverride();

        Assert.IsType<DevSaloonOverrideCleared>(session.UncommittedEvents.Single());
        Assert.Null(session.PendingDevSaloonOverride);
    }

    [Fact]
    public void ClearDevSaloonOverride_WhenNoOverride_IsNoOp()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();

        session.ClearDevSaloonOverride();

        Assert.Empty(session.UncommittedEvents);
        Assert.Null(session.PendingDevSaloonOverride);
    }

    [Fact]
    public void ForceDevSaloonOverride_RejectsTrueCulprit_WhenKillerReleaseGateIsLocked()
    {
        // Default session: killer-release gate is locked (progress=0, threshold=2).
        // The true culprit is gated out of saloon POI, not permanently barred.
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var trueCulpritId = new SuspectId("suspect-2");

        // Verify the gate is locked
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(trueCulpritId)));

        // The rejection message must be gate-aware, not "must never appear"
        Assert.Contains("killer trail is locked", ex.Message.ToLowerInvariant());
        Assert.DoesNotContain("must never appear", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void ForceDevSaloonOverride_AcceptsTrueCulprit_WhenKillerReleaseGateIsOpen()
    {
        // Gate-open session: killer-release progress = threshold = 2.
        // The true culprit is now eligible as a saloon POI candidate.
        var session = TestSessionFactory.CreateWithKillerReleaseGateOpen();
        var trueCulpritId = new SuspectId("suspect-2");

        // Verify the gate is open
        Assert.True(session.CaseFile.KillerReleaseState.IsReleased);

        // Force should succeed — the true culprit is eligible when the gate is open
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(trueCulpritId));
        session.MarkEventsCommitted();

        Assert.NotNull(session.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Suspect, session.PendingDevSaloonOverride!.ForcedKind);
        Assert.Equal(trueCulpritId, session.PendingDevSaloonOverride.ForcedSuspectId);
    }

    [Fact]
    public void ForceDevSaloonOverride_RejectsUnknownSuspect()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var unknownId = new SuspectId("suspect-999");

        Assert.Throws<InvalidOperationException>(() =>
            session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(unknownId)));
    }

    [Fact]
    public void ForceDevSaloonOverride_AcceptsSuspect_WithWarrantAndBadPresence()
    {
        // BUNCH-106 realignment: any non-culprit suspect can appear in any saloon.
        // A suspect with a known warrant and SecuredAlive presence state is still
        // eligible — no town presence, warrant, or poster state gates.
        var session = TestSessionFactory.CreateWithIneligibleWarrantedSuspect();
        var suspectId = new SuspectId("suspect-1");

        // Verify the test setup: suspect-1 is not the true culprit
        Assert.NotEqual(session.CaseFile.TrueCulpritId, suspectId);
        // Verify suspect-1 has a known warrant with SecuredAlive presence
        Assert.True(session.TryGetWantedSuspectPresenceState(suspectId, out var presence));
        Assert.Equal(WantedSuspectPresenceState.SecuredAlive, presence);

        // Force should succeed — warrant/presence state no longer gates saloon POI eligibility
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        Assert.NotNull(session.PendingDevSaloonOverride);
        Assert.Equal(DevSaloonPoiKind.Suspect, session.PendingDevSaloonOverride!.ForcedKind);
        Assert.Equal(suspectId, session.PendingDevSaloonOverride.ForcedSuspectId);
    }

    [Fact]
    public void LookAroundSaloon_WithSuspectOverride_ConsumesOverrideAndSpotsForcedSuspect()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        var suspectId = new SuspectId("suspect-1");
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        // Event stream: DevSaloonOverrideConsumed -> SaloonPersonOfInterestSpotted
        var eventList = session.UncommittedEvents.ToList();
        var consumedIndex = eventList.FindIndex(e => e is DevSaloonOverrideConsumed);
        var spottedIndex = eventList.FindIndex(e => e is SaloonPersonOfInterestSpotted);
        Assert.True(consumedIndex >= 0 && spottedIndex >= 0);
        Assert.True(consumedIndex < spottedIndex);
        var spottedEvent = eventList.OfType<SaloonPersonOfInterestSpotted>().Single();
        Assert.Equal(suspectId, spottedEvent.SuspectId);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, spottedEvent.PersonOfInterestKind);
        // Override is cleared after consumption
        Assert.Null(session.PendingDevSaloonOverride);
        // Active saloon POI state is set from the forced suspect
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.NotNull(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect,
            session.CurrentTownVisit.CurrentTownState.ResolveActiveSaloonPersonOfInterestKind());
    }

    [Fact]
    public void LookAroundSaloon_WithCitizenOverride_ConsumesOverrideAndSpotsCitizen()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        var eventList = session.UncommittedEvents.ToList();
        var consumedIndex = eventList.FindIndex(e => e is DevSaloonOverrideConsumed);
        var spottedIndex = eventList.FindIndex(e => e is SaloonPersonOfInterestSpotted);
        Assert.True(consumedIndex >= 0 && spottedIndex >= 0);
        Assert.True(consumedIndex < spottedIndex);
        var spottedEvent = eventList.OfType<SaloonPersonOfInterestSpotted>().Single();
        Assert.Null(spottedEvent.SuspectId);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, spottedEvent.PersonOfInterestKind);
        Assert.Null(session.PendingDevSaloonOverride);
        // Active saloon POI state is set as a citizen (no suspect id, descriptor present)
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.NotNull(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
        Assert.StartsWith("a stranger with", session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
        Assert.NotNull(spottedEvent.CitizenRole);
        Assert.NotNull(session.CurrentTownVisit.CurrentTownState.ActiveSaloonCitizenRole);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen,
            session.CurrentTownVisit.CurrentTownState.ResolveActiveSaloonPersonOfInterestKind());
    }

    [Fact]
    public void LookAroundSaloon_WithCitizenRoleOverride_ConsumesOverrideAndSpotsForcedCitizen()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen("butcher"));
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        var eventList = session.UncommittedEvents.ToList();
        var spottedEvent = eventList.OfType<SaloonPersonOfInterestSpotted>().Single();
        Assert.Null(spottedEvent.SuspectId);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, spottedEvent.PersonOfInterestKind);
        Assert.Equal("butcher", spottedEvent.CitizenRole);
        Assert.StartsWith("a stranger with", spottedEvent.Descriptor);
        Assert.Null(session.PendingDevSaloonOverride);
        Assert.Equal("butcher", session.CurrentTownVisit.CurrentTownState.ActiveSaloonCitizenRole);
    }

    [Fact]
    public void LookAroundSaloon_WithAnySuspectOverride_ConsumesOverrideAndSpotsEligibleSuspect()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForAnySuspect());
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        var spottedEvent = session.UncommittedEvents.OfType<SaloonPersonOfInterestSpotted>().Single();
        Assert.NotNull(spottedEvent.SuspectId);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, spottedEvent.PersonOfInterestKind);
        Assert.Null(session.PendingDevSaloonOverride);
    }

    [Fact]
    public void LookAroundSaloon_WithNoneOverride_ConsumesOverrideAndSpotsNobody()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForNone());
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        var eventList = session.UncommittedEvents.ToList();
        var consumedIndex = eventList.FindIndex(e => e is DevSaloonOverrideConsumed);
        var spottedIndex = eventList.FindIndex(e => e is SaloonPersonOfInterestSpotted);
        Assert.True(consumedIndex >= 0 && spottedIndex >= 0);
        Assert.True(consumedIndex < spottedIndex);
        var spottedEvent = eventList.OfType<SaloonPersonOfInterestSpotted>().Single();
        Assert.Null(spottedEvent.SuspectId);
        Assert.Null(spottedEvent.Descriptor);
        Assert.Null(spottedEvent.PersonOfInterestKind);
        Assert.Null(spottedEvent.CitizenRole);
        Assert.Null(session.PendingDevSaloonOverride);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
    }

    [Fact]
    public void LookAroundSaloon_WithoutOverride_ProducesNoDevEvents()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Empty(session.UncommittedEvents.OfType<DevSaloonOverrideConsumed>());
        Assert.Empty(session.UncommittedEvents.OfType<DevSaloonOverrideForced>());
        Assert.Null(session.PendingDevSaloonOverride);
    }

    [Fact]
    public void LookAroundSaloon_WithOverride_ConsumesOnce_AndSecondLookAroundIsNormal()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();

        // First look-around consumes the override
        session.LookAroundSaloon();
        session.MarkEventsCommitted();
        Assert.Null(session.PendingDevSaloonOverride);

        // Second look-around should not produce any dev events
        session.LookAroundSaloon();
        Assert.Empty(session.UncommittedEvents.OfType<DevSaloonOverrideConsumed>());
    }

    [Fact]
    public void EventReplay_ForcedThenConsumed_ReconstructsCorrectState()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();

        session.LookAroundSaloon();
        session.MarkEventsCommitted();

        // Rehydrate from the full event stream: Forced -> Consumed -> SaloonPersonOfInterestSpotted
        var setupEvents = TravelTestFactory.RecaptureSetupEventsForReplay(session);
        var events = setupEvents.Concat(session.CommittedEvents.OfType<IDomainEvent>()).ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, events);

        // Critical replay-safety proof: override is null after replay
        Assert.Null(rehydrated.PendingDevSaloonOverride);
    }

    [Fact]
    public void ForceDevSaloonOverride_WhileJourneyActive_Throws()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        // Start a journey using the travel resolver
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId,
                new TownId("connected"),
                session.Player.Inventory)
            .Preview!;
        session.StartJourney(preview);

        Assert.Throws<InvalidOperationException>(() =>
            session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen()));
    }
}
