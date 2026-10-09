using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WildBunch.Application.Games.Commands;
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
        var repository = CreateRepository(fixture, out var unitOfWork);
        var session = CreateSession();
        var originalCaseFile = session.CaseFile;
        await PersistAsync(repository, unitOfWork, session);

        await using (var context = fixture.CreateContext())
        {
            var worldEvent = await context.StoredEvents.SingleAsync(e =>
                e.StreamId == session.Id.Value && e.EventType == "WorldGenerated");
            var payload = System.Text.Json.Nodes.JsonNode.Parse(worldEvent.PayloadJson)!.AsObject();
            Assert.Equal(2, worldEvent.SchemaVersion);
            Assert.NotNull(payload["caseFile"]);
            payload.Remove("caseFile");
            worldEvent.PayloadJson = payload.ToJsonString();
            worldEvent.SchemaVersion = 1;
            await context.SaveChangesAsync();

            var storedLegacyVersion = await context.StoredEvents
                .Where(e => e.StreamId == session.Id.Value && e.EventType == "WorldGenerated")
                .Select(e => e.SchemaVersion).SingleAsync();
            Assert.Equal(1, storedLegacyVersion);
        }

        var loaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(loaded);
        Assert.Equal(originalCaseFile.TrueCulpritId, loaded!.CaseFile.TrueCulpritId);
        Assert.Equal(originalCaseFile.Suspects.Select(s => s.Id), loaded.CaseFile.Suspects.Select(s => s.Id));

        var eventStream = await repository.GetEventStreamAsync(session.Id);
        var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, eventStream);
        Assert.Equal(originalCaseFile.TrueCulpritId, replayed.CaseFile.TrueCulpritId);
        Assert.Equal(originalCaseFile.KnownClues.Select(c => c.Id), replayed.CaseFile.KnownClues.Select(c => c.Id));
        Assert.Contains(await repository.GetByStatusAsync(GameStatus.Active), s => s.Id == session.Id);
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
        loaded.Journey!.MarkCompleted();
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

    [Fact]
    public async Task ReadModel_MalformedActiveJourneyCacheRecoversWithoutWritingBack()
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
        var active = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(active);
        Assert.True(active!.StartJourney(CreateJourneyPreview(
            active.Player.CurrentTownId!.Value,
            new TownId("openpass"),
            "Pinecross",
            "Open Pass")).Success);
        var expectedJourney = active.Journey!.ToSnapshot();
        await PersistAsync(repository, unitOfWork, active);

        string damagedPayload;
        int componentVersion;
        (long? SnapshotVersion, long StreamVersion, long? DiaryStreamVersion, int? DiaryDayCount) expectedEnvelope;
        (long Sequence, Guid EventId, string EventType, string PayloadJson, Guid? CorrelationId, Guid? CausationId, int SchemaVersion)[] expectedEvents;
        (int Sequence, string PayloadJson, int SchemaVersion)[] expectedDiaryRows;
        await using (var context = fixture.CreateContext())
        {
            var journey = await context.GameSessionComponents.SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "journey");
            componentVersion = journey.ComponentVersion;
            var payload = JsonNode.Parse(journey.PayloadJson)!.AsObject();
            payload.Remove("routeProfile");
            journey.PayloadJson = payload.ToJsonString();
            damagedPayload = journey.PayloadJson;

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
            var journey = await context.GameSessionComponents.AsNoTracking().SingleAsync(component =>
                component.SessionId == session.Id.Value && component.ComponentName == "journey");
            Assert.Equal(componentVersion, journey.ComponentVersion);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(damagedPayload), JsonNode.Parse(journey.PayloadJson)));

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
    public async Task MalformedJourneyCacheDoesNotHideInvalidJourneyStartedEvent()
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
            var journeyPayload = JsonNode.Parse(journey.PayloadJson)!.AsObject();
            journeyPayload["routeProfile"] = null;
            journey.PayloadJson = journeyPayload.ToJsonString();

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

    private static GameSession CreateSession()
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
            Array.Empty<Clue>());
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
