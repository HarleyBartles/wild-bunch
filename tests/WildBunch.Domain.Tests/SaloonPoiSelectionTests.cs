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
    public void NormalRollCanProduceSuspectCitizenAndNobodyOutcomes()
    {
        // The normal roll (no dev override) draws from a pool of
        // [eligible suspects + citizen roles + nobody]. With CreateWithConfrontableSaloonSuspect
        // there is 1 eligible suspect (suspect-1), 19 citizen roles, and 1 nobody slot
        // = pool size 21. The roll hash is deterministic from salt + town + day + turn + visit.
        //
        // We brute-force a small set of salt strings to prove all three outcome types
        // are reachable from the normal roll path — not just the dev override path.
        var salts = Enumerable.Range(0, 100).Select(i => $"salt-{i}").ToList();
        var sawSuspect = false;
        var sawCitizen = false;
        var sawNobody = false;

        foreach (var salt in salts)
        {
            var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
            session.ForceDevSaltSource(SaltSource.CreateFixed(salt));
            session.MarkEventsCommitted();

            var result = session.LookAroundSaloon();
            Assert.True(result.Success);

            var kind = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind;
            var hasSuspectId = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId is not null;

            if (hasSuspectId)
                sawSuspect = true;
            else if (kind == SaloonPersonOfInterestKind.Citizen)
                sawCitizen = true;
            else
                sawNobody = true;

            if (sawSuspect && sawCitizen && sawNobody)
                break;
        }

        Assert.True(sawSuspect, "Normal roll never produced a suspect outcome across 100 salts.");
        Assert.True(sawCitizen, "Normal roll never produced a citizen outcome across 100 salts.");
        Assert.True(sawNobody, "Normal roll never produced a nobody outcome across 100 salts.");
    }
}
