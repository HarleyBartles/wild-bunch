using System.Text.Json;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests.Handlers;

public sealed class GetTownStoreOffersHandlerTests
{
    [Fact]
    public async Task GetTownStoreOffersLoadsSessionAndReturnsExpectedCatalog()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new GetTownStoreOffersHandler(repository, new TownStoreCatalogResolver());

        var result = await handler.HandleAsync(new GetTownStoreOffersQuery(session.Id.Value, "redmesa"));

        Assert.True(result.Available);
        Assert.Equal("Red Mesa", result.TownName);
        Assert.Contains(result.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith && offer.DisplayName == "Revolver ammo");
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTownStoreOffersReturnsProsperityBasedCatalogForDestituteTown()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new GetTownStoreOffersHandler(repository, new TownStoreCatalogResolver());

        var result = await handler.HandleAsync(new GetTownStoreOffersQuery(session.Id.Value, "dryfork"));

        // Every town has a store (prosperity-driven). A Destitute town has only
        // general store offers — no stable or gunsmith.
        Assert.True(result.Available);
        Assert.Contains(result.Offers, offer => offer.VendorType == StoreVendorType.GeneralStore);
        Assert.DoesNotContain(result.Offers, offer => offer.VendorType == StoreVendorType.Stable);
        Assert.DoesNotContain(result.Offers, offer => offer.VendorType == StoreVendorType.Gunsmith);
        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
    }

    [Fact]
    public async Task GetTownStoreOffersThrowsWhenTownMissing()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);
        var handler = new GetTownStoreOffersHandler(repository, new TownStoreCatalogResolver());

        var exception = await Assert.ThrowsAsync<TownNotFoundException>(
            () => handler.HandleAsync(new GetTownStoreOffersQuery(session.Id.Value, "missing")));

        Assert.Contains("was not found", exception.Message);
    }

    [Fact]
    public async Task GetTownStoreOffersThrowsWhenGameMissing()
    {
        var handler = new GetTownStoreOffersHandler(new InMemoryGameSessionRepository(), new TownStoreCatalogResolver());

        var exception = await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetTownStoreOffersQuery(Guid.NewGuid(), "pinecross")));

        Assert.Contains("was not found", exception.Message);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None, TownProsperity.Destitute);
        var world = new DomainWorld(
            new[] { pinecross, redmesa, dryfork },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart();
        return session;
    }
}
