using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;

namespace WildBunch.Application.Tests;

public sealed class AdvanceTravelDayHandlerTests
{
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);

    [Fact]
    public async Task HandleAsyncReturnsStructuredTrailEventOnTheTurnResult()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateEasyLuckyFoodSession();
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.True(result.Success || result.JourneyStatus == JourneyStatus.Interrupted);
        if (result.TrailEvent is not null)
        {
            Assert.Contains("cache", result.TrailEvent.Title, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("I ", result.TrailEvent.Message, StringComparison.OrdinalIgnoreCase);
        }

        Assert.True(result.CurrentSession.Inventory.Items.First(item => item.Kind == ItemKind.Food).Quantity >= 2);
    }

    [Fact]
    public async Task HandleAsyncReturnsFirstPersonDiaryForLuckyTrailEvent()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateEasyLuckyFoodSession();
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.NotNull(result.TravelDiary);
        var diaryDay = Assert.Single(result.TravelDiary!.Days);
        Assert.Contains(diaryDay.Entries, entry => entry.StartsWith("I ", StringComparison.Ordinal));
        Assert.DoesNotContain(diaryDay.Entries, entry => entry.Contains("you found", StringComparison.OrdinalIgnoreCase));
        Assert.True(diaryDay.CurrentFood >= 2);
        Assert.Equal(0, diaryDay.CurrentAmmo);
        Assert.Equal(result.CurrentSession.Player.Health, diaryDay.CurrentHealth);
    }

    [Fact]
    public async Task HandleAsyncReturnsPendingEncounterDiaryThatStopsAtTheDecision()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession();
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, result.JourneyStatus);
        Assert.NotNull(result.TravelDiary);
        var diaryDay = Assert.Single(result.TravelDiary!.Days);
        Assert.Single(diaryDay.Entries, entry => entry.Contains("cuts across my path", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("I could run, fight, or bribe", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diaryDay.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsyncReturnsFirstPersonDiaryForHorseLamenessAndFootFallback()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateProgressionSession(
            HorseTravelState.Healthy,
            TrailTerrain.Mountains,
            WaterFeature.None,
            trailRisk: TrailRisk.Low,
            GameDifficulty: GameDifficulty.Challenging);
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.NotNull(result.TravelDiary);
        var diaryDay = Assert.Single(result.TravelDiary!.Days);
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("went lame", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("I kept moving", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("lame", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diaryDay.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsyncKeepsCompletedJourneyUntilArrivalIsAcknowledged()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateEasyLuckyFoodSession();
        repository.Seed(session);

        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var acknowledgeHandler = new AcknowledgeJourneyArrivalHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var secondAdvance = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.NotNull(secondAdvance.CurrentSession.Journey);
        if (secondAdvance.JourneyStatus == JourneyStatus.Completed)
        {
            var acknowledged = await acknowledgeHandler.HandleAsync(new AcknowledgeJourneyArrivalCommand(session.Id.Value));

            Assert.True(acknowledged.Success);
            Assert.Null(acknowledged.CurrentSession.Journey);
            Assert.Equal("openpass", acknowledged.CurrentSession.Player.CurrentTownId);
            Assert.NotNull(acknowledged.TravelDiary);
        }
    }

    [Fact]
    public async Task HandleAsyncCompletesSixFullTrailDaysBeforeArrival()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSixDayQuietSession();
        repository.Seed(session);

        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var acknowledgeHandler = new AcknowledgeJourneyArrivalHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        GameTurnResultDto? result = null;
        for (var day = 1; day <= 6; day++)
        {
            result = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

            Assert.NotNull(result.CurrentSession.Journey);
        }

        if (result is not null && result.JourneyStatus == JourneyStatus.Completed)
        {
            var acknowledged = await acknowledgeHandler.HandleAsync(new AcknowledgeJourneyArrivalCommand(session.Id.Value));

            Assert.True(acknowledged.Success);
            Assert.Null(acknowledged.CurrentSession.Journey);
            Assert.NotNull(acknowledged.TravelDiary);
        }
    }

    private static GameSession CreateEasyLuckyFoodSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var openpass = new Town(new TownId("openpass"), "Open Pass", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, openpass },
            new[]
            {
                new Trail(new TrailId("trail-pine-open"), pinecross.Id, openpass.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 3),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: new CanteenState(1, 2)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1)
        });

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Easy, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, new TownId("openpass"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static GameSession CreateHighRiskSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.Spring)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 3),
            new InventoryItem(ItemKind.Canteen, 1),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1),
            new InventoryItem(ItemKind.Revolver, 1),
            new InventoryItem(ItemKind.RevolverAmmo, 2)
        });

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, dryfork.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        // Force a foe encounter so the test doesn't depend on the deterministic seed
        // producing a foe. The seed hash changed when the difficulty enum was renamed
        // (Normal -> Standard), which shifted which encounters the generator produces.
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        return session;
    }

    private static GameSession CreateSixDayQuietSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var sixmile = new Town(new TownId("sixmile"), "Six Mile", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, sixmile },
            new[]
            {
                new Trail(new TrailId("trail-six-mile"), pinecross.Id, sixmile.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 8),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: new CanteenState(6, 10)),
            new InventoryItem(ItemKind.Knife, 1)
        });

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, sixmile.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static GameSession CreateProgressionSession(
        HorseTravelState horseState,
        TrailTerrain terrain,
        WaterFeature waterFeature,
        bool withSaddle = true,
        int canteenCharges = 2,
        TrailRisk trailRisk = TrailRisk.Low,
        GameDifficulty GameDifficulty = GameDifficulty.Standard)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var midway = new Town(new TownId("midway"), "Midway", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, midway },
            new[]
            {
                new Trail(new TrailId("trail-pine-midway"), pinecross.Id, midway.Id, trailRisk, terrain, waterFeature)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var items = new List<InventoryItem>
        {
            new(ItemKind.Food, 3),
            new(ItemKind.Canteen, 1, canteenState: new CanteenState(canteenCharges, 2)),
            new(ItemKind.Horse, 1, horseState),
            new(ItemKind.Knife, 1)
        };

        if (withSaddle)
        {
            items.Add(new InventoryItem(ItemKind.Saddle, 1));
        }

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), new Inventory(items));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, midway.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }
}




