using WildBunch.Domain;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionAggregateRootTests
{
    [Fact]
    public void GameSessionIsMarkedAsTheMutableAggregateRoot()
    {
        Assert.True(typeof(IAggregateRoot).IsAssignableFrom(typeof(GameSession)));
    }

    [Fact]
    public void SessionLevelMutationMethodsChangeOnlySessionOwnedState()
    {
        var session = CreateSession();
        var beforeLogCount = session.LogEntries.Count;
        var beforeTurn = session.Clock.Turn;

        session.RecordCaseUpdate("A public lead is noted.");

        // RecordCaseUpdate is decoupled from the clock (BUNCH-80 Task 1).
        // It only adds a log entry — turn advancement is handled by EnterActionContext.
        Assert.Equal(beforeTurn, session.Clock.Turn);
        Assert.Equal(beforeLogCount + 1, session.LogEntries.Count);
        Assert.Equal("A public lead is noted.", session.LogEntries[^1].Message);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Jonah Pike", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());

        return GameSession.StartNew("Ranger Vale", new DomainWorld(new[] { pinecross, redmesa }, Array.Empty<Trail>()), caseFile, pinecross.Id);
    }
}
