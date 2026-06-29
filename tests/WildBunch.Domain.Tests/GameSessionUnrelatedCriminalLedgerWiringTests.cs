using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Wiring tests for the <see cref="UnrelatedCriminalLedger"/> hosted by
/// <see cref="GameSession"/>. Covers the gang-side parity path: when a gang
/// member is turned in to the sheriff, the ledger drops the parity target and
/// despawns excess unrelated criminals. See BUNCH-107.
/// </summary>
public sealed class GameSessionUnrelatedCriminalLedgerWiringTests
{
    [Fact]
    public void SessionWithFullUnrelatedRoster_BuildsLedgerAtGangParity()
    {
        var session = CreateSession(gangSuspectCount: 1, unrelatedWarrantCount: 3);

        Assert.Equal(1, session.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.Equal(1, session.UnrelatedCriminalLedger.ActiveCriminalCount);
        Assert.Equal(3, session.UnrelatedCriminalLedger.PoolSize);
    }

    [Fact]
    public void SessionWithPartialRoster_BuildsDegenerateNoOpLedger()
    {
        // 2 gang suspects but only 1 unrelated warrant -> below 3x invariant -> no-op ledger.
        var session = CreateSession(gangSuspectCount: 2, unrelatedWarrantCount: 1);

        Assert.Equal(0, session.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.Equal(0, session.UnrelatedCriminalLedger.ActiveCriminalCount);
    }

    [Fact]
    public void SettlingGangMemberTurnIn_DropsParityAndDespawnsExcessUnrelatedCriminal()
    {
        var session = CreateSession(gangSuspectCount: 1, unrelatedWarrantCount: 3);
        Assert.Equal(1, session.UnrelatedCriminalLedger.ActiveCriminalCount);

        // Drive the full gang turn-in flow: saloon -> confront (surrender) -> sheriff.
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        var result = session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        Assert.True(result.Success);

        // The single gang member taken in -> parity target 0 -> the 1 active unrelated despawned.
        Assert.Equal(0, session.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.Equal(0, session.UnrelatedCriminalLedger.ActiveCriminalCount);
        Assert.Single(session.UnrelatedCriminalLedger.RetiredWarrantIds);
    }

    [Fact]
    public void ReadWantedPosters_DoesNotSurfaceRetiredUnrelatedCriminalWarrants()
    {
        // With 1 gang member and 3 unrelated criminals, the ledger activates 1 unrelated.
        var session = CreateSession(gangSuspectCount: 1, unrelatedWarrantCount: 3);

        // Drive a gang take-in to despawn the active unrelated criminal.
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        // The despawned warrant is now retired.
        var retiredIds = session.UnrelatedCriminalLedger.RetiredWarrantIds;
        Assert.NotEmpty(retiredIds);

        // Reading wanted posters should not surface any retired warrant.
        // The known warrants after reading must not include any retired ID.
        var knownBefore = session.CaseFile.KnownWarrants.Select(w => w.Id).ToHashSet();
        session.ReadWantedPosters();
        var knownAfter = session.CaseFile.KnownWarrants.Select(w => w.Id).ToHashSet();

        // Any newly surfaced warrant must not be a retired one.
        var newlySurfaced = knownAfter.Except(knownBefore);
        Assert.All(newlySurfaced, id => Assert.DoesNotContain(id, retiredIds));
    }

    [Fact]
    public void LedgerReconstructedFromCaseFile_MatchesPersistedGangTakeIns()
    {
        // Simulate a snapshot load: build a case file that already records a gang
        // turn-in settlement, then construct a session. The ledger must reconstruct
        // gang-side parity from the persisted settlements (BUNCH-107 snapshot path).
        var session = CreateSession(gangSuspectCount: 2, unrelatedWarrantCount: 6);
        // No turn-ins yet -> parity at full gang count.
        Assert.Equal(2, session.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.Equal(2, session.UnrelatedCriminalLedger.ActiveCriminalCount);

        // Settle one gang turn-in.
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);

        var caseFileAfterTurnIn = session.CaseFile;
        Assert.Single(caseFileAfterTurnIn.SheriffTurnInSettlements);

        // Reconstruct a fresh session from the same (persisted) case file, as a
        // snapshot load would. The ledger must reflect the one persisted gang take-in.
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, pinecross.Id, TrailRisk.Low) });

        var reloaded = GameSession.StartNew("Ranger Vale", world, caseFileAfterTurnIn, pinecross.Id);

        Assert.Equal(1, reloaded.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.Equal(1, reloaded.UnrelatedCriminalLedger.ActiveCriminalCount);
    }

    private static GameSession CreateSession(int gangSuspectCount, int unrelatedWarrantCount)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, pinecross.Id, TrailRisk.Low) });

        var suspects = Enumerable.Range(1, gangSuspectCount)
            .Select(i => new Suspect(
                new SuspectId($"suspect-{i}"),
                $"Gang Member {i}",
                SuspectTraits.Empty,
                SuspectStatus.AtLarge))
            .ToArray();

        var knownWarrants = Enumerable.Range(1, gangSuspectCount)
            .Select(i => new Warrant(
                new WarrantId($"warrant-gang-{i}"),
                $"Gang Member {i}",
                new WarrantTerms(
                    WarrantDisposition.DeadOrAlive,
                    500m,
                    new[] { $"Alias {i}" },
                    new[] { $"Feature {i}" },
                    "Dodge City Marshal",
                    i == 1 ? InvestigationTargetKind.TrueCulprit : InvestigationTargetKind.GangMember,
                    Array.Empty<OutlawGangId>(),
                    null),
                "Wanted for gang crimes."))
            .ToArray();

        var publicWarrants = Enumerable.Range(1, unrelatedWarrantCount)
            .Select(i => new Warrant(
                new WarrantId($"warrant-unrelated-{i}"),
                $"Outlaw {i}",
                new WarrantTerms(
                    WarrantDisposition.AliveOnly,
                    100m,
                    new[] { $"Outlaw Alias {i}" },
                    new[] { $"Outlaw Feature {i}" },
                    $"Sheriff {i}",
                    InvestigationTargetKind.UnrelatedWantedCriminal,
                    Array.Empty<OutlawGangId>(),
                    null),
                "Wanted for unrelated territorial offenses."))
            .ToArray();

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: knownWarrants,
            publicWarrants: publicWarrants);

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
