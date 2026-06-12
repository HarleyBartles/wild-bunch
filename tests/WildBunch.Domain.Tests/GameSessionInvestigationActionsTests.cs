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
    public void InspectNoticeBoardStaysOnCivicNoticesAndIsIdempotent()
    {
        var session = CreateSession();

        var first = session.InspectNoticeBoard();
        var second = session.InspectNoticeBoard();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, session.Clock.Turn);
        Assert.Equal(3, session.LogEntries.Count);
        Assert.Empty(session.CaseFile.KnownWarrants);
        Assert.Empty(session.CaseFile.KnownClues);
        Assert.Single(session.CaseFile.PublicWarrants);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void CheckSheriffRecordsRevealsLocalRecordsTaggedClueWithoutAdvancingProgress()
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
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
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
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
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
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void GatherLocalGossipSkipsColorOnlyObservationAndReturnsNothingUseful()
    {
        var session = CreateColorOnlyGossipSession();

        var result = session.GatherLocalGossip();

        Assert.True(result.Success);
        Assert.Equal("You ask around for local gossip, but hear nothing new.", result.Message);
        Assert.Empty(session.CaseFile.KnownClues);
        Assert.Single(session.CaseFile.PublicClues);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
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
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);

        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));

        Assert.Equal(new TownId("current"), session.CurrentTownVisit.TownId);
        Assert.False(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead));

        var afterReturn = session.FollowTelegraphLeads();

        Assert.True(afterReturn.Success);
        Assert.Equal("You follow the telegraph leads and uncover a public lead.", afterReturn.Message);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-telegraph-1");
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-telegraph-2");
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
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
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);

        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));

        Assert.Equal(new TownId("current"), session.CurrentTownVisit.TownId);
        Assert.False(session.CurrentTownVisit.WantedPostersSpent);

        var afterReturn = session.ReadWantedPosters();

        Assert.True(afterReturn.Success);
        Assert.Equal("You study the wanted posters, but find nothing new.", afterReturn.Message);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-notice-1");
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Id.Value == "clue-public-record-2");
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void LookAroundSaloonSurfacesOnlyAvailableWantedSuspectsOncePerVisit()
    {
        var session = CreateSaloonLookAroundSession();
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.AvailableInTown);
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-2"), WantedSuspectPresenceState.SecuredAlive);
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-3"), WantedSuspectPresenceState.GoneToGround);

        var first = session.LookAroundSaloon();
        var repeatSameVisit = session.LookAroundSaloon();

        Assert.True(first.Success);
        Assert.True(repeatSameVisit.Success);
        Assert.Equal("You look around the saloon and spot Ira Flint.", first.Message);
        Assert.Equal("You look around the saloon again, but nobody of interest is here.", repeatSameVisit.Message);
        Assert.True(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround));
        Assert.Equal(2, session.Clock.Turn);
        Assert.Equal(3, session.LogEntries.Count);

        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));

        Assert.False(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround));

        var afterReturn = session.LookAroundSaloon();

        Assert.True(afterReturn.Success);
        Assert.Equal("You look around the saloon and spot Ira Flint.", afterReturn.Message);
        Assert.Equal(3, session.Clock.Turn);
    }

    [Fact]
    public void LookAroundSaloonSuppressesUnavailableWantedSuspects()
    {
        var session = CreateSaloonLookAroundSession();
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.SecuredDead);
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-2"), WantedSuspectPresenceState.GoneToGround);
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-3"), WantedSuspectPresenceState.SecuredAlive);

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Equal("You look around the saloon, but nobody of interest is here.", result.Message);
        Assert.True(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.SaloonLookAround));
        Assert.Equal(WantedSuspectPresenceState.Unavailable, session.GetWantedSuspectPresenceState(new SuspectId("suspect-4")));
    }

    [Fact]
    public void NoticeBoardAndLocalRecordsRefreshWhenReturningToAVisitedTown()
    {
        var session = CreateTownSourceRefreshableSession();

        var firstNoticeBoard = session.InspectNoticeBoard();
        var firstLocalRecords = session.CheckSheriffRecords();
        var repeatNoticeBoard = session.InspectNoticeBoard();
        var repeatLocalRecords = session.CheckSheriffRecords();

        Assert.True(firstNoticeBoard.Success);
        Assert.True(firstLocalRecords.Success);
        Assert.Equal("You inspect the notice board again, but nothing new has been posted.", repeatNoticeBoard.Message);
        Assert.Equal("You check the local records again, but find nothing new.", repeatLocalRecords.Message);
        Assert.Empty(session.CaseFile.KnownWarrants);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-record-1");

        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));

        var afterReturnNoticeBoard = session.InspectNoticeBoard();
        var afterReturnLocalRecords = session.CheckSheriffRecords();

        Assert.True(afterReturnNoticeBoard.Success);
        Assert.True(afterReturnLocalRecords.Success);
        Assert.Equal("You inspect the notice board, but find nothing new.", afterReturnNoticeBoard.Message);
        Assert.Equal("You check the local records and uncover a public lead.", afterReturnLocalRecords.Message);
        Assert.Empty(session.CaseFile.KnownWarrants);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.Id.Value == "clue-public-record-2");
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

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

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
                    InvestigationSourceKind.LocalRecords,
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
                        InvestigationSourceKind.SheriffWarrants),
                    "Wanted for a Wild Bunch robbery.")
            });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            currentTown.Id,
            Wallet.Starting(25m),
            inventory,
            TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
    }

    private static GameSession CreateTownSourceRefreshableSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.NoticeBoard);
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

        var publicWarrants = new[]
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
                    InvestigationSourceKind.SheriffWarrants),
                "Wanted for a Wild Bunch robbery."),
            new Warrant(
                new WarrantId("warrant-public-2"),
                "Reno Pike",
                new WarrantTerms(
                    WarrantDisposition.AliveOnly,
                    300m,
                    new[] { "The Magpie", "R. Pike" },
                    new[] { "Mismatched spurs" },
                    "Silver Creek Sheriff",
                    InvestigationTargetKind.UnrelatedWantedCriminal,
                    Array.Empty<OutlawGangId>(),
                    null,
                    InvestigationSourceKind.SheriffWarrants),
                "Wanted for cattle theft.")
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
                    new ClueId("clue-public-record-1"),
                    ClueKind.Record,
                    "A sheriff note ties the rider to a rail ledger.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.LocalRecords,
                    source: "sheriff record",
                    context: "Public notice",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("red hat rider", Feature: "red hat")
                        })),
                new Clue(
                    new ClueId("clue-public-record-2"),
                    ClueKind.Record,
                    "A sheriff ledger in Holloway notes a rider with a red hat paying cash under a clean alias.",
                    new[] { new SuspectId("suspect-2") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.LocalRecords,
                    source: "sheriff record",
                    context: "Public notice",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("red hat rider", Feature: "red hat")
                        })),
            },
            publicWarrants: publicWarrants);

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
                    "A telegraph clerk filed Grey Jay in shorthand.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.TelegraphLead,
                    source: "telegraph clerk",
                    context: "Telegraph lead",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay")
                        })),
                new Clue(
                    new ClueId("clue-public-gossip"),
                    ClueKind.Whereabouts,
                    "Local gossip says the rider with the red hat kept to the rail spur after dark.",
                    new[] { new SuspectId("suspect-2") },
                    InvestigationTargetKind.GangMember,
                    InvestigationSourceKind.LocalGossip,
                    source: "saloon talk",
                    context: "Town gossip",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("red hat rider", Feature: "red hat")
                        })),
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
                    "A telegraph clerk filed Grey Jay in shorthand.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.TelegraphLead,
                    source: "telegraph clerk",
                    context: "Telegraph lead",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay")
                        })),
                new Clue(
                    new ClueId("clue-public-telegraph-2"),
                    ClueKind.Whereabouts,
                    "A rail clerk mentions a rider with a red hat cutting south at dusk.",
                    new[] { new SuspectId("suspect-2") },
                    InvestigationTargetKind.GangMember,
                    InvestigationSourceKind.TelegraphLead,
                    source: "telegraph clerk",
                    context: "Telegraph lead",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("red hat rider", Feature: "red hat")
                        })),
            });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            currentTown.Id,
            Wallet.Starting(25m),
            inventory,
            TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
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
                    "A poster links Grey Jay to a rider with a pale scar.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.SheriffWarrants,
                    source: "notice board",
                    context: "Public wanted poster",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay")
                        })),
                new Clue(
                    new ClueId("clue-public-record-2"),
                    ClueKind.Record,
                    "A sheriff note ties the rider to a rail ledger and notes a red hat.",
                    new[] { new SuspectId("suspect-2") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.LocalRecords,
                    source: "sheriff record",
                    context: "Public wanted poster",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("red hat rider", Feature: "red hat")
                        })),
            });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            currentTown.Id,
            Wallet.Starting(25m),
            inventory,
            TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
    }

    private static GameSession CreateSaloonLookAroundSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.Saloon);
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
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-3"), "Jonah Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>());

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }

    private static GameSession CreateColorOnlyGossipSession()
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
                    new ClueId("clue-public-color"),
                    ClueKind.Whereabouts,
                    "A rider turned north at dusk.",
                    Array.Empty<SuspectId>(),
                    InvestigationTargetKind.GangMember,
                    InvestigationSourceKind.LocalGossip,
                    source: "saloon talk",
                    context: "Town gossip",
                    anchors: new ClueAnchors(
                        locations: new[]
                        {
                            new ClueLocationAnchor("North road", Place: "North road", Route: "North road")
                        },
                        times: new[]
                        {
                            new ClueTimeAnchor(ClueRecency.Yesterday, Day: 2)
                        },
                        directions: new[]
                        {
                            new ClueDirectionAnchor("north", Movement: "turned north", Route: "North road")
                        })),
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
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
        while (session.Journey is not null && session.Journey.Status == JourneyStatus.Active)
        {
            session.AdvanceJourneyDay();
        }

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
