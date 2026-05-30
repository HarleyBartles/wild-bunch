using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Travel;

public sealed class TravelDiaryDayFactoryTests
{
    [Fact]
    public void CreateKeepsRawDiaryEntriesAndLeavesPlayerFacingProseToTheRenderer()
    {
        var journeySnapshot = CreateJourneySnapshot();
        var baseline = new TravelDiaryBaselineState(
            TravelMode.Mounted,
            3m,
            4,
            0,
            new TravelResourceSnapshot(null, 25m, 3, 0, 2, 0, 1000, 0));
        var currentResources = new TravelResourceSnapshot(null, 25m, 3, 0, 2, 0, 1000, 0);

        var day = TravelDiaryDayFactory.Create(
            journeySnapshot,
            baseline,
            currentResources,
            entries: new[] { "I found a cache of jerky and trail biscuits and picked up 2 food." });

        Assert.Null(day.JourneyBeat);
        Assert.Null(day.ResourceBeat);
        Assert.Equal(TrailTerrain.OpenRange, day.Terrain);
        Assert.True(day.RouteWaterSecure);
        Assert.Equal(0, day.CanteenChargesPerDay);
        Assert.Equal(new[] { "I found a cache of jerky and trail biscuits and picked up 2 food." }, day.Entries);
    }

    private static TravelJourneySnapshot CreateJourneySnapshot()
        => new(
            OriginTownId: new TownId("pinecross"),
            DestinationTownId: new TownId("dryfork"),
            OriginTownName: "Pinecross",
            DestinationTownName: "Dry Fork",
            RouteProfile: new TravelRouteProfile("trail-pine-dry", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m, 3m, 3m, Array.Empty<string>()),
            TravelMode: TravelMode.Mounted,
            Status: JourneyStatus.Active,
            MountedTravelAvailable: true,
            WaterSecure: true,
            RideDayDistance: 3m,
            RemainingRideDayDistance: 3m,
            ExpectedDays: 4,
            RemainingDays: 4,
            CanteenChargesPerDay: 0,
            RequiredCanteenCharges: 0,
            AvailableCanteenCharges: 2,
            CanteenReserveCharges: 2,
            DelayMarginDays: 0,
            DelayRisk: false,
            RequiredFood: 3,
            AvailableFood: 3,
            RequiredHorseFeed: 0,
            AvailableHorseFeed: 0,
            HorseState: null,
            OpeningNarration: "Opening narration",
            DaysTravelled: 1,
            DelayDays: 0,
            CurrentDayPlan: null,
            PendingEncounter: null,
            Warnings: Array.Empty<string>());
}
