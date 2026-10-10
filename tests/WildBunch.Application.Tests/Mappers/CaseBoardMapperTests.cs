using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests.Mappers;

public sealed class CaseBoardMapperTests
{
    [Fact]
    public void EqualNameWarrantsRemainDistinctAndSettlementAppliesOnlyToExplicitIdentity()
    {
        var board = CaseBoardMapper.ToDto(
            Array.Empty<Clue>(),
            new[]
            {
                CreateWarrant("warrant-one", "Mira Cline", "suspect-1"),
                CreateWarrant("warrant-two", "Mira Cline", "suspect-2")
            },
            new[] { CreateSettlement("suspect-1", "Mira Cline") });

        Assert.Equal(2, board.Warrants.Count);
        var settled = Assert.Single(board.Warrants, warrant => warrant.Id == "warrant-one");
        Assert.Equal("Mira Cline", settled.TargetName);
        Assert.NotNull(settled.Settlement);
        Assert.True(settled.Settlement!.IsAlive);
        Assert.Equal(250m, settled.Settlement.BountyAmount);
        Assert.Equal(3, settled.Settlement.Day);
        Assert.Equal(1, settled.Settlement.Turn);

        var otherIdentity = Assert.Single(board.Warrants, warrant => warrant.Id == "warrant-two");
        Assert.Equal("Mira Cline", otherIdentity.TargetName);
        Assert.Null(otherIdentity.Settlement);
    }

    [Fact]
    public void UnnamedFeatureObservationAndWantedRecordRemainSeparatePlayerKnownFacts()
    {
        var clue = new Clue(
            new ClueId("clue-unnamed-rider"),
            ClueKind.Whereabouts,
            "A man with a red neckerchief was seen leaving Bulletville headed east.",
            Array.Empty<SuspectId>(),
            InvestigationTargetKind.Suspected,
            InvestigationSourceKind.LocalGossip,
            source: "saloon keeper",
            context: "Seen two days ago",
            anchors: new ClueAnchors(
                subjects: new[] { new ClueSubjectAnchor("a man with a red neckerchief", Feature: "red neckerchief") },
                locations: new[] { new ClueLocationAnchor("Bulletville", TownId: null, Place: "Bulletville") },
                times: new[] { new ClueTimeAnchor(ClueRecency.Old) }));
        var warrant = CreateWarrant("warrant-elzy", "Elzy Lay", "suspect-1", "red neckerchief");

        var board = CaseBoardMapper.ToDto(new[] { clue }, new[] { warrant });

        var wantedRecord = Assert.Single(board.Warrants);
        Assert.Equal("Elzy Lay", wantedRecord.TargetName);
        Assert.Equal("warrant-elzy", wantedRecord.Id);
        Assert.Equal(new[] { "red neckerchief" }, wantedRecord.KnownFeatures);

        var observation = Assert.Single(board.Clues);
        Assert.Equal("clue-unnamed-rider", observation.Id);
        Assert.Equal(ClueKind.Whereabouts, observation.Kind);
        Assert.Equal("A man with a red neckerchief was seen leaving Bulletville headed east.", observation.Description);
        Assert.Equal(InvestigationSourceKind.LocalGossip, observation.SourceKind);
        Assert.Equal("saloon keeper", observation.Source);
        Assert.Equal("Seen two days ago", observation.Context);
        Assert.Equal("red neckerchief", Assert.Single(observation.Anchors.Subjects).Feature);
        Assert.Equal("Bulletville", Assert.Single(observation.Anchors.Locations).Place);
        Assert.Equal(ClueRecency.Old, Assert.Single(observation.Anchors.Times).Recency);
    }

    [Fact]
    public void CapturingWantedPersonKeepsPreviouslyLearnedCluesAndWarrantDetails()
    {
        var session = CreateArmedWantedSessionWithIdentityEvidence();
        var capturedSuspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(capturedSuspectId, WantedSuspectPresenceState.AvailableInTown);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(capturedSuspectId));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();
        var turnIn = session.ConfrontSaloonPersonOfInterest("warrant-mira");
        var caseFile = GameSessionMapper.ToDto(session).CaseFile;

        Assert.True(lookAround.Success);
        Assert.True(turnIn.Success);
        Assert.Single(session.CaseFile.SheriffTurnInSettlements);

        var capturedWarrant = Assert.Single(caseFile.CaseBoard.Warrants, warrant => warrant.Id == "warrant-mira");
        Assert.Equal("Mira Cline", capturedWarrant.TargetName);
        Assert.Equal("Wanted for a stage robbery.", capturedWarrant.Summary);
        Assert.Equal(new[] { "Red Wren" }, capturedWarrant.KnownAliases);
        Assert.Equal(new[] { "Raven-feather pin" }, capturedWarrant.KnownFeatures);
        Assert.NotNull(capturedWarrant.Settlement);
        Assert.True(capturedWarrant.Settlement!.IsAlive);
        Assert.Equal(capturedWarrant.BountyAmount, capturedWarrant.Settlement.BountyAmount);

        var remainingWarrant = Assert.Single(caseFile.CaseBoard.Warrants, warrant => warrant.Id == "warrant-reno");
        Assert.Null(remainingWarrant.Settlement);

        var aliasClue = Assert.Single(caseFile.CaseBoard.Clues, clue => clue.Id == "clue-mira-alias");
        Assert.Equal("wanted poster", aliasClue.Source);
        Assert.Contains("Red Wren", aliasClue.Description);
        var featureClue = Assert.Single(caseFile.CaseBoard.Clues, clue => clue.Id == "clue-mira-feature");
        Assert.Equal("saloon talk", featureClue.Source);
        Assert.Contains("Raven-feather pin", featureClue.Description);
        Assert.Contains(caseFile.CaseBoard.Clues, clue => clue.Id == "clue-reno-feature");
    }

    [Fact]
    public void LegacyWarrantWithoutIdentityIsNotSettledByMatchingDisplayName()
    {
        var warrant = CreateWarrant("legacy-warrant", "Mira Cline", targetSuspectId: null);

        var board = CaseBoardMapper.ToDto(
            Array.Empty<Clue>(),
            new[] { warrant },
            new[] { CreateSettlement("suspect-1", "Mira Cline") });

        Assert.Null(Assert.Single(board.Warrants).Settlement);
    }

    private static Warrant CreateWarrant(
        string warrantId,
        string targetName,
        string? targetSuspectId,
        string feature = "Raven-feather pin")
        => new(
            new WarrantId(warrantId),
            targetName,
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                250m,
                new[] { "Red Wren" },
                new[] { feature },
                "Dodge City Marshal",
                InvestigationTargetKind.GangMember,
                Array.Empty<OutlawGangId>(),
                null),
            $"Wanted for a stage robbery.",
            targetSuspectId is null ? null : new SuspectId(targetSuspectId));

    private static SheriffTurnInSettlementState CreateSettlement(string suspectId, string targetName)
        => new(
            new SuspectId(suspectId),
            targetName,
            WarrantDisposition.DeadOrAlive,
            IsAlive: true,
            BountyAmount: 250m,
            Day: 3,
            Turn: 1);

    private static GameSession CreateArmedWantedSessionWithIdentityEvidence()
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var knownClues = new[]
        {
            new Clue(
                new ClueId("clue-mira-alias"),
                ClueKind.Alias,
                "A wanted poster links Red Wren to a rider in the marshal files.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.SheriffWarrants,
                source: "wanted poster",
                context: "Public notice",
                anchors: new ClueAnchors(subjects: new[] { new ClueSubjectAnchor("Red Wren", Alias: "Red Wren") })),
            new Clue(
                new ClueId("clue-mira-feature"),
                ClueKind.IdentityFact,
                "Saloon gossip says the wanted rider wears a Raven-feather pin.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalGossip,
                source: "saloon talk",
                context: "Identity rumor",
                anchors: new ClueAnchors(subjects: new[] { new ClueSubjectAnchor("Raven-feather pin", Feature: "Raven-feather pin") })),
            new Clue(
                new ClueId("clue-reno-feature"),
                ClueKind.IdentityFact,
                "A deputy remembers a wanted rider with mismatched spurs.",
                new[] { new SuspectId("suspect-2") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalRecords,
                source: "sheriff record",
                context: "Open warrant",
                anchors: new ClueAnchors(subjects: new[] { new ClueSubjectAnchor("Mismatched spurs", Feature: "Mismatched spurs") }))
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: knownClues,
            knownWarrants: new[]
            {
                CreateWarrant("warrant-mira", "Mira Cline", "suspect-1", "Raven-feather pin"),
                CreateWarrant("warrant-reno", "Reno Pike", "suspect-2", "Mismatched spurs")
            });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Revolver, 1),
            new InventoryItem(ItemKind.RevolverAmmo, 2)
        });

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(currentTown.Id);
        session.CompleteGameStart(wallet: null, inventory: inventory);
        return session;
    }
}
