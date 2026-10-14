using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using DomainCanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using WildBunch.Domain.Travel;
using DomainWorld = WildBunch.Domain.World.World;
using DomainTown = WildBunch.Domain.World.Town;
using DomainTownId = WildBunch.Domain.World.TownId;
using DomainTrail = WildBunch.Domain.World.Trail;
using DomainTrailId = WildBunch.Domain.World.TrailId;
using DomainTrailRisk = WildBunch.Domain.World.TrailRisk;
using DomainTrailTerrain = WildBunch.Domain.World.TrailTerrain;
using DomainWaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Tests;

public sealed class TravelRulesProfileTests
{
    [Fact]
    public void NonDefaultProfileCanChangeTravelTuningWithoutDuplicatingRules()
    {
        var easyProfile = TravelRulesProfile.For(GameDifficulty.Easy);
        var horseState = new DomainHorseTravelState(3, 2, 3);
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 5),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: DomainCanteenState.Full(easyProfile.CanteenCapacity)),
            new DomainInventoryItem(DomainItemKind.Horse, 1, horseState),
            new DomainInventoryItem(DomainItemKind.Saddle, 1)
        });
        var world = new DomainWorld(
            new[]
            {
                new DomainTown(new DomainTownId("pinecross"), "Pinecross"),
                new DomainTown(new DomainTownId("holloway"), "Holloway")
            },
            new[]
            {
                new DomainTrail(new DomainTrailId("trail-easy"), new DomainTownId("pinecross"), new DomainTownId("holloway"), DomainTrailRisk.Low, DomainTrailTerrain.OpenRange, DomainWaterFeature.None, 5m)
            });
        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, new DomainTownId("pinecross"), Wallet.Starting(25m), inventory, GameDifficulty.Easy);

        Assert.False(horseState.CanProvideMountedTravel);
        Assert.True(horseState.CanProvideMountedTravelFor(easyProfile));

        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, new DomainTownId("holloway"), session.Player.Inventory, session.TravelRules);

        Assert.True(preview.Success);
        Assert.NotNull(preview.Preview);
        Assert.Equal(TravelMode.Mounted, preview.Preview!.TravelMode);
        Assert.Equal(4, preview.Preview.ExpectedDays);
        Assert.Equal(1.5m, preview.Preview.RouteProfile.MountedRideDayProgress);
        Assert.Equal(0.75m, preview.Preview.RouteProfile.FootRideDayProgress);
        Assert.Equal(10, preview.Preview.AvailableCanteenCharges);
        Assert.Equal(GameDifficulty.Easy, session.GameDifficulty);
    }
}
