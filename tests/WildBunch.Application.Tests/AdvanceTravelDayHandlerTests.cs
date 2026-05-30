using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Models;
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
    [Fact]
    public async Task HandleAsyncReturnsStructuredTrailEventOnTheTurnResult()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateEasyLuckyFoodSession();
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository);

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.True(result.Success);
        Assert.NotNull(result.TrailEvent);
        Assert.Equal(JourneyTrailEventId.LuckyFoodCache, result.TrailEvent!.Id);
        Assert.Equal(JourneyTrailEventKind.Lucky, result.TrailEvent.Kind);
        Assert.Equal("Trail grub cache", result.TrailEvent.Title);
        Assert.Equal(0m, result.TrailEvent.WalletDelta);
        Assert.Equal(2, result.TrailEvent.FoodDelta);
        Assert.Equal(0, result.TrailEvent.CanteenChargeDelta);
        Assert.Contains("jerky", result.TrailEvent.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, result.CurrentSession.Inventory.Items.First(item => item.Kind == ItemKind.Food).Quantity);
    }

    [Fact]
    public async Task HandleAsyncReturnsFirstPersonDiaryForLuckyTrailEvent()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateEasyLuckyFoodSession();
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository);

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.NotNull(result.TravelDiary);
        var diaryDay = Assert.Single(result.TravelDiary!.Days);
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("I found a cache of jerky", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diaryDay.Entries, entry => entry.Contains("you found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsyncReturnsPendingEncounterDiaryThatStopsAtTheDecision()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession();
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository);

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, result.JourneyStatus);
        Assert.NotNull(result.TravelDiary);
        var diaryDay = Assert.Single(result.TravelDiary!.Days);
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("A hard-eyed rider", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("I can run, fight, or bribe", StringComparison.OrdinalIgnoreCase));
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
            trailRisk: TrailRisk.Moderate,
            travelDifficulty: TravelDifficulty.Hard);
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository);

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.NotNull(result.TravelDiary);
        var diaryDay = Assert.Single(result.TravelDiary!.Days);
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("goes lame", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("I keep moving", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("lame", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diaryDay.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsyncKeepsCompletedJourneyUntilArrivalIsAcknowledged()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateEasyLuckyFoodSession();
        repository.Seed(session);

        var advanceHandler = new AdvanceTravelDayHandler(repository);
        var acknowledgeHandler = new AcknowledgeJourneyArrivalHandler(repository);

        var firstAdvance = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.True(firstAdvance.Success);
        Assert.Equal(JourneyStatus.Active, firstAdvance.JourneyStatus);
        Assert.NotNull(firstAdvance.CurrentSession.Journey);
        Assert.Equal(JourneyStatus.Active, firstAdvance.CurrentSession.Journey!.Status);

        var secondAdvance = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.True(secondAdvance.Success);
        Assert.Equal(JourneyStatus.Completed, secondAdvance.JourneyStatus);
        Assert.NotNull(secondAdvance.CurrentSession.Journey);
        Assert.Equal(JourneyStatus.Completed, secondAdvance.CurrentSession.Journey!.Status);
        Assert.Equal(2, secondAdvance.TravelDiary!.Days.Count);
        Assert.Equal(JourneyStatus.Completed, secondAdvance.TravelDiary.Days[^1].Status);

        var acknowledged = await acknowledgeHandler.HandleAsync(new AcknowledgeJourneyArrivalCommand(session.Id.Value));

        Assert.True(acknowledged.Success);
        Assert.Null(acknowledged.CurrentSession.Journey);
        Assert.Equal("openpass", acknowledged.CurrentSession.Player.CurrentTownId);
        Assert.Equal(2, acknowledged.TravelDiary!.Days.Count);
    }

    [Fact]
    public async Task HandleAsyncCompletesSixFullTrailDaysBeforeArrival()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSixDayQuietSession();
        repository.Seed(session);

        var advanceHandler = new AdvanceTravelDayHandler(repository);
        var acknowledgeHandler = new AcknowledgeJourneyArrivalHandler(repository);

        GameTurnResultDto? result = null;
        for (var day = 1; day <= 6; day++)
        {
            result = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

            Assert.True(result.Success);
            Assert.NotNull(result.CurrentSession.Journey);

            if (day < 6)
            {
                Assert.Equal(JourneyStatus.Active, result.JourneyStatus);
                Assert.Equal(day, result.TravelDiary!.Days.Count);
                Assert.Equal(JourneyStatus.Active, result.TravelDiary.Days[^1].Status);
                continue;
            }

            Assert.Equal(JourneyStatus.Completed, result.JourneyStatus);
            Assert.Equal(6, result.TravelDiary!.Days.Count);
            Assert.Equal(JourneyStatus.Completed, result.TravelDiary.Days[^1].Status);
            Assert.Equal("sixmile", result.CurrentSession.Player.CurrentTownId);
            Assert.Equal(7, result.CurrentSession.Clock.Day);
        }

        var acknowledged = await acknowledgeHandler.HandleAsync(new AcknowledgeJourneyArrivalCommand(session.Id.Value));

        Assert.True(acknowledged.Success);
        Assert.Null(acknowledged.CurrentSession.Journey);
        Assert.Equal(6, acknowledged.TravelDiary!.Days.Count);
    }

    private static GameSession CreateEasyLuckyFoodSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Easy);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("openpass"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static GameSession CreateHighRiskSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None)
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, dryfork.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static GameSession CreateSixDayQuietSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Normal);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, sixmile.Id, session.Player.Inventory, session.TravelRules).Preview!;
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
        TravelDifficulty travelDifficulty = TravelDifficulty.Normal)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new Inventory(items), travelDifficulty);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, midway.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }
}
