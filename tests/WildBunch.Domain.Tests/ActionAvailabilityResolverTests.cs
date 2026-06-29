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
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);

    [Fact]
    public void TownWithSuppliesExposesBuySuppliesAction()
    {
        var session = CreateSession(TownServices.None);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.Contains(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewMap);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewJournal);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.BuySupplies);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.LookAroundSaloon);
    }

    [Fact]
    public void TownWithTelegraphExposesSendTelegram()
    {
        var session = CreateSession(TownServices.Telegraph);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.Contains(result, action => action.Kind == AvailableActionKind.SendTelegram);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.FollowTelegraphLeads);
    }

    [Fact]
    public void TownWithNoticeBoardExposesReadWantedPosters()
    {
        var session = CreateSession(TownServices.None);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.Contains(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.GatherLocalGossip);
    }

    [Fact]
    public void TownWithoutNoticeBoardStillExposesBaselineInvestigationActions()
    {
        var session = CreateSession(TownServices.None);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        // ReadWantedPosters is always available - every town has a sheriff's office.
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.LookAroundSaloon);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.GatherLocalGossip);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.FollowTelegraphLeads);
    }

    [Fact]
    public void TownWithoutOutgoingTrailsDoesNotExposeTravel()
    {
        var session = CreateSession(TownServices.None, addTrail: false);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(session);

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewMap);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewJournal);
    }

    [Fact]
    public void ActiveJourneyReplacesTravelWithAdvanceTravelDay()
    {
        var session = CreateSession(TownServices.None);
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("connected"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var resolver = new ActionAvailabilityResolver();
        var result = resolver.Resolve(session);

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.AdvanceTravelDay);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.BuySupplies);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.LookAroundSaloon);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.FollowTelegraphLeads);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.GatherLocalGossip);
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
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.LookAroundSaloon);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
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
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id, wallet: null, inventory: null, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateHighRiskSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-1"), pinecross.Id, dryfork.Id, TrailRisk.High)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, WildBunch.Domain.Economy.Wallet.Starting(25m), inventory, saltSource: DeterministicSaltSource);
    }
}
