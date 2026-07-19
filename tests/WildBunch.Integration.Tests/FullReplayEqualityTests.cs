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
using WildBunch.Persistence.Versioning;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Integration.Tests;

/// <summary>
/// Proves that loading a session from the full event stream (LoadFromEventsAsync)
/// produces the same state as loading from the snapshot (LoadStoreAsync + ToAggregate).
/// This is the completion gate for making event sourcing materially true:
/// the snapshot is a shortcut cache, not a requirement.
/// See ADR-0028 and the event sourcing integrity policy.
/// </summary>
public sealed class FullReplayEqualityTests : IClassFixture<PostgreSqlPersistenceFixture>
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
        services.AddSingleton<TravelDiaryDayProjector>();
        services.AddSingleton<PayloadUpcasterRegistry>(_ => new PayloadUpcasterRegistry([]));
        services.AddSingleton<PersistedPayloadLoader>(sp =>
        {
            var eventUpcasters = sp.GetRequiredService<PayloadUpcasterRegistry>();
            var serializer = sp.GetRequiredService<GameSessionJsonSerializer>();
            var diaryDayProjector = sp.GetRequiredService<TravelDiaryDayProjector>();
            return new PersistedPayloadLoader(
                eventUpcasters,
                serializer,
                diaryDayProjector,
                rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
        });
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<WildBunchDbContext>().Database.Migrate();

        return provider;
    }

    private static GameSession CreateSessionWithJourney()
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
            new DomainInventoryItem(DomainItemKind.Food, 4),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Easy, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    /// <summary>
    /// Resolves a deterministic <see cref="TravelPreview"/> for a journey from the session's
    /// current town to <paramref name="destinationId"/>. Mirrors the TravelTestFactory pattern.
    /// </summary>
    private static TravelPreview ResolveTravelPreview(GameSession session, TownId destinationId)
    {
        var resolver = new TravelResolver();
        var result = resolver.PreviewJourney(
            session.World,
            session.Player.CurrentTownId!.Value,
            destinationId,
            session.Player.Inventory,
            session.TravelRules);
        if (!result.Success || result.Preview is null)
        {
            throw new InvalidOperationException(
                $"Could not create journey preview: {result.Message}");
        }

        return result.Preview;
    }

    [Fact]
    public async Task FullReplay_PurchaseFlow_MatchesSnapshotLoad()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create and store
        var session = CreateSessionWithJourney();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Purchase
        var reloaded = await repo.GetByIdAsync(session.Id);
        var resolver = new TownStoreCatalogResolver();
        var offer = resolver.Resolve(reloaded!.World.GetTown(reloaded.Player.CurrentTownId!.Value))
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == DomainItemKind.Food);
        reloaded.Purchase(offer, 2);
        await repo.StoreAsync(reloaded);
        await uow.CommitAsync();

        // Load from snapshot (fast path — snapshot is current)
        var fromSnapshot = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromSnapshot);

        // Load from events (full replay path — force by calling LoadFromEventsAsync indirectly)
        // We need to force the full replay path. Since the snapshot is current after save,
        // GetByIdAsync will use the fast path. To test the full replay path, we need to
        // either (a) call LoadFromEventsAsync directly (it's private), or (b) corrupt the
        // snapshot version to force the full replay path.
        //
        // Option (b): set SnapshotVersion to a stale value directly in the database.
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var entity = await db.GameSessions.SingleAsync(e => e.Id == session.Id.Value);
        entity.SnapshotVersion = entity.StreamVersion - 1; // Make snapshot stale
        await db.SaveChangesAsync();

        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);

        // State equality proof — aggregate state
        Assert.Equal(fromSnapshot!.Player.Wallet.Cash, fromEvents!.Player.Wallet.Cash);
        Assert.Equal(fromSnapshot.Player.Health, fromEvents.Player.Health);
        Assert.Equal(fromSnapshot.Player.Inventory.GetQuantity(DomainItemKind.Food),
            fromEvents.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(fromSnapshot.Player.Name, fromEvents.Player.Name);
        Assert.Equal(fromSnapshot.Player.CurrentTownId, fromEvents.Player.CurrentTownId);
        Assert.Equal(fromSnapshot.Clock.Day, fromEvents.Clock.Day);
        Assert.Equal(fromSnapshot.PursuitState.Heat, fromEvents.PursuitState.Heat);
        Assert.Equal(fromSnapshot.Status, fromEvents.Status);
        Assert.Equal(fromSnapshot.GameDifficulty, fromEvents.GameDifficulty);
        Assert.Equal(fromSnapshot.SeedCode, fromEvents.SeedCode);
        Assert.Equal(fromSnapshot.Version, fromEvents.Version);

        // State equality proof — diary days (the key proof for Plan B + C)
        Assert.Equal(fromSnapshot.TravelDiaryDays.Count, fromEvents.TravelDiaryDays.Count);
    }

    [Fact]
    public async Task FullReplay_JourneyCycle_MatchesSnapshotLoad()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create session
        var session = CreateSessionWithJourney();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Start journey and advance days
        var reloaded = await repo.GetByIdAsync(session.Id);
        var preview = ResolveTravelPreview(reloaded!, new TownId("quartzsite"));
        reloaded.StartJourney(preview);

        // Force quiet days and complete the journey
        TravelJourneyStepResult result;
        do
        {
            reloaded.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            result = reloaded.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        reloaded.AcknowledgeJourneyArrival();

        await repo.StoreAsync(reloaded);
        await uow.CommitAsync();

        // Load from snapshot
        var fromSnapshot = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromSnapshot);

        // Force full replay by making snapshot stale
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var entity = await db.GameSessions.SingleAsync(e => e.Id == session.Id.Value);
        entity.SnapshotVersion = entity.StreamVersion - 1;
        await db.SaveChangesAsync();

        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);

        // Aggregate state equality
        Assert.Equal(fromSnapshot!.Player.Health, fromEvents!.Player.Health);
        Assert.Equal(fromSnapshot.Player.Wallet.Cash, fromEvents.Player.Wallet.Cash);
        Assert.Equal(fromSnapshot.Player.CurrentTownId, fromEvents.Player.CurrentTownId);
        Assert.Equal(fromSnapshot.Clock.Day, fromEvents.Clock.Day);
        Assert.Equal(fromSnapshot.PursuitState.Heat, fromEvents.PursuitState.Heat);
        Assert.Equal(fromSnapshot.Version, fromEvents.Version);

        // Diary days equality — the key proof
        Assert.Equal(fromSnapshot.TravelDiaryDays.Count, fromEvents.TravelDiaryDays.Count);
        for (var i = 0; i < fromSnapshot.TravelDiaryDays.Count; i++)
        {
            var expected = fromSnapshot.TravelDiaryDays[i];
            var actual = fromEvents.TravelDiaryDays[i];
            Assert.Equal(expected.DayNumber, actual.DayNumber);
            Assert.Equal(expected.OriginTownName, actual.OriginTownName);
            Assert.Equal(expected.DestinationTownName, actual.DestinationTownName);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.HealthDelta, actual.HealthDelta);
            Assert.Equal(expected.CurrentHealth, actual.CurrentHealth);
            Assert.Equal(expected.CurrentWallet, actual.CurrentWallet);
            Assert.Equal(expected.Entries, actual.Entries);
        }

        // Journey state equality
        Assert.Equal(fromSnapshot.CompletedJourneyHistory.Count, fromEvents.CompletedJourneyHistory.Count);
        Assert.Null(fromEvents.Journey); // Journey completed and acknowledged
    }

    [Fact]
    public async Task FullReplay_MissingSnapshot_LoadsFromEvents()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create and store
        var session = CreateSessionWithJourney();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Delete all component rows (simulate missing/corrupted snapshot)
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        await db.GameSessionComponents
            .Where(c => c.SessionId == session.Id.Value)
            .ExecuteDeleteAsync(cancellationToken: default);
        await db.SaveChangesAsync();

        // Load — should fall back to full replay
        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);
        Assert.Equal("Ranger Vale", fromEvents!.Player.Name);
        Assert.Equal(session.Version, fromEvents.Version);
    }

    /// <summary>
    /// Proves that partial component corruption (some required components missing)
    /// triggers the full replay fallback. The missing-snapshot guard checks for all
    /// 5 required components (Player, World, CaseFile, Clock, PursuitState) — if any
    /// are missing, the snapshot is considered incomplete and full replay is used.
    /// </summary>
    [Fact]
    public async Task FullReplay_PartialComponentCorruption_LoadsFromEvents()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create and store
        var session = CreateSessionWithJourney();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Delete only the Player component (simulate partial corruption)
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        await db.GameSessionComponents
            .Where(c => c.SessionId == session.Id.Value
                && c.ComponentName == GameSessionComponentNames.Player)
            .ExecuteDeleteAsync(cancellationToken: default);
        await db.SaveChangesAsync();

        // Load — should fall back to full replay because Player is missing
        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);
        Assert.Equal("Ranger Vale", fromEvents!.Player.Name);
        Assert.Equal(session.Version, fromEvents.Version);
    }

    /// <summary>
    /// Proves that a stale snapshot (SnapshotVersion != StreamVersion) triggers
    /// full replay even when all component rows are present and current. This
    /// tests the envelope-level version check at LoadAsync line 82, distinct
    /// from the component-level missing-snapshot guard.
    /// </summary>
    [Fact]
    public async Task FullReplay_StaleSnapshotWithCurrentComponents_LoadsFromEvents()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create and store
        var session = CreateSessionWithJourney();
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Corrupt the SnapshotVersion so it doesn't match StreamVersion.
        // Components are left intact — this tests the envelope-level check,
        // not the component-level missing-snapshot guard.
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        await db.GameSessions
            .Where(s => s.Id == session.Id.Value)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.SnapshotVersion, x => x.StreamVersion - 1),
                cancellationToken: default);
        await db.SaveChangesAsync();

        // Load — should fall back to full replay because SnapshotVersion != StreamVersion
        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);
        Assert.Equal("Ranger Vale", fromEvents!.Player.Name);
        Assert.Equal(session.Version, fromEvents.Version);
    }

    /// <summary>
    /// Proves that the UnrelatedCriminalLedger is correctly reconstructed by the
    /// full replay path. The constructor builds the ledger from a placeholder CaseFile
    /// (0 suspects), and Apply(CaseFileGenerated) only sets CaseFile — it does not
    /// rebuild the BountyLoop. RehydrateFromEvents must rebuild the ledger after replay.
    /// See PR #169 code review (Critical #1).
    /// </summary>
    [Fact]
    public async Task FullReplay_UnrelatedCriminalLedger_MatchesSnapshotLoad()
    {
        using var database = new PostgreSqlTestDatabase();
        var services = CreateServices(database.ConnectionString);
        using var scope = services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();

        // Create a session with a full unrelated-criminal pool (2 gang, 6 warrants = 3x)
        var session = CreateSessionWithFullLedger(gangSuspectCount: 2, unrelatedWarrantCount: 6);
        await repo.StoreAsync(session);
        await uow.CommitAsync();
        session.MarkEventsCommitted();

        // Load from snapshot (fast path)
        var fromSnapshot = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromSnapshot);

        // Force full replay by making snapshot stale
        await using var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        var entity = await db.GameSessions.SingleAsync(e => e.Id == session.Id.Value);
        entity.SnapshotVersion = entity.StreamVersion - 1;
        await db.SaveChangesAsync();

        var fromEvents = await repo.GetByIdAsync(session.Id);
        Assert.NotNull(fromEvents);

        // The ledger must match — not a no-op (0, 0)
        Assert.Equal(fromSnapshot!.UnrelatedCriminalLedger.GangMembersAvailable, fromEvents!.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.Equal(fromSnapshot.UnrelatedCriminalLedger.PoolSize, fromEvents.UnrelatedCriminalLedger.PoolSize);
        Assert.NotEqual(0, fromEvents.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.NotEqual(0, fromEvents.UnrelatedCriminalLedger.PoolSize);
    }

    private static GameSession CreateSessionWithFullLedger(int gangSuspectCount, int unrelatedWarrantCount)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, pinecross.Id, TrailRisk.Low) });

        var suspects = Enumerable.Range(1, gangSuspectCount)
            .Select(i => new Suspect(
                new SuspectId($"suspect-{i}"),
                $"Gang Member {i}",
                SuspectTraits.Empty,
                SuspectStatus.AtLarge))
            .ToArray();

        var knownWarrants = Enumerable.Range(1, gangSuspectCount)
            .Select(i => new Warrant(
                new WarrantId($"warrant-gang-{i}"),
                $"Gang Member {i}",
                new WarrantTerms(
                    WarrantDisposition.DeadOrAlive,
                    500m,
                    new[] { $"Alias {i}" },
                    new[] { $"Feature {i}" },
                    "Dodge City Marshal",
                    i == 1 ? InvestigationTargetKind.TrueCulprit : InvestigationTargetKind.GangMember,
                    Array.Empty<OutlawGangId>(),
                    null),
                "Wanted for gang crimes."))
            .ToArray();

        var publicWarrants = Enumerable.Range(1, unrelatedWarrantCount)
            .Select(i => new Warrant(
                new WarrantId($"warrant-unrelated-{i}"),
                $"Outlaw {i}",
                new WarrantTerms(
                    WarrantDisposition.AliveOnly,
                    100m,
                    new[] { $"Outlaw Alias {i}" },
                    new[] { $"Outlaw Feature {i}" },
                    $"Sheriff {i}",
                    InvestigationTargetKind.UnrelatedWantedCriminal,
                    Array.Empty<OutlawGangId>(),
                    null),
                "Wanted for unrelated territorial offenses."))
            .ToArray();

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: knownWarrants,
            publicWarrants: publicWarrants);

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart();
        return session;
    }
}
