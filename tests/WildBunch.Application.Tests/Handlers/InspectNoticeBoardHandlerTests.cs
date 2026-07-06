using System.Text.Json;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests.Handlers;

public sealed class InspectNoticeBoardHandlerTests
{
    [Fact]
    public async Task InspectNoticeBoardLoadsSessionSavesSuccessfulMutationAndReturnsExpectedResult()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.None);
        repository.Seed(session);
        var handler = new InspectNoticeBoardHandler(repository, repository, new JournalResolver());

        var result = await handler.HandleAsync(new InspectNoticeBoardCommand(session.Id.Value));

        Assert.True(result.Success);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(1, result.CurrentJournal.Clock.Turn);
        Assert.Equal(2, result.CurrentJournal.LogEntries.Count);
        Assert.Empty(result.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Equal("The Wild Bunch trail is quiet.", result.CurrentJournal.CaseFile.CaseState.StatusText);
        var payload = JsonSerializer.Serialize(result);
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InspectNoticeBoardLoadsSessionSavesSuccessfulMutationEvenWithoutNoticeBoardService()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.None);
        repository.Seed(session);
        var handler = new InspectNoticeBoardHandler(repository, repository, new JournalResolver());

        var result = await handler.HandleAsync(new InspectNoticeBoardCommand(session.Id.Value));

        Assert.True(result.Success);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(1, result.CurrentJournal.Clock.Turn);
        Assert.Equal(2, result.CurrentJournal.LogEntries.Count);
        Assert.Empty(result.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Equal("The Wild Bunch trail is quiet.", result.CurrentJournal.CaseFile.CaseState.StatusText);
    }

    [Fact]
    public async Task InspectNoticeBoardWhileJourneyAwaitingAcknowledgementReturnsFailureWithoutSaving()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.None);
        StartJourney(session);
        session.Journey!.MarkCompleted();
        session.MarkEventsCommitted();
        repository.Seed(session);
        var handler = new InspectNoticeBoardHandler(repository, repository, new JournalResolver());

        var result = await handler.HandleAsync(new InspectNoticeBoardCommand(session.Id.Value));

        Assert.False(result.Success);
        Assert.Equal("Finish the current journey before taking that action.", result.Message);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Empty(result.CurrentJournal.CaseFile.KnownWarrants);
        Assert.Equal(2, result.CurrentJournal.LogEntries.Count);
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
                session.Player.CurrentTownId!.Value,
                new TownId("connected"),
                session.Player.Inventory)
            .Preview!;

        session.StartJourney(preview);
    }
}
