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
using DomainTownServices = WildBunch.Domain.World.TownServices;

namespace WildBunch.Domain.Tests;

public sealed class TravelRulesProfileTests
{
    [Fact]
    public void DefaultProfileKeepsCurrentTravelTuning()
    {
        var profile = TravelRulesProfile.Default;

        Assert.Equal(TravelDifficulty.Normal, profile.Difficulty);
        Assert.Equal(2, profile.CanteenCapacity);
        Assert.Equal(3, profile.HorseHungerDeathThreshold);
        Assert.Equal(2, profile.HorseThirstDeathThreshold);
        Assert.Equal(3, profile.HorseExhaustionLameThreshold);
        Assert.Equal(5, profile.HorseExhaustionDeathThreshold);
        Assert.Equal(1m, profile.MountedRideDayProgress);
        Assert.Equal(0.5m, profile.FootRideDayProgress);
        Assert.Equal(1, profile.FirstEncounterDay);
        Assert.Equal(1, profile.FirstTrailEventDay);
    }

    [Fact]
    public void NonDefaultProfileCanChangeTravelTuningWithoutDuplicatingRules()
    {
        var easyProfile = TravelRulesProfile.For(TravelDifficulty.Easy);
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
                new DomainTown(new DomainTownId("pinecross"), "Pinecross", DomainTownServices.Supplies),
                new DomainTown(new DomainTownId("holloway"), "Holloway", DomainTownServices.Supplies)
            },
            new[]
            {
                new DomainTrail(new DomainTrailId("trail-easy"), new DomainTownId("pinecross"), new DomainTownId("holloway"), DomainTrailRisk.Low, DomainTrailTerrain.OpenRange, DomainWaterFeature.None, 5m)
            });
        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var session = GameSession.StartNew("Ranger Vale", world, caseFile, new DomainTownId("pinecross"), Wallet.Starting(25m), inventory, TravelDifficulty.Easy);

        Assert.False(horseState.CanProvideMountedTravel);
        Assert.True(horseState.CanProvideMountedTravelFor(easyProfile));

        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new DomainTownId("holloway"), session.Player.Inventory, session.TravelRules);

        Assert.True(preview.Success);
        Assert.NotNull(preview.Preview);
        Assert.Equal(TravelMode.Mounted, preview.Preview!.TravelMode);
        Assert.Equal(4, preview.Preview.ExpectedDays);
        Assert.Equal(1.5m, preview.Preview.RouteProfile.MountedRideDayProgress);
        Assert.Equal(0.75m, preview.Preview.RouteProfile.FootRideDayProgress);
        Assert.Equal(10, preview.Preview.AvailableCanteenCharges);
        Assert.Equal(TravelDifficulty.Easy, session.TravelDifficulty);
    }
}
