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

namespace WildBunch.Integration.Tests;

public sealed class EfGameSessionRepositoryTests
{
    [Fact]
    public async Task SaveAndLoadNewSessionRoundTripsThroughSqlite()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var session = CreateSession();

        await repository.SaveAsync(session);
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
        Assert.Equal(session.LogEntries.Count, reloaded.LogEntries.Count);
        Assert.Equal(session.CaseFile.OpeningLead.Description, reloaded.CaseFile.OpeningLead.Description);
        Assert.Equal(session.CaseFile.KillerReleaseState.IsReleased, reloaded.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(session.CaseFile.KillerReleaseState.Progress, reloaded.CaseFile.KillerReleaseState.Progress);
        Assert.Equal(session.CaseFile.KillerReleaseState.RequiredPublicClues, reloaded.CaseFile.KillerReleaseState.RequiredPublicClues);
        Assert.Equal(session.CaseFile.DiscoveredSuspectIds, reloaded.CaseFile.DiscoveredSuspectIds);
        Assert.Equal(session.CaseFile.Suspects[0].Profile.Aliases.Count, reloaded.CaseFile.Suspects[0].Profile.Aliases.Count);
    }

    [Fact]
    public async Task SaveAndLoadEasyTravelSessionRetainsTravelDifficulty()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var session = CreateEasySession();

        await repository.SaveAsync(session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TravelDifficulty.Easy, reloaded!.TravelDifficulty);
        Assert.Equal(10, reloaded.Player.Inventory.GetCanteenState()!.Capacity);
        Assert.True(reloaded.Player.Inventory.GetHorseState()!.CanProvideMountedTravelFor(TravelRulesProfile.For(TravelDifficulty.Easy)));
    }

    [Fact]
    public async Task SaveAfterTravelUpdatesReloadedState()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var resolver = new TravelResolver();
        var session = CreateSession();

        await repository.SaveAsync(session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("holloway"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await repository.SaveAsync(loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(new TownId("dustvale"), reloaded!.Player.CurrentTownId);
        Assert.Equal(loaded!.Player.Wallet.Cash, reloaded.Player.Wallet.Cash);
        Assert.True(new DomainInventoryCapabilityResolver().Resolve(reloaded.Player.Inventory).MountedTravelAvailable);
        Assert.Equal(2, reloaded.Clock.Day);
        Assert.Equal(0, reloaded.Clock.Turn);
        Assert.Equal(2, reloaded.PursuitState.Heat);
        Assert.NotNull(reloaded.Journey);
        Assert.Equal(1, reloaded.Journey!.RemainingDays);
        Assert.Equal(1m, reloaded.Journey.RemainingRideDayDistance);
        Assert.Equal(2, reloaded.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(2, reloaded.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
        Assert.Equal(new DomainHorseTravelState(0, 0, 1), reloaded.Player.Inventory.GetHorseState());
        Assert.Equal(1, reloaded.Player.Inventory.GetCanteenState()!.Charges);
        Assert.Contains(reloaded.LogEntries, entry => entry.Kind == GameLogEntryKind.Travel);
        Assert.Equal(TrailTerrain.Hills, reloaded.World.Trails.Single(trail => trail.Id == new TrailId("trail-2")).Terrain);
        Assert.Equal(WaterFeature.River, reloaded.World.Trails.Single(trail => trail.Id == new TrailId("trail-2")).WaterFeature);
    }

    [Fact]
    public async Task SaveAfterInterruptedTravelRoundTripsPendingEncounterState()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var resolver = new TravelResolver();
        var session = CreateHighRiskSession();

        await repository.SaveAsync(session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("dryfork"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await repository.SaveAsync(loaded);
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
    }

    [Fact]
    public async Task SaveAfterPendingFoeEncounterWithHiddenPressureRoundTripsTheHiddenState()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var resolver = new TravelResolver();
        var session = CreateHighRiskSession();

        await repository.SaveAsync(session);
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

        await repository.SaveAsync(loaded);
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
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var resolver = new TravelResolver();
        var session = CreateLuckySession();

        await repository.SaveAsync(session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("silvercreek"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await repository.SaveAsync(loaded);
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
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var resolver = new TravelResolver();
        var session = CreateDiarySession();

        await repository.SaveAsync(session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("openpass"), loaded.Player.Inventory, loaded.TravelRules);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await repository.SaveAsync(loaded);
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
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var resolver = new TravelResolver();
        var session = CreateDryTravelSession();

        await repository.SaveAsync(session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("dryridge"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await repository.SaveAsync(loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(new TownId("dustvale"), reloaded!.Player.CurrentTownId);
        Assert.Equal(2, reloaded.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(0, reloaded.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
        Assert.Equal(new DomainHorseTravelState(0, 0, 2), reloaded.Player.Inventory.GetHorseState());
        Assert.Equal(8, reloaded.Player.Inventory.GetCanteenState()!.Charges);
        Assert.Equal(5m, reloaded.World.Trails.Single(trail => trail.Id == new TrailId("trail-1")).RideDayDistance);
    }

    [Fact]
    public async Task SaveAfterHorseLossFallbackRoundTripsFootTravelAndHorseState()
    {
        using var fixture = new SqlitePersistenceFixture();
        var repository = CreateRepository(fixture);
        var resolver = new TravelResolver();
        var session = CreateHorseLossFallbackSession();

        await repository.SaveAsync(session);
        var loaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(loaded);

        var preview = resolver.PreviewJourney(loaded!.World, loaded.Player.CurrentTownId, new TownId("midway"), loaded.Player.Inventory);

        Assert.True(preview.Success);
        loaded.StartJourney(preview.Preview!);
        loaded.AdvanceJourneyDay();

        await repository.SaveAsync(loaded);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.NotNull(reloaded!.Journey);
        Assert.Equal(WildBunch.Domain.Travel.TravelMode.Foot, reloaded.Journey!.TravelMode);
        Assert.Equal(1, reloaded.Journey.RemainingDays);
        Assert.Equal(new DomainHorseTravelState(0, 0, 2), reloaded.Player.Inventory.GetHorseState());
        Assert.Contains(reloaded.LogEntries, entry => entry.Kind == GameLogEntryKind.Travel && entry.Message.Contains("went lame", StringComparison.OrdinalIgnoreCase));
    }

    private static EfGameSessionRepository CreateRepository(SqlitePersistenceFixture fixture)
        => new(fixture.CreateContext(), new GameSessionJsonSerializer());

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
                new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true),
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory);
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
                new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true),
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory);
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
            TravelDifficulty.Easy);
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
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, dustvale.Id, Wallet.Starting(25m), inventory);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Hard);
    }

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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Easy);
    }

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
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
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(true, false, true), SuspectStatus.AtLarge)
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
    }
}
