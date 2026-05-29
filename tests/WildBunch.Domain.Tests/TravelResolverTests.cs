using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
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
        Assert.Equal(2, result.Preview.ExpectedDays);
        Assert.Equal(2, result.Preview.RequiredFood);
        Assert.Equal(2, result.Preview.RequiredHorseFeed);
        Assert.Equal(TrailTerrain.Hills, result.Preview.RouteProfile.Terrain);
        Assert.Equal(WaterFeature.River, result.Preview.RouteProfile.WaterFeature);
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
        Assert.Equal(4, result.Preview.ExpectedDays);
        Assert.Equal(0, result.Preview.RequiredHorseFeed);
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
        Assert.Equal(2, session.Journey.RemainingDays);
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
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(2, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
        Assert.Equal(1, session.Journey!.RemainingDays);
        Assert.Equal(TravelMode.Mounted, session.Journey.TravelMode);
        Assert.Equal(HorseCondition.Healthy, session.Player.Inventory.GetHorseCondition());
    }

    [Fact]
    public void AdvanceJourneyDayRecalculatesPacingWhenHorseFeedRunsOut()
    {
        var session = CreateMountedSession(withHorseFeed: 0);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var result = session.AdvanceJourneyDay();

        Assert.True(result.Success);
        Assert.Equal(JourneyStatus.Active, result.Status);
        Assert.NotNull(session.Journey);
        Assert.Equal(TravelMode.Foot, session.Journey!.TravelMode);
        Assert.Equal(HorseCondition.Hungry, session.Player.Inventory.GetHorseCondition());
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
        Assert.Null(session.Journey);
        Assert.Equal(new TownId("holloway"), session.Player.CurrentTownId);
        Assert.Equal(1, session.Clock.Day);
        Assert.Equal(2, session.Clock.Turn);
        Assert.Equal(1, session.Player.Inventory.GetQuantity(DomainItemKind.Food));
        Assert.Equal(0, session.Player.Inventory.GetQuantity(DomainItemKind.HorseFeed));
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
        Assert.Equal(1, session.Clock.Turn);
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
        Assert.Equal(1, session.Clock.Turn);
        Assert.Equal(1, session.Journey!.DaysTravelled);
    }

    [Fact]
    public void ResolveJourneyEncounterRunAddsDelayAndResumesTheTrail()
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
        Assert.NotNull(session.Journey);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
        Assert.Equal(1, session.Journey.DelayDays);
        Assert.Equal(4, session.PursuitState.Heat);
        Assert.Equal(1, session.Clock.Turn);
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
        Assert.Equal(95, session.Player.Health);
        Assert.Equal(JourneyStatus.Active, session.Journey!.Status);
        Assert.Null(session.Journey.PendingEncounter);
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
        Assert.Equal(3m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Journey!.PendingEncounter);
        Assert.Equal(JourneyStatus.Interrupted, session.Journey.Status);
    }

    private static GameSession CreateMountedSession(int withHorseFeed = 2)
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseCondition.Healthy),
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

    private static GameSession CreateHighRiskSession(Wallet? wallet = null, int withRevolverAmmo = 2)
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
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Horse, 1, HorseCondition.Healthy),
            new DomainInventoryItem(DomainItemKind.Saddle, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, withRevolverAmmo)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, wallet ?? Wallet.Starting(25m), inventory);
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
}
