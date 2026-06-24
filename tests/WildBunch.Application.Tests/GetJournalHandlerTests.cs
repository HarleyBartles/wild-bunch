using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class GetJournalHandlerTests
{
    [Fact]
    public async Task GetJournalLoadsSessionAndReturnsExpectedData()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new GetJournalHandler(repository);

        var result = await handler.HandleAsync(new GetJournalQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.Id);
        Assert.Equal(session.Status, result.Status);
        Assert.Equal(session.Clock.Day, result.Clock.Day);
        Assert.Equal(session.Clock.Turn, result.Clock.Turn);
        Assert.Equal(session.Player.CurrentTownId.Value, result.CurrentTown.Id);
        Assert.Equal("Pinecross", result.CurrentTown.Name);
        Assert.Equal(session.CaseFile.OpeningLead.Description, result.CaseFile.OpeningLead);
        Assert.Equal("The Wild Bunch trail is quiet.", result.CaseFile.CaseState.StatusText);
        Assert.Equal("Find the culprit before the law closes in.", result.CaseFile.CaseSummary);
        Assert.Empty(result.CaseFile.DiscoveredSuspects);
        Assert.Equal(session.CaseFile.KnownClues.Count, result.CaseFile.KnownClues.Count);
        Assert.Single(result.CaseFile.WantedPosters);
        Assert.Equal("Butch Cassidy", result.CaseFile.WantedPosters[0].TargetDisplayName);
        Assert.Equal("County marshal", result.CaseFile.WantedPosters[0].LegalTerms.IssuingAuthority);
        Assert.Equal("Dead or alive, $2,500.00 bounty", result.CaseFile.WantedPosters[0].QuickView.PocketCheckDescriptor);
        Assert.Equal(session.LogEntries.Count, result.LogEntries.Count);
        Assert.Empty(session.CaseFile.PublicClues);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.TrueCulpritId);

        var payload = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("suspect-1", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("suspect-2", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspectCount\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"wantedPosters\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(typeof(WildBunch.Application.Games.Models.JournalCaseFileDto).GetProperties(), property => property.Name.Contains("culprit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetJournalProjectsOnlyExplicitlyDiscoveredSuspects()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.CaseFile.DiscoverSuspect(new SuspectId("suspect-2"));
        repository.Seed(session);
        var handler = new GetJournalHandler(repository);

        var result = await handler.HandleAsync(new GetJournalQuery(session.Id.Value));

        Assert.Single(result.CaseFile.DiscoveredSuspects);
        Assert.Equal("suspect-2", result.CaseFile.DiscoveredSuspects[0].Id);
        Assert.Equal("Mira Cline", result.CaseFile.DiscoveredSuspects[0].Name);
        Assert.Equal(SuspectStatus.AtLarge, result.CaseFile.DiscoveredSuspects[0].Status);
        Assert.Equal("The Wild Bunch trail is quiet.", result.CaseFile.CaseState.StatusText);

        var payload = System.Text.Json.JsonSerializer.Serialize(result);
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Jonah Pike", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("trueCulpritId", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetJournalPassesPagingParametersThroughToRepository()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.ProduceEvent(new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.LocalRecords,
            TownId = session.Player.CurrentTownId,
            Message = "Second entry"
        });
        session.ProduceEvent(new InvestigationPerformed
        {
            SourceKind = InvestigationSourceKind.LocalRecords,
            TownId = session.Player.CurrentTownId,
            Message = "Third entry"
        });
        repository.Seed(session);
        var handler = new GetJournalHandler(repository);

        var result = await handler.HandleAsync(new GetJournalQuery(session.Id.Value, Skip: 1, Take: 1));

        Assert.Equal(1, repository.LastJournalSkip);
        Assert.Equal(1, repository.LastJournalTake);
        Assert.Single(result.LogEntries);
        Assert.Equal("Second entry", result.LogEntries[0].Message);
    }

    [Fact]
    public async Task GetJournalThrowsWhenMissing()
    {
        var handler = new GetJournalHandler(new InMemoryGameSessionRepository());

        var exception = await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetJournalQuery(Guid.NewGuid())));

        Assert.Contains("was not found", exception.Message);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Jonah Pike",
                new SuspectProfile(
                    new[] { new SuspectAlias("Grey Jay", AliasKind.Nickname) },
                    new[] { new SuspectIdentityFact("Wears a cracked leather gauntlet on the right hand.") }),
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-2"),
                "Mira Cline",
                new SuspectProfile(
                    new[] { new SuspectAlias("M.K. Rook", AliasKind.KnownAs) },
                    new[] { new SuspectIdentityFact("Carries a tin badge clipped to a saddle strap.") }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge)
        };

        var clues = new[]
        {
            new Clue(new ClueId("clue-1"), ClueKind.Witness, "A rider was seen leaving at dusk."),
            new Clue(new ClueId("clue-2"), ClueKind.Record, "A coded telegraph entry was logged.")
        };

        var caseFile = new CaseFile(
            accusation: new SuspectId("suspect-2"),
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Find the culprit before the law closes in."),
            knownClues: clues,
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Butch Cassidy",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Grey Jay" },
                        new[] { "Raven-feather pin" },
                        "County marshal",
                        InvestigationTargetKind.Suspected,
                        Array.Empty<OutlawGangId>(),
                        null,
                        InvestigationSourceKind.SheriffWarrants),
                    "Wanted for a string of robberies near the county line.")
            });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
