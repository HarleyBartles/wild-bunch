using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;
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

namespace WildBunch.Application.Tests.Execution;

public sealed class GameSessionCommandHandlerTests
{
    [Fact]
    public async Task ExecuteWithRetryAsync_LoadsCommandStoresCommitsAndMarksEventsCommitted()
    {
        var repo = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);

        var handler = new TestCommandHandler(repo, repo);
        var result = await handler.ExecuteWithRetryAsync(
            session.Id,
            async (s, ct) =>
            {
                var resolver = new TownStoreCatalogResolver();
                var offer = resolver.Resolve(s.World.GetTown(s.Player.CurrentTownId!.Value))
                    .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
                s.Purchase(offer, 1);
                await Task.Yield();
                return "purchased";
            });

        Assert.Equal("purchased", result);
        Assert.Equal(1, repo.StoreCalls);
        Assert.Equal(1, repo.CommitCalls);
        var stored = repo.Sessions.Single();
        Assert.Empty(stored.UncommittedEvents);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_DoesNotStoreWhenNoEventsProduced()
    {
        var repo = new InMemoryGameSessionRepository();
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);

        var handler = new TestCommandHandler(repo, repo);
        var result = await handler.ExecuteWithRetryAsync(
            session.Id,
            async (s, ct) =>
            {
                await Task.Yield();
                return "no-op";
            });

        Assert.Equal("no-op", result);
        Assert.Equal(0, repo.StoreCalls);
        Assert.Equal(0, repo.CommitCalls);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_StoreConflictReloadsDurableStateAndCommitsPurchaseOnce()
    {
        var repo = new AuthoritativeRetryRepository(ConcurrencyFailure.StoreOnce);
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);
        var initialWallet = session.Player.Wallet.Cash;
        var initialFood = session.Player.GetQuantity(DomainItemKind.Food);
        var foodOffer = new TownStoreCatalogResolver().Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);

        var handler = new TestCommandHandler(repo, repo);
        var result = await handler.ExecuteWithRetryAsync(
            session.Id,
            async (s, ct) =>
            {
                // Produce an event so StoreAsync is called
                var resolver = new TownStoreCatalogResolver();
                var retryOffer = resolver.Resolve(s.World.GetTown(s.Player.CurrentTownId!.Value))
                    .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
                s.Purchase(retryOffer, 1);
                await Task.Yield();
                return "ok";
            });

        Assert.Equal("ok", result);
        var stored = await repo.GetByIdAsync(session.Id);
        Assert.Equal(initialWallet - foodOffer.Price, stored!.Player.Wallet.Cash);
        Assert.Equal(initialFood + 1, stored.Player.GetQuantity(DomainItemKind.Food));
        Assert.Equal(1, repo.StoreConflictCount);
        AssertSingleFoodPurchase(repo, initialWallet, foodOffer.Price);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_CommitConflictRollsBackAttemptAndCommitsPurchaseOnce()
    {
        var repo = new AuthoritativeRetryRepository(ConcurrencyFailure.CommitOnce);
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);
        var initialWallet = session.Player.Wallet.Cash;
        var initialFood = session.Player.GetQuantity(DomainItemKind.Food);
        var foodOffer = new TownStoreCatalogResolver().Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.ItemKind == DomainItemKind.Food);

        var handler = new TestCommandHandler(repo, repo);
        var result = await handler.ExecuteWithRetryAsync(
            session.Id,
            (s, ct) =>
            {
                var catalog = new TownStoreCatalogResolver();
                var currentOffer = catalog.Resolve(s.World.GetTown(s.Player.CurrentTownId!.Value))
                    .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
                s.Purchase(currentOffer, 1);
                return Task.FromResult("purchased");
            });

        Assert.Equal("purchased", result);
        var stored = await repo.GetByIdAsync(session.Id);
        Assert.Equal(initialWallet - foodOffer.Price, stored!.Player.Wallet.Cash);
        Assert.Equal(initialFood + 1, stored.Player.GetQuantity(DomainItemKind.Food));
        Assert.Equal(1, repo.CommitConflictCount);
        AssertSingleFoodPurchase(repo, initialWallet, foodOffer.Price);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_StoreConflictExhaustionDoesNotPersistFailedPurchase()
    {
        var repo = new AuthoritativeRetryRepository(ConcurrencyFailure.StoreAlways);
        var session = CreateSession();
        session.MarkEventsCommitted();
        repo.Seed(session);
        var initialWallet = session.Player.Wallet.Cash;
        var initialFood = session.Player.GetQuantity(DomainItemKind.Food);
        var initialEventCount = repo.DurableEvents.Count;
        var handler = new TestCommandHandler(repo, repo);

        await Assert.ThrowsAsync<ConcurrencyException>(() => handler.ExecuteWithRetryAsync(
            session.Id,
            (s, ct) =>
            {
                var resolver = new TownStoreCatalogResolver();
                var offer = resolver.Resolve(s.World.GetTown(s.Player.CurrentTownId!.Value))
                    .Offers.Single(o => o.ItemKind == DomainItemKind.Food);
                s.Purchase(offer, 1);
                return Task.FromResult("purchased");
            }));

        var stored = await repo.GetByIdAsync(session.Id);
        Assert.Equal(initialWallet, stored!.Player.Wallet.Cash);
        Assert.Equal(initialFood, stored.Player.GetQuantity(DomainItemKind.Food));
        Assert.Equal(initialEventCount, repo.DurableEvents.Count);
        Assert.Equal(3, repo.StoreConflictCount);
        Assert.Empty(repo.DurableEvents.OfType<StoreItemPurchased>());
    }

    private static void AssertSingleFoodPurchase(
        AuthoritativeRetryRepository repository,
        decimal initialWallet,
        decimal foodPrice)
    {
        var purchase = Assert.Single(repository.DurableEvents.OfType<StoreItemPurchased>());
        Assert.Equal(DomainItemKind.Food, purchase.ItemKind);
        Assert.Equal(1, purchase.Quantity);
        Assert.Equal(foodPrice, purchase.UnitPrice);
        Assert.Equal(initialWallet - foodPrice, purchase.WalletAfter);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa");
        var world = new DomainWorld(
            new[] { pinecross, redmesa },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, redmesa.Id, TrailRisk.Low) });

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

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private sealed class TestCommandHandler : GameSessionCommandHandler
    {
        public TestCommandHandler(IGameSessionRepository repo, IGameSessionUnitOfWork uow)
            : base(repo, uow) { }
    }

    private enum ConcurrencyFailure
    {
        StoreOnce,
        StoreAlways,
        CommitOnce
    }

    private sealed class AuthoritativeRetryRepository : IGameSessionRepository, IGameSessionUnitOfWork
    {
        private readonly ConcurrencyFailure _failure;
        private readonly Dictionary<GameSessionId, DomainWorld> _worlds = new();
        private readonly Dictionary<GameSessionId, List<IDomainEvent>> _durableEvents = new();
        private List<IDomainEvent>? _pendingEvents;
        private int _storeConflicts;
        private int _commitConflicts;

        public AuthoritativeRetryRepository(ConcurrencyFailure failure) => _failure = failure;

        public int StoreConflictCount { get; private set; }
        public int CommitConflictCount { get; private set; }
        public IReadOnlyList<IDomainEvent> DurableEvents => _durableEvents.Values.Single();

        public void Seed(GameSession session)
        {
            _worlds[session.Id] = session.World;
            _durableEvents[session.Id] = session.AllEvents.ToList();
        }

        public Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken ct = default)
        {
            if (!_durableEvents.TryGetValue(id, out var events))
            {
                return Task.FromResult<GameSession?>(null);
            }

            return Task.FromResult<GameSession?>(GameSession.RehydrateFromEvents(id, _worlds[id], events));
        }

        public Task<IReadOnlyList<GameSession>> GetByStatusAsync(GameStatus status, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<GameSession>>(Array.Empty<GameSession>());

        public Task<StartupOperation?> GetStartupOperationAsync(Guid operationId, CancellationToken ct = default)
            => Task.FromResult<StartupOperation?>(null);

        public void StoreStartupOperation(StartupOperation operation) { }

        public Task StoreAsync(GameSession session, Guid? correlationId = null, CancellationToken ct = default)
        {
            if (_failure == ConcurrencyFailure.StoreAlways
                || (_failure == ConcurrencyFailure.StoreOnce && _storeConflicts++ == 0))
            {
                StoreConflictCount++;
                throw new ConcurrencyException(session.Id, 0, 1);
            }

            _pendingEvents = _durableEvents[session.Id].Concat(session.UncommittedEvents).ToList();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IDomainEvent>> GetEventStreamAsync(GameSessionId id, long fromVersion = 0, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<IDomainEvent>>(_durableEvents.GetValueOrDefault(id, []).Skip((int)fromVersion).ToArray());

        public Task<IReadOnlyList<RecordedDomainEvent>> GetRecordedEventStreamAsync(GameSessionId id, long fromVersion = 0, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<RecordedDomainEvent>>(_durableEvents.GetValueOrDefault(id, [])
                .Skip((int)fromVersion)
                .Select((domainEvent, index) => new RecordedDomainEvent(domainEvent, fromVersion + index + 1, DateTime.UnixEpoch))
                .ToArray());

        public Task CommitAsync(CancellationToken ct = default)
        {
            if (_failure == ConcurrencyFailure.CommitOnce && _commitConflicts++ == 0)
            {
                CommitConflictCount++;
                _pendingEvents = null;
                throw new ConcurrencyException("Simulated commit conflict.");
            }

            if (_pendingEvents is not null)
            {
                _durableEvents[_durableEvents.Keys.Single()] = _pendingEvents;
                _pendingEvents = null;
            }

            return Task.CompletedTask;
        }
    }
}
