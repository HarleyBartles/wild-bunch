using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionDevEntropyTests
{
    [Fact]
    public void SetDevEntropy_ChangesGameEntropy()
    {
        var session = TestSessionFactory.CreateDefault();

        session.SetDevEntropy(GameEntropy.Wild);

        Assert.Equal(GameEntropy.Wild, session.GameEntropy);
    }

    [Fact]
    public void SetDevEntropy_ProducesDevEntropyChangedEvent()
    {
        var session = TestSessionFactory.CreateDefault();

        session.SetDevEntropy(GameEntropy.Wild);

        var evt = Assert.Single(session.UncommittedEvents.OfType<DevEntropyChanged>());
        Assert.Equal(GameEntropy.Wild, evt.NewEntropy);
    }

    [Fact]
    public void SetDevEntropy_DoesNotMutateOtherState()
    {
        var session = TestSessionFactory.CreateDefault();
        var healthBefore = session.Player.Health;
        var cashBefore = session.Player.Wallet.Cash;
        var difficultyBefore = session.GameDifficulty;
        var saltBefore = session.SaltSource;
        var statusBefore = session.Status;
        var townBefore = session.CurrentTown.TownId;

        session.SetDevEntropy(GameEntropy.Wild);

        // Falsification: only GameEntropy changes
        Assert.Equal(healthBefore, session.Player.Health);
        Assert.Equal(cashBefore, session.Player.Wallet.Cash);
        Assert.Equal(difficultyBefore, session.GameDifficulty);
        Assert.Equal(saltBefore, session.SaltSource);
        Assert.Equal(statusBefore, session.Status);
        Assert.Equal(townBefore, session.CurrentTown.TownId);
        // Only one event, and it is the dev entropy event
        Assert.Single(session.UncommittedEvents);
        Assert.IsType<DevEntropyChanged>(session.UncommittedEvents[0]);
    }

    [Fact]
    public void SetDevEntropy_WithInvalidEntropy_Throws()
    {
        var session = TestSessionFactory.CreateDefault();

        Assert.Throws<ArgumentException>(() => session.SetDevEntropy((GameEntropy)999));
    }

    [Fact]
    public void SetDevEntropy_CanBeReplayedFromEvents()
    {
        // CreateDefault() starts at Classic. Capture the original GameStarted
        // BEFORE forcing entropy, so the replay stream has a GameStarted
        // with the original entropy (Classic) followed by DevEntropyChanged.
        var session = TestSessionFactory.CreateDefault();
        var originalGameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);

        session.SetDevEntropy(GameEntropy.Wild);
        session.MarkEventsCommitted();

        var events = new[] { originalGameStarted }
            .Concat(session.CommittedEvents.OfType<IDomainEvent>())
            .ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, events);

        // The GameStarted event carries Classic, but the DevEntropyChanged
        // event must override it to Wild during replay.
        Assert.Equal(GameEntropy.Wild, rehydrated.GameEntropy);
    }
}
