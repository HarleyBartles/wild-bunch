using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionInvestigationActionsTests
{
    [Fact]
    public void InspectNoticeBoardRevealsSourceTaggedWarrantAndIsIdempotent()
    {
        var session = CreateSession();

        var first = session.InspectNoticeBoard();
        var second = session.InspectNoticeBoard();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, session.Clock.Turn);
        Assert.Equal(3, session.LogEntries.Count);
        Assert.Single(session.CaseFile.KnownWarrants);
        Assert.Empty(session.CaseFile.PublicWarrants);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void CheckSheriffRecordsRevealsSourceTaggedClueAndAdvancesProgressOnce()
    {
        var session = CreateSession();

        var first = session.CheckSheriffRecords();
        var second = session.CheckSheriffRecords();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, session.Clock.Turn);
        Assert.Equal(3, session.LogEntries.Count);
        Assert.Single(session.CaseFile.KnownClues);
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Equal(1, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void FollowTelegraphLeadsRevealsTelegraphTaggedClueAndIsIdempotent()
    {
        var session = CreateExpandedSession();

        var first = session.FollowTelegraphLeads();
        var second = session.FollowTelegraphLeads();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, session.Clock.Turn);
        Assert.Equal(3, session.LogEntries.Count);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.SourceKind == InvestigationSourceKind.TelegraphLead);
        Assert.Single(session.CaseFile.PublicClues, clue => clue.SourceKind == InvestigationSourceKind.LocalGossip);
        Assert.Equal(1, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void GatherLocalGossipRevealsGossipTaggedClueAndIsIdempotent()
    {
        var session = CreateExpandedSession();

        var first = session.GatherLocalGossip();
        var second = session.GatherLocalGossip();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, session.Clock.Turn);
        Assert.Equal(3, session.LogEntries.Count);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.SourceKind == InvestigationSourceKind.LocalGossip);
        Assert.Single(session.CaseFile.PublicClues, clue => clue.SourceKind == InvestigationSourceKind.TelegraphLead);
        Assert.Equal(1, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void InvestigationActionsFailWhileJourneyAwaitingAcknowledgement()
    {
        var session = CreateSession();
        StartJourney(session);
        session.Journey!.MarkCompleted();

        var noticeBoard = session.InspectNoticeBoard();
        var sheriffRecords = session.CheckSheriffRecords();

        Assert.False(noticeBoard.Success);
        Assert.False(sheriffRecords.Success);
        Assert.Equal("Finish the current journey before taking that action.", noticeBoard.Message);
        Assert.Equal("Finish the current journey before taking that action.", sheriffRecords.Message);
        Assert.Empty(session.CaseFile.KnownClues);
        Assert.Empty(session.CaseFile.KnownWarrants);
        Assert.Equal(2, session.LogEntries.Count);
    }

    [Fact]
    public void TelegraphLeadsResetAfterLeavingAndReturningToTown()
    {
        var session = CreateRefreshableSession();

        var first = session.FollowTelegraphLeads();
        var repeatSameVisit = session.FollowTelegraphLeads();

        Assert.True(first.Success);
        Assert.True(repeatSameVisit.Success);
        Assert.Equal("You ask after telegraph leads again, but no new wire has come in.", repeatSameVisit.Message);
        Assert.True(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead));
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-telegraph-1");
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Id.Value == "clue-public-telegraph-2");
        Assert.Equal(1, session.CaseFile.KillerReleaseProgress);

        TravelToTown(session, new TownId("connected"));
        TravelToTown(session, new TownId("current"));

        Assert.Equal(new TownId("current"), session.CurrentTownVisit.TownId);
        Assert.False(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead));

        var afterReturn = session.FollowTelegraphLeads();

        Assert.True(afterReturn.Success);
        Assert.Equal("You follow the telegraph leads and uncover a public lead.", afterReturn.Message);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-telegraph-1");
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-telegraph-2");
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Equal(2, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void WantedPostersStayClickableButOnlyRevealOncePerVisit()
    {
        var session = CreateWantedPosterRefreshableSession();

        var first = session.ReadWantedPosters();
        var repeatSameVisit = session.ReadWantedPosters();

        Assert.True(first.Success);
        Assert.True(repeatSameVisit.Success);
        Assert.Equal("You study the wanted posters again, but find nothing new.", repeatSameVisit.Message);
        Assert.True(session.CurrentTownVisit.WantedPostersSpent);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-notice-1");
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Id.Value == "clue-public-record-2");
        Assert.Equal(1, session.CaseFile.KillerReleaseProgress);

        TravelToTown(session, new TownId("connected"));
        TravelToTown(session, new TownId("current"));

        Assert.Equal(new TownId("current"), session.CurrentTownVisit.TownId);
        Assert.False(session.CurrentTownVisit.WantedPostersSpent);

        var afterReturn = session.ReadWantedPosters();

        Assert.True(afterReturn.Success);
        Assert.Equal("You study the wanted posters and uncover a public lead.", afterReturn.Message);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-notice-1");
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-record-2");
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Equal(2, session.CaseFile.KillerReleaseProgress);
    }

    private static GameSession CreateSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[]
            {
                new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-1"),
                    ClueKind.Alias,
                    "A sheriff note ties the rider to a rail ledger.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.SheriffRecords,
                    source: "sheriff record",
                    context: "Public notice")
            },
            publicWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-public-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Red Wren", "Aunt Tess" },
                        new[] { "Pale scar across the left cheek" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        [OutlawGangIds.WildBunch],
                        OutlawGangIds.WildBunch,
                        InvestigationSourceKind.NoticeBoard),
                    "Wanted for a Wild Bunch robbery.")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }

    private static GameSession CreateExpandedSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard | TownServices.Telegraph);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[]
            {
                new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-telegraph"),
                    ClueKind.IdentityFact,
                    "A telegraph clerk filed a name in shorthand.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.TelegraphLead,
                    source: "telegraph clerk",
                    context: "Telegraph lead"),
                new Clue(
                    new ClueId("clue-public-gossip"),
                    ClueKind.Whereabouts,
                    "Local gossip says the rider kept to the rail spur after dark.",
                    new[] { new SuspectId("suspect-2") },
                    InvestigationTargetKind.GangMember,
                    InvestigationSourceKind.LocalGossip,
                    source: "saloon talk",
                    context: "Town gossip")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }

    private static GameSession CreateRefreshableSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.Telegraph | TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[]
            {
                new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low)
            });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-telegraph-1"),
                    ClueKind.IdentityFact,
                    "A telegraph clerk filed a name in shorthand.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.TelegraphLead,
                    source: "telegraph clerk",
                    context: "Telegraph lead"),
                new Clue(
                    new ClueId("clue-public-telegraph-2"),
                    ClueKind.Whereabouts,
                    "A rail clerk mentions a rider cutting south at dusk.",
                    new[] { new SuspectId("suspect-2") },
                    InvestigationTargetKind.GangMember,
                    InvestigationSourceKind.TelegraphLead,
                    source: "telegraph clerk",
                    context: "Telegraph lead")
            });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            currentTown.Id,
            Wallet.Starting(25m),
            inventory,
            TravelDifficulty.Easy);
    }

    private static GameSession CreateWantedPosterRefreshableSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[]
            {
                new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low)
            });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-notice-1"),
                    ClueKind.Alias,
                    "A poster mentions a rider with a pale scar.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.NoticeBoard,
                    source: "notice board",
                    context: "Public wanted poster"),
                new Clue(
                    new ClueId("clue-public-record-2"),
                    ClueKind.Record,
                    "A sheriff note ties the rider to a rail ledger.",
                    new[] { new SuspectId("suspect-2") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.SheriffRecords,
                    source: "sheriff record",
                    context: "Public wanted poster")
            });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            currentTown.Id,
            Wallet.Starting(25m),
            inventory,
            TravelDifficulty.Easy);
    }

    private static void TravelToTown(GameSession session, TownId destinationTownId)
    {
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId,
                destinationTownId,
                session.Player.Inventory)
            .Preview!;

        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        var acknowledgment = session.AcknowledgeJourneyArrival();

        Assert.True(acknowledgment.Success);
    }

    private static void StartJourney(GameSession session)
    {
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId,
                new TownId("connected"),
                session.Player.Inventory)
            .Preview!;

        session.StartJourney(preview);
    }
}
