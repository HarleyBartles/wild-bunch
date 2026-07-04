using System.Text.Json;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.Travel;
using WildBunch.Domain.WantedPosters;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class ReadWantedPostersHandlerTests
{
    [Fact]
    public async Task ReadWantedPostersLoadsSessionSavesSuccessfulMutationAndReturnsExpectedResult()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.None);
        repository.Seed(session);
        var handler = new ReadWantedPostersHandler(repository, repository, new JournalResolver());

        var result = await handler.HandleAsync(new ReadWantedPostersCommand(session.Id.Value));

        Assert.True(result.Success);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(1, result.CurrentJournal.Clock.Turn);
        Assert.Equal(2, result.CurrentJournal.LogEntries.Count);
        Assert.Single(result.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Single(result.CurrentJournal.CaseFile.KnownClues);
        Assert.Single(result.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Single(result.WantedPosters);
        Assert.Equal("warrant-public-1", result.WantedPosters[0].PosterId);
        Assert.Equal("Mira Cline", result.WantedPosters[0].TargetDisplayName);
        Assert.Equal("Raven-feather pin", result.WantedPosters[0].QuickView.HeadlineFeatureOrDescriptor);
        Assert.Equal(2, result.WantedPosters[0].Details.Features.Count);
        Assert.Equal(WantedPosterFeatureRenderMode.TextOnly, result.WantedPosters[0].Details.Features[0].RenderMode);
        Assert.Equal(WantedPosterFeatureRenderMode.PortraitRenderable, result.WantedPosters[0].Details.Features[1].RenderMode);
        Assert.Equal("The Wild Bunch trail is quiet.", result.CurrentJournal.CaseFile.CaseState.StatusText);
        var payload = JsonSerializer.Serialize(result);
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("suspect-1", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"wantedPosters\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"targetKind\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadWantedPostersSucceedsEvenWithoutNoticeBoardService()
    {
        // Every town has a sheriff's office. ReadWantedPosters is always available,
        // even in a town with TownServices.None. The action should succeed and
        // reveal warrants/clues just like in a town with NoticeBoard.
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.None);
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new ReadWantedPostersHandler(repository, repository, new JournalResolver());

        var result = await handler.HandleAsync(new ReadWantedPostersCommand(session.Id.Value));

        Assert.True(result.Success);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Single(result.CurrentJournal.CaseFile.KnownClues);
        Assert.Single(result.CurrentJournal.CaseFile.DiscoveredSuspects, suspect => suspect.Id == "suspect-1");
        Assert.Single(result.WantedPosters);
        Assert.Equal("warrant-public-1", result.WantedPosters[0].PosterId);
        Assert.Equal("Mira Cline", result.WantedPosters[0].TargetDisplayName);
        var payload = JsonSerializer.Serialize(result);
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"wantedPosters\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"targetKind\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadWantedPostersWhileJourneyAwaitingAcknowledgementReturnsFailureWithoutSaving()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.None);
        StartJourney(session);
        session.Journey!.MarkCompleted();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new ReadWantedPostersHandler(repository, repository, new JournalResolver());

        var result = await handler.HandleAsync(new ReadWantedPostersCommand(session.Id.Value));

        Assert.False(result.Success);
        Assert.Equal("Finish the current journey before taking that action.", result.Message);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Empty(result.CurrentJournal.CaseFile.KnownClues);
        Assert.Empty(result.WantedPosters);
        Assert.Equal(2, result.CurrentJournal.LogEntries.Count);
    }

    [Fact]
    public async Task ReadWantedPostersThrowsWhenMissing()
    {
        var repository = new InMemoryGameSessionRepository();
        var handler = new ReadWantedPostersHandler(repository, repository, new JournalResolver());

        await Assert.ThrowsAsync<WildBunch.Application.Games.Exceptions.GameSessionNotFoundException>(
            () => handler.HandleAsync(new ReadWantedPostersCommand(Guid.NewGuid())));
    }

    private static GameSession CreateSession(TownServices currentTownServices)
    {
        var currentTown = new Town(new TownId("current"), "Current Town", currentTownServices);
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
                    "A posted notice links Grey Jay to a rider with a faded blue scarf.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.SheriffWarrants,
                    source: "notice board",
                    context: "Public wanted poster",
                    anchors: new ClueAnchors(
                        subjects: new[]
                        {
                            new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay")
                        }))
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
                        new[] { "Raven-feather pin", "Pale scar across the left cheek" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.GangMember,
                        [OutlawGangIds.WildBunch],
                        OutlawGangIds.WildBunch,
                        InvestigationSourceKind.SheriffWarrants),
                    "Wanted for a Wild Bunch robbery.")
            });

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(currentTown.Id);
        session.CompleteGameStart();
        return session;
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
