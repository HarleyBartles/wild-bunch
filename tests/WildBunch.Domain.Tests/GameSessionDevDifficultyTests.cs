using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionDevDifficultyTests
{
    [Fact]
    public void ForceDevDifficulty_ChangesGameDifficultyAndTravelRules()
    {
        var session = TestSessionFactory.CreateDefault();

        session.ForceDevDifficulty(GameDifficulty.Brutal);

        Assert.Equal(GameDifficulty.Brutal, session.GameDifficulty);
        Assert.Equal(GameDifficulty.Brutal, session.TravelRules.Difficulty);
        // Brutal canteen capacity is 1, Easy is 10
        Assert.Equal(1, session.TravelRules.CanteenCapacity);
    }

    [Fact]
    public void ForceDevDifficulty_ProducesDevDifficultyForcedEvent()
    {
        var session = TestSessionFactory.CreateDefault();

        session.ForceDevDifficulty(GameDifficulty.Challenging);

        var evt = Assert.Single(session.UncommittedEvents.OfType<DevDifficultyForced>());
        Assert.Equal(GameDifficulty.Challenging, evt.ForcedDifficulty);
    }

    [Fact]
    public void ForceDevDifficulty_DoesNotMutateOtherState()
    {
        var session = TestSessionFactory.CreateDefault();
        var healthBefore = session.Player.Health;
        var cashBefore = session.Player.Wallet.Cash;
        var entropyBefore = session.GameEntropy;
        var saltBefore = session.SaltSource;
        var statusBefore = session.Status;
        var townBefore = session.CurrentTown.TownId;

        session.ForceDevDifficulty(GameDifficulty.Brutal);

        // Falsification: only GameDifficulty and derived TravelRules change
        Assert.Equal(healthBefore, session.Player.Health);
        Assert.Equal(cashBefore, session.Player.Wallet.Cash);
        Assert.Equal(entropyBefore, session.GameEntropy);
        Assert.Equal(saltBefore, session.SaltSource);
        Assert.Equal(statusBefore, session.Status);
        Assert.Equal(townBefore, session.CurrentTown.TownId);
        // Only one event, and it is the dev difficulty event
        Assert.Single(session.UncommittedEvents);
        Assert.IsType<DevDifficultyForced>(session.UncommittedEvents[0]);
    }

    [Fact]
    public void ForceDevDifficulty_WithInvalidDifficulty_Throws()
    {
        var session = TestSessionFactory.CreateDefault();

        Assert.Throws<ArgumentException>(() => session.ForceDevDifficulty((GameDifficulty)999));
    }

    [Fact]
    public void ForceDevDifficulty_CanBeReplayedFromEvents()
    {
        // CreateDefault() starts at Easy. Capture the original GameStarted
        // BEFORE forcing difficulty, so the replay stream has a GameStarted
        // with the original difficulty (Easy) followed by DevDifficultyForced.
        // This genuinely proves the ApplyEvent case for DevDifficultyForced
        // changes the rehydrated difficulty — if the case were missing, the
        // rehydrated session would stay at Easy.
        var session = TestSessionFactory.CreateDefault();
        var originalGameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);

        session.ForceDevDifficulty(GameDifficulty.Challenging);
        session.MarkEventsCommitted();

        var events = new[] { originalGameStarted }
            .Concat(session.CommittedEvents.OfType<IDomainEvent>())
            .ToList();
        var rehydrated = GameSession.RehydrateFromEvents(
            session.Id, session.World, events);

        // The GameStarted event carries Easy, but the DevDifficultyForced
        // event must override it to Challenging during replay.
        Assert.Equal(GameDifficulty.Challenging, rehydrated.GameDifficulty);
        Assert.Equal(GameDifficulty.Challenging, rehydrated.TravelRules.Difficulty);
    }
}
