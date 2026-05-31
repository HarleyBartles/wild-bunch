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

namespace WildBunch.Application.Tests;

public sealed class CheckSheriffRecordsHandlerTests
{
    [Fact]
    public async Task CheckSheriffRecordsLoadsSessionSavesSuccessfulMutationAndReturnsExpectedResult()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.NoticeBoard);
        repository.Seed(session);
        var handler = new CheckSheriffRecordsHandler(repository, new JournalResolver());

        var result = await handler.HandleAsync(new CheckSheriffRecordsCommand(session.Id.Value));

        Assert.True(result.Success);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(1, result.CurrentJournal.Clock.Turn);
        Assert.Equal(2, result.CurrentJournal.LogEntries.Count);
        Assert.Single(result.CurrentJournal.CaseFile.KnownClues);
        Assert.Equal(1, result.CurrentJournal.CaseFile.KillerReleaseState.Progress);
        var payload = JsonSerializer.Serialize(result);
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckSheriffRecordsReturnsFailureWithoutSavingWhenActionUnavailable()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.None);
        repository.Seed(session);
        var handler = new CheckSheriffRecordsHandler(repository, new JournalResolver());

        var result = await handler.HandleAsync(new CheckSheriffRecordsCommand(session.Id.Value));

        Assert.False(result.Success);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Empty(result.CurrentJournal.CaseFile.KnownClues);
        Assert.Empty(result.CurrentJournal.CaseFile.DiscoveredSuspects);
        Assert.Equal(0, result.CurrentJournal.CaseFile.KillerReleaseState.Progress);
    }

    [Fact]
    public async Task CheckSheriffRecordsWhileJourneyAwaitingAcknowledgementReturnsFailureWithoutSaving()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession(TownServices.NoticeBoard);
        StartJourney(session);
        session.Journey!.MarkCompleted();
        repository.Seed(session);
        var handler = new CheckSheriffRecordsHandler(repository, new JournalResolver());

        var result = await handler.HandleAsync(new CheckSheriffRecordsCommand(session.Id.Value));

        Assert.False(result.Success);
        Assert.Equal("Finish the current journey before taking that action.", result.Message);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Empty(result.CurrentJournal.CaseFile.KnownClues);
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
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-1"),
                    ClueKind.Record,
                    "A sheriff note ties the rider to a rail ledger.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.SheriffRecords,
                    source: "sheriff record",
                    context: "Public notice")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
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
