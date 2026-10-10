using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class ActionAvailabilityResolverTests
{
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);

    [Fact]
    public void TownWithSuppliesExposesBuySuppliesAction()
    {
        var session = CreateSession();
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(CreateContext(session));

        Assert.Contains(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewMap);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewJournal);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.BuySupplies);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.LookAroundSaloon);
    }

    [Fact]
    public void TownWithTelegraphBuildingDoesNotExposeTelegraphActions()
    {
        var session = CreateSession();
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(CreateContext(session));

        Assert.Equal(
            new[]
            {
                AvailableActionKind.Travel,
                AvailableActionKind.ViewMap,
                AvailableActionKind.ViewJournal,
                AvailableActionKind.ReadWantedPosters,
                AvailableActionKind.BuySupplies,
                AvailableActionKind.InspectNoticeBoard,
                AvailableActionKind.CheckSheriffRecords,
                AvailableActionKind.GatherLocalGossip,
                AvailableActionKind.LookAroundSaloon
            },
            result.Select(action => action.Kind));
    }

    [Fact]
    public void TownWithNoticeBoardExposesReadWantedPosters()
    {
        var session = CreateSession();
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(CreateContext(session));

        Assert.Contains(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.GatherLocalGossip);
    }

    [Fact]
    public void TownWithoutNoticeBoardStillExposesBaselineInvestigationActions()
    {
        var session = CreateSession();
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(CreateContext(session));

        // ReadWantedPosters is always available - every town has a sheriff's office.
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.LookAroundSaloon);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.GatherLocalGossip);
    }

    [Fact]
    public void TownWithoutOutgoingTrailsDoesNotExposeTravel()
    {
        var session = CreateSession(addTrail: false);
        var resolver = new ActionAvailabilityResolver();

        var result = resolver.Resolve(CreateContext(session));

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewMap);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ViewJournal);
    }

    [Fact]
    public void ActiveJourneyReplacesTravelWithAdvanceTravelDay()
    {
        var session = CreateSession();
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, new TownId("connected"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var resolver = new ActionAvailabilityResolver();
        var result = resolver.Resolve(CreateContext(session));

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.AdvanceTravelDay);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.BuySupplies);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.LookAroundSaloon);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.GatherLocalGossip);
    }

    [Fact]
    public void PendingEncounterReplacesAdvanceTravelDayWithResolveEncounter()
    {
        var session = CreateHighRiskSession();
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, new TownId("dryfork"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        session.Journey!.MarkInterrupted(CreateFoeEncounter());

        var resolver = new ActionAvailabilityResolver();
        var result = resolver.Resolve(CreateContext(session));

        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.Travel);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.AdvanceTravelDay);
        Assert.Contains(result, action => action.Kind == AvailableActionKind.ResolveTravelEncounter);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.LookAroundSaloon);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.DoesNotContain(result, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
    }

    [Fact]
    public void CompletedJourneyOffersNoFurtherAdvanceUntilArrivalIsAcknowledged()
    {
        var (session, preview) = TravelTestFactory.CreateSixDayQuietJourney();
        var start = session.StartJourney(preview);
        Assert.True(start.Success, start.Message);

        while (session.Journey?.Status == JourneyStatus.Active)
        {
            var advance = session.AdvanceJourneyDay();
            Assert.True(advance.Success, advance.Message);
        }

        Assert.Equal(JourneyStatus.Completed, session.Journey!.Status);

        var resolver = new ActionAvailabilityResolver();
        var arrivalPending = resolver.Resolve(CreateContext(session));
        Assert.DoesNotContain(arrivalPending, action => action.Kind == AvailableActionKind.AdvanceTravelDay);

        var acknowledgement = session.AcknowledgeJourneyArrival();
        Assert.True(acknowledgement.Success, acknowledgement.Message);

        var afterArrival = resolver.Resolve(CreateContext(session));
        Assert.Contains(afterArrival, action => action.Kind == AvailableActionKind.BuySupplies);
        Assert.Contains(afterArrival, action => action.Kind == AvailableActionKind.ReadWantedPosters);
        Assert.DoesNotContain(afterArrival, action => action.Kind == AvailableActionKind.AdvanceTravelDay);
    }

    [Fact]
    public void SetupWithoutSelectedTownHasNoActions()
    {
        var context = new ActionAvailabilityContext(
            StartFlowPhase.PrologueViewed,
            new DomainWorld([new Town(new TownId("current"), "Current Town")], []),
            CurrentTownId: null,
            Journey: null);

        var result = new ActionAvailabilityResolver().Resolve(context);

        Assert.Empty(result);
    }

    private static ActionAvailabilityContext CreateContext(GameSession session)
        => new(
            session.StartFlowPhase,
            session.World,
            session.Player.CurrentTownId,
            session.Journey?.ToSnapshot(session.TravelRules));

    private static JourneyEncounterState CreateFoeEncounter()
        => JourneyEncounterState.CreateFoe(
            "A hard-eyed rider cuts across my path.",
            new JourneyFoeProfile(5, 5, 8m));

    private static GameSession CreateSession(bool addTrail = true)
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
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
        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, currentTown.Id, wallet: null, inventory: null, gameDifficulty: GameDifficulty.Standard, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreateHighRiskSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork");
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

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, pinecross.Id, WildBunch.Domain.Economy.Wallet.Starting(25m), inventory, GameDifficulty.Standard, saltSource: DeterministicSaltSource);
    }
}
