using WildBunch.Application.Games.Commands;
using WildBunch.Application.Projections;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests;

public sealed class ResolveJourneyEncounterHandlerTests
{
    private static readonly TravelRandomnessState DeterministicTravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty);

    [Fact]
    public async Task HandleAsyncReturnsFirstPersonDiaryForResolvedRunChoice()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession();
        repository.Seed(session);
        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var resolveHandler = new ResolveJourneyEncounterHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var advanceResult = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, advanceResult.JourneyStatus);

        var result = await resolveHandler.HandleAsync(new ResolveJourneyEncounterCommand(session.Id.Value, "run", ForcedRoll: 0UL));

        Assert.True(result.Success);
        Assert.NotNull(result.TravelDiary);
        var diaryDay = Assert.Single(result.TravelDiary!.Days);
        Assert.Equal(1, diaryDay.Entries.Count(entry => entry.Contains("got away", StringComparison.OrdinalIgnoreCase)));
        Assert.Contains(diaryDay.Entries, entry => entry.Contains("I spurred the horse and got away before the rider could close in.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(diaryDay.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsyncKeepsTheTravelDiaryOnOneFrozenDayWhenResolvingTheEncounter()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession();
        repository.Seed(session);
        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var resolveHandler = new ResolveJourneyEncounterHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var advanceResult = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.Single(advanceResult.TravelDiary!.Days);

        var resolveResult = await resolveHandler.HandleAsync(new ResolveJourneyEncounterCommand(session.Id.Value, "run", ForcedRoll: 0UL));

        Assert.True(resolveResult.Success);
        var resolvedDay = Assert.Single(resolveResult.TravelDiary!.Days);
        Assert.Equal(1, resolvedDay.Entries.Count(entry => entry.Contains("got away", StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(resolvedDay.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task HandleAsyncPersistsAmmoTotalsIntoTheResolvedTravelDiary()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession();
        repository.Seed(session);
        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var resolveHandler = new ResolveJourneyEncounterHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var advanceResult = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, advanceResult.JourneyStatus);

        var resolveResult = await resolveHandler.HandleAsync(new ResolveJourneyEncounterCommand(session.Id.Value, "fight", BulletSpend: 2, ForcedRoll: 0UL));

        Assert.True(resolveResult.Success);
        var resolvedDay = Assert.Single(resolveResult.TravelDiary!.Days);
        Assert.Equal(resolveResult.CurrentSession.Player.Health, resolvedDay.CurrentHealth);
        Assert.Equal(
            resolveResult.CurrentSession.Inventory.Items.Where(item => item.Kind is ItemKind.RevolverAmmo or ItemKind.RifleAmmo).Sum(item => item.Quantity),
            resolvedDay.CurrentAmmo);
        Assert.Equal(2, resolvedDay.EncounterResolution!.AmmoSpent);
        Assert.Equal(1, resolvedDay.Entries.Count(entry => entry.Contains("forced the rider off the trail", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task HandleAsyncPersistsBribeAmountsIntoTheResolvedTravelDiary()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession(wallet: Wallet.Starting(20m));
        repository.Seed(session);
        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var resolveHandler = new ResolveJourneyEncounterHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var advanceResult = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, advanceResult.JourneyStatus);

        var bribeAmount = session.Journey!.PendingEncounter!.FoeProfile!.MinimumBribe;
        var resolveResult = await resolveHandler.HandleAsync(new ResolveJourneyEncounterCommand(session.Id.Value, "bribe", BribeAmount: bribeAmount, ForcedRoll: 0UL));

        Assert.True(resolveResult.Success);
        var resolvedDay = Assert.Single(resolveResult.TravelDiary!.Days);
        Assert.Equal(20m - bribeAmount, resolveResult.CurrentSession.Inventory.Wallet.Cash);
        Assert.Equal(-bribeAmount, resolvedDay.EncounterResolution!.WalletDelta);
        Assert.Equal(1, resolvedDay.Entries.Count(entry => entry.Contains("let me pass", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task HandleAsyncAppendsUnresolvedBribeAttemptsToTheTravelDiary()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession(wallet: Wallet.Starting(20m));
        repository.Seed(session);
        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var resolveHandler = new ResolveJourneyEncounterHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var advanceResult = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, advanceResult.JourneyStatus);

        var bribeAmount = Math.Max(1m, session.Journey!.PendingEncounter!.FoeProfile!.MinimumBribe - 1m);
        var resolveResult = await resolveHandler.HandleAsync(new ResolveJourneyEncounterCommand(session.Id.Value, "bribe", BribeAmount: bribeAmount, ForcedRoll: 99UL));

        Assert.False(resolveResult.Success);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, resolveResult.JourneyStatus);
        var resolvedDay = Assert.Single(resolveResult.TravelDiary!.Days);
        Assert.Contains(resolvedDay.Entries, entry => entry.Contains("cuts across my path", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, resolvedDay.Entries.Count(entry => entry.Contains("pocketed it without moving aside", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(20m - bribeAmount, resolvedDay.CurrentWallet);
        Assert.Equal(-bribeAmount, resolvedDay.WalletDelta);
    }

    [Fact]
    public async Task HandleAsyncAppendsUnresolvedRunAttemptsToTheTravelDiary()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession();
        repository.Seed(session);
        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var resolveHandler = new ResolveJourneyEncounterHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var advanceResult = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, advanceResult.JourneyStatus);

        var resolveResult = await resolveHandler.HandleAsync(new ResolveJourneyEncounterCommand(session.Id.Value, "run", ForcedRoll: 99UL));

        Assert.False(resolveResult.Success);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, resolveResult.JourneyStatus);
        var resolvedDay = Assert.Single(resolveResult.TravelDiary!.Days);
        Assert.Contains(resolvedDay.Entries, entry => entry.Contains("cuts across my path", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, resolvedDay.Entries.Count(entry => entry.Contains("horse still had to work for it", StringComparison.OrdinalIgnoreCase)));
        Assert.True(resolvedDay.HeatIncrease > 0);
    }

    [Fact]
    public async Task HandleAsyncAppendsUnresolvedFightAttemptsToTheTravelDiary()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateHighRiskSession();
        repository.Seed(session);
        var advanceHandler = new AdvanceTravelDayHandler(repository, repository,
            new HudProjector(), new DiaryProjector());
        var resolveHandler = new ResolveJourneyEncounterHandler(repository, repository,
            new HudProjector(), new DiaryProjector());

        var advanceResult = await advanceHandler.HandleAsync(new AdvanceTravelDayCommand(session.Id.Value));
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, advanceResult.JourneyStatus);

        var resolveResult = await resolveHandler.HandleAsync(new ResolveJourneyEncounterCommand(session.Id.Value, "fight", BulletSpend: 1, ForcedRoll: 99UL));

        Assert.False(resolveResult.Success);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, resolveResult.JourneyStatus);
        var resolvedDay = Assert.Single(resolveResult.TravelDiary!.Days);
        Assert.Contains(resolvedDay.Entries, entry => entry.Contains("cuts across my path", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, resolvedDay.Entries.Count(entry => entry.Contains("spent 1 round(s), but the rider kept coming", StringComparison.OrdinalIgnoreCase)));
        Assert.Equal(1, resolvedDay.AmmoSpent);
    }

    private static GameSession CreateHighRiskSession(Wallet? wallet = null)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new World(
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, wallet ?? Wallet.Starting(25m), inventory, travelRandomness: DeterministicTravelRandomness);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, dryfork.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        session.PursuitState.IncreaseHeat((int)TrailRisk.High);
        return session;
    }
}




