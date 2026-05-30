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
        Assert.Equal(JourneyStatus.Active, result.Status);
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
        Assert.Equal(JourneyStatus.Active, result.Status);
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
            HorseTravelState.Healthy,
            TrailTerrain.Mountains,
            WaterFeature.None,
            trailRisk: TrailRisk.Moderate,
            travelDifficulty: TravelDifficulty.Hard);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("midway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(result.Journey);
        Assert.Equal(TravelMode.Foot, session.Journey!.TravelMode);
        Assert.Equal(TravelMode.Foot, result.Journey!.TravelMode);
        Assert.Equal(1, session.Journey.RemainingDays);
        Assert.Equal(1, result.Journey.RemainingDays);
        Assert.Equal(new HorseTravelState(1, 0, 2), session.Player.Inventory.GetHorseState());
        Assert.Contains("goes lame", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("on foot", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdvanceJourneyDaySwitchesToFootImmediatelyWhenHorseDiesAndRecalculatesRemainingDays()
    {
        var session = CreateProgressionSession(new HorseTravelState(1, 1, 0), TrailTerrain.Badlands, WaterFeature.None, canteenCharges: 0, trailRisk: TrailRisk.Moderate);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("midway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
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

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
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
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(result.Journey);
        Assert.NotNull(result.TrailEvent);
        Assert.Equal(JourneyTrailEventKind.Lucky, result.TrailEvent!.Kind);
        Assert.Equal(JourneyTrailEventId.LuckyCoinCache, result.TrailEvent.Id);
        Assert.Equal("I found a hidden cache of trail coins and pocketed an extra $3.00.", result.TrailEvent.Message);
        Assert.Equal(28m, session.Player.Wallet.Cash);
        Assert.Equal(0, session.Journey!.DelayDays);
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

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(result.Journey);
        Assert.NotNull(result.TrailEvent);
        Assert.Equal(JourneyTrailEventKind.BadLuck, result.TrailEvent!.Kind);
        Assert.Equal(JourneyTrailEventId.BadLuckWashout, result.TrailEvent.Id);
        Assert.Equal(TravelRulesProfile.Default.BadLuckTrailDelayDays, result.TrailEvent.DelayDays);
        Assert.Equal(TravelRulesProfile.Default.TrailEventHeatIncrease, result.TrailEvent.HeatIncrease);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.True(session.Journey!.DelayDays >= 1);
        Assert.True(session.Journey.RemainingDays >= 2);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void AdvanceJourneyDayCanTriggerAnAdditionalLuckyFoodCacheEventOnEasyOpenRangeRoutes()
    {
        var session = CreateEasyLuckyFoodSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("openpass"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.NotNull(result.TrailEvent);
        Assert.Equal(JourneyTrailEventKind.Lucky, result.TrailEvent!.Kind);
        Assert.Equal(JourneyTrailEventId.LuckyFoodCache, result.TrailEvent.Id);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(4, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(0, session.Journey!.DelayDays);
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
    public void AdvanceJourneyDayCanTriggerAnAdditionalBadLuckFoodLossEventOnHardBadlandsRoutes()
    {
        var session = CreateHardBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("hardpan"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.NotNull(result.TrailEvent);
        Assert.Equal(JourneyTrailEventKind.BadLuck, result.TrailEvent!.Kind);
        Assert.Equal(JourneyTrailEventId.BadLuckFoodLoss, result.TrailEvent.Id);
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(0, session.Player.Inventory.GetCanteenState()!.Charges);
        Assert.Equal(2, session.Journey!.DelayDays);
    }

    [Fact]
    public void AdvanceJourneyDayCanTriggerAnAdditionalBadLuckSpookedHorseEventOnHardMountedHillsRoutes()
    {
        var session = CreateHardMountedHorseSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("ridgeway"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.NotNull(result.TrailEvent);
        Assert.Equal(JourneyTrailEventKind.BadLuck, result.TrailEvent!.Kind);
        Assert.Equal(JourneyTrailEventId.BadLuckSpookedHorse, result.TrailEvent.Id);
        Assert.Equal(TravelMode.Foot, session.Journey!.TravelMode);
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
        Assert.Equal(new HorseTravelState(0, 0, 1), session.Player.Inventory.GetHorseState());
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

        var mountedSession = GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), mountedInventory);
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

        var footSession = GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), footInventory);
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

        var withHorseSession = GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), withHorseInventory);
        var withHorsePreview = resolver.PreviewJourney(withHorseSession.World, withHorseSession.Player.CurrentTownId, new TownId("dryfork"), withHorseSession.Player.Inventory).Preview!;

        var noHorseSession = GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), noHorseInventory);
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

        var firstDay = session.AdvanceJourneyDay();
        var secondDay = session.AdvanceJourneyDay();

        Assert.True(firstDay.Success);
        Assert.Equal(JourneyStatus.Active, firstDay.Status);
        Assert.True(secondDay.Success);
        Assert.Equal(JourneyStatus.Completed, secondDay.Status);
        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Completed, session.Journey!.Status);
        Assert.Equal(new TownId("holloway"), session.Player.CurrentTownId);
        Assert.Equal(3, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(2, session.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
        Assert.Equal(10, session.Player.Inventory.GetCanteenState()!.Charges);
    }

    [Fact]
    public void AdvanceJourneyDayCanPauseForAHighRiskFoeEncounter()
    {
        var session = CreateHighRiskSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

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

        var result = session.ResolveJourneyEncounter("run");

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
        Assert.Equal(StartingHealthFor(session.TravelDifficulty), session.Player.Health);
        Assert.Equal(4, session.PursuitState.Heat);
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

        var result = session.ResolveJourneyEncounter("run");

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.DoesNotContain("delay", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, session.Journey!.DelayDays);
        Assert.Equal(5, session.PursuitState.Heat);
        Assert.Equal(TravelMode.Foot, session.Journey.TravelMode);
        Assert.Equal(StartingHealthFor(session.TravelDifficulty) - session.TravelRules.EncounterRunFootHealthLoss, session.Player.Health);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterRunCanLameMountedHorseAndFallBackToFoot()
    {
        var session = CreateHighRiskSession(travelDifficulty: TravelDifficulty.Hard);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        var remainingDaysBeforeRun = session.Journey!.RemainingDays;

        var result = session.ResolveJourneyEncounter("run");

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.Equal(TravelMode.Foot, session.Journey!.TravelMode);
        Assert.Equal(TravelMode.Foot, result.Journey!.TravelMode);
        Assert.Equal(remainingDaysBeforeRun, session.Journey.RemainingDays);
        Assert.Equal(0, session.Journey.DelayDays);
        Assert.Equal(new HorseTravelState(1, 0, 3), session.Player.Inventory.GetHorseState());
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterFightConsumesAmmoAndDamagesThePlayer()
    {
        var session = CreateHighRiskSession(withRevolverAmmo: 1);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();

        var result = session.ResolveJourneyEncounter("fight");

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.RevolverAmmo));
        Assert.Equal(StartingHealthFor(session.TravelDifficulty) - session.TravelRules.EncounterFightAmmoHealthLoss, session.Player.Health);
        Assert.Equal(4, session.PursuitState.Heat);
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

        var result = session.ResolveJourneyEncounter("fight");

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.Equal(StartingHealthFor(session.TravelDifficulty) - session.TravelRules.EncounterFightUnarmedHealthLoss, session.Player.Health);
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.RevolverAmmo));
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
    }

    [Fact]
    public void ResolveJourneyEncounterBribeSpendsCashAndResumesTheTrail()
    {
        var session = CreateHighRiskSession(wallet: Wallet.Starting(10m));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();

        var result = session.ResolveJourneyEncounter("bribe");

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.Equal(5m, session.Player.Wallet.Cash);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(2, session.Clock.Day);
        Assert.Equal(0, session.Clock.Turn);
        Assert.Contains(session.TravelDiaryDays[^1].Entries, entry => entry == "I bribe the rider with $5.00 and continue on.");
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), inventory);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, new TownId("pinecross"), Wallet.Starting(25m), inventory);
    }

    private static GameSession CreateDryMountedSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 2m)
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
    }

    private static DomainWorld CreateParityWorld(TrailRisk trailRisk = TrailRisk.Low)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
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
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
    }

    private static GameSession CreateLuckyFootSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var world = new DomainWorld(
            new[] { pinecross, silvercreek },
            new[]
            {
                new Trail(new TrailId("trail-pine-silver"), pinecross.Id, silvercreek.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek)
            });

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
    }

    private static GameSession CreateBadLuckSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
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

        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Easy);
    }

    private static GameSession CreateEasyLuckyWaterSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryspring = new Town(new TownId("dryspring"), "Dry Spring", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryspring },
            new[]
            {
                new Trail(new TrailId("trail-pine-dryspring"), pinecross.Id, dryspring.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m)
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Easy);
    }

    private static GameSession CreateHardBadLuckSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var hardpan = new Town(new TownId("hardpan"), "Hardpan", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, hardpan },
            new[]
            {
                new Trail(new TrailId("trail-pine-hardpan"), pinecross.Id, hardpan.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m)
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Hard);
    }

    private static GameSession CreateHardMountedHorseSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Hard);
    }

    private static GameSession CreateHighRiskSession(
        Wallet? wallet = null,
        int withRevolverAmmo = 2,
        bool withHorse = true,
        TravelDifficulty travelDifficulty = TravelDifficulty.Normal)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None)
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, wallet ?? Wallet.Starting(25m), inventory, travelDifficulty);
    }

    private static GameSession CreateProgressionSession(
        HorseTravelState horseState,
        TrailTerrain terrain,
        WaterFeature waterFeature,
        bool withSaddle = true,
        int canteenCharges = 2,
        TrailRisk trailRisk = TrailRisk.Low,
        TravelDifficulty travelDifficulty = TravelDifficulty.Normal)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new DomainInventory(items), travelDifficulty);
    }

    private static DomainWorld CreateWorld()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
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
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        return new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
    }

    private static int StartingHealthFor(TravelDifficulty travelDifficulty)
        => travelDifficulty switch
        {
            TravelDifficulty.Easy => 1250,
            TravelDifficulty.Hard => 800,
            _ => 1000
        };
}
