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
        var session = CreateProgressionSession(new HorseTravelState(0, 0, 2), TrailTerrain.Hills, WaterFeature.River);
        repository.Seed(session);
        var handler = new AdvanceTravelDayHandler(repository);

        var result = await handler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));

        Assert.NotNull(result.TravelDiary);
        var diaryDay = Assert.Single(result.TravelDiary!.Days);
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("I had to finish the trail on foot", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("lame", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diaryDay.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
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

    private static GameSession CreateProgressionSession(
        HorseTravelState horseState,
        TrailTerrain terrain,
        WaterFeature waterFeature,
        bool withSaddle = true,
        int canteenCharges = 2,
        TrailRisk trailRisk = TrailRisk.Low)
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new Inventory(items));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, midway.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }
}
