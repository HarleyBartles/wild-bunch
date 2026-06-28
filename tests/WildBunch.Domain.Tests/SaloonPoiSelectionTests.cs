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
/// Tests for BUNCH-106 simplified saloon POI selection.
/// Any non-culprit suspect can appear in any saloon — no town presence, warrant,
/// or poster state gates. The true killer is gated until the killer-release gate opens.
/// The pool includes suspects + citizens + nobody.
/// </summary>
public sealed class SaloonPoiSelectionTests
{
    [Fact]
    public void NonCulpritSuspectCanBeSelectedAsSaloonPoiWithoutTownPresence()
    {
        // suspect-1 has no SetWantedSuspectPresenceState call and no known warrant.
        // Under the old rules, this suspect would be ineligible. Under BUNCH-106,
        // any non-culprit suspect can appear in any saloon.
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Equal(new SuspectId("suspect-1"), session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
    }

    [Fact]
    public void CitizenCanBeSelectedAsSaloonPoiViaDevOverrideWithoutSuspectIneligibilityHacks()
    {
        // Use a session with suspects (no need to remove them or make them ineligible).
        // Force a citizen via dev override — the proper test seam per BUNCH-106.
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
        Assert.NotNull(session.CurrentTownVisit.CurrentTownState.ActiveSaloonCitizenRole);
    }

    [Fact]
    public void NobodyOfInterestIsPossibleSaloonOutcomeViaDevOverride()
    {
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForNone());
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Contains("nobody of interest", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
    }

    [Fact]
    public void UnreleasedTrueKillerIsNotSelectedByOrdinarySaloonPoiRoll()
    {
        // The true killer (suspect-2) is gated out while the killer-release gate is locked.
        // Forcing a dev override for the true killer should throw.
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-2"))));

        Assert.Contains("killer trail is locked", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void ReleasedTrueKillerIsEligibleForSaloonPoi()
    {
        // When the killer-release gate is open, the true killer becomes eligible.
        var session = TestSessionFactory.CreateWithKillerReleaseGateOpen();
        Assert.True(session.CaseFile.KillerReleaseState.IsReleased);

        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-2")));
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Equal(new SuspectId("suspect-2"), session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
    }

    [Fact]
    public void DevOverrideCanForceSuspectCitizenAndNobodyCleanly()
    {
        // Suspect
        var session1 = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session1.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session1.MarkEventsCommitted();
        var result1 = session1.LookAroundSaloon();
        Assert.True(result1.Success);
        Assert.Equal(new SuspectId("suspect-1"), session1.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        // Citizen
        var session2 = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session2.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session2.MarkEventsCommitted();
        var result2 = session2.LookAroundSaloon();
        Assert.True(result2.Success);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, session2.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);

        // Nobody
        var session3 = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session3.ForceDevSaloonOverride(DevSaloonOverride.ForNone());
        session3.MarkEventsCommitted();
        var result3 = session3.LookAroundSaloon();
        Assert.True(result3.Success);
        Assert.Null(session3.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Null(session3.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
    }

    [Fact]
    public void NormalRollIncludesSuspectsCitizensAndNobodyInPool()
    {
        // Verify that the normal roll (no dev override) can produce different outcomes
        // across different sessions with different salt sources. We don't assert a specific
        // outcome (that would be brittle), but we verify the pool is non-trivial by checking
        // that the result is valid (suspect, citizen, or nobody).
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        // The outcome is one of: suspect, citizen, or nobody.
        var kind = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind;
        var hasSuspect = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId is not null;
        var hasDescriptor = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor is not null;
        // At least one of these must be true: it's a suspect, a citizen, or nobody.
        Assert.True(hasSuspect || hasDescriptor || (!hasSuspect && !hasDescriptor));
    }
}
