using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;

namespace WildBunch.Domain.Tests;

public sealed class KillerReleaseProgressionTests
{
    [Fact]
    public void FiveDistinctOtherGangSettlementsReleaseCulpritAfterFourRemainLocked()
    {
        var session = CreateRoundupSession();

        for (var number = 1; number <= 4; number++)
        {
            BringGangMemberIn(session, number);
        }

        Assert.Equal(4, session.CaseFile.KillerReleaseState.Progress);
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal("The killer trail is locked until 1 more gang member is brought to justice.", session.CaseFile.KillerReleaseState.StatusText);
        Assert.Throws<InvalidOperationException>(() =>
            session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-7"))));

        BringGangMemberIn(session, 5);

        Assert.Equal(5, session.CaseFile.KillerReleaseState.Progress);
        Assert.True(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal("The killer trail is released.", session.CaseFile.KillerReleaseState.StatusText);
        Assert.Equal(5, session.AllEvents.OfType<SheriffTurnInSettled>().Count());

        var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, session.AllEvents);
        Assert.Equal(5, replayed.CaseFile.KillerReleaseState.Progress);
        Assert.True(replayed.CaseFile.KillerReleaseState.IsReleased);
    }

    [Fact]
    public void SettlementQualificationExcludesTheCulpritAndNonGangWarrants()
    {
        var culpritId = new SuspectId("suspect-1");
        var gangId = new SuspectId("suspect-2");
        var citizenId = new SuspectId("suspect-3");
        var suspects = new[]
        {
            new Suspect(culpritId, "Culprit", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(gangId, "Gang Member", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(citizenId, "Citizen", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };
        var warrants = new[]
        {
            CreateWarrant("culprit-warrant", culpritId, InvestigationTargetKind.TrueCulprit, OutlawGangIds.WildBunch),
            CreateWarrant("gang-warrant", gangId, InvestigationTargetKind.GangMember, OutlawGangIds.WildBunch),
            CreateWarrant("citizen-warrant", citizenId, InvestigationTargetKind.Suspected, OutlawGangIds.WildBunch)
        };
        var settlements = new[]
        {
            CreateSettlement(culpritId, "Culprit", isAlive: false),
            CreateSettlement(gangId, "Gang Member", isAlive: false),
            CreateSettlement(citizenId, "Citizen", isAlive: true)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            culpritId,
            CaseOpeningLead.Create("Find the culprit."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: warrants,
            sheriffTurnInSettlements: settlements);

        Assert.Equal(1, caseFile.KillerReleaseState.Progress);
        Assert.False(caseFile.KillerReleaseState.IsReleased);
    }

    private static void BringGangMemberIn(GameSession session, int number)
    {
        var suspectId = new SuspectId($"suspect-{number}");
        var warrant = session.CaseFile.KnownWarrants.Single(candidate => candidate.TargetSuspectId == suspectId);

        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var observed = session.LookAroundSaloon();
        Assert.True(observed.Success);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var result = session.ConfrontSaloonPersonOfInterest(warrant.Id.Value);

        Assert.True(result.Success, result.Message);
        Assert.Contains(session.AllEvents.OfType<SheriffTurnInSettled>(), settlement => settlement.TargetSuspectId == suspectId);

        if (number == 1)
        {
            var duplicate = session.SettleSheriffTurnIn(suspectId, isAlive: true);
            Assert.False(duplicate.Success);
            Assert.Equal(1, session.CaseFile.KillerReleaseState.Progress);
            Assert.Single(session.AllEvents.OfType<SheriffTurnInSettled>());
        }
    }

    private static GameSession CreateRoundupSession()
    {
        var town = new Town(new TownId("roundup-town"), "Roundup Town");
        var connected = new Town(new TownId("connected-town"), "Connected Town");
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("roundup-trail"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = Enumerable.Range(1, 7)
            .Select(number => new Suspect(
                new SuspectId($"suspect-{number}"),
                $"Gang Member {number}",
                SuspectTraits.Empty,
                SuspectStatus.AtLarge))
            .ToArray();
        var warrants = suspects.Select((suspect, index) =>
        {
            var isCulprit = index == 6;
            var targetKind = isCulprit ? InvestigationTargetKind.TrueCulprit : InvestigationTargetKind.GangMember;
            return new Warrant(
                new WarrantId($"warrant-{index + 1}"),
                suspect.Name,
                new WarrantTerms(
                    WarrantDisposition.DeadOrAlive,
                    10m,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    "Roundup Sheriff",
                    targetKind,
                    new[] { OutlawGangIds.WildBunch },
                    isCulprit ? null : OutlawGangIds.WildBunch,
                    InvestigationSourceKind.SheriffWarrants),
                "Wanted for Wild Bunch crimes.",
                suspect.Id);
        }).ToArray();
        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: suspects[6].Id,
            openingLead: CaseOpeningLead.Create("A Wild Bunch member bears a dark scar."),
            knownClues: Array.Empty<Clue>(),
            killerReleaseThreshold: 5,
            knownWarrants: warrants);
        var inventory = new DomainInventory(new[] { new InventoryItem(ItemKind.Revolver, 1) });

        return TestSessionFactory.StartGameCanonical(
            "Ranger Vale",
            world,
            caseFile,
            town.Id,
            Wallet.Starting(25m),
            inventory,
            GameDifficulty.Standard);
    }

    private static Warrant CreateWarrant(
        string warrantId,
        SuspectId suspectId,
        InvestigationTargetKind targetKind,
        OutlawGangId? advancesGangPressureFor)
        => new(
            new WarrantId(warrantId),
            suspectId.Value,
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                100m,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "Sheriff",
                targetKind,
                advancesGangPressureFor is null ? Array.Empty<OutlawGangId>() : new[] { advancesGangPressureFor.Value },
                advancesGangPressureFor,
                InvestigationSourceKind.SheriffWarrants),
            "Test warrant.",
            suspectId);

    private static SheriffTurnInSettlementState CreateSettlement(SuspectId suspectId, string targetName, bool isAlive)
        => new(suspectId, targetName, WarrantDisposition.DeadOrAlive, isAlive, 100m, 1, 1);
}
