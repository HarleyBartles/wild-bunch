using System.Text.Json;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;

namespace WildBunch.Application.Tests;

public sealed class GetGameSessionHandlerTests
{
    [Fact]
    public async Task GetGameSessionReturnsSavedSessionDto()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = new StubNewGameFactory().CreatedSession;
        repository.Seed(session);
        var handler = new GetGameSessionHandler(repository);

        var result = await handler.HandleAsync(new GetGameSessionQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.Id);
        Assert.Equal(session.Player.Name, result.Player.Name);
        Assert.Equal(session.Player.CurrentTownId.Value, result.Player.CurrentTownId);
        Assert.Equal(session.GameDifficulty, result.GameDifficulty);
        Assert.Equal(session.Player.Wallet.Cash, result.Inventory.Wallet.Cash);
        Assert.Equal(session.Player.Inventory.Items.Count, result.Inventory.Items.Count);
        Assert.NotNull(result.Inventory.HorseState);
        Assert.Equal(session.Player.Inventory.GetHorseState()!.Hunger, result.Inventory.HorseState!.Hunger);
        Assert.Equal(session.Player.Inventory.GetHorseState()!.Thirst, result.Inventory.HorseState.Thirst);
        Assert.Equal(session.Player.Inventory.GetHorseState()!.Exhaustion, result.Inventory.HorseState.Exhaustion);
        Assert.NotNull(result.Inventory.CanteenState);
        Assert.Equal(session.Player.Inventory.GetCanteenState()!.Charges, result.Inventory.CanteenState!.Charges);
        Assert.Equal(session.Player.Inventory.GetCanteenState()!.Capacity, result.Inventory.CanteenState.Capacity);
        var capabilityResolver = new InventoryCapabilityResolver();
        var expectedCapabilities = capabilityResolver.Resolve(session.Player.Inventory);
        Assert.Equal(expectedCapabilities.MountedTravelAvailable, result.Inventory.Capabilities.MountedTravelAvailable);
        Assert.Equal(expectedCapabilities.GunfightCapable, result.Inventory.Capabilities.GunfightCapable);
        Assert.Equal(session.Clock.Day, result.Clock.Day);
        Assert.Equal(session.Clock.Turn, result.Clock.Turn);
        Assert.Equal(session.PursuitState.Heat, result.PursuitState.Heat);
        Assert.Equal(session.CaseFile.OpeningLead.Description, result.CaseFile.OpeningLead);
        Assert.Equal("The Wild Bunch trail is quiet.", result.CaseFile.CaseState.StatusText);
        Assert.Empty(result.CaseFile.DiscoveredSuspects);
        Assert.Equal(new SuspectId("suspect-1"), session.CaseFile.TrueCulpritId);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("Ira Flint", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("suspect-1", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("At large", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueculpritid\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"suspectCount\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(typeof(WildBunch.Application.Games.Models.CaseFileDto).GetProperties(), property => property.Name.Contains("culprit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetGameSessionProjectsOnlyExplicitlyDiscoveredSuspects()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithDiscoveredSuspect();
        repository.Seed(session);
        var handler = new GetGameSessionHandler(repository);

        var result = await handler.HandleAsync(new GetGameSessionQuery(session.Id.Value));

        Assert.Single(result.CaseFile.DiscoveredSuspects);
        Assert.Equal("suspect-1", result.CaseFile.DiscoveredSuspects[0].Id);
        Assert.Equal("Ira Flint", result.CaseFile.DiscoveredSuspects[0].Name);
        Assert.Equal(SuspectStatus.AtLarge, result.CaseFile.DiscoveredSuspects[0].Status);
        Assert.Single(session.CaseFile.Suspects);
        Assert.Equal("The Wild Bunch trail is quiet.", result.CaseFile.CaseState.StatusText);

        var payload = JsonSerializer.Serialize(result);
        Assert.Contains("\"discoveredSuspects\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("suspect-2", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetGameSessionProjectsSafeClueAnchors()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithAnchoredClue();
        repository.Seed(session);
        var handler = new GetGameSessionHandler(repository);

        var result = await handler.HandleAsync(new GetGameSessionQuery(session.Id.Value));

        var clue = Assert.Single(result.CaseFile.KnownClues);
        Assert.Equal("saloon talk", clue.SourceLabel);
        Assert.Equal("Town rumor", clue.Context);
        Assert.Equal("Red Mesa rider", clue.Anchors.Subjects[0].Label);
        Assert.Equal("Grey Jay", clue.Anchors.Subjects[0].Alias);
        Assert.Equal("cracked gauntlet", clue.Anchors.Subjects[0].Feature);
        Assert.Equal("left town at dusk", clue.Anchors.Subjects[0].Fact);
        Assert.Equal("Red Mesa road", clue.Anchors.Locations[0].Label);
        Assert.Equal("rail spur", clue.Anchors.Locations[0].Route);
        Assert.Equal(ClueRecency.Recent, clue.Anchors.Times[0].Recency);
        Assert.Equal("heading east", clue.Anchors.Directions[0].Movement);

        var anchorsPayload = JsonSerializer.Serialize(clue.Anchors);
        Assert.DoesNotContain("\"suspectId\"", anchorsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"townId\"", anchorsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"destinationTownId\"", anchorsPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"linkedSuspectIds\"", anchorsPayload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetGameSessionThrowsWhenMissing()
    {
        var handler = new GetGameSessionHandler(new InMemoryGameSessionRepository());

        var exception = await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetGameSessionQuery(Guid.NewGuid())));

        Assert.Contains("was not found", exception.Message);
    }

    private static GameSession CreateSessionWithDiscoveredSuspect()
    {
        var session = new StubNewGameFactory().CreatedSession;
        session.CaseFile.DiscoverSuspect(new SuspectId("suspect-1"));
        return session;
    }

    private static GameSession CreateSessionWithAnchoredClue()
    {
        var pinecross = new WildBunch.Domain.World.Town(new WildBunch.Domain.World.TownId("pinecross"), "Pinecross", WildBunch.Domain.World.TownServices.Supplies | WildBunch.Domain.World.TownServices.Lodging);
        var redmesa = new WildBunch.Domain.World.Town(new WildBunch.Domain.World.TownId("redmesa"), "Red Mesa", WildBunch.Domain.World.TownServices.Supplies | WildBunch.Domain.World.TownServices.Telegraph);
        var world = new WildBunch.Domain.World.World(
            new[] { pinecross, redmesa },
            new[]
            {
                new WildBunch.Domain.World.Trail(new WildBunch.Domain.World.TrailId("trail-1"), pinecross.Id, redmesa.Id, WildBunch.Domain.World.TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Ira Flint",
                new SuspectProfile(
                    new[] { new SuspectAlias("Grey Jay", AliasKind.Nickname) },
                    new[] { new SuspectIdentityFact("Wears a cracked leather gauntlet on the right hand.") }),
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge)
        };

        var clues = new[]
        {
            new Clue(
                new ClueId("clue-anchored"),
                ClueKind.Witness,
                "A rider was seen leaving at dusk.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalGossip,
                "saloon talk",
                "Town rumor",
                new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor("Red Mesa rider", Alias: "Grey Jay", Feature: "cracked gauntlet", Fact: "left town at dusk")
                    },
                    locations: new[]
                    {
                        new ClueLocationAnchor("Red Mesa road", Place: "Red Mesa road", Route: "rail spur")
                    },
                    times: new[]
                    {
                        new ClueTimeAnchor(ClueRecency.Recent, Day: 5, Turn: 1)
                    },
                    directions: new[]
                    {
                        new ClueDirectionAnchor("heading east", Movement: "heading east", Route: "rail spur")
                    }))
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("A rider with a pale scar across the left cheek."),
            knownClues: clues);

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
