using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionWantedSuspectPresenceTests
{
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);

    [Fact]
    public void WantedSuspectPresenceDefaultsToUnavailableUntilSet()
    {
        var session = CreateSession();
        var suspectId = new SuspectId("suspect-1");

        Assert.Equal(WantedSuspectPresenceState.Unavailable, session.GetWantedSuspectPresenceState(suspectId));
        Assert.False(session.TryGetWantedSuspectPresenceState(suspectId, out var state));
        Assert.Equal(WantedSuspectPresenceState.Unavailable, state);
        Assert.Empty(session.WantedSuspectPresenceEntries);
    }

    [Fact]
    public void WantedSuspectPresenceCanBeUpdatedAndClearedThroughGameSession()
    {
        var session = CreateSession();
        var suspectId = new SuspectId("suspect-1");

        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);

        Assert.Equal(WantedSuspectPresenceState.AvailableInTown, session.GetWantedSuspectPresenceState(suspectId));
        Assert.Single(session.WantedSuspectPresenceEntries);
        Assert.Equal(WantedSuspectPresenceState.AvailableInTown, session.WantedSuspectPresenceEntries[0].State);

        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.GoneToGround);

        Assert.Equal(WantedSuspectPresenceState.GoneToGround, session.GetWantedSuspectPresenceState(suspectId));
        Assert.Single(session.WantedSuspectPresenceEntries);
        Assert.Equal(WantedSuspectPresenceState.GoneToGround, session.WantedSuspectPresenceEntries[0].State);

        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.Unavailable);

        Assert.Equal(WantedSuspectPresenceState.Unavailable, session.GetWantedSuspectPresenceState(suspectId));
        Assert.Empty(session.WantedSuspectPresenceEntries);

        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.SecuredDead);

        Assert.Equal(WantedSuspectPresenceState.SecuredDead, session.GetWantedSuspectPresenceState(suspectId));
        Assert.Single(session.WantedSuspectPresenceEntries);
        Assert.Equal(WantedSuspectPresenceState.SecuredDead, session.WantedSuspectPresenceEntries[0].State);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.None);
        var world = new WildBunch.Domain.World.World(
            new[] { pinecross, holloway },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, holloway.Id, TrailRisk.Low) });

        var caseFile = new CaseFile(
            null,
            new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
            },
            new SuspectId("suspect-1"),
            Array.Empty<Clue>());

        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        return TestSessionFactory.StartGameCanonical(
            "Ranger Vale",
            world,
            caseFile,
            pinecross.Id,
            Wallet.Starting(25m),
            inventory,
            GameDifficulty.Standard,
            saltSource: DeterministicSaltSource);
    }
}
