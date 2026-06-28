using Microsoft.EntityFrameworkCore;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
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
using System.Text.Json;

namespace WildBunch.Integration.Tests;

public sealed class EfGameSessionRepositoryTests
{
    private static readonly TravelRandomnessState DeterministicTravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty);

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
        Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
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

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("holloway"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
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
        Assert.Equal(2, reloaded.Player.Inventory.GetCanteenState()!.Charges);
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

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("dryfork"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
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

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("dryfork"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
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

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("silvercreek"), loaded.Player.Inventory);

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

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("openpass"), loaded.Player.Inventory, loaded.TravelRules);

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

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("dryridge"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
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

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("midway"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
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

        var firstPreview = CreateJourneyPreview(loaded!.Player.CurrentTownId, new TownId("openpass"), "Pinecross", "Open Pass");
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

        var secondPreview = CreateJourneyPreview(reloaded.Player.CurrentTownId, new TownId("dryfork"), "Open Pass", "Dry Fork");
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

        var preview = travelResolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("holloway"), loaded.Player.Inventory);
        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await PersistAsync(commandRepository, unitOfWork, loaded);

        var readRepository = new EfGameSessionReadRepository(fixture.CreateContext(), new GameSessionJsonSerializer());
        var journalRepository = new EfGameJournalReadRepository(fixture.CreateContext(), new GameSessionJsonSerializer());

        var sessionRead = await readRepository.GetByIdAsync(session.Id);
        var journalRead = await journalRepository.GetByIdAsync(session.Id, take: 2);

        Assert.NotNull(sessionRead);
        Assert.Equal(loaded!.Status, sessionRead!.Status);
        Assert.Equal(loaded.GameDifficulty, sessionRead.GameDifficulty);
        Assert.Equal(loaded.Player.CurrentTownId, sessionRead.Player.CurrentTownId);
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

    private static EfGameSessionRepository CreateRepository(PostgreSqlPersistenceFixture fixture, out EfGameSessionUnitOfWork unitOfWork)
    {
        var context = fixture.CreateContext();
        unitOfWork = new EfGameSessionUnitOfWork(context);
        return new EfGameSessionRepository(context, new GameSessionJsonSerializer());
    }

    private static async Task PersistAsync(
        EfGameSessionRepository repository,
        EfGameSessionUnitOfWork unitOfWork,
        GameSession session)
    {
        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();
    }

    private static GameSession CreateSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

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
                    new[] { new SuspectIdentityFact("Wears a brass buckle with a cracked star engraving.") }),
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory, travelRandomness: DeterministicTravelRandomness);
    }

    private static GameSession CreateLuckySession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
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
                    new[] { new SuspectIdentityFact("Wears a brass buckle with a cracked star engraving.") }),
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory, travelRandomness: DeterministicTravelRandomness);
    }

    private static GameSession CreateEasySession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);

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

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            dustvale.Id,
            Wallet.Starting(25m),
            inventory,
            GameDifficulty.Easy, travelRandomness: DeterministicTravelRandomness);
    }

    private static GameSession CreateDryTravelSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory, travelRandomness: DeterministicTravelRandomness);
    }

    private static GameSession CreateHorseLossFallbackSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var midway = new Town(new TownId("midway"), "Midway", TownServices.None);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Hard, travelRandomness: DeterministicTravelRandomness);
    }

    private static GameSession CreateJourneyHistorySession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var openpass = new Town(new TownId("openpass"), "Open Pass", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Easy, travelRandomness: DeterministicTravelRandomness);
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
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var openpass = new Town(new TownId("openpass"), "Open Pass", TownServices.None);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Easy, travelRandomness: DeterministicTravelRandomness);
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
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, travelRandomness: DeterministicTravelRandomness);
    }
}
