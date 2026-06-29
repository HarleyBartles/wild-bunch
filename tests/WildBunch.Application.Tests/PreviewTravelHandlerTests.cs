using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;

namespace WildBunch.Application.Tests;

public sealed class PreviewTravelHandlerTests
{
    [Fact]
    public async Task HandleAsyncReturnsStructuredPreviewForTheCockpit()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateMountedSession();
        repository.Seed(session);
        var handler = new PreviewTravelHandler(repository, new TravelResolver());

        var result = await handler.HandleAsync(new PreviewTravelQuery(session.Id.Value, "dryfork"));

        Assert.True(result.Success);
        Assert.Contains("Previewed mounted travel", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.Preview);
        Assert.Equal("pinecross", result.Preview!.OriginTownId);
        Assert.Equal("dryfork", result.Preview.DestinationTownId);
        Assert.Equal(TravelMode.Mounted, result.Preview.TravelMode);
        Assert.Equal(6m, result.Preview.RideDayDistance);
        Assert.Equal(4, result.Preview.ExpectedDays);
        Assert.Equal(2, result.Preview.CanteenChargesPerDay);
        Assert.Equal(8, result.Preview.RequiredCanteenCharges);
        Assert.Equal(8, result.Preview.AvailableCanteenCharges);
        Assert.Equal(0, result.Preview.CanteenReserveCharges);
        Assert.Equal(0, result.Preview.DelayMarginDays);
        Assert.True(result.Preview.DelayRisk);
        Assert.NotNull(result.Preview.HorseState);
        Assert.True(result.Preview.HorseState!.CanProvideMountedTravel);
        Assert.Equal("trail-pine-dry", result.Preview.RouteProfile.TrailId);
        Assert.Equal(TrailTerrain.OpenRange, result.Preview.RouteProfile.Terrain);
        Assert.Equal(WaterFeature.None, result.Preview.RouteProfile.WaterFeature);
        Assert.Equal(6m, result.Preview.RouteProfile.RideDayDistance);
        Assert.Equal(1.5m, result.Preview.RouteProfile.MountedRideDayProgress);
        Assert.Equal(0.75m, result.Preview.RouteProfile.FootRideDayProgress);
        Assert.Contains(result.Preview.RouteProfile.Warnings, warning => warning.Contains("water", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Preview.Warnings, warning => warning.Contains("exactly covers the base trail", StringComparison.OrdinalIgnoreCase));
    }

    private static GameSession CreateMountedSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 6m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 10),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: new CanteenState(8, 10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Easy);
    }
}
