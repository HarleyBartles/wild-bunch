using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Integration.Tests;

/// <summary>
/// Consolidated end-to-end tests that prove the full event-sourcing flow:
/// create -> store -> reload -> command -> store -> replay -> project.
/// These tests are the capstone proof for ADR-0028.
/// </summary>
public sealed class EventSourcingEndToEndTests : IClassFixture<PostgreSqlPersistenceFixture>
{
    private static ServiceProvider CreateServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<WildBunchDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<GameSessionJsonSerializer>();
        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
        services.AddScoped<IGameSessionUnitOfWork, EfGameSessionUnitOfWork>();
        services.AddSingleton<HudProjector>();
        services.AddSingleton<DiaryProjector>();
        services.AddSingleton<FullAuditProjector>();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<WildBunchDbContext>().Database.Migrate();

        return provider;
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var quartzsite = new Town(new TownId("quartzsite"), "Quartzsite", TownServices.Telegraph);
        var world = new DomainWorld(
            new[] { pinecross, quartzsite },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, quartzsite.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
    }

    [Fact]
    public async Task FullFlow_CreateStoreReloadCommandStoreReplayProject()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        var hudProjector = scope.ServiceProvider.GetRequiredService<HudProjector>();
        var auditProjector = scope.ServiceProvider.GetRequiredService<FullAuditProjector>();

        // 1. Create session
        var session = CreateSession();
        Assert.Single(session.UncommittedEvents);
        Assert.IsType<GameStarted>(session.UncommittedEvents[0]);

        // 2. Store + commit
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();
        Assert.Empty(session.UncommittedEvents);
        Assert.Equal(1, session.Version);

        // 3. Reload from snapshot
        var reloaded = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(1, reloaded!.Version);
        Assert.Equal("Ranger Vale", reloaded.Player.Name);

        // 4. Command: purchase
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(reloaded.World.GetTown(reloaded.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        reloaded.Purchase(offer, 3);
        Assert.Equal(2, reloaded.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(reloaded.UncommittedEvents[0]);
        Assert.IsType<StoreItemPurchased>(reloaded.UncommittedEvents[1]);

        // 5. Store + commit
        await repo.StoreAsync(reloaded);
        await uow.CommitAsync();
        reloaded.MarkEventsCommitted();
        Assert.Equal(3, reloaded.Version);

        // 6. Replay from events
        var events = await repo.GetEventStreamAsync(session.Id);
        Assert.Equal(3, events.Count);
        Assert.IsType<GameStarted>(events[0]);
        Assert.IsType<TownActionContextEntered>(events[1]);
        Assert.IsType<StoreItemPurchased>(events[2]);

        // 7. Project from events
        var hud = hudProjector.Project(events);
        Assert.Equal("Ranger Vale", hud.PlayerName);
        Assert.Equal(19m, hud.WalletCash); // 25 - 6 = 19
        Assert.Equal(4, hud.InventoryItems.Single(i => i.ItemKind == DomainItemKind.Food).Quantity); // 1 + 3 = 4

        var audit = auditProjector.Project(events);
        Assert.Equal(3, audit.Entries.Count);
        Assert.Equal("GameStarted", audit.Entries[0].EventType);
        Assert.Equal("TownActionContextEntered", audit.Entries[1].EventType);
        Assert.Equal("StoreItemPurchased", audit.Entries[2].EventType);
    }

    [Fact]
    public async Task FullFlow_ReplayFromEvents_MatchesSnapshotLoad()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create and store
        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Reload and purchase
        var reloaded = await repo.GetByIdAsync(session.Id);
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(reloaded!.World.GetTown(reloaded.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        reloaded.Purchase(offer, 2);
        await repo.StoreAsync(reloaded);
        await uow.CommitAsync();

        // Load from snapshot
        var fromSnapshot = await repo.GetByIdAsync(session.Id);

        // Load from event stream (full replay)
        var events = await repo.GetEventStreamAsync(session.Id);
        var fromEvents = GameSession.RehydrateFromEvents(
            session.Id,
            fromSnapshot!.World,
            events);

        // State equality proof
        Assert.Equal(fromSnapshot.Player.Wallet.Cash, fromEvents.Player.Wallet.Cash);
        Assert.Equal(fromSnapshot.Player.Inventory.GetQuantity(DomainItemKind.Food), fromEvents.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(fromSnapshot.Version, fromEvents.Version);
        Assert.Equal(fromSnapshot.Player.Name, fromEvents.Player.Name);
    }

    [Fact]
    public async Task FullFlow_ConcurrencyConflict_PreventsDoubleAppend()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create and store
        var session = CreateSession();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Two concurrent loads
        var copy1 = await repo.GetByIdAsync(session.Id);
        var copy2 = await repo.GetByIdAsync(session.Id);

        // First purchase succeeds
        var resolver = new TownStoreCatalogResolver();
        var offer1 = resolver.Resolve(copy1!.World.GetTown(copy1.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        copy1.Purchase(offer1, 1);
        await repo.StoreAsync(copy1);
        await uow.CommitAsync();

        // Second purchase should fail with ConcurrencyException
        var offer2 = resolver.Resolve(copy2!.World.GetTown(copy2.Player.CurrentTownId))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        copy2.Purchase(offer2, 1);

        await Assert.ThrowsAsync<Application.Games.Exceptions.ConcurrencyException>(() => repo.StoreAsync(copy2));
    }
}
