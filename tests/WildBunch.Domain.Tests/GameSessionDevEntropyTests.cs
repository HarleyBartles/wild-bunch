using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Persistence.Serialization;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionDevEntropyTests
{
    // --- Basic mutation + event production ---

    [Fact]
    public void SetDevEntropy_SetsEntropyAndProducesEvent()
    {
        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();

        session.SetDevEntropy(GameEntropy.Wild);

        Assert.Equal(GameEntropy.Wild, session.GameEntropy);
        Assert.Contains(session.UncommittedEvents, e => e is DevEntropyChanged);
    }

    [Fact]
    public void SetDevEntropy_ValidatesEnumIsDefined()
    {
        var session = TestSessionFactory.CreateDefault();
        Assert.Throws<ArgumentException>(() => session.SetDevEntropy((GameEntropy)999));
    }

    [Fact]
    public void Apply_DevEntropyChanged_RestoresEntropyOnReplay()
    {
        var session = TestSessionFactory.CreateDefault();
        var changed = new DevEntropyChanged { NewEntropy = GameEntropy.Adventurous };
        session.Apply(changed);
        Assert.Equal(GameEntropy.Adventurous, session.GameEntropy);
    }

    [Fact]
    public void SetDevEntropy_ProducesOnlyDevEntropyChangedEvent()
    {
        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();
        session.SetDevEntropy(GameEntropy.Wild);
        Assert.Single(session.UncommittedEvents);
        Assert.IsType<DevEntropyChanged>(session.UncommittedEvents.Single());
    }

    // --- Event-store proof: serializer round-trip ---

    [Fact]
    public void DevEntropyChanged_RoundTripsThroughEventSerializer()
    {
        var serializer = new GameSessionJsonSerializer();
        var original = new DevEntropyChanged { NewEntropy = GameEntropy.Wild };

        var json = serializer.SerializeEvent(original);
        var deserialized = serializer.DeserializeEvent(nameof(DevEntropyChanged), json);

        var roundTripped = Assert.IsType<DevEntropyChanged>(deserialized);
        Assert.Equal(GameEntropy.Wild, roundTripped.NewEntropy);
    }

    [Fact]
    public void ResolveEventType_MapsDevEntropyChanged()
    {
        var serializer = new GameSessionJsonSerializer();
        var original = new DevEntropyChanged { NewEntropy = GameEntropy.Boring };

        var json = serializer.SerializeEvent(original);
        // If ResolveEventType does not map DevEntropyChanged, this throws InvalidOperationException.
        var deserialized = serializer.DeserializeEvent(nameof(DevEntropyChanged), json);
        Assert.IsType<DevEntropyChanged>(deserialized);
    }

    // --- Event-store proof: rehydrate from events ---

    [Fact]
    public void DevEntropyChanged_RehydratesFromEventStream()
    {
        // Create a fresh session without marking events committed, so we can collect
        // the full event stream (GameStarted + DevEntropyChanged) for rehydration.
        var session = CreateFreshSessionForReplay();
        session.SetDevEntropy(GameEntropy.Wild);
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id,
            session.World,
            session.CaseFile,
            events);

        Assert.Equal(GameEntropy.Wild, rehydrated.GameEntropy);
    }

    // --- Falsification: does not mutate other state ---

    [Fact]
    public void SetDevEntropy_DoesNotMutateOtherState()
    {
        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();

        // Capture all state before the call
        var difficultyBefore = session.GameDifficulty;
        var saltSourceBefore = session.SaltSource;
        var healthBefore = session.Player.Health;
        var walletBefore = session.Player.Wallet;
        var walletCashBefore = session.Player.Wallet.Cash;
        var journeyBefore = session.Journey;
        var actionContextBefore = session.CurrentActionContext;
        var statusBefore = session.Status;
        var currentTownIdBefore = session.Player.CurrentTownId;
        var caseFileBefore = session.CaseFile;
        var inventoryCountBefore = session.Player.Inventory.Items.Count;

        session.SetDevEntropy(GameEntropy.Wild);

        // Assert only GameEntropy changed
        Assert.Equal(GameEntropy.Wild, session.GameEntropy);

        // Assert all other state is unchanged
        Assert.Equal(difficultyBefore, session.GameDifficulty);
        Assert.Equal(saltSourceBefore, session.SaltSource);
        Assert.Equal(healthBefore, session.Player.Health);
        Assert.Equal(walletBefore, session.Player.Wallet);
        Assert.Equal(walletCashBefore, session.Player.Wallet.Cash);
        Assert.Equal(journeyBefore, session.Journey);
        Assert.Equal(actionContextBefore, session.CurrentActionContext);
        Assert.Equal(statusBefore, session.Status);
        Assert.Equal(currentTownIdBefore, session.Player.CurrentTownId);
        Assert.Equal(caseFileBefore, session.CaseFile);
        Assert.Equal(inventoryCountBefore, session.Player.Inventory.Items.Count);

        // Assert only one event produced and it is DevEntropyChanged
        Assert.Single(session.UncommittedEvents);
        Assert.IsType<DevEntropyChanged>(session.UncommittedEvents.Single());
    }

    [Fact]
    public void SetDevEntropy_DoesNotMutateHiddenCulpritTruth()
    {
        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();

        var trueCulpritIdBefore = session.CaseFile.TrueCulpritId;
        var suspectsBefore = session.CaseFile.Suspects;

        session.SetDevEntropy(GameEntropy.Adventurous);

        Assert.Equal(trueCulpritIdBefore, session.CaseFile.TrueCulpritId);
        Assert.Equal(suspectsBefore, session.CaseFile.Suspects);
    }

    /// <summary>
    /// Creates a fresh session without calling MarkEventsCommitted, so UncommittedEvents
    /// contains the GameStarted event. Used for rehydrate-from-events tests that need
    /// the full event stream.
    /// </summary>
    private static GameSession CreateFreshSessionForReplay()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());

        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id,
            Wallet.Starting(25m), inventory);
    }
}
