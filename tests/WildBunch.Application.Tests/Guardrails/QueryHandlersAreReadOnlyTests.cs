using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests.Guardrails;

public sealed class QueryHandlersAreReadOnlyTests
{
    [Fact]
    public async Task QueryHandlersDoNotPersistGameSessionState()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);

        var gameSessionHandler = new GetGameSessionHandler(repository);
        var journalHandler = new GetJournalHandler(repository);
        IGameSessionReadRepository readRepository = repository;
        var availableActionsHandler = new GetAvailableActionsHandler(readRepository, new WildBunch.Domain.Actions.ActionAvailabilityResolver());
        var worldMapHandler = new GetWorldMapHandler(readRepository);
        var storeOffersHandler = new GetTownStoreOffersHandler(readRepository, new WildBunch.Domain.Economy.TownStoreCatalogResolver());
        var travelPreviewHandler = new PreviewTravelHandler(readRepository, new WildBunch.Domain.Travel.TravelResolver());

        _ = await gameSessionHandler.HandleAsync(new GetGameSessionQuery(session.Id.Value));
        _ = await journalHandler.HandleAsync(new GetJournalQuery(session.Id.Value));
        _ = await availableActionsHandler.HandleAsync(new GetAvailableActionsQuery(session.Id.Value));
        _ = await worldMapHandler.HandleAsync(new GetWorldMapQuery(session.Id.Value));
        _ = await storeOffersHandler.HandleAsync(new GetTownStoreOffersQuery(session.Id.Value, "pinecross"));
        _ = await travelPreviewHandler.HandleAsync(new PreviewTravelQuery(session.Id.Value, "redmesa"));

        Assert.Equal(0, repository.StoreCalls);
        Assert.Equal(0, repository.CommitCalls);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Single(GameSessionLogProjection.Project(session));
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa");
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Jonah Pike", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart();
        return session;
    }
}
