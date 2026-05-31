using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class ActionAvailabilityResolverTests
{
    private static readonly TravelRandomnessState DeterministicTravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty);
    [Fact]
    public void TownWithSuppliesAndLodgingExposesSupplyAndLodgingActions()
    {
        var session = CreateSession(TownServices.Supplies | TownServices.Lodging);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.Contains(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewMap);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewJournal);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.BuySupplies);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.StayAtLodging);
    }

    [Fact]
    public void TownWithTelegraphExposesSendTelegram()
    {
        var session = CreateSession(TownServices.Telegraph);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.Contains(result, action => action.Kind == AvailableActionKind.SendTelegram);
    }

    [Fact]
    public void TownWithNoticeBoardExposesReadWantedPosters()
    {
        var session = CreateSession(TownServices.NoticeBoard);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.Contains(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
    }

    [Fact]
    public void TownWithoutNoticeBoardDoesNotExposeReadWantedPosters()
    {
        var session = CreateSession(TownServices.Supplies);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
    }

    [Fact]
    public void TownWithoutDoctorDoesNotExposeVisitDoctor()
    {
        var session = CreateSession(TownServices.Supplies);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.VisitDoctor);
    }

    [Fact]
    public void TownWithoutOutgoingTrailsDoesNotExposeTravel()
    {
        var session = CreateSession(TownServices.Supplies, addTrail: false);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewMap);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewJournal);
    }

    [Fact]
    public void ActiveJourneyReplacesTravelWithAdvanceTravelDay()
    {
        var session = CreateSession(TownServices.Supplies);
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("connected"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var resolver = new ActionAvailabilityResolver();
        var result = resolver.Resolve(session);

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.AdvanceTravelDay);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.BuySupplies);
    }

    [Fact]
    public void PendingEncounterReplacesAdvanceTravelDayWithResolveEncounter()
    {
        var session = CreateHighRiskSession();
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.MarkInterrupted(CreateFoeEncounter());

        var resolver = new ActionAvailabilityResolver();
        var result = resolver.Resolve(session);

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.AdvanceTravelDay);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ResolveTravelEncounter);
    }

    private static JourneyEncounterState CreateFoeEncounter()
        => JourneyEncounterState.CreateFoe(
            "A hard-eyed rider cuts across my path.",
            new JourneyFoeProfile(5, 5, 8m));

    private static GameSession CreateSession(TownServices currentTownServices, bool addTrail = true)
    {
        var currentTown = new Town(new TownId("current"), "Current Town", currentTownServices);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            addTrail
                ? new[]
                {
                    new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low)
                }
                : Array.Empty<Trail>());

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id, wallet: null, inventory: null, travelRandomness: DeterministicTravelRandomness);
    }

    private static GameSession CreateHighRiskSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, dryfork.Id, TrailRisk.High)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new WildBunch.Domain.Inventory.Inventory(new[]
        {
            new WildBunch.Domain.Inventory.InventoryItem(WildBunch.Domain.Inventory.ItemKind.Food, 3),
            new WildBunch.Domain.Inventory.InventoryItem(WildBunch.Domain.Inventory.ItemKind.Canteen, 1),
            new WildBunch.Domain.Inventory.InventoryItem(WildBunch.Domain.Inventory.ItemKind.Horse, 1, WildBunch.Domain.Inventory.HorseTravelState.Healthy),
            new WildBunch.Domain.Inventory.InventoryItem(WildBunch.Domain.Inventory.ItemKind.Saddle, 1),
            new WildBunch.Domain.Inventory.InventoryItem(WildBunch.Domain.Inventory.ItemKind.Knife, 1),
            new WildBunch.Domain.Inventory.InventoryItem(WildBunch.Domain.Inventory.ItemKind.Revolver, 1),
            new WildBunch.Domain.Inventory.InventoryItem(WildBunch.Domain.Inventory.ItemKind.RevolverAmmo, 2)
        });

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, WildBunch.Domain.Economy.Wallet.Starting(25m), inventory, travelRandomness: DeterministicTravelRandomness);
    }
}

