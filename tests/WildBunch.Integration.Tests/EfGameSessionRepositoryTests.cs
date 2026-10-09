using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Models;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using DomainCanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainInventoryCapabilityResolver = WildBunch.Domain.Inventory.InventoryCapabilityResolver;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;
using WildBunch.Persistence;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Data.Common;

namespace WildBunch.Integration.Tests;

public sealed class EfGameSessionRepositoryTests
{
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);

    [Fact]
    public async Task LegacyWorldGenerated_LoadsFromPersistedEvents_AndCurrentWritesUseV2()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = CreateSession(includeKnownClue: true);
        var expectedPlayerName = session.Player.Name;
        var originalCaseFile = session.CaseFile;
        PurchaseFood(session, 1);
        await PersistAsync(writer, writerUnitOfWork, session);

        var legacyState = await DowngradeWorldGeneratedAndStalePlayerCacheAsync(fixture, session.Id);
        Assert.True(legacyState.SnapshotVersion < legacyState.StreamVersion);
        Assert.Equal("Stale Player Cache", JsonNode.Parse(legacyState.StalePlayerPayloadJson)!["name"]!.GetValue<string>());

        var repository = CreateRepository(fixture, out var unitOfWork);
        var loaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(loaded);
        Assert.Equal(expectedPlayerName, loaded!.Player.Name);
        Assert.Equal(originalCaseFile.TrueCulpritId, loaded!.CaseFile.TrueCulpritId);
        Assert.Equal(originalCaseFile.Suspects.Select(s => s.Id), loaded.CaseFile.Suspects.Select(s => s.Id));
        Assert.Equal(originalCaseFile.KnownClues.Select(clue => clue.Id), loaded.CaseFile.KnownClues.Select(clue => clue.Id));

        var worldEventAndCaseFile = await ReadWorldAndCaseFileEventsAsync(fixture, session.Id);
        Assert.Equal(1, worldEventAndCaseFile.World.SchemaVersion);
        Assert.Equal(legacyState.LegacyWorldPayloadJson, worldEventAndCaseFile.World.PayloadJson);
        Assert.True(worldEventAndCaseFile.CaseFile.Sequence > worldEventAndCaseFile.World.Sequence);

        var offer = new TownStoreCatalogResolver()
            .Resolve(loaded.World.GetTown(loaded.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.ItemKind == DomainItemKind.Food);
        Assert.True(loaded.Purchase(offer, 1).Success);
        var repairedStreamVersion = loaded.Version;
        Assert.True(repairedStreamVersion > legacyState.StreamVersion);
        await PersistAsync(repository, unitOfWork, loaded);

        var fresh = await CreateRepository(fixture, out _).GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        Assert.Equal(expectedPlayerName, fresh!.Player.Name);
        Assert.Equal(originalCaseFile.TrueCulpritId, fresh.CaseFile.TrueCulpritId);
        Assert.Equal(repairedStreamVersion, fresh.Version);

        await using var verificationContext = fixture.CreateContext();
        var persistedPlayerPayload = await verificationContext.GameSessionComponents.AsNoTracking()
            .Where(component => component.SessionId == session.Id.Value && component.ComponentName == "player")
            .Select(component => component.PayloadJson)
            .SingleAsync();
        var persistedEnvelope = await verificationContext.GameSessions.AsNoTracking()
            .Where(envelope => envelope.Id == session.Id.Value)
            .Select(envelope => new { envelope.SnapshotVersion, envelope.StreamVersion })
            .SingleAsync();
        var persistedWorldAndCaseFile = await ReadWorldAndCaseFileEventsAsync(fixture, session.Id);
        Assert.Equal(expectedPlayerName, JsonNode.Parse(persistedPlayerPayload)!["name"]!.GetValue<string>());
        Assert.Equal(persistedEnvelope.StreamVersion, persistedEnvelope.SnapshotVersion);
        Assert.Equal(repairedStreamVersion, persistedEnvelope.StreamVersion);
        Assert.Equal(legacyState.LegacyWorldPayloadJson, persistedWorldAndCaseFile.World.PayloadJson);
        Assert.Equal(1, persistedWorldAndCaseFile.World.SchemaVersion);
        Assert.True(persistedWorldAndCaseFile.CaseFile.Sequence > persistedWorldAndCaseFile.World.Sequence);
        Assert.Equal("StoreItemPurchased", await verificationContext.StoredEvents.AsNoTracking()
            .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
            .OrderByDescending(storedEvent => storedEvent.Sequence)
            .Select(storedEvent => storedEvent.EventType)
            .FirstAsync());
    }

    [Fact]
    public async Task ReadModels_LegacyWorldGeneratedEventUpcastsThroughProductionLoaderWithoutWriteback()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var unitOfWork);
        var session = CreateSession(includeKnownClue: true);
        var expectedPlayerName = session.Player.Name;
        var expectedOpeningLead = session.CaseFile.OpeningLead.Description;
        var expectedKnownClueIds = session.CaseFile.KnownClues.Select(clue => clue.Id).ToArray();
        PurchaseFood(session, 1);
        await PersistAsync(writer, unitOfWork, session);

        var legacyState = await DowngradeWorldGeneratedAndStalePlayerCacheAsync(fixture, session.Id);

        var playerRead = await new EfGameSessionReadRepository(
            fixture.CreateContext(),
            CreateReadStoreLoader()).GetByIdAsync(session.Id);
        var journalRead = await new EfGameJournalReadRepository(
            fixture.CreateContext(),
            CreateReadStoreLoader()).GetByIdAsync(session.Id);

        Assert.NotNull(playerRead);
        Assert.Equal(expectedPlayerName, playerRead!.Player.Name);
        Assert.Equal(expectedOpeningLead, playerRead.CaseFile.OpeningLead.Description);
        Assert.Equal(expectedKnownClueIds, playerRead.CaseFile.KnownClues.Select(clue => clue.Id));
        Assert.NotNull(journalRead);
        Assert.Equal(expectedOpeningLead, journalRead!.OpeningLead);
        Assert.Equal(expectedKnownClueIds, journalRead.KnownClues.Select(clue => clue.Id));

        await using var verificationContext = fixture.CreateContext();
        var persistedPlayerPayload = await verificationContext.GameSessionComponents.AsNoTracking()
            .Where(component => component.SessionId == session.Id.Value && component.ComponentName == "player")
            .Select(component => component.PayloadJson)
            .SingleAsync();
        var persistedEnvelope = await verificationContext.GameSessions.AsNoTracking()
            .Where(envelope => envelope.Id == session.Id.Value)
            .Select(envelope => new { envelope.SnapshotVersion, envelope.StreamVersion })
            .SingleAsync();
        var persistedWorld = await verificationContext.StoredEvents.AsNoTracking()
            .Where(storedEvent => storedEvent.StreamId == session.Id.Value && storedEvent.EventType == "WorldGenerated")
            .Select(storedEvent => new { storedEvent.PayloadJson, storedEvent.SchemaVersion })
            .SingleAsync();
        Assert.Equal(legacyState.StalePlayerPayloadJson, persistedPlayerPayload);
        Assert.Equal(legacyState.SnapshotVersion, persistedEnvelope.SnapshotVersion);
        Assert.Equal(legacyState.StreamVersion, persistedEnvelope.StreamVersion);
        Assert.Equal(legacyState.LegacyWorldPayloadJson, persistedWorld.PayloadJson);
        Assert.Equal(1, persistedWorld.SchemaVersion);
    }

    [Fact]
    public async Task LegacyWorldGenerated_WithoutCaseFileEventFailsClosed()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var unitOfWork);
        var session = CreateSession(includeKnownClue: true);
        PurchaseFood(session, 1);
        await PersistAsync(writer, unitOfWork, session);

        await DowngradeWorldGeneratedAndStalePlayerCacheAsync(fixture, session.Id);
        await using (var context = fixture.CreateContext())
        {
            var caseFileEvent = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == "CaseFileGenerated");
            context.StoredEvents.Remove(caseFileEvent);
            await context.SaveChangesAsync();
        }

        const string expectedMessage = "Cannot replay a legacy WorldGenerated event without a CaseFileGenerated event.";
        var commandException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateRepository(fixture, out _).GetByIdAsync(session.Id));
        Assert.Equal(expectedMessage, commandException.Message);

        var readException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new EfGameSessionReadRepository(fixture.CreateContext(), CreateReadStoreLoader()).GetByIdAsync(session.Id));
        Assert.Equal(expectedMessage, readException.Message);

        var journalException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new EfGameJournalReadRepository(fixture.CreateContext(), CreateReadStoreLoader()).GetByIdAsync(session.Id));
        Assert.Equal(expectedMessage, journalException.Message);
    }

    [Fact]
    public async Task SaveAndLoadNewSessionRoundTripsThroughPostgreSql()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateSession();
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.AvailableInTown);

        await PersistAsync(repository, unitOfWork, session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(session.Id, reloaded!.Id);
        Assert.Equal(session.Player.Name, reloaded.Player.Name);
        Assert.Equal(session.Player.CurrentTownId!.Value, reloaded.Player.CurrentTownId);
        Assert.Equal(session.Player.Wallet.Cash, reloaded.Player.Wallet.Cash);
        Assert.Equal(session.Player.Inventory.Items.Count, reloaded.Player.Inventory.Items.Count);
        Assert.Equal(session.Player.Inventory.GetHorseState(), reloaded.Player.Inventory.GetHorseState());
        Assert.Equal(session.Player.Inventory.GetCanteenState(), reloaded.Player.Inventory.GetCanteenState());
        Assert.Equal(session.World.Trails.First().RideDayDistance, reloaded.World.Trails.First().RideDayDistance);
        Assert.Equal(session.Status, reloaded.Status);
        Assert.Equal(GameSessionLogProjection.Project(session).Count, GameSessionLogProjection.Project(reloaded).Count);
        Assert.Equal(session.CaseFile.OpeningLead.Description, reloaded.CaseFile.OpeningLead.Description);
        Assert.Equal(session.CaseFile.KillerReleaseState.IsReleased, reloaded.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(session.CaseFile.KillerReleaseState.Progress, reloaded.CaseFile.KillerReleaseState.Progress);
        Assert.Equal(session.CaseFile.KillerReleaseState.RequiredPublicClues, reloaded.CaseFile.KillerReleaseState.RequiredPublicClues);
        Assert.Equal(session.CaseFile.DiscoveredSuspectIds, reloaded.CaseFile.DiscoveredSuspectIds);
        Assert.Equal(session.CaseFile.Suspects[0].Profile.Aliases.Count, reloaded.CaseFile.Suspects[0].Profile.Aliases.Count);
        Assert.Equal(WantedSuspectPresenceState.AvailableInTown, reloaded.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
    }

    [Fact]
    public async Task SaveAndLoadWithSeedCode_RetainsSeedCode()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var seedCode = "test-seed-code-event-sourced-12345";
        var session = CreateSessionWithSeedCode(seedCode);

        await PersistAsync(repository, unitOfWork, session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        // Seed code is restored from the start flow events via event replay
        Assert.Equal(seedCode, reloaded!.SeedCode);
    }

    [Fact]
    public async Task BoringEntropy_SeedAndSaltMayHaveSameValueButReportedSeparately()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var seedCode = "same-value-for-testing";
        var session = CreateSessionWithSeedCode(seedCode, GameEntropy.Boring, SaltSource.CreateFixed(seedCode));

        await PersistAsync(repository, unitOfWork, session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        // Seed code and salt may have the same value (for Boring entropy)
        // but they are separate concepts and reported separately
        Assert.Equal(seedCode, reloaded!.SeedCode);
        Assert.Equal(WildBunch.Domain.Game.SaltSourceMode.Fixed, reloaded.SaltSource.Mode);
        Assert.Equal(seedCode, reloaded.SaltSource.Salt); // Boring uses seed as salt
    }

    [Fact]
    public async Task ClassicEntropy_SeedRetainedWhileSaltRuntime()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var seedCode = "test-seed-code-classic-entropy";
        var session = CreateSessionWithSeedCode(seedCode, GameEntropy.Classic, SaltSource.CreateRuntime());

        await PersistAsync(repository, unitOfWork, session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        // Seed code is retained for debugging
        Assert.Equal(seedCode, reloaded!.SeedCode);
        // Salt is runtime (not seed-derived) for Classic entropy
        Assert.Equal(WildBunch.Domain.Game.SaltSourceMode.Runtime, reloaded.SaltSource.Mode);
        Assert.NotEqual(seedCode, reloaded.SaltSource.Salt);
    }

    [Fact]
    public async Task MissingSaltSourceComponent_RehydratesRecordedSaltFromWorldGeneratedEvent()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writeRepository = CreateRepository(fixture, out var unitOfWork);
        var expectedSalt = SaltSource.CreateFixed("recorded-classic-salt");
        var session = CreateSessionWithSeedCode(
            "recorded-seed",
            GameEntropy.Classic,
            expectedSalt);

        await PersistAsync(writeRepository, unitOfWork, session);
        var recordedEvents = await writeRepository.GetEventStreamAsync(session.Id);
        Assert.Equal(expectedSalt, Assert.IsType<WorldGenerated>(recordedEvents.OfType<WorldGenerated>().Single()).SaltSource);

        await using (var damageContext = fixture.CreateContext())
        {
            await damageContext.GameSessionComponents
                .Where(component => component.SessionId == session.Id.Value
                    && component.ComponentName == GameSessionComponentNames.SaltSource)
                .ExecuteDeleteAsync();
        }

        var freshRepository = CreateRepository(fixture, out _);
        var reloaded = await freshRepository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(expectedSalt, reloaded!.SaltSource);
    }

    [Fact]
    public async Task SaveAndLoadEasyTravelSessionRetainsGameDifficulty()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateEasySession();

        await PersistAsync(repository, unitOfWork, session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(GameDifficulty.Easy, reloaded!.GameDifficulty);
        Assert.Equal(10, reloaded.Player.Inventory.GetCanteenState()!.Capacity);
        Assert.True(reloaded.Player.Inventory.GetHorseState()!.CanProvideMountedTravelFor(TravelRulesProfile.For(GameDifficulty.Easy)));
    }

    [Fact]
    public async Task SaveAfterTravelUpdatesReloadedState()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var resolver = new TravelResolver();
        var session = CreateSession();

        await PersistAsync(repository, unitOfWork, session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId!.Value, new TownId("holloway"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        loaded.AdvanceJourneyDay();

        await PersistAsync(repository, unitOfWork, loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(new TownId("dustvale"), reloaded!.Player.CurrentTownId);
        Assert.Equal(loaded!.Player.Wallet.Cash, reloaded.Player.Wallet.Cash);
        Assert.True(new DomainInventoryCapabilityResolver().Resolve(reloaded.Player.Inventory).MountedTravelAvailable);
        Assert.Equal(2, reloaded.Clock.Day);
        Assert.Equal(0, reloaded.Clock.Turn);
        Assert.Equal(0, reloaded.PursuitState.Heat);
        Assert.NotNull(reloaded.Journey);
        Assert.Equal(1, reloaded.Journey!.RemainingDays);
        Assert.Equal(1m, reloaded.Journey.RemainingRideDayDistance);
        Assert.Equal(2, reloaded.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(2, reloaded.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
        Assert.Equal(new DomainHorseTravelState(0, 0, 1), reloaded.Player.Inventory.GetHorseState());
        Assert.Equal(1, reloaded.Player.Inventory.GetCanteenState()!.Charges);
        Assert.Contains(GameSessionLogProjection.Project(reloaded), entry => entry.Kind == GameLogEntryKind.Travel);
        Assert.Equal(TrailTerrain.Hills, reloaded.World.Trails.Single(trail => trail.Id == new TrailId("trail-2")).Terrain);
        Assert.Equal(WaterFeature.River, reloaded.World.Trails.Single(trail => trail.Id == new TrailId("trail-2")).WaterFeature);
    }

    [Fact]
    public async Task SaveAfterInterruptedTravelRoundTripsPendingEncounterState()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var resolver = new TravelResolver();
        var session = CreateHighRiskSession();

        await PersistAsync(repository, unitOfWork, session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId!.Value, new TownId("dryfork"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        loaded.AdvanceJourneyDay();

        await PersistAsync(repository, unitOfWork, loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Interrupted, reloaded!.Journey!.Status);
        Assert.Equal(1, reloaded.Journey.DaysTravelled);
        Assert.NotNull(reloaded.Journey.PendingEncounter);
        Assert.Equal("foe", reloaded.Journey.PendingEncounter!.Kind);
        Assert.Equal(3, reloaded.Journey.PendingEncounter.Choices.Count);
        Assert.NotNull(reloaded.Journey.PendingEncounter.FoeProfile);
        Assert.Equal(0, reloaded.Journey.PendingEncounter.ResolutionAttempts);
        Assert.NotNull(reloaded.Journey.PendingEncounter.HiddenState);
        Assert.Equal(0, reloaded.Journey.PendingEncounter.HiddenState!.BribeOffersMade);
        Assert.Equal(0m, reloaded.Journey.PendingEncounter.HiddenState.CumulativeBribePaid);
        Assert.False(reloaded.Journey.PendingEncounter.HiddenState.BribeLockedOut);
        Assert.Equal(0, reloaded.Journey.PendingEncounter.HiddenState.ChaseFatigue);
        Assert.Equal(0, reloaded.Journey.PendingEncounter.HiddenState.Annoyance);
        Assert.False(reloaded.Journey.PendingEncounter.HiddenState.Shaken);
        var loadedJourney = loaded.Journey!;
        var loadedEncounter = loadedJourney.PendingEncounter!;
        var reloadedEncounter = reloaded.Journey.PendingEncounter!;
        Assert.Equal(loadedEncounter.FoeProfile, reloadedEncounter.FoeProfile);

        var dtoPayload = JsonSerializer.Serialize(GameSessionMapper.ToDto(reloaded));
        Assert.DoesNotContain("foeProfile", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("minimumBribe", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fightStrength", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("resolutionAttempts", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bribeOffersMade", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cumulativeBribePaid", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bribeLockedOut", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chaseFatigue", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("annoyance", dtoPayload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("shaken", dtoPayload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAfterPendingFoeEncounterWithHiddenPressureRoundTripsTheHiddenState()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var resolver = new TravelResolver();
        var session = CreateHighRiskSession();

        await PersistAsync(repository, unitOfWork, session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId!.Value, new TownId("dryfork"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        loaded.AdvanceJourneyDay();

        var pendingEncounter = loaded.Journey!.PendingEncounter!;
        var mutatedEncounter = pendingEncounter.WithHiddenState(new JourneyEncounterHiddenState(BribeOffersMade: 1, CumulativeBribePaid: 5m, ChaseFatigue: 2, Annoyance: 1, Shaken: true));
        loaded.Journey.UpdatePendingEncounter(mutatedEncounter);

        Assert.NotNull(loaded.Journey.PendingEncounter);
        Assert.Equal(1, loaded.Journey.PendingEncounter!.HiddenState!.BribeOffersMade);
        Assert.Equal(5m, loaded.Journey.PendingEncounter.HiddenState.CumulativeBribePaid);
        Assert.True(loaded.Journey.PendingEncounter.HiddenState.Shaken);

        await PersistAsync(repository, unitOfWork, loaded);

        await using (var context = fixture.CreateContext())
        {
            context.GameSessionComponents.Add(new GameSessionComponentEntity
            {
                SessionId = session.Id.Value,
                ComponentName = "completedJourneyHistory",
                ComponentVersion = ProjectionVersions.ForComponent("completedJourneyHistory"),
                PayloadJson = "null",
                UpdatedAtUtc = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.NotNull(reloaded!.Journey!.PendingEncounter);
        Assert.Equal(1, reloaded.Journey.PendingEncounter!.HiddenState!.BribeOffersMade);
        Assert.Equal(5m, reloaded.Journey.PendingEncounter.HiddenState.CumulativeBribePaid);
        Assert.Equal(1, reloaded.Journey.PendingEncounter.HiddenState.Annoyance);
        Assert.Equal(2, reloaded.Journey.PendingEncounter.HiddenState.ChaseFatigue);
        Assert.True(reloaded.Journey.PendingEncounter.HiddenState.Shaken);
    }

    [Fact]
    public async Task SaveAfterLuckyTrailEventRoundTripsWalletGain()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var resolver = new TravelResolver();
        var session = CreateLuckySession();

        await PersistAsync(repository, unitOfWork, session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId!.Value, new TownId("silvercreek"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await PersistAsync(repository, unitOfWork, loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(loaded!.Player.Wallet.Cash, reloaded.Player.Wallet.Cash);
        Assert.NotNull(reloaded.Journey);
        Assert.Equal(1, reloaded.Journey!.RemainingDays);
        Assert.Equal(0, reloaded.Journey.DelayDays);
        Assert.Equal(2, reloaded.Clock.Day);
        Assert.Equal(0, reloaded.Clock.Turn);
    }

    [Fact]
    public async Task SaveAndLoadTravelDiaryRoundTripsStructuredDiaryState()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var resolver = new TravelResolver();
        var session = CreateDiarySession();

        await PersistAsync(repository, unitOfWork, session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId!.Value, new TownId("openpass"), loaded.Player.Inventory, loaded.TravelRules);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await PersistAsync(repository, unitOfWork, loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        var dto = GameSessionMapper.ToDto(reloaded!);
        Assert.NotNull(dto.TravelDiary);
        var diaryDay = Assert.Single(dto.TravelDiary!.Days);
        Assert.Contains(diaryDay.Entries, entry => entry.StartsWith("I ", StringComparison.Ordinal));
        Assert.DoesNotContain(diaryDay.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveAfterDryTravelRoundTripsHorseAndCanteenState()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var resolver = new TravelResolver();
        var session = CreateDryTravelSession();

        await PersistAsync(repository, unitOfWork, session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId!.Value, new TownId("dryridge"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        loaded.AdvanceJourneyDay();

        await PersistAsync(repository, unitOfWork, loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(new TownId("dustvale"), reloaded!.Player.CurrentTownId);
        Assert.Equal(2, reloaded.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(0, reloaded.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
        Assert.Equal(new DomainHorseTravelState(0, 0, 1), reloaded.Player.Inventory.GetHorseState());
        Assert.Equal(8, reloaded.Player.Inventory.GetCanteenState()!.Charges);
        Assert.Equal(5m, reloaded.World.Trails.Single(trail => trail.Id == new TrailId("trail-1")).RideDayDistance);
    }

    [Fact]
    public async Task SaveAfterHorseLossFallbackRoundTripsFootTravelAndHorseState()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var resolver = new TravelResolver();
        var session = CreateHorseLossFallbackSession();

        await PersistAsync(repository, unitOfWork, session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId!.Value, new TownId("midway"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        loaded.AdvanceJourneyDay();

        await PersistAsync(repository, unitOfWork, loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.NotNull(reloaded!.Journey);
        Assert.Equal(WildBunch.Domain.Travel.TravelMode.Foot, reloaded.Journey!.TravelMode);
        Assert.Equal(1, reloaded.Journey.RemainingDays);
        Assert.Equal(new DomainHorseTravelState(0, 0, 2), reloaded.Player.Inventory.GetHorseState());
        Assert.Contains(GameSessionLogProjection.Project(reloaded), entry => entry.Kind == GameLogEntryKind.Travel && entry.Message.Contains("went lame", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveAfterJourneyAcknowledgementRoundTripsActiveSequenceAndCompletedHistory()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateJourneyHistorySession();

        await PersistAsync(repository, unitOfWork, session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var firstPreview = CreateJourneyPreview(loaded!.Player.CurrentTownId!.Value, new TownId("openpass"), "Pinecross", "Open Pass");
        loaded.StartJourney(firstPreview);
        Assert.Equal(1, loaded.Journey!.JourneySequence);

        await PersistAsync(repository, unitOfWork, loaded);
        var activeReload = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(activeReload);
        Assert.NotNull(activeReload!.Journey);
        Assert.Equal(1, activeReload.Journey!.JourneySequence);

        loaded = activeReload;
        loaded.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        var travelDay = loaded.AdvanceJourneyDay();
        Assert.True(travelDay.Success);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Completed, loaded.Journey!.Status);
        Assert.True(loaded.AcknowledgeJourneyArrival().Success);

        await PersistAsync(repository, unitOfWork, loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Null(reloaded!.Journey);
        Assert.Single(reloaded.CompletedJourneyHistory);
        Assert.Equal(1, reloaded.CompletedJourneyHistory[0].JourneySequence);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Completed, reloaded.CompletedJourneyHistory[0].Status);

        var secondPreview = CreateJourneyPreview(reloaded.Player.CurrentTownId!.Value, new TownId("dryfork"), "Open Pass", "Dry Fork");
        reloaded.StartJourney(secondPreview);
        Assert.Equal(2, reloaded.Journey!.JourneySequence);

        await PersistAsync(repository, unitOfWork, reloaded);
        var secondReload = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(secondReload);
        Assert.NotNull(secondReload!.Journey);
        Assert.Equal(2, secondReload.Journey!.JourneySequence);
        Assert.Single(secondReload.CompletedJourneyHistory);
        Assert.Equal(1, secondReload.CompletedJourneyHistory[0].JourneySequence);
    }

    [Theory]
    [InlineData("missing-row")]
    [InlineData("null-root")]
    [InlineData("empty-array")]
    public async Task CommandLoad_CompletedJourneyHistoryCacheRecoversFromEventsWithoutWritingBack(string damage)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var unitOfWork);
        var session = CreateJourneyHistorySession();
        await PersistAsync(writer, unitOfWork, session);

        var active = await writer.GetByIdAsync(session.Id);
        Assert.NotNull(active);
        Assert.True(active!.StartJourney(CreateJourneyPreview(
            active.Player.CurrentTownId!.Value,
            new TownId("openpass"),
            "Pinecross",
            "Open Pass")).Success);
        active.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        Assert.True(active.AdvanceJourneyDay().Success);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Completed, active.Journey!.Status);
        var expectedSnapshot = active.Journey.ToSnapshot();
        Assert.True(active.AcknowledgeJourneyArrival().Success);
        var acknowledgement = Assert.Single(active.AllEvents.OfType<JourneyArrivalAcknowledged>());
        Assert.Equal(expectedSnapshot.JourneySequence, acknowledgement.JourneySequence);
        Assert.Equal(expectedSnapshot.DestinationTownId, acknowledgement.JourneySnapshot.DestinationTownId);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Completed, acknowledgement.JourneySnapshot.Status);
        await PersistAsync(writer, unitOfWork, active);

        int originalComponentVersion;
        string? damagedPayload = null;
        (long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount) expectedEnvelope;
        (long Sequence, Guid EventId, string EventType, string PayloadJson, int SchemaVersion)[] expectedEvents;
        (int Sequence, string PayloadJson, int SchemaVersion)[] expectedDiaryRows;
        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "completedJourneyHistory");
            originalComponentVersion = component.ComponentVersion;
            if (damage == "missing-row")
            {
                context.GameSessionComponents.Remove(component);
            }
            else
            {
                component.PayloadJson = damage == "null-root" ? "null" : "[]";
                damagedPayload = component.PayloadJson;
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            expectedEnvelope = (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount);
            expectedEvents = (await context.StoredEvents.AsNoTracking()
                    .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                    .OrderBy(storedEvent => storedEvent.Sequence)
                    .Select(storedEvent => new
                    {
                        storedEvent.Sequence,
                        storedEvent.EventId,
                        storedEvent.EventType,
                        storedEvent.PayloadJson,
                        storedEvent.SchemaVersion
                    })
                    .ToArrayAsync())
                .Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.SchemaVersion))
                .ToArray();
            expectedDiaryRows = (await context.GameSessionDiaryDays.AsNoTracking()
                    .Where(day => day.SessionId == session.Id.Value)
                    .OrderBy(day => day.Sequence)
                    .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                    .ToArrayAsync())
                .Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion))
                .ToArray();
            await context.SaveChangesAsync();
        }

        var recoveringRepository = CreateRepository(fixture, out var recoveryUnitOfWork);
        var recovered = await recoveringRepository.GetByIdAsync(session.Id);
        Assert.NotNull(recovered);
        Assert.Null(recovered!.Journey);
        Assert.True(recovered.StartJourney(CreateJourneyPreview(
            recovered.Player.CurrentTownId!.Value,
            new TownId("dryfork"),
            "Open Pass",
            "Dry Fork")).Success);
        Assert.Equal(2, recovered.Journey!.JourneySequence);
        var restoredSnapshot = Assert.Single(recovered.CompletedJourneyHistory);
        Assert.Equal(expectedSnapshot.JourneySequence, restoredSnapshot.JourneySequence);
        Assert.Equal(expectedSnapshot.OriginTownId, restoredSnapshot.OriginTownId);
        Assert.Equal(expectedSnapshot.DestinationTownId, restoredSnapshot.DestinationTownId);
        Assert.Equal(WildBunch.Domain.Travel.JourneyStatus.Completed, restoredSnapshot.Status);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.AsNoTracking().SingleOrDefaultAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "completedJourneyHistory");
            if (damage == "missing-row")
            {
                Assert.Null(component);
            }
            else
            {
                Assert.NotNull(component);
                Assert.Equal(originalComponentVersion, component!.ComponentVersion);
                Assert.Equal(damagedPayload, component.PayloadJson);
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            Assert.Equal(expectedEnvelope, (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount));
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            Assert.Equal(expectedEvents, events.Select(storedEvent => (
                storedEvent.Sequence,
                storedEvent.EventId,
                storedEvent.EventType,
                storedEvent.PayloadJson,
                storedEvent.SchemaVersion)).ToArray());
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                .ToArrayAsync();
            Assert.Equal(expectedDiaryRows, diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }

        await PersistAsync(recoveringRepository, recoveryUnitOfWork, recovered);

        var fresh = await CreateRepository(fixture, out _).GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        Assert.Single(fresh!.CompletedJourneyHistory);
        Assert.Equal(1, fresh.CompletedJourneyHistory[0].JourneySequence);
        Assert.Equal(2, fresh.Journey!.JourneySequence);
        await using var repairedContext = fixture.CreateContext();
        var repairedComponent = await repairedContext.GameSessionComponents.AsNoTracking().SingleAsync(candidate =>
            candidate.SessionId == session.Id.Value && candidate.ComponentName == "completedJourneyHistory");
        Assert.Equal(ProjectionVersions.ForComponent("completedJourneyHistory"), repairedComponent.ComponentVersion);
    }

    [Fact]
    public async Task DamagedCompletedJourneyHistoryCacheDoesNotHideInvalidAcknowledgementEvent()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var unitOfWork);
        var session = CreateJourneyHistorySession();
        await PersistAsync(writer, unitOfWork, session);

        var active = await writer.GetByIdAsync(session.Id);
        Assert.NotNull(active);
        Assert.True(active!.StartJourney(CreateJourneyPreview(
            active.Player.CurrentTownId!.Value,
            new TownId("openpass"),
            "Pinecross",
            "Open Pass")).Success);
        active.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        Assert.True(active.AdvanceJourneyDay().Success);
        Assert.True(active.AcknowledgeJourneyArrival().Success);
        await PersistAsync(writer, unitOfWork, active);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "completedJourneyHistory");
            context.GameSessionComponents.Remove(component);
            var acknowledgement = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == nameof(JourneyArrivalAcknowledged));
            acknowledgement.PayloadJson = "{}";
            await context.SaveChangesAsync();
        }

        var recoveringRepository = CreateRepository(fixture, out _);
        await Assert.ThrowsAsync<JsonException>(() => recoveringRepository.GetByIdAsync(session.Id));
    }

    [Fact]
    public async Task ReadRepositoriesProjectComposedSessionAndJournalViews()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var commandRepository = CreateRepository(fixture, out var unitOfWork);
        var travelResolver = new TravelResolver();
        var session = CreateSession();

        await PersistAsync(commandRepository, unitOfWork, session);
        var loaded = await commandRepository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = travelResolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId!.Value, new TownId("holloway"), loaded.Player.Inventory);
        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await PersistAsync(commandRepository, unitOfWork, loaded);

        var serializer = new GameSessionJsonSerializer();
        var upcasters = DependencyInjection.CreateDefaultUpcasters();
        var registry = new PayloadUpcasterRegistry(upcasters);
        var payloadLoader = new PersistedPayloadLoader(
            registry,
            serializer,
            new TravelDiaryDayProjector(),
            rebuildSessionFromEvents: _ => throw new InvalidOperationException("Rebuild not expected in greenfield tests."));
        var readStoreLoader = new GameSessionReadStoreLoader(payloadLoader, serializer);
        var readRepository = new EfGameSessionReadRepository(fixture.CreateContext(), readStoreLoader);
        var journalRepository = new EfGameJournalReadRepository(fixture.CreateContext(), readStoreLoader);

        var sessionRead = await readRepository.GetByIdAsync(session.Id);
        var journalRead = await journalRepository.GetByIdAsync(session.Id, take: 2);

        Assert.NotNull(sessionRead);
        Assert.Equal(loaded!.Status, sessionRead!.Status);
        Assert.Equal(loaded.GameDifficulty, sessionRead.GameDifficulty);
        Assert.Equal(loaded.Player.CurrentTownId!.Value, sessionRead.Player.CurrentTownId);
        Assert.Equal(loaded.Player.Wallet.Cash, sessionRead.Player.Wallet.Cash);
        Assert.NotNull(sessionRead.Journey);
        Assert.Equal(loaded.Journey!.Status, sessionRead.Journey!.Status);
        Assert.Equal(loaded.TravelDiaryDays.Count, sessionRead.TravelDiaryDays.Count);
        Assert.Equal(GameSessionLogProjection.Project(loaded).Count, sessionRead.LogEntries.Count);

        Assert.NotNull(journalRead);
        Assert.Equal(loaded.Id.Value, journalRead!.SessionId);
        Assert.Equal(loaded.Clock.Day, journalRead.Day);
        Assert.Equal(loaded.Clock.Turn, journalRead.Turn);
        Assert.Equal(2, journalRead.LogEntries.Count);
        Assert.Equal(GameSessionLogProjection.Project(loaded).Take(2).Select(entry => entry.Message), journalRead.LogEntries.Select(entry => entry.Message));
        Assert.DoesNotContain("true culprit", System.Text.Json.JsonSerializer.Serialize(journalRead), StringComparison.OrdinalIgnoreCase);

        await using var verificationContext = fixture.CreateContext();
        // After BUNCH-86, log entries are derived from the event stream via
        // JournalLogProjector, not stored in a GameSessionLogEntries table.
        // Verify the event stream has events rather than checking a log table.
        Assert.True(await verificationContext.StoredEvents.AnyAsync(e => e.StreamId == session.Id.Value));
        Assert.Equal(loaded.TravelDiaryDays.Count, await verificationContext.GameSessionDiaryDays.CountAsync(day => day.SessionId == session.Id.Value));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ReadModel_PartialDiaryDayCacheRebuildsFromEventsWithoutWritingBack(int missingSequence)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var commandRepository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateJourneyHistorySession();
        await PersistAsync(commandRepository, unitOfWork, session);

        var active = await commandRepository.GetByIdAsync(session.Id);
        Assert.NotNull(active);
        var preview = CreateJourneyPreview(active!.Player.CurrentTownId!.Value, new TownId("openpass"), "Pinecross", "Open Pass");
        preview = preview with
        {
            RideDayDistance = 4m,
            RemainingRideDayDistance = 4m,
            BaselineRideDays = 4,
            ExpectedDays = 4,
            RemainingDays = 4,
            RouteProfile = preview.RouteProfile with { RideDayDistance = 4m }
        };
        Assert.True(active.StartJourney(preview).Success);

        for (var day = 0; day < 3; day++)
        {
            active.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
            Assert.True(active.AdvanceJourneyDay().Success);
            await PersistAsync(commandRepository, unitOfWork, active);
            active = await commandRepository.GetByIdAsync(session.Id);
            Assert.NotNull(active);
        }

        Assert.Equal(3, active!.TravelDiaryDays.Count);
        var originalEventStream = await commandRepository.GetEventStreamAsync(session.Id);
        var originalProjection = new TravelDiaryDayProjector().Project(originalEventStream).Days;
        var expectedDays = originalProjection.Select(day => day.DayNumber).ToArray();
        Assert.Equal(expectedDays, active.TravelDiaryDays.Select(day => day.DayNumber));
        var existingPayloads = new Dictionary<int, string>();
        var existingSchemaVersions = new Dictionary<int, int>();
        long streamVersion;
        long? snapshotVersion;
        int eventCount;
        await using (var context = fixture.CreateContext())
        {
            var rows = await context.GameSessionDiaryDays
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .ToListAsync();
            Assert.Equal(new[] { 0, 1, 2 }, rows.Select(day => day.Sequence));
            existingPayloads = rows.Where(day => day.Sequence != missingSequence).ToDictionary(day => day.Sequence, day => day.PayloadJson);
            existingSchemaVersions = rows.Where(day => day.Sequence != missingSequence).ToDictionary(day => day.Sequence, day => day.SchemaVersion);
            var envelope = await context.GameSessions.AsNoTracking().SingleAsync(game => game.Id == session.Id.Value);
            streamVersion = envelope.StreamVersion;
            snapshotVersion = envelope.SnapshotVersion;
            eventCount = await context.StoredEvents.CountAsync(gameEvent => gameEvent.StreamId == session.Id.Value);
            context.GameSessionDiaryDays.Remove(rows.Single(day => day.Sequence == missingSequence));
            await context.SaveChangesAsync();
        }

        var serializer = new GameSessionJsonSerializer();
        var registry = new PayloadUpcasterRegistry(DependencyInjection.CreateDefaultUpcasters());
        var payloadLoader = new PersistedPayloadLoader(registry, serializer, new TravelDiaryDayProjector(),
            rebuildSessionFromEvents: _ => throw new InvalidOperationException("Diary recovery must not rebuild the aggregate."));
        var readStoreLoader = new GameSessionReadStoreLoader(payloadLoader, serializer);
        var readRepository = new EfGameSessionReadRepository(fixture.CreateContext(), readStoreLoader);

        var recovered = await readRepository.GetByIdAsync(session.Id);

        Assert.NotNull(recovered);
        Assert.Equal(expectedDays, recovered!.TravelDiaryDays.Select(day => day.DayNumber));
        await using var verificationContext = fixture.CreateContext();
        var persistedRows = await verificationContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == session.Id.Value)
            .ToDictionaryAsync(day => day.Sequence, day => day.PayloadJson);
        Assert.Equal(existingPayloads, persistedRows);
        var persistedSchemaVersions = await verificationContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == session.Id.Value)
            .ToDictionaryAsync(day => day.Sequence, day => day.SchemaVersion);
        Assert.Equal(existingSchemaVersions, persistedSchemaVersions);
        var unchangedEnvelope = await verificationContext.GameSessions.AsNoTracking().SingleAsync(game => game.Id == session.Id.Value);
        Assert.Equal(streamVersion, unchangedEnvelope.StreamVersion);
        Assert.Equal(snapshotVersion, unchangedEnvelope.SnapshotVersion);
        Assert.Equal(streamVersion, unchangedEnvelope.TravelDiaryProjectionStreamVersion);
        Assert.Equal(3, unchangedEnvelope.TravelDiaryProjectionDayCount);
        Assert.Equal(eventCount, await verificationContext.StoredEvents.CountAsync(gameEvent => gameEvent.StreamId == session.Id.Value));

        var repairRepository = CreateRepository(fixture, out var repairUnitOfWork);
        var commandLoad = await repairRepository.GetByIdAsync(session.Id);
        Assert.NotNull(commandLoad);
        Assert.Equal(expectedDays, commandLoad!.TravelDiaryDays.Select(day => day.DayNumber));
        commandLoad.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        Assert.True(commandLoad.AdvanceJourneyDay().Success);
        await PersistAsync(repairRepository, repairUnitOfWork, commandLoad);

        var eventStream = await repairRepository.GetEventStreamAsync(session.Id);
        var expectedProjection = new TravelDiaryDayProjector().Project(eventStream).Days;
        await using var repairedContext = fixture.CreateContext();
        var repairedRows = await repairedContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == session.Id.Value)
            .OrderBy(day => day.Sequence)
            .ToArrayAsync();
        Assert.Equal(Enumerable.Range(0, expectedProjection.Count), repairedRows.Select(day => day.Sequence));
        Assert.Equal(expectedProjection.Select(day => day.DayNumber), repairedRows.Select(day => JsonDocument.Parse(day.PayloadJson).RootElement.GetProperty("dayNumber").GetInt32()));
        var afterSaveRead = await new EfGameSessionReadRepository(fixture.CreateContext(), readStoreLoader).GetByIdAsync(session.Id);
        Assert.NotNull(afterSaveRead);
        Assert.Equal(expectedProjection.Select(day => day.DayNumber), afterSaveRead!.TravelDiaryDays.Select(day => day.DayNumber));

        Dictionary<int, string> unverifiedPayloads;
        await using (var context = fixture.CreateContext())
        {
            var rows = await context.GameSessionDiaryDays
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .ToListAsync();
            (rows[0].PayloadJson, rows[1].PayloadJson) = (rows[1].PayloadJson, rows[0].PayloadJson);
            unverifiedPayloads = rows.ToDictionary(day => day.Sequence, day => day.PayloadJson);
            var envelope = await context.GameSessions.SingleAsync(game => game.Id == session.Id.Value);
            envelope.TravelDiaryProjectionStreamVersion = null;
            envelope.TravelDiaryProjectionDayCount = null;
            await context.SaveChangesAsync();
        }

        var legacyReadRepository = new EfGameSessionReadRepository(fixture.CreateContext(), readStoreLoader);
        var legacyRead = await legacyReadRepository.GetByIdAsync(session.Id);
        Assert.NotNull(legacyRead);
        Assert.Equal(expectedProjection.Select(day => day.DayNumber), legacyRead!.TravelDiaryDays.Select(day => day.DayNumber));
        await using (var context = fixture.CreateContext())
        {
            var envelope = await context.GameSessions.AsNoTracking().SingleAsync(game => game.Id == session.Id.Value);
            Assert.Null(envelope.TravelDiaryProjectionStreamVersion);
            Assert.Null(envelope.TravelDiaryProjectionDayCount);
            var unchangedRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .ToArrayAsync();
            Assert.Equal(unverifiedPayloads.Keys, unchangedRows.Select(day => day.Sequence));
            Assert.Equal(unverifiedPayloads.Values, unchangedRows.Select(day => day.PayloadJson));
        }

        var orphanSession = CreateJourneyHistorySession();
        await PersistAsync(repairRepository, repairUnitOfWork, orphanSession);
        var orphanPayload = repairedRows[0].PayloadJson;
        await using (var context = fixture.CreateContext())
        {
            context.GameSessionDiaryDays.Add(new GameSessionDiaryDayEntity
            {
                SessionId = orphanSession.Id.Value,
                Sequence = 0,
                PayloadJson = orphanPayload,
                RecordedAtUtc = DateTime.UtcNow,
                SchemaVersion = ProjectionVersions.DiaryDay
            });
            await context.SaveChangesAsync();
        }

        var orphanReadRepository = new EfGameSessionReadRepository(fixture.CreateContext(), readStoreLoader);
        var orphanRead = await orphanReadRepository.GetByIdAsync(orphanSession.Id);
        Assert.NotNull(orphanRead);
        Assert.Empty(orphanRead!.TravelDiaryDays);
        await using var orphanVerificationContext = fixture.CreateContext();
        var orphanRow = await orphanVerificationContext.GameSessionDiaryDays.AsNoTracking()
            .SingleAsync(day => day.SessionId == orphanSession.Id.Value);
        Assert.Equal(orphanPayload, orphanRow.PayloadJson);
        Assert.Equal(ProjectionVersions.DiaryDay, orphanRow.SchemaVersion);
    }

    [Fact]
    public async Task ReadModel_StaleSnapshotRebuildsPlayerAndJournalFromEvents()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var commandRepository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateSession();
        await PersistAsync(commandRepository, unitOfWork, session);

        string oldPlayerPayload;
        await using (var context = fixture.CreateContext())
        {
            oldPlayerPayload = await context.GameSessionComponents.AsNoTracking()
                .Where(component => component.SessionId == session.Id.Value && component.ComponentName == "player")
                .Select(component => component.PayloadJson)
                .SingleAsync();
        }

        var loaded = await commandRepository.GetByIdAsync(session.Id);
        Assert.NotNull(loaded);
        var offer = new TownStoreCatalogResolver()
            .Resolve(loaded!.World.GetTown(loaded.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.ItemKind == DomainItemKind.Food);
        Assert.True(loaded.Purchase(offer, 2).Success);
        await PersistAsync(commandRepository, unitOfWork, loaded);

        long staleSnapshotVersion;
        long streamVersion;
        await using (var context = fixture.CreateContext())
        {
            var playerComponent = await context.GameSessionComponents.SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "player");
            var envelope = await context.GameSessions.SingleAsync(entity => entity.Id == session.Id.Value);
            playerComponent.PayloadJson = oldPlayerPayload;
            envelope.SnapshotVersion = envelope.StreamVersion - 1;
            staleSnapshotVersion = envelope.SnapshotVersion.Value;
            streamVersion = envelope.StreamVersion;
            await context.SaveChangesAsync();
        }

        Assert.True(staleSnapshotVersion < streamVersion);

        var readStoreLoader = CreateReadStoreLoader();
        var readRepository = new EfGameSessionReadRepository(fixture.CreateContext(), readStoreLoader);
        var journalRepository = new EfGameJournalReadRepository(fixture.CreateContext(), readStoreLoader);
        var sessionRead = await readRepository.GetByIdAsync(session.Id);
        var journalRead = await journalRepository.GetByIdAsync(session.Id);

        Assert.NotNull(sessionRead);
        Assert.Equal(loaded.Player.Wallet.Cash, sessionRead!.Player.Wallet.Cash);
        Assert.Equal(loaded.Player.Inventory.GetQuantity(DomainItemKind.Food), sessionRead.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.NotNull(journalRead);
        Assert.Contains(journalRead!.LogEntries, entry =>
            entry.Kind == GameLogEntryKind.Purchase && entry.Message.Contains("Purchased 2 Food", StringComparison.Ordinal));

        await using var verificationContext = fixture.CreateContext();
        var persistedPlayerPayload = await verificationContext.GameSessionComponents.AsNoTracking()
            .Where(component => component.SessionId == session.Id.Value && component.ComponentName == "player")
            .Select(component => component.PayloadJson)
            .SingleAsync();
        var persistedVersions = await verificationContext.GameSessions.AsNoTracking()
            .Where(entity => entity.Id == session.Id.Value)
            .Select(entity => new { entity.SnapshotVersion, entity.StreamVersion })
            .SingleAsync();
        Assert.Equal(oldPlayerPayload, persistedPlayerPayload);
        Assert.Equal(staleSnapshotVersion, persistedVersions.SnapshotVersion);
        Assert.Equal(streamVersion, persistedVersions.StreamVersion);
    }

    [Theory]
    [InlineData("missing-row")]
    [InlineData("null-entropy")]
    [InlineData("unsupported-entropy")]
    public async Task ReadModel_CurrentSetupEntropyCacheRecoversFromEvents(string damage)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = CreateSessionWithSeedCode("setup-entropy-cache", GameEntropy.Wild);
        await PersistAsync(writer, writerUnitOfWork, session);

        const GameEntropy expectedEntropy = GameEntropy.Wild;
        string? damagedPayload = null;
        int? setupComponentVersion = null;
        (long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount) expectedEnvelope;
        (long Sequence, Guid EventId, string EventType, string PayloadJson, Guid? CorrelationId, Guid? CausationId, int SchemaVersion)[] expectedEvents;
        await using (var context = fixture.CreateContext())
        {
            var setup = await context.GameSessionComponents.SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "setup");
            setupComponentVersion = setup.ComponentVersion;
            if (damage == "missing-row")
            {
                context.GameSessionComponents.Remove(setup);
            }
            else
            {
                var payload = JsonNode.Parse(setup.PayloadJson)!.AsObject();
                payload["gameEntropy"] = damage == "unsupported-entropy" ? 99 : null;
                setup.PayloadJson = payload.ToJsonString();
                damagedPayload = setup.PayloadJson;
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            expectedEnvelope = (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount);
            var storedEvents = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            expectedEvents = storedEvents.Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion))
                .ToArray();
            await context.SaveChangesAsync();
        }

        await using (var context = fixture.CreateContext())
        {
            var readRepository = new EfGameSessionReadRepository(context, CreateReadStoreLoader());
            var readModel = await readRepository.GetByIdAsync(session.Id);

            Assert.NotNull(readModel);
            Assert.Equal(expectedEntropy, readModel!.GameEntropy);
        }

        var commandRepository = CreateRepository(fixture, out var commandUnitOfWork);
        var recovered = await commandRepository.GetByIdAsync(session.Id);
        Assert.NotNull(recovered);
        Assert.Equal(expectedEntropy, recovered!.GameEntropy);

        await using (var context = fixture.CreateContext())
        {
            var setup = await context.GameSessionComponents.SingleOrDefaultAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "setup");
            if (damage == "missing-row")
            {
                Assert.Null(setup);
            }
            else
            {
                Assert.NotNull(setup);
                Assert.Equal(setupComponentVersion, setup!.ComponentVersion);
                Assert.True(JsonNode.DeepEquals(JsonNode.Parse(damagedPayload!), JsonNode.Parse(setup!.PayloadJson)));
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            Assert.Equal(expectedEnvelope, (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount));
            var storedEvents = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            var actualEvents = storedEvents.Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion))
                .ToArray();
            Assert.Equal(expectedEvents, actualEvents);
        }

        var offer = new TownStoreCatalogResolver()
            .Resolve(recovered.World.GetTown(recovered.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.ItemKind == DomainItemKind.Food);
        Assert.True(recovered.Purchase(offer, 1).Success);
        await PersistAsync(commandRepository, commandUnitOfWork, recovered);

        await using (var context = fixture.CreateContext())
        {
            var setup = await context.GameSessionComponents.AsNoTracking().SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "setup");

            Assert.Equal(ProjectionVersions.ForComponent("setup"), setup.ComponentVersion);
            Assert.Equal(expectedEntropy, new GameSessionJsonSerializer().DeserializeSetup(setup.PayloadJson));
        }

        var freshRepository = CreateRepository(fixture, out _);
        var repaired = await freshRepository.GetByIdAsync(session.Id);
        Assert.NotNull(repaired);
        Assert.Equal(expectedEntropy, repaired!.GameEntropy);
    }

    [Theory]
    [InlineData("missing-row")]
    [InlineData("missing-route-profile")]
    public async Task ReadModel_DamagedActiveJourneyCacheRecoversWithoutWritingBack(string damage)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var initialRepository = CreateRepository(fixture, out var initialUnitOfWork);
        var session = CreateJourneyHistorySession();
        await PersistAsync(initialRepository, initialUnitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var readRepository = new EfGameSessionReadRepository(context, CreateReadStoreLoader());
            var initialRead = await readRepository.GetByIdAsync(session.Id);
            Assert.NotNull(initialRead);
            Assert.Null(initialRead!.Journey);
        }

        var repository = CreateRepository(fixture, out var unitOfWork);
        var firstJourney = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(firstJourney);
        Assert.True(firstJourney!.StartJourney(CreateJourneyPreview(
            firstJourney.Player.CurrentTownId!.Value,
            new TownId("openpass"),
            "Pinecross",
            "Open Pass")).Success);
        firstJourney.Journey!.MarkCompleted();
        Assert.True(firstJourney.AcknowledgeJourneyArrival().Success);
        await PersistAsync(repository, unitOfWork, firstJourney);

        var acknowledgedRepository = CreateRepository(fixture, out _);
        var acknowledged = await acknowledgedRepository.GetByIdAsync(session.Id);
        Assert.NotNull(acknowledged);
        Assert.Null(acknowledged!.Journey);
        Assert.Single(acknowledged.CompletedJourneyHistory);
        Assert.False(JourneyCacheRecovery.HasCurrentJourney(acknowledged.AllEvents));
        await using (var context = fixture.CreateContext())
        {
            Assert.False(await context.GameSessionComponents.AnyAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "journey"));
        }

        repository = CreateRepository(fixture, out unitOfWork);
        var active = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(active);
        Assert.True(active!.StartJourney(CreateJourneyPreview(
            active.Player.CurrentTownId!.Value,
            new TownId("dryfork"),
            "Open Pass",
            "Dry Fork")).Success);
        var expectedJourney = active.Journey!.ToSnapshot();
        Assert.Equal(2, expectedJourney.JourneySequence);
        Assert.True(JourneyCacheRecovery.HasCurrentJourney(active.AllEvents));
        await PersistAsync(repository, unitOfWork, active);

        string? damagedPayload = null;
        int componentVersion;
        (long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount) expectedEnvelope;
        (long Sequence, Guid EventId, string EventType, string PayloadJson, Guid? CorrelationId, Guid? CausationId, int SchemaVersion)[] expectedEvents;
        (int Sequence, string PayloadJson, int SchemaVersion)[] expectedDiaryRows;
        await using (var context = fixture.CreateContext())
        {
            var journey = await context.GameSessionComponents.SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "journey");
            componentVersion = journey.ComponentVersion;
            if (damage == "missing-row")
            {
                context.GameSessionComponents.Remove(journey);
            }
            else
            {
                var payload = JsonNode.Parse(journey.PayloadJson)!.AsObject();
                payload.Remove("routeProfile");
                journey.PayloadJson = payload.ToJsonString();
                damagedPayload = journey.PayloadJson;
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            expectedEnvelope = (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount);
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            expectedEvents = events.Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion))
                .ToArray();
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .ToArrayAsync();
            expectedDiaryRows = diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray();
            await context.SaveChangesAsync();
        }

        static void AssertJourneyFacts(TravelJourneySnapshot expected, TravelJourneySnapshot? actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.JourneySequence, actual!.JourneySequence);
            Assert.Equal(expected.OriginTownId, actual.OriginTownId);
            Assert.Equal(expected.DestinationTownId, actual.DestinationTownId);
            Assert.Equal(expected.RouteProfile.TrailId, actual.RouteProfile.TrailId);
            Assert.Equal(expected.Status, actual.Status);
            Assert.Equal(expected.RemainingRideDayDistance, actual.RemainingRideDayDistance);
            Assert.Equal(expected.RemainingDays, actual.RemainingDays);
        }

        await using (var context = fixture.CreateContext())
        {
            var readRepository = new EfGameSessionReadRepository(context, CreateReadStoreLoader());
            var readModel = await readRepository.GetByIdAsync(session.Id);
            Assert.NotNull(readModel);
            AssertJourneyFacts(expectedJourney, readModel!.Journey);
        }

        var recoveryRepository = CreateRepository(fixture, out var recoveryUnitOfWork);
        var recovered = await recoveryRepository.GetByIdAsync(session.Id);
        Assert.NotNull(recovered);
        AssertJourneyFacts(expectedJourney, recovered!.Journey!.ToSnapshot());

        await using (var context = fixture.CreateContext())
        {
            var journey = await context.GameSessionComponents.AsNoTracking().SingleOrDefaultAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "journey");
            if (damage == "missing-row")
            {
                Assert.Null(journey);
            }
            else
            {
                Assert.NotNull(journey);
                Assert.Equal(componentVersion, journey!.ComponentVersion);
                Assert.True(JsonNode.DeepEquals(JsonNode.Parse(damagedPayload!), JsonNode.Parse(journey.PayloadJson)));
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            Assert.Equal(expectedEnvelope, (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount));
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            var actualEvents = events.Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion))
                .ToArray();
            Assert.Equal(expectedEvents, actualEvents);
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .ToArrayAsync();
            Assert.Equal(expectedDiaryRows, diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }



        Assert.True(recovered.AdvanceJourneyDay().Success);
        await PersistAsync(recoveryRepository, recoveryUnitOfWork, recovered);

        await using (var context = fixture.CreateContext())
        {
            var journey = await context.GameSessionComponents.AsNoTracking().SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "journey");
            Assert.Equal(ProjectionVersions.ForComponent("journey"), journey.ComponentVersion);
            AssertJourneyFacts(recovered.Journey!.ToSnapshot(), new GameSessionJsonSerializer().DeserializeJourneySnapshot(journey.PayloadJson));
        }

        var freshRepository = CreateRepository(fixture, out _);
        var fresh = await freshRepository.GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        AssertJourneyFacts(recovered.Journey!.ToSnapshot(), fresh!.Journey!.ToSnapshot());
    }

    [Fact]
    public async Task ReadModel_MalformedTownVisitWantedSuspectShapeRecoversFromEvents()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var session = CreateSessionWithSaloonSuspect();
        var expectedSuspectId = new SuspectId("saloon-suspect");
        Assert.True(session.LookAroundSaloon().Success);
        var spotted = Assert.Single(session.AllEvents.OfType<SaloonPersonOfInterestSpotted>());
        Assert.True(
            expectedSuspectId == spotted.SuspectId,
            $"Expected the deterministic saloon roll to select {expectedSuspectId.Value}; got {spotted.SuspectId?.Value ?? "no suspect"} at day {session.Clock.Day}, turn {session.Clock.Turn}, visit {session.CurrentTownVisit.CurrentTownState.VisitNumber}.");
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, spotted.PersonOfInterestKind);
        var expectedDescriptor = spotted.Descriptor;
        Assert.NotNull(expectedDescriptor);

        await PersistAsync(CreateRepository(fixture, out var unitOfWork), unitOfWork, session);

        async Task<(int ComponentVersion, string ComponentPayload, long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount, (long Sequence, Guid EventId, string EventType, string PayloadJson, int SchemaVersion)[] Events, (int Sequence, string PayloadJson, int SchemaVersion)[] Diary)> CapturePersistedStateAsync()
        {
            await using var context = fixture.CreateContext();
            var component = await context.GameSessionComponents.AsNoTracking().SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "townVisitState");
            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                .ToArrayAsync();
            return (
                component.ComponentVersion,
                component.PayloadJson,
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount,
                events.Select(storedEvent => (
                        storedEvent.Sequence,
                        storedEvent.EventId,
                        storedEvent.EventType,
                        storedEvent.PayloadJson,
                        storedEvent.SchemaVersion))
                    .ToArray(),
                diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "townVisitState");
            var payload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            var townEntry = Assert.Single(payload["townStates"]!.AsArray(), entry =>
                entry?["townId"]?.GetValue<string>() == session.CurrentTownVisit.CurrentTownId.Value);
            Assert.Equal((int)SaloonPersonOfInterestKind.WantedSuspect, townEntry!["activeSaloonPersonOfInterestKind"]!.GetValue<int>());
            Assert.Equal(expectedDescriptor, townEntry["activeSaloonPersonOfInterestDescriptor"]!.GetValue<string>());
            townEntry.AsObject().Remove("activeSaloonPersonOfInterestId");
            component.PayloadJson = payload.ToJsonString();
            await context.SaveChangesAsync();
        }

        var expectedPersistedState = await CapturePersistedStateAsync();

        void AssertRecoveredTownVisit(TownVisitState? townVisit)
        {
            Assert.NotNull(townVisit);
            Assert.Equal(session.CurrentTownVisit.CurrentTownId, townVisit!.CurrentTownId);
            Assert.Equal(expectedSuspectId, townVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
            Assert.Equal(expectedDescriptor, townVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
            Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, townVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
            Assert.Contains(InvestigationSourceKind.SaloonLookAround, townVisit.SpentInvestigationSources);
        }

        await using (var context = fixture.CreateContext())
        {
            var readRepository = new EfGameSessionReadRepository(context, CreateReadStoreLoader());
            var readModel = await readRepository.GetByIdAsync(session.Id);
            Assert.NotNull(readModel);
            AssertRecoveredTownVisit(readModel!.TownVisitState);
        }

        var recovered = await CreateRepository(fixture, out _).GetByIdAsync(session.Id);
        Assert.NotNull(recovered);
        AssertRecoveredTownVisit(recovered!.CurrentTownVisit);

        var actualPersistedState = await CapturePersistedStateAsync();
        Assert.Equal(expectedPersistedState.ComponentVersion, actualPersistedState.ComponentVersion);
        Assert.Equal(expectedPersistedState.ComponentPayload, actualPersistedState.ComponentPayload);
        Assert.Equal(expectedPersistedState.SnapshotVersion, actualPersistedState.SnapshotVersion);
        Assert.Equal(expectedPersistedState.StreamVersion, actualPersistedState.StreamVersion);
        Assert.Equal(expectedPersistedState.DiaryStreamVersion, actualPersistedState.DiaryStreamVersion);
        Assert.Equal(expectedPersistedState.DiaryDayCount, actualPersistedState.DiaryDayCount);
        Assert.Equal(expectedPersistedState.Events, actualPersistedState.Events);
        Assert.Equal(expectedPersistedState.Diary, actualPersistedState.Diary);

        var citizenSession = CreateSession();
        Assert.True(citizenSession.LookAroundSaloon().Success);
        var citizenSpot = Assert.Single(citizenSession.AllEvents.OfType<SaloonPersonOfInterestSpotted>());
        Assert.Null(citizenSpot.SuspectId);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, citizenSpot.PersonOfInterestKind);
        await PersistAsync(CreateRepository(fixture, out var citizenUnitOfWork), citizenUnitOfWork, citizenSession);
        var citizenReload = await CreateRepository(fixture, out _).GetByIdAsync(citizenSession.Id);
        Assert.NotNull(citizenReload);
        Assert.Null(citizenReload!.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, citizenReload.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
    }

    [Fact]
    public async Task ReadModel_CaseFileCacheMissingDiscoveredSuspectsRecoversFromEventsWithoutWritingBack()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var session = CreateSession(includeKnownClue: true);
        var expectedDiscoveredSuspectIds = session.CaseFile.DiscoveredSuspectIds.Select(id => id.Value).ToArray();
        var caseFileGenerated = Assert.Single(session.AllEvents.OfType<CaseFileGenerated>());
        Assert.Contains(expectedDiscoveredSuspectIds[0], caseFileGenerated.CaseFile.DiscoveredSuspectIds);
        await PersistAsync(CreateRepository(fixture, out var unitOfWork), unitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var payload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            Assert.NotNull(payload["discoveredSuspectIds"]);
            payload.Remove("discoveredSuspectIds");
            component.PayloadJson = payload.ToJsonString();
            await context.SaveChangesAsync();
        }

        async Task<(int ComponentVersion, string ComponentPayload, long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount, (long Sequence, Guid EventId, string EventType, string PayloadJson, int SchemaVersion)[] Events, (int Sequence, string PayloadJson, int SchemaVersion)[] Diary)> CapturePersistedStateAsync()
        {
            await using var context = fixture.CreateContext();
            var component = await context.GameSessionComponents.AsNoTracking().SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                .ToArrayAsync();
            return (
                component.ComponentVersion,
                component.PayloadJson,
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount,
                events.Select(storedEvent => (
                        storedEvent.Sequence,
                        storedEvent.EventId,
                        storedEvent.EventType,
                        storedEvent.PayloadJson,
                        storedEvent.SchemaVersion))
                    .ToArray(),
                diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }

        var expectedPersistedState = await CapturePersistedStateAsync();
        GameSessionReadModel? playerRead;
        await using (var context = fixture.CreateContext())
        {
            playerRead = await new EfGameSessionReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id);
        }
        var journalRead = await new EfGameJournalReadRepository(
            fixture.CreateContext(),
            CreateReadStoreLoader()).GetByIdAsync(session.Id);
        var repairRepository = CreateRepository(fixture, out var repairUnitOfWork);
        var commandRead = await repairRepository.GetByIdAsync(session.Id);

        Assert.NotNull(playerRead);
        Assert.Equal(expectedDiscoveredSuspectIds, playerRead!.CaseFile.DiscoveredSuspectIds.Select(id => id.Value));
        Assert.NotNull(journalRead);
        Assert.Equal(expectedDiscoveredSuspectIds, journalRead!.DiscoveredSuspects.Select(suspect => suspect.Id.Value));
        Assert.NotNull(commandRead);
        Assert.Equal(expectedDiscoveredSuspectIds, commandRead!.CaseFile.DiscoveredSuspectIds.Select(id => id.Value));

        var actualPersistedState = await CapturePersistedStateAsync();
        Assert.Equal(expectedPersistedState.ComponentVersion, actualPersistedState.ComponentVersion);
        Assert.Equal(expectedPersistedState.ComponentPayload, actualPersistedState.ComponentPayload);
        Assert.Equal(expectedPersistedState.SnapshotVersion, actualPersistedState.SnapshotVersion);
        Assert.Equal(expectedPersistedState.StreamVersion, actualPersistedState.StreamVersion);
        Assert.Equal(expectedPersistedState.DiaryStreamVersion, actualPersistedState.DiaryStreamVersion);
        Assert.Equal(expectedPersistedState.DiaryDayCount, actualPersistedState.DiaryDayCount);
        Assert.Equal(expectedPersistedState.Events, actualPersistedState.Events);
        Assert.Equal(expectedPersistedState.Diary, actualPersistedState.Diary);

        PurchaseFood(commandRead!, 1);
        await PersistAsync(repairRepository, repairUnitOfWork, commandRead!);

        await using (var context = fixture.CreateContext())
        {
            var repairedComponent = await context.GameSessionComponents.AsNoTracking().SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "caseFile");
            Assert.Equal(ProjectionVersions.ForComponent("caseFile"), repairedComponent.ComponentVersion);
            Assert.Equal(
                expectedDiscoveredSuspectIds,
                JsonNode.Parse(repairedComponent.PayloadJson)!["discoveredSuspectIds"]!.AsArray().Select(id => id!.GetValue<string>()));
        }

        var fresh = await CreateRepository(fixture, out _).GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        Assert.Equal(expectedDiscoveredSuspectIds, fresh!.CaseFile.DiscoveredSuspectIds.Select(id => id.Value));
    }

    [Fact]
    public async Task ReadModel_CaseFileCacheMissingOpeningLeadRecoversFromEventsWithoutWritingBack()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var session = CreateSession(includeKnownClue: true);
        var expectedOpeningLead = session.CaseFile.OpeningLead.Description;
        var caseFileGenerated = Assert.Single(session.AllEvents.OfType<CaseFileGenerated>());
        Assert.Equal(expectedOpeningLead, caseFileGenerated.CaseFile.OpeningLead.Description);
        await PersistAsync(CreateRepository(fixture, out var unitOfWork), unitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var payload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            Assert.NotNull(payload["openingLead"]);
            payload.Remove("openingLead");
            component.PayloadJson = payload.ToJsonString();
            await context.SaveChangesAsync();
        }

        async Task<(int ComponentVersion, string ComponentPayload, long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount, (long Sequence, Guid EventId, string EventType, string PayloadJson, int SchemaVersion)[] Events, (int Sequence, string PayloadJson, int SchemaVersion)[] Diary)> CapturePersistedStateAsync()
        {
            await using var context = fixture.CreateContext();
            var component = await context.GameSessionComponents.AsNoTracking().SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                .ToArrayAsync();
            return (
                component.ComponentVersion,
                component.PayloadJson,
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount,
                events.Select(storedEvent => (
                        storedEvent.Sequence,
                        storedEvent.EventId,
                        storedEvent.EventType,
                        storedEvent.PayloadJson,
                        storedEvent.SchemaVersion))
                    .ToArray(),
                diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }

        var expectedPersistedState = await CapturePersistedStateAsync();
        GameSessionReadModel? playerRead;
        await using (var context = fixture.CreateContext())
        {
            playerRead = await new EfGameSessionReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id);
        }
        var journalRead = await new EfGameJournalReadRepository(
            fixture.CreateContext(),
            CreateReadStoreLoader()).GetByIdAsync(session.Id);
        var repairRepository = CreateRepository(fixture, out var repairUnitOfWork);
        var commandRead = await repairRepository.GetByIdAsync(session.Id);

        Assert.NotNull(playerRead);
        Assert.Equal(expectedOpeningLead, playerRead!.CaseFile.OpeningLead.Description);
        Assert.NotNull(journalRead);
        Assert.Equal(expectedOpeningLead, journalRead!.OpeningLead);
        Assert.NotNull(commandRead);
        Assert.Equal(expectedOpeningLead, commandRead!.CaseFile.OpeningLead.Description);

        var actualPersistedState = await CapturePersistedStateAsync();
        Assert.Equal(expectedPersistedState.ComponentVersion, actualPersistedState.ComponentVersion);
        Assert.Equal(expectedPersistedState.ComponentPayload, actualPersistedState.ComponentPayload);
        Assert.Equal(expectedPersistedState.SnapshotVersion, actualPersistedState.SnapshotVersion);
        Assert.Equal(expectedPersistedState.StreamVersion, actualPersistedState.StreamVersion);
        Assert.Equal(expectedPersistedState.DiaryStreamVersion, actualPersistedState.DiaryStreamVersion);
        Assert.Equal(expectedPersistedState.DiaryDayCount, actualPersistedState.DiaryDayCount);
        Assert.Equal(expectedPersistedState.Events, actualPersistedState.Events);
        Assert.Equal(expectedPersistedState.Diary, actualPersistedState.Diary);

        PurchaseFood(commandRead!, 1);
        await PersistAsync(repairRepository, repairUnitOfWork, commandRead!);

        await using (var context = fixture.CreateContext())
        {
            var repairedComponent = await context.GameSessionComponents.AsNoTracking().SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "caseFile");
            Assert.Equal(ProjectionVersions.ForComponent("caseFile"), repairedComponent.ComponentVersion);
            Assert.Equal(
                expectedOpeningLead,
                JsonNode.Parse(repairedComponent.PayloadJson)!["openingLead"]!.GetValue<string>());
        }

        var fresh = await CreateRepository(fixture, out _).GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        Assert.Equal(expectedOpeningLead, fresh!.CaseFile.OpeningLead.Description);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadModel_CaseFileCacheMissingOrNullPublicCluesRecoversFromEventsWithoutWritingBack(bool setExplicitNull)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var expectedClue = new Clue(
            new ClueId("public-gossip-cache-clue"),
            ClueKind.Whereabouts,
            "Local gossip says the red-hat rider kept to the rail spur after dark.",
            new[] { new SuspectId("suspect-1") },
            InvestigationTargetKind.GangMember,
            InvestigationSourceKind.LocalGossip,
            source: "saloon talk",
            context: "Town gossip",
            anchors: new ClueAnchors(
                subjects: new[]
                {
                    new ClueSubjectAnchor("red hat rider", Feature: "red hat")
                }));
        var session = CreateSession(publicClues: new[] { expectedClue });
        var caseFileGenerated = Assert.Single(session.AllEvents.OfType<CaseFileGenerated>());
        var recordedClue = Assert.Single(caseFileGenerated.CaseFile.PublicClues);
        Assert.Equal(expectedClue.Id.Value, recordedClue.Id);
        Assert.Equal(expectedClue.Description, recordedClue.Description);
        await PersistAsync(CreateRepository(fixture, out var unitOfWork), unitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var payload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            Assert.NotNull(payload["publicClues"]);
            if (setExplicitNull)
            {
                payload["publicClues"] = null;
            }
            else
            {
                payload.Remove("publicClues");
            }
            component.PayloadJson = payload.ToJsonString();
            await context.SaveChangesAsync();
        }

        async Task<(int ComponentVersion, string ComponentPayload, long? SnapshotVersion, long StreamVersion, (long Sequence, Guid EventId, string EventType, string PayloadJson, int SchemaVersion)[] Events, (int Sequence, string PayloadJson, int SchemaVersion)[] Diary)> CapturePersistedStateAsync()
        {
            await using var context = fixture.CreateContext();
            var component = await context.GameSessionComponents.AsNoTracking().SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new { entity.SnapshotVersion, entity.StreamVersion })
                .SingleAsync();
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                .ToArrayAsync();
            return (
                component.ComponentVersion,
                component.PayloadJson,
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                events.Select(storedEvent => (
                        storedEvent.Sequence,
                        storedEvent.EventId,
                        storedEvent.EventType,
                        storedEvent.PayloadJson,
                        storedEvent.SchemaVersion))
                    .ToArray(),
                diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }

        var expectedPersistedState = await CapturePersistedStateAsync();
        GameSessionReadModel? playerRead;
        await using (var context = fixture.CreateContext())
        {
            playerRead = await new EfGameSessionReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id);
        }
        var journalRead = await new EfGameJournalReadRepository(
            fixture.CreateContext(),
            CreateReadStoreLoader()).GetByIdAsync(session.Id);
        var repairRepository = CreateRepository(fixture, out var repairUnitOfWork);
        var commandRead = await repairRepository.GetByIdAsync(session.Id);

        Assert.NotNull(playerRead);
        Assert.Equal(
            new[] { expectedClue.Id.Value },
            playerRead!.CaseFile.PublicClues.Select(clue => clue.Id.Value));
        Assert.DoesNotContain(expectedClue.Id.Value, JsonSerializer.Serialize(GameSessionMapper.ToDto(playerRead)), StringComparison.Ordinal);
        Assert.DoesNotContain(expectedClue.Description, JsonSerializer.Serialize(GameSessionMapper.ToDto(playerRead)), StringComparison.Ordinal);
        Assert.NotNull(journalRead);
        Assert.DoesNotContain(expectedClue.Id.Value, JsonSerializer.Serialize(journalRead), StringComparison.Ordinal);
        Assert.DoesNotContain(expectedClue.Description, JsonSerializer.Serialize(journalRead), StringComparison.Ordinal);
        Assert.NotNull(commandRead);
        Assert.Equal(
            new[] { expectedClue.Id.Value },
            commandRead!.CaseFile.PublicClues.Select(clue => clue.Id.Value));

        var actualPersistedState = await CapturePersistedStateAsync();
        Assert.Equal(expectedPersistedState.ComponentVersion, actualPersistedState.ComponentVersion);
        Assert.Equal(expectedPersistedState.ComponentPayload, actualPersistedState.ComponentPayload);
        Assert.Equal(expectedPersistedState.SnapshotVersion, actualPersistedState.SnapshotVersion);
        Assert.Equal(expectedPersistedState.StreamVersion, actualPersistedState.StreamVersion);
        Assert.Equal(expectedPersistedState.Events, actualPersistedState.Events);
        Assert.Equal(expectedPersistedState.Diary, actualPersistedState.Diary);

        var gossipResult = commandRead.GatherLocalGossip();
        Assert.True(gossipResult.Success);
        Assert.Contains(
            commandRead.UncommittedEvents.OfType<InvestigationPerformed>(),
            performed => performed.ClueId?.Value == expectedClue.Id.Value);
        await PersistAsync(repairRepository, repairUnitOfWork, commandRead);

        await using (var context = fixture.CreateContext())
        {
            var repairedComponent = await context.GameSessionComponents.AsNoTracking().SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "caseFile");
            Assert.Equal(ProjectionVersions.ForComponent("caseFile"), repairedComponent.ComponentVersion);
            Assert.Empty(JsonNode.Parse(repairedComponent.PayloadJson)!["publicClues"]!.AsArray());
        }

        var fresh = await CreateRepository(fixture, out _).GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        Assert.Contains(fresh!.CaseFile.KnownClues, clue => clue.Id.Equals(expectedClue.Id));
        Assert.Empty(fresh.CaseFile.PublicClues);
    }

    [Fact]
    public async Task DamagedCaseFileCacheDoesNotHideMissingPublicCluesInCaseFileGeneratedEvent()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var clue = new Clue(
            new ClueId("recorded-public-clue"),
            ClueKind.IdentityFact,
            "A witness remembers the red hat rider.",
            new[] { new SuspectId("suspect-1") },
            InvestigationTargetKind.GangMember,
            InvestigationSourceKind.LocalGossip,
            anchors: new ClueAnchors(
                subjects: new[]
                {
                    new ClueSubjectAnchor("red hat rider", Feature: "red hat")
                }));
        var session = CreateSession(publicClues: new[] { clue });
        await PersistAsync(CreateRepository(fixture, out var unitOfWork), unitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var componentPayload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            componentPayload.Remove("publicClues");
            component.PayloadJson = componentPayload.ToJsonString();

            var caseFileEvent = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == "CaseFileGenerated");
            var eventPayload = JsonNode.Parse(caseFileEvent.PayloadJson)!.AsObject();
            eventPayload["caseFile"]!["publicClues"] = null;
            caseFileEvent.PayloadJson = eventPayload.ToJsonString();
            await context.SaveChangesAsync();
        }

        var commandException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateRepository(fixture, out _).GetByIdAsync(session.Id));
        Assert.Contains("recorded public clues", commandException.Message, StringComparison.OrdinalIgnoreCase);

        await using (var context = fixture.CreateContext())
        {
            var playerException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EfGameSessionReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id));
            Assert.Equal(commandException.Message, playerException.Message);
        }

        await using (var context = fixture.CreateContext())
        {
            var journalException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EfGameJournalReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id));
            Assert.Equal(commandException.Message, journalException.Message);
        }
    }

    [Fact]
    public async Task DamagedCaseFileCacheDoesNotHideMissingDiscoveredSuspectsInCaseFileGeneratedEvent()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var session = CreateSession(includeKnownClue: true);
        await PersistAsync(CreateRepository(fixture, out var unitOfWork), unitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var componentPayload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            componentPayload.Remove("discoveredSuspectIds");
            component.PayloadJson = componentPayload.ToJsonString();

            var caseFileEvent = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == "CaseFileGenerated");
            var eventPayload = JsonNode.Parse(caseFileEvent.PayloadJson)!.AsObject();
            eventPayload["caseFile"]!.AsObject().Remove("discoveredSuspectIds");
            caseFileEvent.PayloadJson = eventPayload.ToJsonString();
            await context.SaveChangesAsync();
        }

        var commandException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateRepository(fixture, out _).GetByIdAsync(session.Id));
        Assert.Contains("recorded discovered suspect ids", commandException.Message, StringComparison.Ordinal);

        await using (var context = fixture.CreateContext())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EfGameSessionReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id));
        }

        await using (var context = fixture.CreateContext())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EfGameJournalReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id));
        }
    }

    [Fact]
    public async Task DamagedCaseFileCacheDoesNotHideMissingOpeningLeadInCaseFileGeneratedEvent()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var session = CreateSession(includeKnownClue: true);
        await PersistAsync(CreateRepository(fixture, out var unitOfWork), unitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "caseFile");
            var componentPayload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            componentPayload.Remove("openingLead");
            component.PayloadJson = componentPayload.ToJsonString();

            var caseFileEvent = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == "CaseFileGenerated");
            var eventPayload = JsonNode.Parse(caseFileEvent.PayloadJson)!.AsObject();
            eventPayload["caseFile"]!["openingLead"] = null;
            caseFileEvent.PayloadJson = eventPayload.ToJsonString();
            await context.SaveChangesAsync();
        }

        var commandException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateRepository(fixture, out _).GetByIdAsync(session.Id));
        Assert.Contains("recorded opening lead", commandException.Message, StringComparison.Ordinal);

        await using (var context = fixture.CreateContext())
        {
            var playerException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EfGameSessionReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id));
            Assert.Contains("recorded opening lead", playerException.Message, StringComparison.Ordinal);
        }

        await using (var context = fixture.CreateContext())
        {
            var journalException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => new EfGameJournalReadRepository(context, CreateReadStoreLoader()).GetByIdAsync(session.Id));
            Assert.Contains("recorded opening lead", journalException.Message, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("missing-row")]
    [InlineData("null-root")]
    public async Task ReadModel_DamagedTownVisitCacheRecoversWithoutWritingBack(string damage)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = CreateSession();
        Assert.True(session.LookAroundSaloon().Success);
        var spotted = Assert.Single(session.AllEvents.OfType<SaloonPersonOfInterestSpotted>());
        var expectedTownId = session.CurrentTownVisit.CurrentTownId;
        var expectedVisitNumber = session.CurrentTownVisit.CurrentTownState.VisitNumber;
        var expectedDescriptor = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor;
        var expectedSuspectId = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId;
        var expectedKind = session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind;
        Assert.Equal(expectedTownId, spotted.TownId);
        Assert.Equal(expectedDescriptor, spotted.Descriptor);
        Assert.Equal(expectedSuspectId, spotted.SuspectId);
        Assert.Equal(expectedKind, spotted.PersonOfInterestKind);
        Assert.Contains(InvestigationSourceKind.SaloonLookAround, session.CurrentTownVisit.SpentInvestigationSources);
        await PersistAsync(writer, writerUnitOfWork, session);

        int? originalComponentVersion;
        string? damagedPayload = null;
        (long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount) expectedEnvelope;
        (long Sequence, Guid EventId, string EventType, string PayloadJson, Guid? CorrelationId, Guid? CausationId, int SchemaVersion)[] expectedEvents;
        (int Sequence, string PayloadJson, int SchemaVersion)[] expectedDiaryRows;
        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "townVisitState");
            originalComponentVersion = component.ComponentVersion;
            if (damage == "missing-row")
            {
                context.GameSessionComponents.Remove(component);
            }
            else
            {
                component.PayloadJson = "null";
                damagedPayload = component.PayloadJson;
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            expectedEnvelope = (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount);
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            expectedEvents = events.Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion))
                .ToArray();
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .ToArrayAsync();
            expectedDiaryRows = diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray();

            await context.SaveChangesAsync();
        }

        void AssertVisitFacts(TownVisitState? actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expectedTownId, actual!.CurrentTownId);
            Assert.Equal(expectedVisitNumber, actual.CurrentTownState.VisitNumber);
            Assert.Contains(InvestigationSourceKind.SaloonLookAround, actual.SpentInvestigationSources);
            Assert.Equal(expectedDescriptor, actual.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
            Assert.Equal(expectedSuspectId, actual.CurrentTownState.ActiveSaloonPersonOfInterestId);
            Assert.Equal(expectedKind, actual.CurrentTownState.ActiveSaloonPersonOfInterestKind);
        }

        await using (var context = fixture.CreateContext())
        {
            var readRepository = new EfGameSessionReadRepository(context, CreateReadStoreLoader());
            var readModel = await readRepository.GetByIdAsync(session.Id);
            Assert.NotNull(readModel);
            AssertVisitFacts(readModel!.TownVisitState);
        }

        var commandRepository = CreateRepository(fixture, out _);
        var recovered = await commandRepository.GetByIdAsync(session.Id);
        Assert.NotNull(recovered);
        AssertVisitFacts(recovered!.CurrentTownVisit);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.AsNoTracking().SingleOrDefaultAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "townVisitState");
            if (damage == "missing-row")
            {
                Assert.Null(component);
            }
            else
            {
                Assert.NotNull(component);
                Assert.Equal(originalComponentVersion, component!.ComponentVersion);
                Assert.Equal(damagedPayload, component.PayloadJson);
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            Assert.Equal(expectedEnvelope, (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount));
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            var actualEvents = events.Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion))
                .ToArray();
            Assert.Equal(expectedEvents, actualEvents);
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .ToArrayAsync();
            Assert.Equal(expectedDiaryRows, diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }

        var repairRepository = CreateRepository(fixture, out var repairUnitOfWork);
        var repairSession = await repairRepository.GetByIdAsync(session.Id);
        Assert.NotNull(repairSession);
        var foodOffer = new TownStoreCatalogResolver()
            .Resolve(repairSession!.World.GetTown(repairSession.Player.CurrentTownId!.Value))
            .Offers.Single(offer => offer.ItemKind == DomainItemKind.Food);
        Assert.True(repairSession.Purchase(foodOffer, 1).Success);
        await PersistAsync(repairRepository, repairUnitOfWork, repairSession);

        var freshRepository = CreateRepository(fixture, out _);
        var repaired = await freshRepository.GetByIdAsync(session.Id);
        Assert.NotNull(repaired);
        AssertVisitFacts(repaired!.CurrentTownVisit);
        await using var repairedContext = fixture.CreateContext();
        var repairedComponent = await repairedContext.GameSessionComponents.AsNoTracking().SingleAsync(candidate =>
            candidate.SessionId == session.Id.Value && candidate.ComponentName == "townVisitState");
        Assert.Equal(ProjectionVersions.ForComponent("townVisitState"), repairedComponent.ComponentVersion);
    }

    [Theory]
    [InlineData("missing-row")]
    [InlineData("null-root")]
    [InlineData("wanted-suspect-without-id")]
    public async Task DamagedTownVisitCacheDoesNotHideInvalidEventHistory(string damage)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = damage == "wanted-suspect-without-id" ? CreateSessionWithSaloonSuspect() : CreateSession();
        Assert.True(session.LookAroundSaloon().Success);
        await PersistAsync(writer, writerUnitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "townVisitState");
            if (damage == "missing-row")
            {
                context.GameSessionComponents.Remove(component);
            }
            else if (damage == "null-root")
            {
                component.PayloadJson = "null";
            }
            else
            {
                var payload = JsonNode.Parse(component.PayloadJson)!.AsObject();
                var townEntry = Assert.Single(payload["townStates"]!.AsArray(), entry =>
                    entry?["townId"]?.GetValue<string>() == session.CurrentTownVisit.CurrentTownId.Value);
                Assert.Equal((int)SaloonPersonOfInterestKind.WantedSuspect, townEntry!["activeSaloonPersonOfInterestKind"]!.GetValue<int>());
                townEntry.AsObject().Remove("activeSaloonPersonOfInterestId");
                component.PayloadJson = payload.ToJsonString();
            }

            var gameStarted = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == nameof(GameStarted));
            gameStarted.PayloadJson = "{}";
            await context.SaveChangesAsync();
        }

        var commandRepository = CreateRepository(fixture, out _);
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => commandRepository.GetByIdAsync(session.Id));

        await using var readContext = fixture.CreateContext();
        var readRepository = new EfGameSessionReadRepository(readContext, CreateReadStoreLoader());
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => readRepository.GetByIdAsync(session.Id));
    }

    [Theory]
    [InlineData("missing-row")]
    [InlineData("null-root")]
    public async Task CommandLoad_CurrentActionContextCacheRecoversFromEventsWithoutWritingBack(string damage)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = CreateSession();
        Assert.True(session.EnterActionContext(TownActionContext.Saloon));
        var contextEvent = Assert.Single(session.AllEvents.OfType<TownActionContextEntered>());
        var expectedContext = session.CurrentActionContext;
        var expectedTownId = session.CurrentActionContextTownId;
        var expectedClock = (session.Clock.Day, session.Clock.Turn, session.Clock.TimeOfDay, session.PursuitState.Heat);
        Assert.Equal(expectedContext, contextEvent.Context);
        Assert.Equal(expectedTownId, contextEvent.TownId);
        Assert.Equal(expectedClock, (contextEvent.Day, contextEvent.Turn, contextEvent.TimeOfDay, contextEvent.PursuitHeat));
        await PersistAsync(writer, writerUnitOfWork, session);

        int originalComponentVersion;
        string? damagedPayload = null;
        (long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount) expectedEnvelope;
        (long Sequence, Guid EventId, string EventType, string PayloadJson, Guid? CorrelationId, Guid? CausationId, int SchemaVersion)[] expectedEvents;
        (int Sequence, string PayloadJson, int SchemaVersion)[] expectedDiaryRows;
        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "currentActionContext");
            originalComponentVersion = component.ComponentVersion;
            if (damage == "missing-row")
            {
                context.GameSessionComponents.Remove(component);
            }
            else
            {
                component.PayloadJson = "null";
                damagedPayload = component.PayloadJson;
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            expectedEnvelope = (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount);
            var eventRows = await context.StoredEvents.AsNoTracking()
                    .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                    .OrderBy(storedEvent => storedEvent.Sequence)
                    .Select(storedEvent => new
                    {
                        storedEvent.Sequence,
                        storedEvent.EventId,
                        storedEvent.EventType,
                        storedEvent.PayloadJson,
                        storedEvent.CorrelationId,
                        storedEvent.CausationId,
                        storedEvent.SchemaVersion
                    })
                    .ToArrayAsync();
            expectedEvents = eventRows.Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion))
                .ToArray();
            expectedDiaryRows = (await context.GameSessionDiaryDays.AsNoTracking()
                    .Where(day => day.SessionId == session.Id.Value)
                    .OrderBy(day => day.Sequence)
                    .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                    .ToArrayAsync())
                .Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion))
                .ToArray();
            await context.SaveChangesAsync();
        }

        var commandRepository = CreateRepository(fixture, out var commandUnitOfWork);
        var recovered = await commandRepository.GetByIdAsync(session.Id);
        Assert.NotNull(recovered);
        Assert.Equal(expectedContext, recovered!.CurrentActionContext);
        Assert.Equal(expectedTownId, recovered.CurrentActionContextTownId);
        Assert.Equal(expectedClock, (recovered.Clock.Day, recovered.Clock.Turn, recovered.Clock.TimeOfDay, recovered.PursuitState.Heat));
        Assert.False(recovered.EnterActionContext(TownActionContext.Saloon));
        Assert.Equal(expectedClock, (recovered.Clock.Day, recovered.Clock.Turn, recovered.Clock.TimeOfDay, recovered.PursuitState.Heat));
        Assert.Single(recovered.AllEvents.OfType<TownActionContextEntered>());

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.AsNoTracking().SingleOrDefaultAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "currentActionContext");
            if (damage == "missing-row")
            {
                Assert.Null(component);
            }
            else
            {
                Assert.NotNull(component);
                Assert.Equal(originalComponentVersion, component!.ComponentVersion);
                Assert.Equal(damagedPayload, component.PayloadJson);
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            Assert.Equal(expectedEnvelope, (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount));
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            Assert.Equal(expectedEvents, events.Select(storedEvent => (
                storedEvent.Sequence,
                storedEvent.EventId,
                storedEvent.EventType,
                storedEvent.PayloadJson,
                storedEvent.CorrelationId,
                storedEvent.CausationId,
                storedEvent.SchemaVersion)).ToArray());
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                .ToArrayAsync();
            Assert.Equal(expectedDiaryRows, diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }

        Assert.True(recovered.EnterActionContext(TownActionContext.Store));
        await PersistAsync(commandRepository, commandUnitOfWork, recovered);
        var freshRepository = CreateRepository(fixture, out _);
        var fresh = await freshRepository.GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        Assert.Equal(TownActionContext.Store, fresh!.CurrentActionContext);
        Assert.Equal(expectedTownId, fresh.CurrentActionContextTownId);
        await using var repairedContext = fixture.CreateContext();
        var repairedComponent = await repairedContext.GameSessionComponents.AsNoTracking().SingleAsync(candidate =>
            candidate.SessionId == session.Id.Value && candidate.ComponentName == "currentActionContext");
        Assert.Equal(ProjectionVersions.ForComponent("currentActionContext"), repairedComponent.ComponentVersion);
    }

    [Fact]
    public async Task DamagedCurrentActionContextCacheDoesNotHideInvalidEventHistory()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = CreateSession();
        Assert.True(session.EnterActionContext(TownActionContext.Saloon));
        await PersistAsync(writer, writerUnitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "currentActionContext");
            context.GameSessionComponents.Remove(component);
            var contextEvent = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == nameof(TownActionContextEntered));
            contextEvent.PayloadJson = "{}";
            await context.SaveChangesAsync();
        }

        var commandRepository = CreateRepository(fixture, out _);
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => commandRepository.GetByIdAsync(session.Id));
    }

    [Theory]
    [InlineData("missing-row")]
    [InlineData("null-root")]
    public async Task CommandLoad_WantedSuspectPresenceCacheRecoversFromEventsWithoutWritingBack(string damage)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = CreateSessionWithFledWantedSuspect();
        var targetSuspectId = new SuspectId("suspect-presence-target");
        var confrontationEvent = Assert.Single(session.AllEvents.OfType<WantedSuspectConfronted>());
        Assert.Equal(targetSuspectId, confrontationEvent.TargetSuspectId);
        Assert.Equal(WantedSuspectConfrontationChoice.Fled, confrontationEvent.Choice);
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, confrontationEvent.Outcome);
        var expectedPresence = session.GetWantedSuspectPresenceState(targetSuspectId);
        Assert.Equal(WantedSuspectPresenceState.GoneToGround, expectedPresence);
        await PersistAsync(writer, writerUnitOfWork, session);

        int originalComponentVersion;
        string? damagedPayload = null;
        (long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount) expectedEnvelope;
        (long Sequence, Guid EventId, string EventType, string PayloadJson, Guid? CorrelationId, Guid? CausationId, int SchemaVersion)[] expectedEvents;
        (int Sequence, string PayloadJson, int SchemaVersion)[] expectedDiaryRows;
        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "wantedSuspectPresenceLedger");
            originalComponentVersion = component.ComponentVersion;
            if (damage == "missing-row")
            {
                context.GameSessionComponents.Remove(component);
            }
            else
            {
                component.PayloadJson = "null";
                damagedPayload = component.PayloadJson;
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            expectedEnvelope = (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount);
            expectedEvents = (await context.StoredEvents.AsNoTracking()
                    .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                    .OrderBy(storedEvent => storedEvent.Sequence)
                    .Select(storedEvent => new
                    {
                        storedEvent.Sequence,
                        storedEvent.EventId,
                        storedEvent.EventType,
                        storedEvent.PayloadJson,
                        storedEvent.CorrelationId,
                        storedEvent.CausationId,
                        storedEvent.SchemaVersion
                    })
                    .ToArrayAsync())
                .Select(storedEvent => (
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion))
                .ToArray();
            expectedDiaryRows = (await context.GameSessionDiaryDays.AsNoTracking()
                    .Where(day => day.SessionId == session.Id.Value)
                    .OrderBy(day => day.Sequence)
                    .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                    .ToArrayAsync())
                .Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion))
                .ToArray();
            await context.SaveChangesAsync();
        }

        var commandRepository = CreateRepository(fixture, out var commandUnitOfWork);
        var recovered = await commandRepository.GetByIdAsync(session.Id);

        Assert.NotNull(recovered);
        Assert.Equal(expectedPresence, recovered!.GetWantedSuspectPresenceState(targetSuspectId));
        Assert.Single(recovered.CaseFile.WantedSuspectConfrontations);
        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.AsNoTracking().SingleOrDefaultAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "wantedSuspectPresenceLedger");
            if (damage == "missing-row")
            {
                Assert.Null(component);
            }
            else
            {
                Assert.NotNull(component);
                Assert.Equal(originalComponentVersion, component!.ComponentVersion);
                Assert.Equal(damagedPayload, component.PayloadJson);
            }

            var envelope = await context.GameSessions.AsNoTracking()
                .Where(entity => entity.Id == session.Id.Value)
                .Select(entity => new
                {
                    entity.SnapshotVersion,
                    entity.StreamVersion,
                    entity.TravelDiaryProjectionStreamVersion,
                    entity.TravelDiaryProjectionDayCount
                })
                .SingleAsync();
            Assert.Equal(expectedEnvelope, (
                envelope.SnapshotVersion,
                envelope.StreamVersion,
                envelope.TravelDiaryProjectionStreamVersion,
                envelope.TravelDiaryProjectionDayCount));
            var events = await context.StoredEvents.AsNoTracking()
                .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
                .OrderBy(storedEvent => storedEvent.Sequence)
                .Select(storedEvent => new
                {
                    storedEvent.Sequence,
                    storedEvent.EventId,
                    storedEvent.EventType,
                    storedEvent.PayloadJson,
                    storedEvent.CorrelationId,
                    storedEvent.CausationId,
                    storedEvent.SchemaVersion
                })
                .ToArrayAsync();
            Assert.Equal(expectedEvents, events.Select(storedEvent => (
                storedEvent.Sequence,
                storedEvent.EventId,
                storedEvent.EventType,
                storedEvent.PayloadJson,
                storedEvent.CorrelationId,
                storedEvent.CausationId,
                storedEvent.SchemaVersion)).ToArray());
            var diaryRows = await context.GameSessionDiaryDays.AsNoTracking()
                .Where(day => day.SessionId == session.Id.Value)
                .OrderBy(day => day.Sequence)
                .Select(day => new { day.Sequence, day.PayloadJson, day.SchemaVersion })
                .ToArrayAsync();
            Assert.Equal(expectedDiaryRows, diaryRows.Select(day => (day.Sequence, day.PayloadJson, day.SchemaVersion)).ToArray());
        }

        var foodOffer = new TownStoreCatalogResolver()
            .Resolve(recovered.World.GetTown(recovered.Player.CurrentTownId!.Value))
            .Offers.Single(offer => offer.ItemKind == DomainItemKind.Food);
        Assert.True(recovered.Purchase(foodOffer, 1).Success);
        await PersistAsync(commandRepository, commandUnitOfWork, recovered);

        var freshRepository = CreateRepository(fixture, out _);
        var fresh = await freshRepository.GetByIdAsync(session.Id);
        Assert.NotNull(fresh);
        Assert.Equal(expectedPresence, fresh!.GetWantedSuspectPresenceState(targetSuspectId));
        await using var repairedContext = fixture.CreateContext();
        var repairedComponent = await repairedContext.GameSessionComponents.AsNoTracking().SingleAsync(candidate =>
            candidate.SessionId == session.Id.Value && candidate.ComponentName == "wantedSuspectPresenceLedger");
        Assert.Equal(ProjectionVersions.ForComponent("wantedSuspectPresenceLedger"), repairedComponent.ComponentVersion);
    }

    [Fact]
    public async Task DamagedWantedSuspectPresenceCacheDoesNotHideInvalidEventHistory()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = CreateSessionWithFledWantedSuspect();
        await PersistAsync(writer, writerUnitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == "wantedSuspectPresenceLedger");
            context.GameSessionComponents.Remove(component);
            var confrontationEvent = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == nameof(WantedSuspectConfronted));
            confrontationEvent.PayloadJson = "{}";
            await context.SaveChangesAsync();
        }

        var commandRepository = CreateRepository(fixture, out _);
        await Assert.ThrowsAsync<JsonException>(() => commandRepository.GetByIdAsync(session.Id));
    }

    [Theory]
    [InlineData("missing-row")]
    [InlineData("malformed-cache")]
    public async Task DamagedJourneyCacheDoesNotHideInvalidJourneyStartedEvent(string damage)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateJourneyHistorySession();
        await PersistAsync(repository, unitOfWork, session);
        var active = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(active);
        Assert.True(active!.StartJourney(CreateJourneyPreview(
            active.Player.CurrentTownId!.Value,
            new TownId("openpass"),
            "Pinecross",
            "Open Pass")).Success);
        await PersistAsync(repository, unitOfWork, active);

        await using (var context = fixture.CreateContext())
        {
            var journey = await context.GameSessionComponents.SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "journey");
            if (damage == "missing-row")
            {
                context.GameSessionComponents.Remove(journey);
            }
            else
            {
                var journeyPayload = JsonNode.Parse(journey.PayloadJson)!.AsObject();
                journeyPayload["routeProfile"] = null;
                journey.PayloadJson = journeyPayload.ToJsonString();
            }

            var journeyStarted = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == nameof(JourneyStarted));
            var eventPayload = JsonNode.Parse(journeyStarted.PayloadJson)!.AsObject();
            eventPayload["journeySnapshot"]!["journeySequence"] = "not-an-integer";
            journeyStarted.PayloadJson = eventPayload.ToJsonString();
            await context.SaveChangesAsync();
        }

        var commandRepository = CreateRepository(fixture, out _);
        await Assert.ThrowsAsync<JsonException>(() => commandRepository.GetByIdAsync(session.Id));

        await using var readContext = fixture.CreateContext();
        var readRepository = new EfGameSessionReadRepository(readContext, CreateReadStoreLoader());
        await Assert.ThrowsAsync<JsonException>(() => readRepository.GetByIdAsync(session.Id));
    }

    [Fact]
    public async Task InvalidSetupEntropyCacheDoesNotHideUnreplayableHistory()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var writer = CreateRepository(fixture, out var writerUnitOfWork);
        var session = CreateSessionWithSeedCode("setup-entropy-history", GameEntropy.Wild);
        await PersistAsync(writer, writerUnitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var setup = await context.GameSessionComponents.SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "setup");
            var payload = JsonNode.Parse(setup.PayloadJson)!.AsObject();
            payload["gameEntropy"] = null;
            setup.PayloadJson = payload.ToJsonString();

            var worldGenerated = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == "WorldGenerated");
            context.StoredEvents.Remove(worldGenerated);
            await context.SaveChangesAsync();
        }

        var commandRepository = CreateRepository(fixture, out _);
        var commandError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => commandRepository.GetByIdAsync(session.Id));
        Assert.Contains("Sequence contains no elements", commandError.Message, StringComparison.Ordinal);

        await using var readContext = fixture.CreateContext();
        var readRepository = new EfGameSessionReadRepository(readContext, CreateReadStoreLoader());
        var queryError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => readRepository.GetByIdAsync(session.Id));
        Assert.Contains("Sequence contains no elements", queryError.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("player", "wallet")]
    [InlineData("player", "inventory")]
    [InlineData("player", "inventory.items")]
    [InlineData("world", "towns")]
    [InlineData("caseFile", "suspects")]
    [InlineData("clock", "day")]
    [InlineData("clock", "turn")]
    [InlineData("pursuitState", "heat")]
    [InlineData("saltSource", "salt")]
    [InlineData("saltSource", "mode")]
    public async Task ReadModel_CurrentRequiredComponentCacheShapeRebuildsFromEvents(string componentName, string missingProperty)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var commandRepository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateSession();
        await PersistAsync(commandRepository, unitOfWork, session);

        var purchased = await commandRepository.GetByIdAsync(session.Id);
        Assert.NotNull(purchased);
        var offer = new TownStoreCatalogResolver()
            .Resolve(purchased!.World.GetTown(purchased.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.ItemKind == DomainItemKind.Food);
        Assert.True(purchased.Purchase(offer, 2).Success);
        await PersistAsync(commandRepository, unitOfWork, purchased);

        var expectedName = purchased.Player.Name;
        var expectedCash = purchased.Player.Wallet.Cash;
        var expectedFood = purchased.Player.Inventory.GetQuantity(DomainItemKind.Food);
        var expectedTownIds = purchased.World.Towns.Select(town => town.Id.Value).ToArray();
        var expectedCulpritId = purchased.CaseFile.TrueCulpritId.Value;
        var expectedOpeningLead = purchased.CaseFile.OpeningLead.Description;
        var expectedCurrentTownName = purchased.World.GetTown(purchased.Player.CurrentTownId!.Value).Name;
        string malformedPayload;
        int componentVersion;
        long snapshotVersion;
        long streamVersion;
        long? diaryProjectionStreamVersion;
        int? diaryProjectionDayCount;
        int storedEventCount;
        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == componentName);
            var envelope = await context.GameSessions.SingleAsync(entity => entity.Id == session.Id.Value);
            var payload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            if (missingProperty == "inventory.items")
            {
                payload["inventory"]!["items"] = null;
            }
            else
            {
                payload[missingProperty] = null;
            }
            malformedPayload = payload.ToJsonString();
            component.PayloadJson = malformedPayload;
            componentVersion = component.ComponentVersion;
            snapshotVersion = envelope.SnapshotVersion!.Value;
            streamVersion = envelope.StreamVersion;
            diaryProjectionStreamVersion = envelope.TravelDiaryProjectionStreamVersion;
            diaryProjectionDayCount = envelope.TravelDiaryProjectionDayCount;
            storedEventCount = await context.StoredEvents.CountAsync(storedEvent => storedEvent.StreamId == session.Id.Value);
            await context.SaveChangesAsync();
        }

        Assert.Equal(snapshotVersion, streamVersion);
        Assert.Equal(ProjectionVersions.ForComponent(componentName), componentVersion);

        await using (var context = fixture.CreateContext())
        {
            var readRepository = new EfGameSessionReadRepository(context, CreateReadStoreLoader());
            var readModel = await readRepository.GetByIdAsync(session.Id);

            Assert.NotNull(readModel);
            Assert.Equal(expectedName, readModel!.Player.Name);
            Assert.Equal(expectedCash, readModel.Player.Wallet.Cash);
            Assert.Equal(expectedFood, readModel.Player.Inventory.GetQuantity(DomainItemKind.Food));
            Assert.Equal(expectedTownIds, readModel.World.Towns.Select(town => town.Id.Value));
            Assert.Equal(expectedCulpritId, readModel.CaseFile.TrueCulpritId.Value);
        }

        await using (var context = fixture.CreateContext())
        {
            var journalRepository = new EfGameJournalReadRepository(context, CreateReadStoreLoader());
            var journal = await journalRepository.GetByIdAsync(session.Id);

            Assert.NotNull(journal);
            Assert.Contains(journal!.LogEntries, entry =>
                entry.Kind == GameLogEntryKind.Purchase && entry.Message.Contains("Purchased 2 Food", StringComparison.Ordinal));
            Assert.Equal(expectedOpeningLead, journal.OpeningLead);
            Assert.Equal(expectedCurrentTownName, journal.CurrentTownName);
        }

        var aggregateRepository = CreateRepository(fixture, out var aggregateUnitOfWork);
        var aggregate = await aggregateRepository.GetByIdAsync(session.Id);
        Assert.NotNull(aggregate);
        Assert.Equal(expectedName, aggregate!.Player.Name);
        Assert.Equal(expectedCash, aggregate.Player.Wallet.Cash);
        Assert.Equal(expectedFood, aggregate.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(expectedTownIds, aggregate.World.Towns.Select(town => town.Id.Value));
        Assert.Equal(expectedCulpritId, aggregate.CaseFile.TrueCulpritId.Value);

        await using var verificationContext = fixture.CreateContext();
        var persistedComponent = await verificationContext.GameSessionComponents.AsNoTracking()
            .Where(component => component.SessionId == session.Id.Value && component.ComponentName == componentName)
            .Select(component => new { component.PayloadJson, component.ComponentVersion })
            .SingleAsync();
        var persistedEnvelope = await verificationContext.GameSessions.AsNoTracking()
            .Where(entity => entity.Id == session.Id.Value)
            .Select(entity => new
            {
                entity.SnapshotVersion,
                entity.StreamVersion,
                entity.TravelDiaryProjectionStreamVersion,
                entity.TravelDiaryProjectionDayCount
            })
            .SingleAsync();
        var persistedEventCount = await verificationContext.StoredEvents.CountAsync(storedEvent => storedEvent.StreamId == session.Id.Value);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(malformedPayload), JsonNode.Parse(persistedComponent.PayloadJson)));
        Assert.Equal(componentVersion, persistedComponent.ComponentVersion);
        Assert.Equal(snapshotVersion, persistedEnvelope.SnapshotVersion);
        Assert.Equal(streamVersion, persistedEnvelope.StreamVersion);
        Assert.Equal(diaryProjectionStreamVersion, persistedEnvelope.TravelDiaryProjectionStreamVersion);
        Assert.Equal(diaryProjectionDayCount, persistedEnvelope.TravelDiaryProjectionDayCount);
        Assert.Equal(storedEventCount, persistedEventCount);

        var resumedOffer = new TownStoreCatalogResolver()
            .Resolve(aggregate.World.GetTown(aggregate.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.ItemKind == DomainItemKind.Food);
        Assert.True(aggregate.Purchase(resumedOffer, 1).Success);
        var resumedCash = aggregate.Player.Wallet.Cash;
        var resumedFood = aggregate.Player.Inventory.GetQuantity(DomainItemKind.Food);
        await PersistAsync(aggregateRepository, aggregateUnitOfWork, aggregate);

        var freshRepository = CreateRepository(fixture, out _);
        var freshlyLoaded = await freshRepository.GetByIdAsync(session.Id);
        Assert.NotNull(freshlyLoaded);
        Assert.Equal(resumedCash, freshlyLoaded!.Player.Wallet.Cash);
        Assert.Equal(resumedFood, freshlyLoaded.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(expectedTownIds, freshlyLoaded.World.Towns.Select(town => town.Id.Value));
        Assert.Equal(expectedCulpritId, freshlyLoaded.CaseFile.TrueCulpritId.Value);

        await using var repairedContext = fixture.CreateContext();
        var repairedPayload = await repairedContext.GameSessionComponents.AsNoTracking()
            .Where(component => component.SessionId == session.Id.Value && component.ComponentName == componentName)
            .Select(component => component.PayloadJson)
            .SingleAsync();
        var repairedComponent = JsonNode.Parse(repairedPayload)!.AsObject();
        if (missingProperty == "inventory.items")
        {
            Assert.NotNull(repairedComponent["inventory"]);
            Assert.NotNull(repairedComponent["inventory"]!["items"]);
        }
        else
        {
            Assert.NotNull(repairedComponent[missingProperty]);
        }
    }

    [Theory]
    [InlineData("player", "wallet")]
    [InlineData("world", "towns")]
    [InlineData("caseFile", "suspects")]
    [InlineData("clock", "day")]
    [InlineData("pursuitState", "heat")]
    [InlineData("saltSource", "salt")]
    public async Task InvalidRequiredComponentCacheDoesNotHideUnreplayableHistory(string componentName, string missingProperty)
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var commandRepository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateSession();
        await PersistAsync(commandRepository, unitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var component = await context.GameSessionComponents.SingleAsync(candidate =>
                candidate.SessionId == session.Id.Value && candidate.ComponentName == componentName);
            var payload = JsonNode.Parse(component.PayloadJson)!.AsObject();
            payload[missingProperty] = null;
            component.PayloadJson = payload.ToJsonString();

            var worldGenerated = await context.StoredEvents.SingleAsync(storedEvent =>
                storedEvent.StreamId == session.Id.Value && storedEvent.EventType == "WorldGenerated");
            context.StoredEvents.Remove(worldGenerated);
            await context.SaveChangesAsync();
        }

        var aggregateRepository = CreateRepository(fixture, out _);
        var aggregateError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => aggregateRepository.GetByIdAsync(session.Id));
        Assert.Contains("Sequence contains no elements", aggregateError.Message, StringComparison.Ordinal);

        if (componentName != "saltSource")
        {
            await using var readContext = fixture.CreateContext();
            var readRepository = new EfGameSessionReadRepository(readContext, CreateReadStoreLoader());
            var readError = await Assert.ThrowsAsync<InvalidOperationException>(
                () => readRepository.GetByIdAsync(session.Id));
            Assert.Contains("Sequence contains no elements", readError.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ReadModel_StartingTownSelectedRestoresPhase()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);
        var town = new Town(new TownId("dustvale"), "Dustvale");
        var world = new WildBunch.Domain.World.World(new[] { town }, Array.Empty<Trail>());
        var session = GameSession.StartSetup(
            "Ranger Vale", world, CreateCaseFile(),
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(town.Id);

        await PersistAsync(repository, unitOfWork, session);

        var readRepository = new EfGameSessionReadRepository(fixture.CreateContext(), CreateReadStoreLoader());
        var readModel = await readRepository.GetByIdAsync(session.Id);

        Assert.NotNull(readModel);
        Assert.Equal(StartFlowPhase.StartingTownSelected, readModel!.StartFlowPhase);
        Assert.Null(readModel.TownVisitState);
    }

    [Fact]
    public async Task ReadModels_ConcurrentArchiveReturnsOneCoherentState()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var setupRepository = CreateRepository(fixture, out var setupUnitOfWork);
        var session = CreateSession();
        await PersistAsync(setupRepository, setupUnitOfWork, session);
        var foodPrice = new TownStoreCatalogResolver()
            .Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(offer => offer.ItemKind == DomainItemKind.Food)
            .Price;

        var interceptor = new PauseAfterTwoEnvelopeQueriesInterceptor();
        var readOptions = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(fixture.Database.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        var readStoreLoader = CreateReadStoreLoader();
        await using var playerReadContext = new WildBunchDbContext(readOptions);
        await using var journalReadContext = new WildBunchDbContext(readOptions);
        var readRepository = new EfGameSessionReadRepository(playerReadContext, readStoreLoader);
        var journalRepository = new EfGameJournalReadRepository(journalReadContext, readStoreLoader);

        var playerReadTask = readRepository.GetByIdAsync(session.Id);
        var journalReadTask = journalRepository.GetByIdAsync(session.Id);
        try
        {
            await interceptor.BothEnvelopeQueriesExecuted.WaitAsync(TimeSpan.FromSeconds(30));

            var writerRepository = CreateRepository(fixture, out var writerUnitOfWork);
            var writerSession = await writerRepository.GetByIdAsync(session.Id);
            Assert.NotNull(writerSession);
            var foodOffer = new TownStoreCatalogResolver()
                .Resolve(writerSession!.World.GetTown(writerSession.Player.CurrentTownId!.Value))
                .Offers.Single(offer => offer.ItemKind == DomainItemKind.Food);
            Assert.True(writerSession.Purchase(foodOffer, 2).Success);
            writerSession.ArchivePlaythrough("start-over", new DateTime(2026, 10, 8, 12, 0, 0, DateTimeKind.Utc));
            await PersistAsync(writerRepository, writerUnitOfWork, writerSession);
        }
        finally
        {
            interceptor.ReleaseReaders();
        }

        var playerRead = await playerReadTask;
        var journalRead = await journalReadTask;
        Assert.NotNull(playerRead);
        Assert.Equal(GameStatus.Active, playerRead!.Status);
        Assert.Equal(session.Player.Wallet.Cash, playerRead.Player.Wallet.Cash);
        Assert.DoesNotContain(playerRead.LogEntries, entry => entry.Kind == GameLogEntryKind.Purchase);
        Assert.NotNull(journalRead);
        Assert.Equal(GameStatus.Active, journalRead!.Status);
        Assert.DoesNotContain(journalRead.LogEntries, entry => entry.Kind == GameLogEntryKind.Purchase);

        await using var freshPlayerReadContext = fixture.CreateContext();
        await using var freshJournalReadContext = fixture.CreateContext();
        var freshReadRepository = new EfGameSessionReadRepository(freshPlayerReadContext, readStoreLoader);
        var freshJournalRepository = new EfGameJournalReadRepository(freshJournalReadContext, readStoreLoader);
        var freshPlayerRead = await freshReadRepository.GetByIdAsync(session.Id);
        var freshJournalRead = await freshJournalRepository.GetByIdAsync(session.Id);
        Assert.NotNull(freshPlayerRead);
        Assert.Equal(GameStatus.Archived, freshPlayerRead!.Status);
        Assert.Equal(session.Player.Wallet.Cash - foodPrice * 2, freshPlayerRead.Player.Wallet.Cash);
        Assert.NotNull(freshJournalRead);
        Assert.Equal(GameStatus.Archived, freshJournalRead!.Status);
        Assert.Contains(freshJournalRead.LogEntries, entry => entry.Kind == GameLogEntryKind.Purchase);
    }

    [Fact]
    public async Task CommandLoad_ConcurrentAppendReturnsOneConsistentSnapshot()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        await using var setupContext = fixture.CreateContext();
        var setupRepository = CreateRepository(setupContext, out var setupUnitOfWork);
        var session = CreateSession();
        await PersistAsync(setupRepository, setupUnitOfWork, session);
        var initialCash = session.Player.Wallet.Cash;
        var initialFood = session.Player.GetQuantity(DomainItemKind.Food);
        var interceptor = new PauseAfterSecondCommandEnvelopeReadInterceptor();
        var readOptions = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(fixture.Database.ConnectionString)
            .AddInterceptors(interceptor)
            .Options;
        await using var readerContext = new WildBunchDbContext(readOptions);
        var readerRepository = CreateRepository(readerContext, out _);
        Task<GameSession?>? readerTask = null;
        long expectedVersion = 0;
        decimal expectedCash = 0;
        int expectedFood = 0;

        try
        {
            readerTask = readerRepository.GetByIdAsync(session.Id);
            await interceptor.SecondEnvelopeReadPaused.WaitAsync(TimeSpan.FromSeconds(30));

            await using var writerContext = fixture.CreateContext();
            var writerRepository = CreateRepository(writerContext, out var writerUnitOfWork);
            var writerSession = await writerRepository.GetByIdAsync(session.Id);
            Assert.NotNull(writerSession);
            var foodOffer = new TownStoreCatalogResolver()
                .Resolve(writerSession!.World.GetTown(writerSession.Player.CurrentTownId!.Value))
                .Offers.Single(offer => offer.ItemKind == DomainItemKind.Food);
            Assert.True(writerSession.Purchase(foodOffer, 2).Success);
            expectedVersion = session.Version + writerSession.UncommittedEvents.Count;
            expectedCash = writerSession.Player.Wallet.Cash;
            expectedFood = writerSession.Player.GetQuantity(DomainItemKind.Food);
            await PersistAsync(writerRepository, writerUnitOfWork, writerSession);
            Assert.Equal(expectedVersion, writerSession.Version);
        }
        finally
        {
            interceptor.ReleaseReader();
        }

        var inFlightRead = await readerTask!;
        Assert.NotNull(inFlightRead);
        Assert.Equal(session.Version, inFlightRead!.Version);
        Assert.Equal(initialCash, inFlightRead.Player.Wallet.Cash);
        Assert.Equal(initialFood, inFlightRead.Player.GetQuantity(DomainItemKind.Food));

        await using var verificationContext = fixture.CreateContext();
        var verificationRepository = CreateRepository(verificationContext, out _);
        var freshRead = await verificationRepository.GetByIdAsync(session.Id);
        Assert.NotNull(freshRead);
        Assert.Equal(expectedVersion, freshRead!.Version);
        Assert.Equal(expectedCash, freshRead.Player.Wallet.Cash);
        Assert.Equal(expectedFood, freshRead.Player.GetQuantity(DomainItemKind.Food));

        var events = await verificationRepository.GetEventStreamAsync(session.Id);
        Assert.Equal(expectedVersion, events.Count);
        Assert.Single(events.OfType<StoreItemPurchased>());

        var storedEvents = await verificationContext.StoredEvents.AsNoTracking()
            .Where(storedEvent => storedEvent.StreamId == session.Id.Value)
            .OrderBy(storedEvent => storedEvent.Sequence)
            .Select(storedEvent => new { storedEvent.Sequence, storedEvent.EventType })
            .ToArrayAsync();
        Assert.Equal(
            Enumerable.Range(1, checked((int)expectedVersion)).Select(sequence => (long)sequence),
            storedEvents.Select(storedEvent => storedEvent.Sequence));
        Assert.Single(storedEvents, storedEvent => storedEvent.EventType == nameof(StoreItemPurchased));
    }

    private static EfGameSessionRepository CreateRepository(PostgreSqlPersistenceFixture fixture, out EfGameSessionUnitOfWork unitOfWork)
    {
        return CreateRepository(fixture.CreateContext(), out unitOfWork);
    }

    private static EfGameSessionRepository CreateRepository(WildBunchDbContext context, out EfGameSessionUnitOfWork unitOfWork)
    {
        unitOfWork = new EfGameSessionUnitOfWork(context);
        var serializer = new GameSessionJsonSerializer();
        var registry = new PayloadUpcasterRegistry(DependencyInjection.CreateDefaultUpcasters());
        var payloadLoader = new PersistedPayloadLoader(
            registry,
            serializer,
            new TravelDiaryDayProjector(),
            rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
        return new EfGameSessionRepository(context, serializer, new TravelDiaryDayProjector(), registry, payloadLoader);
    }

    private static GameSessionReadStoreLoader CreateReadStoreLoader()
    {
        var serializer = new GameSessionJsonSerializer();
        var registry = new PayloadUpcasterRegistry(DependencyInjection.CreateDefaultUpcasters());
        var payloadLoader = new PersistedPayloadLoader(
            registry,
            serializer,
            new TravelDiaryDayProjector(),
            rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
        return new GameSessionReadStoreLoader(payloadLoader, serializer);
    }

    private sealed record LegacyWorldGeneratedState(
        string LegacyWorldPayloadJson,
        string StalePlayerPayloadJson,
        long SnapshotVersion,
        long StreamVersion);

    private sealed record StoredEventEvidence(
        string EventType,
        long Sequence,
        string PayloadJson,
        int SchemaVersion);

    private static async Task<LegacyWorldGeneratedState> DowngradeWorldGeneratedAndStalePlayerCacheAsync(
        PostgreSqlPersistenceFixture fixture,
        GameSessionId sessionId)
    {
        await using var context = fixture.CreateContext();
        var worldEvent = await context.StoredEvents.SingleAsync(storedEvent =>
            storedEvent.StreamId == sessionId.Value && storedEvent.EventType == "WorldGenerated");
        var legacyPayload = JsonNode.Parse(worldEvent.PayloadJson)!.AsObject();
        Assert.Equal(2, worldEvent.SchemaVersion);
        Assert.NotNull(legacyPayload["caseFile"]);
        legacyPayload.Remove("caseFile");
        worldEvent.PayloadJson = legacyPayload.ToJsonString();
        worldEvent.SchemaVersion = 1;

        var playerComponent = await context.GameSessionComponents.SingleAsync(component =>
            component.SessionId == sessionId.Value && component.ComponentName == "player");
        var stalePlayer = JsonNode.Parse(playerComponent.PayloadJson)!.AsObject();
        stalePlayer["name"] = "Stale Player Cache";
        playerComponent.PayloadJson = stalePlayer.ToJsonString();

        var envelope = await context.GameSessions.SingleAsync(entity => entity.Id == sessionId.Value);
        envelope.SnapshotVersion = envelope.StreamVersion - 1;
        await context.SaveChangesAsync();
        await context.Entry(worldEvent).ReloadAsync();
        await context.Entry(playerComponent).ReloadAsync();
        await context.Entry(envelope).ReloadAsync();
        return new LegacyWorldGeneratedState(
            worldEvent.PayloadJson,
            playerComponent.PayloadJson,
            envelope.SnapshotVersion!.Value,
            envelope.StreamVersion);
    }

    private static async Task<(StoredEventEvidence World, StoredEventEvidence CaseFile)> ReadWorldAndCaseFileEventsAsync(
        PostgreSqlPersistenceFixture fixture,
        GameSessionId sessionId)
    {
        await using var context = fixture.CreateContext();
        var events = await context.StoredEvents.AsNoTracking()
            .Where(storedEvent => storedEvent.StreamId == sessionId.Value
                && (storedEvent.EventType == "WorldGenerated" || storedEvent.EventType == "CaseFileGenerated"))
            .Select(storedEvent => new StoredEventEvidence(
                storedEvent.EventType,
                storedEvent.Sequence,
                storedEvent.PayloadJson,
                storedEvent.SchemaVersion))
            .ToArrayAsync();
        return (
            events.Single(storedEvent => storedEvent.EventType == "WorldGenerated"),
            events.Single(storedEvent => storedEvent.EventType == "CaseFileGenerated"));
    }

    private sealed class PauseAfterTwoEnvelopeQueriesInterceptor : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _bothEnvelopeQueriesExecuted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseReaders = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _envelopeQueryCount;

        public Task BothEnvelopeQueriesExecuted => _bothEnvelopeQueriesExecuted.Task;

        public void ReleaseReaders() => _releaseReaders.TrySetResult();

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"GameSessions\"", StringComparison.Ordinal))
            {
                if (Interlocked.Increment(ref _envelopeQueryCount) == 2)
                {
                    _bothEnvelopeQueriesExecuted.TrySetResult();
                }

                await _releaseReaders.Task.WaitAsync(cancellationToken);
            }

            return result;
        }
    }

    private sealed class PauseAfterSecondCommandEnvelopeReadInterceptor : DbCommandInterceptor
    {
        private readonly TaskCompletionSource _secondEnvelopeReadPaused = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseReader = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _envelopeQueryCount;

        public Task SecondEnvelopeReadPaused => _secondEnvelopeReadPaused.Task;

        public void ReleaseReader() => _releaseReader.TrySetResult();

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FROM \"GameSessions\"", StringComparison.Ordinal)
                && Interlocked.Increment(ref _envelopeQueryCount) == 2)
            {
                _secondEnvelopeReadPaused.TrySetResult();
                await _releaseReader.Task.WaitAsync(cancellationToken);
            }

            return result;
        }
    }

    private static async Task PersistAsync(
        EfGameSessionRepository repository,
        EfGameSessionUnitOfWork unitOfWork,
        GameSession session)
    {
        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();
    }

    private static void PurchaseFood(GameSession session, int quantity)
    {
        var offer = new TownStoreCatalogResolver()
            .Resolve(session.World.GetTown(session.Player.CurrentTownId!.Value))
            .Offers.Single(candidate => candidate.ItemKind == DomainItemKind.Food);
        Assert.True(session.Purchase(offer, quantity).Success);
    }

    private static GameSession CreateSessionWithSeedCode(string seedCode, GameEntropy gameEntropy = GameEntropy.Classic, SaltSource? saltSource = null)
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale");
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek");
        var holloway = new Town(new TownId("holloway"), "Holloway");
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge");

        var world = new WildBunch.Domain.World.World(
            new[] { dustvale, silvercreek, holloway, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low),
                new Trail(new TrailId("trail-2"), dustvale.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.River)
            });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Ira Flint",
                new SuspectProfile(
                    new[] { new SuspectAlias("Dust Runner", AliasKind.Nickname) },
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("Wears a brass buckle with a cracked star engraving.", "a brass buckle with a cracked star engraving", "wears a brass buckle with a cracked star engraving")) }),
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            null,
            suspects,
            new SuspectId("suspect-1"),
            CaseOpeningLead.Create("A brass buckle bears a cracked star engraving."),
            Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 4),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: DomainCanteenState.Full(10)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale",
            world,
            caseFile,
            GameDifficulty.Standard,
            gameEntropy,
            seedCode,
            saltSource ?? DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(dustvale.Id);
        session.CompleteGameStart(WildBunch.Domain.Economy.Wallet.Starting(25m), inventory);
        session.MarkEventsCommitted();
        return session;
    }

    private static GameSession CreateSessionWithFledWantedSuspect()
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
        var world = new WildBunch.Domain.World.World(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-presence"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });
        var targetId = new SuspectId("suspect-presence-target");
        var culpritId = new SuspectId("suspect-presence-culprit");
        var caseFile = new CaseFile(
            accusation: null,
            new[]
            {
                new Suspect(targetId, "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
                new Suspect(culpritId, "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
            },
            trueCulpritId: culpritId,
            openingLead: CaseOpeningLead.Create("Look for a recognizable wanted suspect."),
            knownClues: Array.Empty<Clue>(),
            discoveredSuspectIds: new[] { targetId },
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        250m,
                        new[] { "Red Wren" },
                        new[] { "Raven-feather pin" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for a stage robbery.")
            });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(currentTown.Id);
        session.CompleteGameStart();
        session.SetWantedSuspectPresenceState(targetId, WantedSuspectPresenceState.AvailableInTown);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(targetId));
        Assert.True(session.LookAroundSaloon().Success);
        var confrontation = session.ConfrontSaloonPersonOfInterest("warrant-1");
        Assert.True(confrontation.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Fled, confrontation.Outcome);
        return session;
    }

    private static GameSession CreateSessionWithSaloonSuspect()
    {
        var startingTown = new Town(new TownId("dustvale"), "Dustvale");
        var destinationTown = new Town(new TownId("silvercreek"), "Silver Creek");
        var world = new WildBunch.Domain.World.World(
            new[] { startingTown, destinationTown },
            new[] { new Trail(new TrailId("saloon-test-trail"), startingTown.Id, destinationTown.Id, TrailRisk.Low) });
        var saloonSuspectId = new SuspectId("saloon-suspect");
        var culpritId = new SuspectId("culprit");
        var suspects = new[]
        {
            new Suspect(saloonSuspectId, "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(culpritId, "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(
            null,
            suspects,
            culpritId,
            CaseOpeningLead.Create("Look for a recognizable wanted suspect."),
            Array.Empty<Clue>(),
            discoveredSuspectIds: new[] { saloonSuspectId });

        var session = GameSession.StartSetup(
            "Ranger Vale",
            world,
            caseFile,
            GameDifficulty.Standard,
            GameEntropy.Classic,
            "saloon-cache-seed",
            SaltSource.CreateFixed("saloon-14"));
        session.ViewPrologue("saloon-cache-prologue");
        session.SelectStartingTown(startingTown.Id);
        session.CompleteGameStart(Wallet.Starting(25m), new DomainInventory(Array.Empty<DomainInventoryItem>()));
        return session;
    }

    private static GameSession CreateSession(bool includeKnownClue = false, IEnumerable<Clue>? publicClues = null)
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale");
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek");
        var holloway = new Town(new TownId("holloway"), "Holloway");
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge");

        var world = new WildBunch.Domain.World.World(
            new[] { dustvale, silvercreek, holloway, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low),
                new Trail(new TrailId("trail-2"), dustvale.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.River)
            });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Ira Flint",
                new SuspectProfile(
                    new[] { new SuspectAlias("Dust Runner", AliasKind.Nickname) },
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("Wears a brass buckle with a cracked star engraving.", "a brass buckle with a cracked star engraving", "wears a brass buckle with a cracked star engraving")) }),
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge)
        };

        var knownClues = includeKnownClue
            ? new[]
            {
                new Clue(
                    new ClueId("known-legacy-clue"),
                    ClueKind.Record,
                    "A known record clue for historical replay.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.TrueCulprit)
            }
            : Array.Empty<Clue>();
        var caseFile = new CaseFile(
            null,
            suspects,
            new SuspectId("suspect-1"),
            CaseOpeningLead.Create("A brass buckle bears a cracked star engraving."),
            knownClues,
            publicClues: publicClues);
        caseFile.DiscoverSuspect(new SuspectId("suspect-1"));

        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.HorseFeed, 2),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new DomainCanteenState(1, 2)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, 4)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(dustvale.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private static GameSession CreateLuckySession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale");
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek");
        var world = new WildBunch.Domain.World.World(
            new[] { dustvale, silvercreek },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek)
            });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Ira Flint",
                new SuspectProfile(
                    new[] { new SuspectAlias("Dust Runner", AliasKind.Nickname) },
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("Wears a brass buckle with a cracked star engraving.", "a brass buckle with a cracked star engraving", "wears a brass buckle with a cracked star engraving")) }),
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            null,
            suspects,
            new SuspectId("suspect-1"),
            CaseOpeningLead.Create("A brass buckle bears a cracked star engraving."),
            Array.Empty<Clue>());

        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(dustvale.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private static GameSession CreateEasySession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale");
        var holloway = new Town(new TownId("holloway"), "Holloway");

        var world = new WildBunch.Domain.World.World(
            new[] { dustvale, holloway },
            new[]
            {
                new Trail(new TrailId("trail-easy"), dustvale.Id, holloway.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 5m)
            });

        var caseFile = new CaseFile(
            null,
            Array.Empty<Suspect>(),
            new SuspectId("suspect-1"),
            CaseOpeningLead.Create("A brass buckle bears a cracked star engraving."),
            Array.Empty<Clue>());

        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new DomainCanteenState(10, 10)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, new DomainHorseTravelState(3, 2, 3)),
            new DomainInventoryItem(DomainItemKind.Saddle, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Easy, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(dustvale.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private static GameSession CreateDryTravelSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale");
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge");
        var world = new WildBunch.Domain.World.World(
            new[] { dustvale, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, dryridge.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 5m)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());

        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.HorseFeed, 1),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(dustvale.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private static GameSession CreateHorseLossFallbackSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var midway = new Town(new TownId("midway"), "Midway");
        var world = new WildBunch.Domain.World.World(
            new[] { pinecross, midway },
            new[]
            {
                new Trail(new TrailId("trail-pine-midway"), pinecross.Id, midway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.River, 2m)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, new DomainHorseTravelState(0, 0, 1)),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Challenging, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private static GameSession CreateJourneyHistorySession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var openpass = new Town(new TownId("openpass"), "Open Pass");
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork");
        var world = new WildBunch.Domain.World.World(
            new[] { pinecross, openpass, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-open"), pinecross.Id, openpass.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m),
                new Trail(new TrailId("trail-open-dry"), openpass.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 6),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new DomainCanteenState(6, 6)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Easy, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private static TravelPreview CreateJourneyPreview(TownId originTownId, TownId destinationTownId, string originTownName, string destinationTownName)
        => new(
            originTownId,
            destinationTownId,
            originTownName,
            destinationTownName,
            new TravelRouteProfile("trail-preview", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 1m, 1m, 1m, Array.Empty<string>()),
            TravelMode.Mounted,
            MountedTravelAvailable: true,
            WaterSecure: true,
            RideDayDistance: 1m,
            RemainingRideDayDistance: 1m,
            BaselineRideDays: 1,
            ExpectedDays: 1,
            RemainingDays: 1,
            CanteenChargesPerDay: 0,
            RequiredCanteenCharges: 0,
            AvailableCanteenCharges: 0,
            CanteenReserveCharges: 0,
            DelayMarginDays: 0,
            DelayRisk: false,
            RequiredFood: 1,
            AvailableFood: 6,
            RequiredHorseFeed: 0,
            AvailableHorseFeed: 0,
            HorseState: DomainHorseTravelState.Healthy,
            Warnings: Array.Empty<string>());

    private static GameSession CreateDiarySession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var openpass = new Town(new TownId("openpass"), "Open Pass");
        var world = new WildBunch.Domain.World.World(
            new[] { pinecross, openpass },
            new[]
            {
                new Trail(new TrailId("trail-diary"), pinecross.Id, openpass.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Easy, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        return new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
    }

    private static GameSession CreateHighRiskSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork");
        var world = new WildBunch.Domain.World.World(
            new[] { pinecross, dryfork },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, DomainHorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, 2)
        });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart(Wallet.Starting(25m), inventory);
        return session;
    }

    /// <summary>
    /// Regression: DeriveStartFlowPhase previously did not check for
    /// StartingTownSelected. A session whose latest event was StartingTownSelected
    /// would reload as PrologueViewed instead of StartingTownSelected.
    /// In production, SelectStartingTown and CompleteGameStart are always called
    /// in the same command (CompleteGameStartHandler), so StartingTownSelected
    /// is always followed by GameStarted in the same transaction. This test
    /// persists the intermediate state directly to verify DeriveStartFlowPhase
    /// handles it correctly if the flow is ever split.
    /// </summary>
    [Fact]
    public async Task SaveAndLoad_WithStartingTownSelectedOnly_RestoresCorrectStartFlowPhase()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = CreateRepository(fixture, out var unitOfWork);

        var dustvale = new Town(new TownId("dustvale"), "Dustvale");
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek");
        var world = new WildBunch.Domain.World.World(
            new[] { dustvale, silvercreek },
            new[] { new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low) });
        var caseFile = CreateCaseFile();

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", DeterministicSaltSource);
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(dustvale.Id);
        // Do NOT call CompleteGameStart — persist with StartingTownSelected as latest event.

        await PersistAsync(repository, unitOfWork, session);

        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(StartFlowPhase.StartingTownSelected, reloaded!.StartFlowPhase);
    }
}
