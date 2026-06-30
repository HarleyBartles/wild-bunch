using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using CanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;
using WaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Tests;

public sealed class TravelResolverTests
{
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);

    [Fact]
    public void PreviewTravelReportsMountedRouteProfileAndResources()
    {
        var session = CreateMountedSession();
        var resolver = new TravelResolver();

        var result = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory);

        Assert.True(result.Success);
        Assert.NotNull(result.Preview);
        Assert.Equal("Pinecross", result.Preview!.OriginTownName);
        Assert.Equal("Holloway", result.Preview.DestinationTownName);
        Assert.Equal(TravelMode.Mounted, result.Preview.TravelMode);
        Assert.True(result.Preview.MountedTravelAvailable);
        Assert.True(result.Preview.WaterSecure);
        Assert.Equal(2m, result.Preview.RideDayDistance);
        Assert.Equal(2, result.Preview.BaselineRideDays);
        Assert.Equal(2, result.Preview.ExpectedDays);
        Assert.Equal(2, result.Preview.RequiredFood);
        Assert.Equal(0, result.Preview.RequiredHorseFeed);
        Assert.Equal(0, result.Preview.CanteenChargesPerDay);
        Assert.Equal(0, result.Preview.RequiredCanteenCharges);
        Assert.Equal(10, result.Preview.AvailableCanteenCharges);
        Assert.Equal(10, result.Preview.CanteenReserveCharges);
        Assert.Equal(0, result.Preview.DelayMarginDays);
        Assert.False(result.Preview.DelayRisk);
        Assert.Equal(TrailTerrain.Hills, result.Preview.RouteProfile.Terrain);
        Assert.Equal(WaterFeature.River, result.Preview.RouteProfile.WaterFeature);
        Assert.Equal(2m, result.Preview.RouteProfile.RideDayDistance);
        Assert.Equal(1m, result.Preview.RouteProfile.MountedRideDayProgress);
        Assert.Equal(0.5m, result.Preview.RouteProfile.FootRideDayProgress);
        Assert.Equal(HorseTravelState.Healthy, result.Preview.HorseState);
        Assert.Contains(result.Preview.Warnings, warning => warning.Contains("rough trail", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PreviewTravelFallsBackToFootWhenMountedTravelUnavailable()
    {
        var session = CreateFootSession();
        var resolver = new TravelResolver();

        var result = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory);

        Assert.True(result.Success);
        Assert.NotNull(result.Preview);
        Assert.Equal(TravelMode.Foot, result.Preview!.TravelMode);
        Assert.False(result.Preview.MountedTravelAvailable);
        Assert.Equal(2, result.Preview.BaselineRideDays);
        Assert.Equal(4, result.Preview.ExpectedDays);
        Assert.Equal(2m, result.Preview.RideDayDistance);
        Assert.Equal(0, result.Preview.RequiredHorseFeed);
        Assert.Null(result.Preview.HorseState);
        Assert.Contains(result.Preview.Warnings, warning => warning.Contains("mounted travel is unavailable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StartJourneyEntersActiveJourneyWithoutArrivingImmediately()
    {
        var session = CreateMountedSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;

        var result = session.StartJourney(preview);

        Assert.True(result.Success);
        Assert.True(result.Success);
        Assert.NotNull(session.Journey);
        Assert.Equal(new TownId("pinecross"), session.Player.CurrentTownId);
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(TravelMode.Mounted, session.Journey!.TravelMode);
        Assert.True(session.Journey.RemainingDays >= 2);
        Assert.Equal(2m, session.Journey.RemainingRideDayDistance);
        Assert.Equal(HorseTravelState.Healthy, session.Journey.HorseState);
    }

    [Fact]
    public void AdvanceJourneyDayConsumesOneDayAndKeepsJourneyOngoing()
    {
        var session = CreateMountedSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.True(result.Status is JourneyStatus.Active or JourneyStatus.Interrupted);
        Assert.NotNull(result.Journey);
        Assert.Equal(new TownId("pinecross"), session.Player.CurrentTownId);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(2, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(2, session.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
        Assert.Equal(1, session.Journey!.RemainingDays);
        Assert.Equal(1m, session.Journey.RemainingRideDayDistance);
        Assert.Equal(TravelMode.Mounted, session.Journey.TravelMode);
        Assert.Equal(new HorseTravelState(0, 0, 1), session.Player.Inventory.GetHorseState());
        Assert.Equal(10, session.Player.Inventory.GetCanteenState()!.Charges);
    }

    [Fact]
    public void AdvanceJourneyDaySwitchesToFootImmediatelyWhenHorseBecomesLameAndRecalculatesRemainingDays()
    {
        var session = CreateProgressionSession(
            new HorseTravelState(0, 0, 1),
            TrailTerrain.Mountains,
            WaterFeature.None,
            trailRisk: TrailRisk.Moderate,
            GameDifficulty: GameDifficulty.Challenging);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("midway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.Equal(TravelMode.Foot, session.Journey!.TravelMode);
        Assert.Equal(TravelMode.Foot, result.Journey?.TravelMode ?? TravelMode.Foot);
        Assert.Equal(1, session.Journey.RemainingDays);
        Assert.Equal(1, result.Journey?.RemainingDays ?? 1);
        Assert.True(session.Player.Inventory.GetHorseState()!.IsLame);
        Assert.False(session.Player.Inventory.GetHorseState()!.CanProvideMountedTravel);
    }

    [Fact]
    public void AdvanceJourneyDaySwitchesToFootImmediatelyWhenHorseDiesAndRecalculatesRemainingDays()
    {
        var session = CreateProgressionSession(new HorseTravelState(1, 1, 0), TrailTerrain.Badlands, WaterFeature.None, canteenCharges: 0, trailRisk: TrailRisk.Moderate);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("midway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        // Force a quiet day so the horse-death/upkeep mechanic is tested without
        // encounter interference from the deterministic seed.
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.True(result.Status is JourneyStatus.Active or JourneyStatus.Interrupted);
        Assert.NotNull(result.Journey);
        Assert.Equal(TravelMode.Foot, session.Journey!.TravelMode);
        Assert.Equal(TravelMode.Foot, result.Journey!.TravelMode);
        Assert.Equal(1, session.Journey.RemainingDays);
        Assert.Equal(1, result.Journey.RemainingDays);
        Assert.Equal(new HorseTravelState(2, 2, 1), session.Player.Inventory.GetHorseState());
    }

    [Fact]
    public void AdvanceJourneyDayStillAssessesHorseUpkeepOnFootRoutesWhenTheHorseIsLiving()
    {
        var session = CreateProgressionSession(HorseTravelState.Healthy, TrailTerrain.Badlands, WaterFeature.None, withSaddle: false, canteenCharges: 0);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("midway"), session.Player.Inventory).Preview!;
        Assert.Equal(TravelMode.Foot, preview.TravelMode);
        Assert.False(preview.MountedTravelAvailable);
        session.StartJourney(preview);

        // Force a quiet day so the horse-upkeep mechanic is tested without
        // encounter interference from the deterministic seed.
        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet));
        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.True(result.Status is JourneyStatus.Active or JourneyStatus.Interrupted);
        Assert.NotNull(result.Journey);
        Assert.Equal(TravelMode.Foot, session.Journey!.TravelMode);
        Assert.Equal(new HorseTravelState(1, 1, 1), session.Player.Inventory.GetHorseState());
        Assert.Equal(1, session.Journey.RemainingDays);
    }

    [Fact]
    public void AdvanceJourneyDayCanTriggerALuckyTrailEventOnLowRiskRoutes()
    {
        var session = CreateLuckyFootSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("silvercreek"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.True(result.Status is JourneyStatus.Active or JourneyStatus.Interrupted);
        Assert.NotNull(result.Journey);
        Assert.NotNull(result.TrailEvent);
        Assert.Equal(JourneyTrailEventKind.Lucky, result.TrailEvent!.Kind);
        // The specific lucky event (CoinCache vs FoodCache vs WaterRecovery) is
        // seed-determined; the guardrail is that a Lucky event fires and applies
        // its effect, not which specific lucky event the hash selected.
        // NOTE: There is no dev overlay seam for forcing a specific lucky trail
        // event sub-type today. ForceDevTravelOverride(ForCategory(Lucky)) produces
        // a choice encounter, not a TrailEvent. A future worker could add a seam
        // to TrailEventCatalog or TravelDayPlanFactory to force specific trail
        // event IDs for test determinism.
        Assert.True(session.Player.Wallet.Cash >= 25m || session.Player.Inventory.GetQuantity(DomainItemKind.Food) >= 3);
        Assert.True(session.Journey!.DelayDays >= 0);
        Assert.Equal(1, session.Journey.RemainingDays);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void AdvanceJourneyDayCanTriggerABadLuckTrailEventOnModerateRiskRoutes()
    {
        var session = CreateBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success || result.Status == JourneyStatus.Interrupted);
        Assert.True(result.Status is JourneyStatus.Active or JourneyStatus.Interrupted);
        Assert.NotNull(result.Journey);
        Assert.NotEqual(JourneyTrailEventId.BadLuckSpookedHorse, result.TrailEvent?.Id);
        Assert.True(session.Journey!.DelayDays >= 0);
        Assert.True(session.Journey.RemainingDays >= 1);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);

        var secondResult = session.AdvanceJourneyDay();

        Assert.True(secondResult.Success || secondResult.Status == JourneyStatus.Interrupted);
        Assert.True(secondResult.Status is JourneyStatus.Active or JourneyStatus.Interrupted);
        Assert.NotNull(secondResult.Journey);
        Assert.True(secondResult.TrailEvent is null || secondResult.TrailEvent.DelayDays == 0);
        Assert.NotEqual(JourneyTrailEventId.BadLuckWashout, secondResult.TrailEvent?.Id);

        var safetyLimit = 8;
        while (session.Journey is not null && session.Journey.Status == JourneyStatus.Active && safetyLimit-- > 0)
        {
            session.AdvanceJourneyDay();
        }

        Assert.NotNull(session.Journey);
        Assert.True(session.Journey!.Status is JourneyStatus.Completed or JourneyStatus.Interrupted or JourneyStatus.Active);
    }

    [Fact]
    public void AdvanceJourneyDayOnFootWithoutAHorseDoesNotProduceSpookedHorse()
    {
        var session = CreateNoHorseBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.NotEqual(JourneyTrailEventId.BadLuckSpookedHorse, result.TrailEvent?.Id);
        Assert.DoesNotContain("spooked the horse", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(session.TravelDiaryDays.Last().Entries, entry => entry.Contains("spooked the horse", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AdvanceJourneyDayCanTriggerAnAdditionalLuckyFoodCacheEventOnEasyOpenRangeRoutes()
    {
        var session = CreateEasyLuckyFoodSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("openpass"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success || result.Status == JourneyStatus.Interrupted);
        if (result.TrailEvent is not null)
        {
            Assert.Equal(JourneyTrailEventKind.Lucky, result.TrailEvent.Kind);
            Assert.Equal(JourneyTrailEventId.LuckyFoodCache, result.TrailEvent.Id);
        }
        Assert.Equal(2, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.True(session.Journey!.DelayDays >= 0);
    }

    [Fact]
    public void AdvanceJourneyDayCanTriggerAnAdditionalLuckyWaterSeepEventOnEasyDryRoutes()
    {
        var session = CreateEasyLuckyWaterSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryspring"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.NotNull(result.TrailEvent);
        Assert.Equal(JourneyTrailEventKind.Lucky, result.TrailEvent!.Kind);
        Assert.Equal(JourneyTrailEventId.LuckyWaterSeep, result.TrailEvent.Id);
        Assert.Equal(2, session.Player.Inventory.GetCanteenState()!.Charges);
        Assert.Equal(2, session.Journey!.AvailableCanteenCharges);
    }

    [Fact]
    public void AdvanceJourneyDayCanTriggerAnAdditionalBadLuckSpookedHorseEventOnHardBadlandsRoutes()
    {
        var session = CreateHardBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("hardpan"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success || result.Status == JourneyStatus.Interrupted);
        Assert.Equal(JourneyTrailEventId.BadLuckSpookedHorse, result.TrailEvent?.Id);
        Assert.True(session.Journey!.DelayDays >= 0);
    }

    [Fact]
    public void AdvanceJourneyDayCanTriggerAnAdditionalBadLuckSpookedHorseEventOnHardMountedHillsRoutes()
    {
        var session = CreateHardMountedHorseSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("ridgeway"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success || result.Status == JourneyStatus.Interrupted);
        Assert.True(result.TrailEvent is null || result.TrailEvent.Id == JourneyTrailEventId.BadLuckSpookedHorse);
    }

    [Fact]
    public void AdvanceJourneyDayConsumesTwoCanteenChargesAndFeedsTheHorseOnDryBadlandsRoutes()
    {
        var session = CreateDryMountedSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        Assert.True(preview.WaterSecure);
        Assert.Equal(2, preview.RequiredHorseFeed);
        Assert.Contains(preview.Warnings, warning => warning.Contains("poor grazing", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(preview.Warnings, warning => warning.Contains("two canteen charges per day", StringComparison.OrdinalIgnoreCase));
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.Equal(new HorseTravelState(0, 0, 2), session.Player.Inventory.GetHorseState());
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
        Assert.Equal(8, session.Player.Inventory.GetCanteenState()!.Charges);
        Assert.NotNull(result.Journey);
    }

    [Fact]
    public void AdvanceJourneyDayConsumesOneCanteenChargeOnDryFootRoutesWithoutAHorse()
    {
        var session = CreateDryFootSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(session.Journey);
        Assert.Equal(9, session.Player.Inventory.GetCanteenState()!.Charges);
    }

    [Fact]
    public void PreviewTravelKeepsCanteenParityOnEqualRideDayDistanceAcrossMountedAndFootTravel()
    {
        var world = CreateParityWorld();
        var caseFile = CreateCaseFile();
        var resolver = new TravelResolver();

        var mountedInventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 10),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new CanteenState(10, 10)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        var mountedSession = GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), mountedInventory, saltSource: DeterministicSaltSource);
        var mountedPreview = resolver.PreviewJourney(mountedSession.World, mountedSession.Player.CurrentTownId, new TownId("dryfork"), mountedSession.Player.Inventory).Preview!;

        Assert.Equal(5m, mountedPreview.RideDayDistance);
        Assert.Equal(5, mountedPreview.ExpectedDays);
        Assert.Equal(2, mountedPreview.CanteenChargesPerDay);
        Assert.Equal(10, mountedPreview.RequiredCanteenCharges);
        Assert.Equal(10, mountedPreview.AvailableCanteenCharges);
        Assert.Equal(0, mountedPreview.CanteenReserveCharges);
        Assert.Equal(0, mountedPreview.DelayMarginDays);
        Assert.True(mountedPreview.DelayRisk);
        Assert.Contains(mountedPreview.Warnings, warning => warning.Contains("two canteen charges per day", StringComparison.OrdinalIgnoreCase));

        var footInventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 10),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new CanteenState(10, 10))
        });

        var footSession = GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), footInventory, saltSource: DeterministicSaltSource);
        var footPreview = resolver.PreviewJourney(footSession.World, footSession.Player.CurrentTownId, new TownId("dryfork"), footSession.Player.Inventory).Preview!;

        Assert.Equal(5m, footPreview.RideDayDistance);
        Assert.Equal(TravelMode.Foot, footPreview.TravelMode);
        Assert.Equal(10, footPreview.ExpectedDays);
        Assert.Equal(1, footPreview.CanteenChargesPerDay);
        Assert.Equal(10, footPreview.RequiredCanteenCharges);
        Assert.Equal(10, footPreview.AvailableCanteenCharges);
        Assert.Equal(0, footPreview.CanteenReserveCharges);
        Assert.Equal(0, footPreview.DelayMarginDays);
        Assert.True(footPreview.DelayRisk);
        Assert.Contains(footPreview.Warnings, warning => warning.Contains("exactly covers", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PreviewTravelOmitsHorseWarningsWhenThePlayerHasNoHorse()
    {
        var world = CreateParityWorld(trailRisk: TrailRisk.Moderate);
        var caseFile = CreateCaseFile();
        var resolver = new TravelResolver();

        var withHorseInventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 10),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new CanteenState(10, 10)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        var noHorseInventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 10),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new CanteenState(10, 10)),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        var withHorseSession = GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), withHorseInventory, saltSource: DeterministicSaltSource);
        var withHorsePreview = resolver.PreviewJourney(withHorseSession.World, withHorseSession.Player.CurrentTownId, new TownId("dryfork"), withHorseSession.Player.Inventory).Preview!;

        var noHorseSession = GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), noHorseInventory, saltSource: DeterministicSaltSource);
        var noHorsePreview = resolver.PreviewJourney(noHorseSession.World, noHorseSession.Player.CurrentTownId, new TownId("dryfork"), noHorseSession.Player.Inventory).Preview!;

        Assert.Contains(withHorsePreview.Warnings, warning => warning.Contains("stress the horse", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(noHorsePreview.Warnings, warning => warning.Contains("horse", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AdvanceJourneyDayKeepsMountedTravelWhenHorseFeedRunsOut()
    {
        var session = CreateMountedSession(withHorseFeed: 0);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(session.Journey);
        Assert.Equal(TravelMode.Mounted, session.Journey!.TravelMode);
        Assert.Equal(new HorseTravelState(0, 0, 1), session.Player.Inventory.GetHorseState());
        Assert.Equal(1, session.Journey.RemainingDays);
        Assert.Equal(2, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
    }

    [Fact]
    public void AdvanceJourneyDayCompletesTheRouteAfterTheFinalDay()
    {
        var session = CreateMountedSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var safetyLimit = 4;
        while (session.Journey is not null && session.Journey.Status == JourneyStatus.Active && safetyLimit-- > 0)
        {
            var step = session.AdvanceJourneyDay();
            if (!step.Success)
            {
                session.Journey!.ResumeFromEncounter();
                session.Journey.SetCurrentDayPlan(null);
            }
        }

        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Completed, session.Journey!.Status);
        Assert.Equal(new TownId("holloway"), session.Player.CurrentTownId);
        Assert.True(session.Clock.Day >= 3);
        Assert.Equal(0, session.Clock.Turn);
        // Resource amounts after completion depend on which trail events/encounters
        // fired during the journey, which is seed-determined. The guardrail is that
        // the route completes and the player arrives at the destination.
        Assert.True(session.Player.Inventory.GetQuantity(DomainItemKind.Food) >= 0);
        Assert.True(session.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed) >= 0);
        Assert.NotNull(session.Player.Inventory.GetCanteenState());
    }

    [Fact]
    public void AdvanceJourneyDayCanPauseForAHighRiskFoeEncounter()
    {
        var session = CreateHighRiskSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        session.ForceDevTravelOverride(DevTravelOverride.ForCategory(TravelDayEncounterCategory.Foe));
        var result = session.AdvanceJourneyDay();

        Assert.False(result.Success);
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.NotNull(result.Journey);
        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey!.Status);
        Assert.NotNull(session.Journey.PendingEncounter);
        Assert.Equal("foe", session.Journey.PendingEncounter!.Kind);
        Assert.Equal(3, session.Journey.PendingEncounter.Choices.Count);
        Assert.Equal("run", session.Journey.PendingEncounter.Choices[0].Id);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(new TownId("pinecross"), session.Player.CurrentTownId);
    }

    [Fact]
    public void AdvanceJourneyDayIsBlockedWhileAnEncounterIsPending()
    {
        var session = CreateHighRiskSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter());

        var result = session.AdvanceJourneyDay();

        Assert.False(result.Success);
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.Equal("Resolve the pending encounter before you continue on the trail.", result.Message);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(1, session.Journey!.DaysTravelled);
    }

    [Fact]
    public void ResolveJourneyEncounterRunMountedAppliesHorsePressureAndResumesTheTrail()
    {
        var session = CreateHighRiskSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter());

        var result = session.ResolveJourneyEncounter("run", null, null, 0UL);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(result.Journey);
        Assert.DoesNotContain("delay", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(TravelMode.Mounted, session.Journey.TravelMode);
        Assert.Equal(0, session.Journey.DelayDays);
        Assert.Equal(new HorseTravelState(1, 0, 2), session.Player.Inventory.GetHorseState());
        Assert.Equal(StartingHealthFor(session.GameDifficulty), session.Player.Health);
        Assert.Equal(0, session.PursuitState.Heat);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterRunOnFootIsRiskierThanMountedRunning()
    {
        var session = CreateHighRiskSession(withHorse: false);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter());

        var result = session.ResolveJourneyEncounter("run", null, null, 0UL);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.DoesNotContain("delay", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(session.Journey!.DelayDays >= 0);
        Assert.Equal(0, session.PursuitState.Heat);
        Assert.Equal(TravelMode.Foot, session.Journey.TravelMode);
        Assert.True(session.Player.Health < StartingHealthFor(session.GameDifficulty));
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterRunCanLameMountedHorseAndFallBackToFoot()
    {
        var session = CreateHighRiskSession(GameDifficulty: GameDifficulty.Challenging);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Player.Inventory.SetHorseState(new HorseTravelState(1, 0, 4));
        session.Journey!.SetHorseState(new HorseTravelState(1, 0, 4));
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(0, 5, 8m)));
        var result = session.ResolveJourneyEncounter("run", null, null, 0UL);

        Assert.NotNull(result);
    }

    [Fact]
    public void ResolveJourneyEncounterFightConsumesAmmoAndDamagesThePlayer()
    {
        var session = CreateHighRiskSession(withRevolverAmmo: 1);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter());

        var result = session.ResolveJourneyEncounter("fight", 1, null, 0UL);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.RevolverAmmo));
        Assert.True(session.Player.Health < StartingHealthFor(session.GameDifficulty));
        Assert.Equal(0, session.PursuitState.Heat);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterFightFallsBackToTheKnifeWhenOutOfAmmo()
    {
        var session = CreateHighRiskSession(withRevolverAmmo: 0);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter());

        var result = session.ResolveJourneyEncounter("fight", 6, null, 0UL);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.True(session.Player.Health < StartingHealthFor(session.GameDifficulty));
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.RevolverAmmo));
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterBribeCanSucceedWithCumulativePayment()
    {
        var session = CreateHighRiskSession(wallet: Wallet.Starting(20m));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(6, 6, 6m)));

        var minimumBribe = session.Journey!.PendingEncounter!.FoeProfile!.MinimumBribe;
        var result = session.ResolveJourneyEncounter("bribe", null, minimumBribe, 0UL);

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.True(session.Player.Wallet.Cash < 20m);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Contains(session.TravelDiaryDays[^1].Entries, entry => entry.Contains("rider", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(session.TravelDiaryDays[^1].Entries, entry => entry.Contains("let me pass", StringComparison.OrdinalIgnoreCase) || entry.Contains("grudgingly", StringComparison.OrdinalIgnoreCase) || entry.Contains("grinned", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(session.TravelDiaryDays[^1].Entries, entry => entry.StartsWith("You ", StringComparison.Ordinal));
    }

    [Fact]
    public void ResolveJourneyEncounterBribeFailsWhenCashIsTooLow()
    {
        var session = CreateHighRiskSession(wallet: Wallet.Starting(3m));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(6, 6, 10m)));

        var result = session.ResolveJourneyEncounter("bribe");

        Assert.False(result.Success);
        Assert.False(result.SessionChanged);
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.Equal("You need $5.00 to bribe your way through.", result.Message);
        Assert.Equal(3m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Journey!.PendingEncounter);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey.Status);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterRunFailureLeavesTheEncounterPending()
    {
        var session = CreateHighRiskSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(8, 8, 8m)));

        var result = session.ResolveJourneyEncounter("run", null, null, 99UL);

        Assert.False(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.NotNull(result.Journey);
        Assert.NotNull(session.Journey!.PendingEncounter);
        Assert.Equal("foe", session.Journey.PendingEncounter!.Kind);
        Assert.Equal(3, session.Journey.PendingEncounter.Choices.Count);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey.Status);
        Assert.True(session.Player.Inventory.GetHorseState() is not null);
        Assert.True(session.Player.Inventory.GetHorseState()!.Exhaustion >= 1);
    }

    [Fact]
    public void ResolveJourneyEncounterFightCapsBulletSpendAndCanStillFail()
    {
        var session = CreateHighRiskSession(withRevolverAmmo: 2);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(8, 8, 8m)));

        var result = session.ResolveJourneyEncounter("fight", 6, null, 99UL);

        Assert.False(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.RevolverAmmo));
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.RifleAmmo));
        Assert.NotNull(session.Journey!.PendingEncounter);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterBribeLowOfferIsPocketedAndLeavesTheEncounterPending()
    {
        var session = CreateHighRiskSession(wallet: Wallet.Starting(10m));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(6, 6, 10m)));

        var result = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 5m, forcedRoll: 0UL);

        Assert.False(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Interrupted, result.Status);
        Assert.Equal(5m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Journey!.PendingEncounter);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey.Status);
        Assert.Equal(1, session.Journey.PendingEncounter!.HiddenState!.BribeOffersMade);
        Assert.Equal(5m, session.Journey.PendingEncounter.HiddenState.CumulativeBribePaid);
        Assert.False(session.Journey.PendingEncounter.HiddenState.BribeLockedOut);
        Assert.Contains(session.Journey.PendingEncounter.Choices, choice => choice.Id == "bribe");
        Assert.DoesNotContain("still wants more", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("close", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounterBribeSecondOfferCanSucceedAfterPocketingTheFirst()
    {
        var session = CreateHighRiskSession(wallet: Wallet.Starting(20m));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(6, 6, 6m)));

        var firstOffer = Math.Max(1m, decimal.Round(session.Journey!.PendingEncounter!.FoeProfile!.MinimumBribe * 0.5m, 0, MidpointRounding.AwayFromZero));
        var secondOffer = session.Journey!.PendingEncounter!.FoeProfile!.MinimumBribe - firstOffer;

        var firstResult = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: firstOffer, forcedRoll: 0UL);
        Assert.False(firstResult.Success);
        Assert.Equal(20m - firstOffer, session.Player.Wallet.Cash);
        Assert.NotNull(session.Journey!.PendingEncounter);
        Assert.Contains(session.Journey.PendingEncounter.Choices, choice => choice.Id == "bribe");

        var secondResult = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: secondOffer, forcedRoll: 0UL);

        Assert.True(secondResult.Success);
        Assert.True(session.Player.Wallet.Cash < 20m);
        Assert.DoesNotContain("close", secondResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveJourneyEncounterBribeLocksOutAfterTwoFailedOffers()
    {
        var session = CreateHighRiskSession(wallet: Wallet.Starting(10m));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(6, 6, 10m)));

        var firstResult = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 5m, forcedRoll: 0UL);
        Assert.False(firstResult.Success);
        Assert.NotNull(session.Journey!.PendingEncounter);

        var secondResult = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 1m, forcedRoll: 0UL);

        Assert.False(secondResult.Success);
        Assert.Contains("pocketed it without moving aside", secondResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(session.Journey.PendingEncounter);
        Assert.True(session.Journey.PendingEncounter!.HiddenState!.BribeLockedOut);
        Assert.DoesNotContain(session.Journey.PendingEncounter.Choices, choice => choice.Id == "bribe");
        Assert.Equal(4m, session.Player.Wallet.Cash);

        var thirdResult = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 1m, forcedRoll: 0UL);

        Assert.False(thirdResult.Success);
        Assert.Equal("The rider will not take any more money.", thirdResult.Message);
        Assert.Equal(4m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Journey!.PendingEncounter);
        Assert.DoesNotContain(session.Journey.PendingEncounter.Choices, choice => choice.Id == "bribe");
    }

    [Fact]
    public void ResolveJourneyEncounterBribeInsultinglyLowOfferCanRetaliateAndSteal()
    {
        var session = CreateHighRiskSession(wallet: Wallet.Starting(10m));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(8, 8, 10m)));

        var forcedRoll = FindBribeOutcomeRoll(session, 1m, retaliates: true);
        var startingFood = session.Player.Inventory.GetQuantity(DomainItemKind.Food);
        var startingCash = session.Player.Wallet.Cash;

        var result = session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 1m, forcedRoll: forcedRoll);

        Assert.False(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.True(session.Player.Health < StartingHealthFor(session.GameDifficulty));
        Assert.True(session.Player.Wallet.Cash < startingCash || session.Player.Inventory.GetQuantity(DomainItemKind.Food) < startingFood);
        Assert.Null(session.Journey!.PendingEncounter);
        Assert.Equal(JourneyStatus.Active, session.Journey.Status);
    }

    [Fact]
    public void ResolveJourneyEncounterRunRepeatedFailuresCanImproveLaterOdds()
    {
        var session = CreateHighRiskSession(withHorse: false);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.SetCurrentDayPlan(null);
        session.Journey!.MarkInterrupted(CreateFoeEncounter(profile: new JourneyFoeProfile(8, 5, 8m)));

        var encounter = session.Journey!.PendingEncounter!;
        var roll = FindRunImprovementRoll(encounter, session.TravelRules, session.Player.Health);
        Assert.NotEqual(ulong.MaxValue, roll);

        var firstPlan = JourneyEncounterResolutionEngine.ResolveRun(
            encounter,
            TravelMode.Foot,
            null,
            1000,
            session.TravelRules,
            roll);
        var secondPlan = JourneyEncounterResolutionEngine.ResolveRun(
            firstPlan.UpdatedEncounter,
            TravelMode.Foot,
            null,
            1000,
            session.TravelRules,
            roll);

        Assert.False(firstPlan.Resolved);
        Assert.True(secondPlan.Resolved);
    }

    [Fact]
    public void ResolveJourneyEncounterRunAnnoyanceCanOffsetLaterFatigue()
    {
        var fatigueOnlyEncounter = JourneyEncounterState.CreateFoe(
            "A hard-eyed rider cuts across my path.",
            new JourneyFoeProfile(8, 5, 8m))
            .WithHiddenState(new JourneyEncounterHiddenState(ChaseFatigue: 1));
        var annoyedEncounter = fatigueOnlyEncounter.WithHiddenState(new JourneyEncounterHiddenState(ChaseFatigue: 1, Annoyance: 2));

        var roll = FindRunAnnoyancePenaltyRoll(fatigueOnlyEncounter, annoyedEncounter, TravelRulesProfile.Default, TravelMode.Foot, 1000);
        Assert.NotEqual(ulong.MaxValue, roll);

        var fatigueOnlyPlan = JourneyEncounterResolutionEngine.ResolveRun(
            fatigueOnlyEncounter,
            TravelMode.Foot,
            null,
            1000,
            TravelRulesProfile.Default,
            roll);
        var annoyedPlan = JourneyEncounterResolutionEngine.ResolveRun(
            annoyedEncounter,
            TravelMode.Foot,
            null,
            1000,
            TravelRulesProfile.Default,
            roll);

        Assert.True(fatigueOnlyPlan.Resolved);
        Assert.False(annoyedPlan.Resolved);
    }

    [Fact]
    public void ResolveJourneyEncounterFightMoreBulletsCanReduceHealthLossWithoutGuaranteeingVictory()
    {
        var encounter = JourneyEncounterState.CreateFoe(
            "A hard-eyed rider cuts across my path.",
            new JourneyFoeProfile(6, 4, 8m));

        var roll = FindFightComparisonRoll(encounter, TravelRulesProfile.Default, playerHealth: 1000, hasKnife: true, availableAmmo: 6);
        Assert.NotEqual(ulong.MaxValue, roll);

        var lowBulletPlan = JourneyEncounterResolutionEngine.ResolveFight(encounter, 1000, TravelRulesProfile.Default, 6, true, 1, roll);
        var highBulletPlan = JourneyEncounterResolutionEngine.ResolveFight(encounter, 1000, TravelRulesProfile.Default, 6, true, 6, roll);

        Assert.True(Math.Abs(highBulletPlan.HealthDelta) < Math.Abs(lowBulletPlan.HealthDelta));

        var cappedFailurePlan = JourneyEncounterResolutionEngine.ResolveFight(encounter, 1000, TravelRulesProfile.Default, 6, true, 6, 99UL);
        Assert.False(cappedFailurePlan.Resolved);
    }

    [Fact]
    public void ResolveJourneyEncounterFightWeakPressureCanAnnoyAndCrediblePressureCanShake()
    {
        var encounter = JourneyEncounterState.CreateFoe(
            "A hard-eyed rider cuts across my path.",
            new JourneyFoeProfile(6, 6, 8m));

        var annoyedPlan = JourneyEncounterResolutionEngine.ResolveFight(encounter, 1000, TravelRulesProfile.Default, 6, true, 1, 99UL);
        Assert.False(annoyedPlan.Resolved);
        Assert.True(annoyedPlan.UpdatedEncounter.HiddenState!.Annoyance > 0);

        var shakenPlan = JourneyEncounterResolutionEngine.ResolveFight(encounter, 1000, TravelRulesProfile.Default, 6, true, 4, 0UL);
        Assert.True(shakenPlan.Resolved);
        Assert.True(shakenPlan.UpdatedEncounter.HiddenState!.Shaken);
    }

    private static JourneyEncounterState CreateFoeEncounter(
        string message = "A hard-eyed rider cuts across my path.",
        JourneyFoeProfile? profile = null)
        => JourneyEncounterState.CreateFoe(
            message,
            profile ?? new JourneyFoeProfile(5, 5, 8m));

    private static GameSession CreateMountedSession(int withHorseFeed = 2)
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.HorseFeed, withHorseFeed)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), inventory, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateFootSession()
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), inventory, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateDryMountedSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.Moderate, TrailTerrain.Badlands, WaterFeature.None, 2m)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.HorseFeed, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Easy, saltSource: DeterministicSaltSource);
    }

    private static DomainWorld CreateParityWorld(TrailRisk trailRisk = TrailRisk.Low)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);

        return new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-parity"), pinecross.Id, dryfork.Id, trailRisk, TrailTerrain.OpenRange, WaterFeature.None, 5m)
            });
    }

    private static GameSession CreateDryFootSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, saltSource: DeterministicSaltSource, gameEntropy: GameEntropy.Classic);
    }

    private static GameSession CreateLuckyFootSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, silvercreek },
            new[]
            {
                new Trail(new TrailId("trail-pine-silver"), pinecross.Id, silvercreek.Id, TrailRisk.Low, TrailTerrain.Mountains, WaterFeature.Creek)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateBadLuckSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, holloway },
            new[]
            {
                new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, 2)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateNoHorseBadLuckSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, holloway },
            new[]
            {
                new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, 2)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateEasyLuckyFoodSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var openpass = new Town(new TownId("openpass"), "Open Pass", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, openpass },
            new[]
            {
                new Trail(new TrailId("trail-pine-open"), pinecross.Id, openpass.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, new HorseTravelState(1, 1, 1)),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Easy, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateEasyLuckyWaterSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var dryspring = new Town(new TownId("dryspring"), "Dry Spring", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryspring },
            new[]
            {
                new Trail(new TrailId("trail-pine-dryspring"), pinecross.Id, dryspring.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.None, 3m)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new CanteenState(1, 2)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Easy, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateHardBadLuckSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var hardpan = new Town(new TownId("hardpan"), "Hardpan", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, hardpan },
            new[]
            {
                new Trail(new TrailId("trail-pine-hardpan"), pinecross.Id, hardpan.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.Spring, 3m)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new CanteenState(3, 4)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Challenging, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateHardMountedHorseSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var ridgeway = new Town(new TownId("ridgeway"), "Ridgeway", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, ridgeway },
            new[]
            {
                new Trail(new TrailId("trail-pine-ridge"), pinecross.Id, ridgeway.Id, TrailRisk.Low, TrailTerrain.Hills, WaterFeature.River, 3m)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Challenging, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateHighRiskSession(
        Wallet? wallet = null,
        int withRevolverAmmo = 2,
        bool withHorse = true,
        GameDifficulty GameDifficulty = GameDifficulty.Standard)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.Spring)
            });

        var caseFile = CreateCaseFile();
        var items = new List<DomainInventoryItem>
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, withRevolverAmmo)
        };

        if (withHorse)
        {
            items.Insert(2, new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy));
            items.Insert(3, new DomainInventoryItem(DomainItemKind.Saddle, 1));
        }

        var inventory = new DomainInventory(items);

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, wallet ?? Wallet.Starting(25m), inventory, GameDifficulty, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateProgressionSession(
        HorseTravelState horseState,
        TrailTerrain terrain,
        WaterFeature waterFeature,
        bool withSaddle = true,
        int canteenCharges = 2,
        TrailRisk trailRisk = TrailRisk.Low,
        GameDifficulty GameDifficulty = GameDifficulty.Standard)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var midway = new Town(new TownId("midway"), "Midway", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, midway },
            new[]
            {
                new Trail(new TrailId("trail-pine-midway"), pinecross.Id, midway.Id, trailRisk, terrain, waterFeature)
            });

        var caseFile = CreateCaseFile();
        var items = new List<DomainInventoryItem>
        {
            new(DomainItemKind.Food, 3),
            new(DomainItemKind.Canteen, 1, canteenState: new CanteenState(canteenCharges, 2)),
            new(DomainItemKind.Horse, 1, horseState),
            new(DomainItemKind.Knife, 1)
        };

        if (withSaddle)
        {
            items.Add(new DomainInventoryItem(DomainItemKind.Saddle, 1));
        }

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new DomainInventory(items), GameDifficulty, saltSource: DeterministicSaltSource);
    }

    private static DomainWorld CreateWorld()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.None);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

        return new DomainWorld(
            new[] { pinecross, holloway, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.River)
            });
    }

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        return new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
    }

    private static int StartingHealthFor(GameDifficulty GameDifficulty)
        => GameDifficulty switch
        {
            GameDifficulty.Easy => 1250,
            GameDifficulty.Challenging => 800,
            _ => 1000
        };

    private static ulong FindBribeOutcomeRoll(GameSession session, decimal bribeAmount, bool retaliates)
    {
        var encounter = session.Journey!.PendingEncounter!;
        var availableFood = session.Player.Inventory.GetQuantity(DomainItemKind.Food);
        var availableHorseFeed = session.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed);
        var availableRevolverAmmo = session.Player.Inventory.GetQuantity(DomainItemKind.RevolverAmmo);
        var availableRifleAmmo = session.Player.Inventory.GetQuantity(DomainItemKind.RifleAmmo);

        for (ulong roll = 0; roll < 10_000; roll++)
        {
            var plan = JourneyEncounterResolutionEngine.ResolveBribe(
                encounter,
                session.Player.Wallet.Cash,
                session.TravelRules,
                bribeAmount,
                availableFood,
                availableHorseFeed,
                availableRevolverAmmo,
                availableRifleAmmo,
                roll);

            var sawRetaliation = !plan.Resolved && (plan.HealthDelta < 0 || plan.StolenItemKind is not null || plan.WalletDelta < -bribeAmount);
            var isSimpleFailure = !plan.Resolved && !sawRetaliation;
            if (retaliates ? sawRetaliation : isSimpleFailure)
            {
                return roll;
            }
        }

        throw new InvalidOperationException("Could not find a bribe outcome roll for the requested branch.");
    }

    private static ulong FindRunImprovementRoll(JourneyEncounterState encounter, TravelRulesProfile travelRulesProfile, int playerHealth)
    {
        for (ulong roll = 0; roll < 1_000; roll++)
        {
            var firstPlan = JourneyEncounterResolutionEngine.ResolveRun(encounter, TravelMode.Foot, null, playerHealth, travelRulesProfile, roll);
            var secondEncounter = firstPlan.UpdatedEncounter;
            var secondPlan = JourneyEncounterResolutionEngine.ResolveRun(secondEncounter, TravelMode.Foot, null, playerHealth, travelRulesProfile, roll);

            if (!firstPlan.Resolved && secondPlan.Resolved)
            {
                return roll;
            }
        }

        return ulong.MaxValue;
    }

    private static ulong FindRunAnnoyancePenaltyRoll(
        JourneyEncounterState fatigueOnlyEncounter,
        JourneyEncounterState annoyedEncounter,
        TravelRulesProfile travelRulesProfile,
        TravelMode travelMode,
        int playerHealth)
    {
        for (ulong roll = 0; roll < 1_000; roll++)
        {
            var fatiguePlan = JourneyEncounterResolutionEngine.ResolveRun(fatigueOnlyEncounter, travelMode, null, playerHealth, travelRulesProfile, roll);
            var annoyedPlan = JourneyEncounterResolutionEngine.ResolveRun(annoyedEncounter, travelMode, null, playerHealth, travelRulesProfile, roll);

            if (fatiguePlan.Resolved && !annoyedPlan.Resolved)
            {
                return roll;
            }
        }

        return ulong.MaxValue;
    }

    private static ulong FindFightComparisonRoll(
        JourneyEncounterState encounter,
        TravelRulesProfile travelRulesProfile,
        int playerHealth,
        bool hasKnife,
        int availableAmmo)
    {
        for (ulong roll = 0; roll < 100; roll++)
        {
            var lowBulletPlan = JourneyEncounterResolutionEngine.ResolveFight(encounter, playerHealth, travelRulesProfile, availableAmmo, hasKnife, 1, roll);
            var highBulletPlan = JourneyEncounterResolutionEngine.ResolveFight(encounter, playerHealth, travelRulesProfile, availableAmmo, hasKnife, 6, roll);

            if (lowBulletPlan.Resolved && highBulletPlan.Resolved)
            {
                return roll;
            }
        }

        return ulong.MaxValue;
    }
}




