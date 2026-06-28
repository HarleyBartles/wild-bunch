using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using DomainTown = WildBunch.Domain.World.Town;
using DomainTownServices = WildBunch.Domain.World.TownServices;
using DomainTrail = WildBunch.Domain.World.Trail;
using DomainTownId = WildBunch.Domain.World.TownId;
using TownId = WildBunch.Domain.World.TownId;
using DomainTrailId = WildBunch.Domain.World.TrailId;
using DomainTrailRisk = WildBunch.Domain.World.TrailRisk;
using DomainTrailTerrain = WildBunch.Domain.World.TrailTerrain;
using DomainWaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionJourneyHistoryTests
{
    private static readonly TravelRandomnessState DeterministicTravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty);

    [Fact]
    public void StartJourneyAssignsSessionScopedSequenceAndArchivesCompletedJourneyHistory()
    {
        var session = CreateSession();
        var firstPreview = CreateJourneyPreview(
            session.Player.CurrentTownId,
            new TownId("openpass"),
            "Pinecross",
            "Open Pass");

        session.StartJourney(firstPreview);

        Assert.NotNull(session.Journey);
        Assert.Equal(1, session.Journey!.JourneySequence);
        Assert.Equal(1, session.Journey.ToSnapshot(session.TravelRules).JourneySequence);

        session.Journey!.MarkCompleted();

        var acknowledgeResult = session.AcknowledgeJourneyArrival();

        Assert.True(acknowledgeResult.Success);
        Assert.Null(session.Journey);
        Assert.Single(session.CompletedJourneyHistory);
        Assert.Equal(1, session.CompletedJourneyHistory[0].JourneySequence);
        Assert.Equal(JourneyStatus.Completed, session.CompletedJourneyHistory[0].Status);
        Assert.Equal("openpass", session.CompletedJourneyHistory[0].DestinationTownId.Value);

        var secondPreview = CreateJourneyPreview(
            session.Player.CurrentTownId,
            new TownId("dryfork"),
            "Open Pass",
            "Dry Fork");
        session.StartJourney(secondPreview);

        Assert.NotNull(session.Journey);
        Assert.Equal(2, session.Journey!.JourneySequence);
        Assert.Single(session.CompletedJourneyHistory);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new DomainTown(new DomainTownId("pinecross"), "Pinecross", DomainTownServices.Supplies | DomainTownServices.Lodging | DomainTownServices.NoticeBoard);
        var openpass = new DomainTown(new DomainTownId("openpass"), "Open Pass", DomainTownServices.None);
        var dryfork = new DomainTown(new DomainTownId("dryfork"), "Dry Fork", DomainTownServices.None);

        var world = new DomainWorld(
            new[] { pinecross, openpass, dryfork },
            new[]
            {
                new DomainTrail(new DomainTrailId("trail-pine-open"), pinecross.Id, openpass.Id, DomainTrailRisk.Low, DomainTrailTerrain.OpenRange, DomainWaterFeature.None, 3m),
                new DomainTrail(new DomainTrailId("trail-open-dry"), openpass.Id, dryfork.Id, DomainTrailRisk.Low, DomainTrailTerrain.OpenRange, DomainWaterFeature.None, 3m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 6),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(6)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1)
        });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            pinecross.Id,
            Wallet.Starting(25m),
            inventory,
            GameDifficulty.Easy,
            travelRandomness: DeterministicTravelRandomness);
    }

    private static TravelPreview CreateJourneyPreview(TownId originTownId, TownId destinationTownId, string originTownName, string destinationTownName)
    {
        var routeProfile = new TravelRouteProfile(
            "trail-preview",
            DomainTrailRisk.Low,
            DomainTrailTerrain.OpenRange,
            DomainWaterFeature.None,
            1m,
            1m,
            1m,
            Array.Empty<string>());

        return new TravelPreview(
            originTownId,
            destinationTownId,
            originTownName,
            destinationTownName,
            routeProfile,
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
            HorseState: HorseTravelState.Healthy,
            Warnings: Array.Empty<string>());
    }
}
