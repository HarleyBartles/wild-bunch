using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

public sealed class DevSaltSourceTests
{
    [Fact]
    public void ForceDevSaltSource_SetsFixedSaltAndProducesEvent()
    {
        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();

        var fixedSalt = SaltSource.CreateFixed("deadbeef");
        session.ForceDevSaltSource(fixedSalt);

        Assert.Equal(SaltSourceMode.Fixed, session.SaltSource.Mode);
        Assert.Equal("deadbeef", session.SaltSource.Salt);
        Assert.Contains(session.UncommittedEvents, e => e is DevSaltSourceForced);
    }

    [Fact]
    public void ClearDevSaltSource_RestoresRuntimeModeAndProducesEvent()
    {
        var session = TestSessionFactory.CreateDefault();
        session.ForceDevSaltSource(SaltSource.CreateFixed("deadbeef"));
        session.MarkEventsCommitted();

        session.ClearDevSaltSource();

        Assert.Equal(SaltSourceMode.Runtime, session.SaltSource.Mode);
        Assert.Contains(session.UncommittedEvents, e => e is DevSaltSourceCleared);
    }

    [Fact]
    public void Apply_DevSaltSourceForced_RestoresSaltOnReplay()
    {
        var session = TestSessionFactory.CreateDefault();
        var forced = new DevSaltSourceForced
        {
            ForcedSaltSource = SaltSource.CreateFixed("cafe")
        };
        session.Apply(forced);
        Assert.Equal(SaltSourceMode.Fixed, session.SaltSource.Mode);
        Assert.Equal("cafe", session.SaltSource.Salt);
    }

    // --- RNG mutation falsification proof ---

    [Fact]
    public void ForceDevSaltSource_DoesNotMutateJourneyState()
    {
        var session = TestSessionFactory.CreateDefault();
        var journeyBefore = session.Journey;
        session.ForceDevSaltSource(SaltSource.CreateFixed("abc123"));
        Assert.Equal(journeyBefore, session.Journey);
    }

    [Fact]
    public void ForceDevSaltSource_DoesNotMutateCurrentActionContext()
    {
        var session = TestSessionFactory.CreateDefault();
        var actionContextBefore = session.CurrentActionContext;
        session.ForceDevSaltSource(SaltSource.CreateFixed("abc123"));
        Assert.Equal(actionContextBefore, session.CurrentActionContext);
    }

    [Fact]
    public void ForceDevSaltSource_DoesNotMutatePlayerState()
    {
        var session = TestSessionFactory.CreateDefault();
        var walletBefore = session.Player.Wallet;
        var inventoryCountBefore = session.Player.Inventory.Items.Count;
        session.ForceDevSaltSource(SaltSource.CreateFixed("abc123"));
        Assert.Equal(walletBefore, session.Player.Wallet);
        Assert.Equal(inventoryCountBefore, session.Player.Inventory.Items.Count);
    }

    [Fact]
    public void ForceDevSaltSource_DoesNotMutateGameDifficultyOrEntropy()
    {
        var session = TestSessionFactory.CreateDefault();
        var difficultyBefore = session.GameDifficulty;
        var entropyBefore = session.GameEntropy;
        session.ForceDevSaltSource(SaltSource.CreateFixed("abc123"));
        Assert.Equal(difficultyBefore, session.GameDifficulty);
        Assert.Equal(entropyBefore, session.GameEntropy);
    }

    [Fact]
    public void ClearDevSaltSource_DoesNotMutateJourneyOrPlayerState()
    {
        var session = TestSessionFactory.CreateDefault();
        session.ForceDevSaltSource(SaltSource.CreateFixed("abc123"));
        session.MarkEventsCommitted();
        var journeyBefore = session.Journey;
        var walletBefore = session.Player.Wallet;
        session.ClearDevSaltSource();
        Assert.Equal(journeyBefore, session.Journey);
        Assert.Equal(walletBefore, session.Player.Wallet);
    }

    [Fact]
    public void ForceDevSaltSource_ProducesOnlyDevSaltSourceForcedEvent()
    {
        var session = TestSessionFactory.CreateDefault();
        session.MarkEventsCommitted();
        session.ForceDevSaltSource(SaltSource.CreateFixed("abc123"));
        // Only one event produced: DevSaltSourceForced. No journey/player/saloon events.
        Assert.Single(session.UncommittedEvents);
        Assert.IsType<DevSaltSourceForced>(session.UncommittedEvents.Single());
    }

    [Fact]
    public void ClearDevSaltSource_ProducesOnlyDevSaltSourceClearedEvent()
    {
        var session = TestSessionFactory.CreateDefault();
        session.ForceDevSaltSource(SaltSource.CreateFixed("abc123"));
        session.MarkEventsCommitted();
        session.ClearDevSaltSource();
        Assert.Single(session.UncommittedEvents);
        Assert.IsType<DevSaltSourceCleared>(session.UncommittedEvents.Single());
    }
}
