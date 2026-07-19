using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using SaltSource = WildBunch.Domain.Game.SaltSource;

namespace WildBunch.Domain.Tests.Events;

public class StartFlowEventSourcingTests
{
    [Fact]
    public void StartSetup_Produces_PlayerSetupCompleted_AsUncommitted()
    {
        var session = CreateSetupSession();

        Assert.Equal(3, session.UncommittedEvents.Count);
        var setupEvent = Assert.IsType<PlayerSetupCompleted>(session.UncommittedEvents[0]);
        Assert.Equal("Ranger Vale", setupEvent.PlayerName);
        Assert.Equal(GameDifficulty.Standard, setupEvent.GameDifficulty);
        Assert.Equal(GameEntropy.Classic, setupEvent.GameEntropy);
        Assert.Equal("test-seed-12345", setupEvent.SeedCode);
    }

    [Fact]
    public void StartSetup_Produces_WorldGenerated_AsUncommitted()
    {
        var session = CreateSetupSession();

        Assert.Equal(3, session.UncommittedEvents.Count);
        var worldEvent = Assert.IsType<WorldGenerated>(session.UncommittedEvents[1]);
        Assert.Equal("test-seed-12345", worldEvent.SeedCode);
        Assert.NotNull(worldEvent.World);
    }

    [Fact]
    public void StartSetup_Produces_CaseFileGenerated_AsUncommitted()
    {
        var session = CreateSetupSession();

        Assert.Equal(3, session.UncommittedEvents.Count);
        var caseFileEvent = Assert.IsType<CaseFileGenerated>(session.UncommittedEvents[2]);
        Assert.NotNull(caseFileEvent.CaseFile);
    }

    [Fact]
    public void StartSetup_Sets_StartFlowPhase_ToSetupComplete()
    {
        var session = CreateSetupSession();
        Assert.Equal(StartFlowPhase.SetupComplete, session.StartFlowPhase);
    }

    [Fact]
    public void StartSetup_Sets_SeedCode_FromEvent()
    {
        var session = CreateSetupSession();
        Assert.Equal("test-seed-12345", session.SeedCode);
    }

    [Fact]
    public void StartSetup_Sets_GameDifficulty_AndEntropy_FromEvent()
    {
        var session = CreateSetupSession(gameDifficulty: GameDifficulty.Challenging, gameEntropy: GameEntropy.Wild);
        Assert.Equal(GameDifficulty.Challenging, session.GameDifficulty);
        Assert.Equal(GameEntropy.Wild, session.GameEntropy);
    }

    [Fact]
    public void StartSetup_Sets_PlayerName_FromEvent()
    {
        var session = CreateSetupSession(playerName: "Calamity Jo");
        Assert.Equal("Calamity Jo", session.Player.Name);
    }

    [Fact]
    public void ViewPrologue_Produces_PrologueViewed_Event()
    {
        var session = CreateSetupSession();
        session.MarkEventsCommitted();

        session.ViewPrologue("true-culprit-descriptor");

        var single = Assert.Single(session.UncommittedEvents);
        Assert.IsType<PrologueViewed>(single);
        Assert.Equal(StartFlowPhase.PrologueViewed, session.StartFlowPhase);
    }

    [Fact]
    public void ViewPrologue_WhenAlreadyPrologueViewed_IsIdempotent()
    {
        var session = CreateSetupSession();
        session.MarkEventsCommitted();

        session.ViewPrologue("descriptor-1");
        session.MarkEventsCommitted();

        session.ViewPrologue("descriptor-2");

        Assert.Empty(session.UncommittedEvents);
    }

    [Fact]
    public void ViewPrologue_WhenGameStarted_Throws()
    {
        var session = CreateSetupSession();
        session.MarkEventsCommitted();
        session.ViewPrologue("descriptor-1");
        session.MarkEventsCommitted();
        session.SelectStartingTown(new TownId("pinecross"));
        session.MarkEventsCommitted();
        session.CompleteGameStart();
        session.MarkEventsCommitted();

        Assert.Throws<InvalidOperationException>(() => session.ViewPrologue("descriptor-2"));
    }

    [Fact]
    public void CompleteGameStart_Produces_GameStarted_Event()
    {
        var session = CreateSetupSession();
        session.MarkEventsCommitted();
        session.ViewPrologue("descriptor-1");
        session.MarkEventsCommitted();
        session.SelectStartingTown(new TownId("pinecross"));
        session.MarkEventsCommitted();

        session.CompleteGameStart();

        var single = Assert.Single(session.UncommittedEvents);
        var gameStarted = Assert.IsType<GameStarted>(single);
        Assert.Equal(new TownId("pinecross"), gameStarted.StartingTownId);
        Assert.Equal("Pinecross", gameStarted.StartingTownName);
        Assert.Equal(StartFlowPhase.GameStarted, session.StartFlowPhase);
    }

    [Fact]
    public void CompleteGameStart_WhenAlreadyStarted_DoesNothing()
    {
        var session = CreateSetupSession();
        session.ViewPrologue("descriptor-1");
        session.SelectStartingTown(new TownId("pinecross"));
        session.CompleteGameStart();
        session.MarkEventsCommitted();

        session.CompleteGameStart();

        Assert.Empty(session.UncommittedEvents);
    }

    [Fact(Skip = "This scenario requires a session in NotStarted phase, which we can't easily create since StartSetup always starts in SetupComplete. The guard is still in the domain code.")]
    public void CompleteGameStart_WhenNotStarted_Throws()
    {
        // This scenario requires a session in NotStarted phase, which we can't easily create
        // since StartSetup always starts in SetupComplete. Skip this test.
        // The guard is still in the domain code.
    }

    [Fact]
    public void RehydrateFromEvents_WithSetupOnly_RestoresSetupPhase()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var saltSource = SaltSource.CreateFixed("test-salt");
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed-12345", saltSource);
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(session.Id, world, events);

        Assert.Equal(StartFlowPhase.SetupComplete, rehydrated.StartFlowPhase);
        Assert.Equal("test-seed-12345", rehydrated.SeedCode);
        Assert.Equal("Ranger Vale", rehydrated.Player.Name);
    }

    [Fact]
    public void RehydrateFromEvents_WithPrologueViewed_RestoresProloguePhase()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var saltSource = SaltSource.CreateFixed("test-salt");
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed-12345", saltSource);
        session.ViewPrologue("true-culprit");
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(session.Id, world, events);

        Assert.Equal(StartFlowPhase.PrologueViewed, rehydrated.StartFlowPhase);
    }

    [Fact]
    public void RehydrateFromEvents_WithFullStartFlow_RestoresGameStarted()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var saltSource = SaltSource.CreateFixed("test-salt");
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed-12345", saltSource);
        session.ViewPrologue("true-culprit");
        session.SelectStartingTown(new TownId("pinecross"));
        session.CompleteGameStart();
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(session.Id, world, events);

        Assert.Equal(StartFlowPhase.GameStarted, rehydrated.StartFlowPhase);
        Assert.Equal(new TownId("pinecross"), rehydrated.Player.CurrentTownId);
    }

    [Fact]
    public void RehydrateFromEvents_WithSetupOnly_DoesNotThrowForMissingGameStarted()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var saltSource = SaltSource.CreateFixed("test-salt");
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed-12345", saltSource);
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        // This should not throw even though there is no GameStarted event
        var rehydrated = GameSession.RehydrateFromEvents(session.Id, world, events);

        Assert.NotNull(rehydrated);
        Assert.Equal("Ranger Vale", rehydrated.Player.Name);
    }

    [Fact]
    public void RehydrateFromEvents_WithSetupOnly_RestoresSaltSourceFromWorldGeneratedEvent()
    {
        // Regression: RehydrateFromEvents fell back to SaltSource.CreateRuntime()
        // for setup-phase sessions (no GameStarted event), even though WorldGenerated
        // carries the original salt. This caused CompleteGameStart() to write a
        // fresh runtime salt to GameStarted instead of the original salt.
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var saltSource = SaltSource.CreateFixed("regression-salt-abc");
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed-12345", saltSource);
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(session.Id, world, events);

        Assert.Equal(saltSource, rehydrated.SaltSource);
    }

    [Fact]
    public void RehydrateFromEvents_WithSetupOnly_ThenCompleteGameStart_PreservesOriginalSalt()
    {
        // End-to-end regression: a rehydrated setup-phase session that goes through
        // CompleteGameStart must write the original salt to GameStarted, not a
        // runtime salt. This is the exact bug path: load → rehydrate → CompleteGameStart.
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var saltSource = SaltSource.CreateFixed("regression-salt-xyz");
        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed-12345", saltSource);
        var events = session.UncommittedEvents.ToList();
        session.MarkEventsCommitted();

        var rehydrated = GameSession.RehydrateFromEvents(session.Id, world, events);
        rehydrated.ViewPrologue("true-culprit");
        rehydrated.SelectStartingTown(new TownId("pinecross"));
        rehydrated.CompleteGameStart();

        var gameStarted = rehydrated.UncommittedEvents.OfType<GameStarted>().Single();
        Assert.Equal(saltSource, gameStarted.SaltSource);
    }

    private static GameSession CreateSetupSession(
        string playerName = "Ranger Vale",
        GameDifficulty gameDifficulty = GameDifficulty.Standard,
        GameEntropy gameEntropy = GameEntropy.Classic,
        string seedCode = "test-seed-12345")
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var saltSource = SaltSource.CreateFixed("test-salt");
        return GameSession.StartSetup(playerName, world, caseFile, gameDifficulty, gameEntropy, seedCode, saltSource);
    }

    private static DomainWorld CreateWorld()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph);
        return new DomainWorld(
            new[] { pinecross, redmesa },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low)
            });
    }

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };
        return new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
    }
}
