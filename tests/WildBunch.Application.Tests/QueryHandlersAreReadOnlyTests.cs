using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class QueryHandlersAreReadOnlyTests
{
    [Fact]
    public async Task QueryHandlersDoNotPersistGameSessionState()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        repository.Seed(session);

        var gameSessionHandler = new GetGameSessionHandler(repository);
        var journalHandler = new GetJournalHandler(repository, new JournalResolver());
        var availableActionsHandler = new GetAvailableActionsHandler(repository, new WildBunch.Domain.Actions.ActionAvailabilityResolver());
        var storeOffersHandler = new GetTownStoreOffersHandler(repository, new WildBunch.Domain.Economy.TownStoreCatalogResolver());

        _ = await gameSessionHandler.HandleAsync(new GetGameSessionQuery(session.Id.Value));
        _ = await journalHandler.HandleAsync(new GetJournalQuery(session.Id.Value));
        _ = await availableActionsHandler.HandleAsync(new GetAvailableActionsQuery(session.Id.Value));
        _ = await storeOffersHandler.HandleAsync(new GetTownStoreOffersQuery(session.Id.Value, "pinecross"));

        Assert.Equal(0, repository.SaveCalls);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Single(session.LogEntries);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Jonah Pike", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id);
    }
}
