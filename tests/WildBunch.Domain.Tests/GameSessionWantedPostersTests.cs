using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.WantedPosters;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionWantedPostersTests
{
    [Fact]
    public void ReadingWantedPostersInSupportedTownAddsPublicClueAndLogEntry()
    {
        var session = CreateSession();

        var result = session.ReadWantedPosters();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(2, GameSessionLogProjection.Project(session).Count);
        // The WantedPosterResolver gates the true-culprit warrant behind the killer-release gate.
        Assert.Single(session.CaseFile.KnownWarrants);
        Assert.Equal("Ira Flint", session.CaseFile.KnownWarrants[0].TargetName);
        Assert.Single(session.CaseFile.KnownClues);
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Single(session.CaseFile.PublicWarrants);
        Assert.Single(session.CaseFile.DiscoveredSuspectIds);
        Assert.Contains(new SuspectId("suspect-1"), session.CaseFile.DiscoveredSuspectIds);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.TrueCulpritId);
    }

    [Fact]
    public void ReadingWantedPostersTwiceDoesNotDuplicateTheSameClue()
    {
        var session = CreateSession();

        var first = session.ReadWantedPosters();
        var second = session.ReadWantedPosters();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, session.Clock.Turn); // BUNCH-80: only first ReadWantedPosters advances turn (same context)
        Assert.Equal(3, GameSessionLogProjection.Project(session).Count);
        Assert.Single(session.CaseFile.KnownWarrants);
        Assert.Single(session.CaseFile.KnownClues);
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Single(session.CaseFile.PublicWarrants);
        Assert.Single(session.CaseFile.DiscoveredSuspectIds);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void ReadingWantedPostersSkipsTelegraphAndGossipClues()
    {
        var session = CreateSession(includeSourceSpecificClues: true);

        var first = session.ReadWantedPosters();

        Assert.True(first.Success);
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(2, GameSessionLogProjection.Project(session).Count);
        Assert.Single(session.CaseFile.KnownWarrants);
        Assert.Single(session.CaseFile.KnownClues, clue => clue.SourceKind == InvestigationSourceKind.SheriffWarrants);
        Assert.Equal(2, session.CaseFile.PublicClues.Count);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.SourceKind == InvestigationSourceKind.LocalGossip);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.SourceKind == InvestigationSourceKind.LocalGossip);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
    }

    [Fact]
    public void ReadingWantedPostersInTownWithoutNoticeBoardStillSucceeds()
    {
        // Every town has a sheriff's office. ReadWantedPosters is always available
        // and reveals its warrants and clues.
        var session = CreateSession();

        var result = session.ReadWantedPosters();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // The WantedPosterResolver gates the true-culprit warrant, so the gang-member warrant surfaces first.
        Assert.Single(session.CaseFile.KnownWarrants);
        Assert.Equal("Ira Flint", session.CaseFile.KnownWarrants[0].TargetName);
        Assert.Single(session.CaseFile.KnownClues);
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Single(session.CaseFile.PublicWarrants);
        Assert.Single(session.CaseFile.DiscoveredSuspectIds);
        Assert.Contains(new SuspectId("suspect-1"), session.CaseFile.DiscoveredSuspectIds);
    }

    [Fact]
    public void ReadingWantedPostersWhileJourneyAwaitingAcknowledgementFailsWithoutMutation()
    {
        var session = CreateSession();
        StartJourney(session);
        session.Journey!.MarkCompleted();

        var result = session.ReadWantedPosters();

        Assert.False(result.Success);
        Assert.False(result.SessionChanged);
        Assert.Equal("Finish the current journey before taking that action.", result.Message);
        Assert.Empty(session.CaseFile.KnownClues);
        Assert.Single(session.CaseFile.PublicClues);
        Assert.Empty(session.CaseFile.KnownWarrants);
        Assert.Equal(2, session.CaseFile.PublicWarrants.Count);
        Assert.Equal(0, session.CaseFile.KillerReleaseProgress);
        Assert.Equal(2, GameSessionLogProjection.Project(session).Count);
        Assert.Equal(JourneyStatus.Completed, session.Journey.Status);
    }

    private static GameSession CreateSession(bool includeSourceSpecificClues = false)
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
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
                    "A posted notice describes a rider wearing a faded blue scarf.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.GangMember,
                    InvestigationSourceKind.SheriffWarrants,
                    source: "notice board",
                    context: "Public wanted poster")
            }.Concat(includeSourceSpecificClues
                ? new[]
                {
                    new Clue(
                        new ClueId("clue-public-telegraph"),
                        ClueKind.IdentityFact,
                        "A telegraph clerk filed a name in shorthand.",
                        new[] { new SuspectId("suspect-2") },
                        InvestigationTargetKind.Suspected,
                        InvestigationSourceKind.LocalGossip,
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
                }
                : Array.Empty<Clue>()).ToArray(),
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
                    "Wanted for a Wild Bunch robbery."),
                new Warrant(
                    new WarrantId("warrant-public-2"),
                    "Ira Flint",
                    new WarrantTerms(
                        WarrantDisposition.AliveOnly,
                        300m,
                        new[] { "The Magpie", "R. Pike" },
                        new[] { "Mismatched spurs" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.GangMember,
                        [OutlawGangIds.WildBunch],
                        null,
                        InvestigationSourceKind.SheriffWarrants),
                    "Wanted as a member of the Wild Bunch.")
            });

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, currentTown.Id, gameDifficulty: GameDifficulty.Standard);
    }

    private static void StartJourney(GameSession session)
    {
        var travelResolver = new TravelResolver();
        var destinationTownId = new TownId("connected");
        var preview = travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId!.Value,
                destinationTownId,
                session.Player.Inventory)
            .Preview!;

        session.StartJourney(preview);
    }
}
