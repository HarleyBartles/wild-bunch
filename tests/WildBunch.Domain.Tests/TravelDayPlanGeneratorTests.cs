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
using TownId = WildBunch.Domain.World.TownId;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class TravelDayPlanGeneratorTests
{
    [Fact]
    public void CreateTravelDayGenerationContextDerivesPressureBandsFromSessionState()
    {
        var session = CreatePressureSession(new HorseTravelState(0, 0, 2), food: 1, horseFeed: 0, canteenCharges: 0, wallet: 3m);
        var context = session.CreateTravelDayGenerationContext(gameSeed: "seed-1", scenarioProfileId: "profile-1");

        Assert.Equal(TravelDayPlanGenerator.CurrentVersion, context.GeneratorVersion);
        Assert.Equal("seed-1", context.GameSeed);
        Assert.Equal("profile-1", context.ScenarioProfileId);
        Assert.Equal(TravelPressureBand.High, context.FoodPressure);
        Assert.Equal(TravelPressureBand.Critical, context.CanteenPressure);
        Assert.Equal(TravelPressureBand.Critical, context.HorseFeedPressure);
        Assert.Equal(HorseConditionBand.Worn, context.HorseConditionBand);
        Assert.Equal(PursuitHeatBand.Calm, context.PursuitHeatBand);
        Assert.Equal(WalletBand.Tight, context.WalletBand);
        Assert.True(context.IsMounted);
        Assert.False(context.WaterSecure);
        Assert.Empty(context.RecentTrailEventKinds);
    }

    [Fact]
    public void GenerateIsDeterministicForTheSameContext()
    {
        var session = CreatePressureSession(HorseTravelState.Healthy);
        var context = session.CreateTravelDayGenerationContext(gameSeed: "seed-2", scenarioProfileId: "profile-2");

        var firstPlan = TravelDayPlanGenerator.Generate(context);
        var secondPlan = TravelDayPlanGenerator.Generate(context);

        Assert.True(PlansAreEquivalent(firstPlan, secondPlan));
    }

    [Fact]
    public void GenerateChangesWhenMountedStateChanges()
    {
        var baselineSession = CreateSeedSensitiveSession(withHorse: true, withSaddle: true);
        var stressedSession = CreateSeedSensitiveSession(withHorse: false, withSaddle: false);
        var seeds = new[] { "seed-1", "seed-2", "seed-3", "seed-4", "seed-5", "seed-6" };
        var foundDifference = false;

        foreach (var seed in seeds)
        {
            var baselinePlan = TravelDayPlanGenerator.Generate(baselineSession.CreateTravelDayGenerationContext(gameSeed: seed, scenarioProfileId: "profile-3"));
            var stressedPlan = TravelDayPlanGenerator.Generate(stressedSession.CreateTravelDayGenerationContext(gameSeed: seed, scenarioProfileId: "profile-3"));

            if (!PlansAreEquivalent(baselinePlan, stressedPlan))
            {
                foundDifference = true;
                break;
            }
        }

        Assert.True(foundDifference);
    }

    [Fact]
    public void GenerateChangesWhenSeedOrScenarioProfileChanges()
    {
        var firstSession = CreateSeedSensitiveSession();
        var secondSession = CreateSeedSensitiveSession();

        var seeds = new[] { ("seed-a", "profile-a"), ("seed-b", "profile-b"), ("seed-c", "profile-c"), ("seed-d", "profile-d") };
        var foundDifference = false;

        foreach (var (seed, profile) in seeds)
        {
            var firstPlan = TravelDayPlanGenerator.Generate(firstSession.CreateTravelDayGenerationContext(gameSeed: seed, scenarioProfileId: profile));
            var secondPlan = TravelDayPlanGenerator.Generate(secondSession.CreateTravelDayGenerationContext(gameSeed: seed + "-alt", scenarioProfileId: profile + "-alt"));

            if (!PlansAreEquivalent(firstPlan, secondPlan))
            {
                foundDifference = true;
                break;
            }
        }

        Assert.True(foundDifference);
    }

    private static GameSession CreatePressureSession(
        HorseTravelState horseState,
        int food = 1,
        int horseFeed = 0,
        int canteenCharges = 0,
        decimal wallet = 3m,
        bool withHorse = true,
        bool withSaddle = true)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pressure"), pinecross.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var items = new List<DomainInventoryItem>
        {
            new(DomainItemKind.Food, food),
            new(DomainItemKind.Canteen, 1, canteenState: new CanteenState(canteenCharges, Math.Max(2, canteenCharges))),
            new(DomainItemKind.Knife, 1)
        };

        if (withHorse)
        {
            items.Add(new DomainInventoryItem(DomainItemKind.Horse, 1, horseState));
        }

        if (horseFeed > 0)
        {
            items.Add(new DomainInventoryItem(DomainItemKind.HorseFeed, horseFeed));
        }

        if (withSaddle)
        {
            items.Add(new DomainInventoryItem(DomainItemKind.Saddle, 1));
        }

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(wallet), new DomainInventory(items));
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, dryfork.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static GameSession CreateSeedSensitiveSession(bool withHorse = true, bool withSaddle = true)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-sensitive"), pinecross.Id, dryfork.Id, TrailRisk.Moderate, TrailTerrain.OpenRange, WaterFeature.None, 3m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var items = new List<DomainInventoryItem>
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1, canteenState: new CanteenState(3, 4)),
            new DomainInventoryItem(DomainItemKind.Knife, 1)
        };

        if (withHorse)
        {
            items.Add(new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy));
        }

        if (withSaddle)
        {
            items.Add(new DomainInventoryItem(DomainItemKind.Saddle, 1));
        }

        var inventory = new DomainInventory(items);

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, dryfork.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static bool PlansAreEquivalent(TravelDayPlanState left, TravelDayPlanState right)
    {
        if (left.DayNumber != right.DayNumber || left.CurrentEncounterIndex != right.CurrentEncounterIndex || left.IsComplete != right.IsComplete || left.Encounters.Count != right.Encounters.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Encounters.Count; index++)
        {
            if (!EncountersAreEquivalent(left.Encounters[index], right.Encounters[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool EncountersAreEquivalent(TravelDayEncounterState left, TravelDayEncounterState right)
    {
        if (left.EncounterIndex != right.EncounterIndex
            || left.Category != right.Category
            || left.Title != right.Title
            || left.Message != right.Message
            || !Equals(left.TrailEvent, right.TrailEvent)
            || !ResolutionsAreEquivalent(left.PendingEncounter, right.PendingEncounter)
            || !EncounterResolutionsAreEquivalent(left.Resolution, right.Resolution))
        {
            return false;
        }

        return true;
    }

    private static bool ResolutionsAreEquivalent(JourneyEncounterState? left, JourneyEncounterState? right)
    {
        if (left is null || right is null)
        {
            return left is null && right is null;
        }

        if (left.Kind != right.Kind || left.Message != right.Message || left.Choices.Count != right.Choices.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Choices.Count; index++)
        {
            if (left.Choices[index].Id != right.Choices[index].Id || left.Choices[index].Label != right.Choices[index].Label)
            {
                return false;
            }
        }

        return true;
    }

    private static bool EncounterResolutionsAreEquivalent(TravelDiaryEncounterResolutionState? left, TravelDiaryEncounterResolutionState? right)
        => left is null
            ? right is null
            : right is not null
              && left.ChoiceId == right.ChoiceId
              && left.ChoiceLabel == right.ChoiceLabel
              && left.HealthDelta == right.HealthDelta
              && left.WalletDelta == right.WalletDelta
              && left.AmmoSpent == right.AmmoSpent
              && left.HeatIncrease == right.HeatIncrease
              && left.HorseExhaustionDelta == right.HorseExhaustionDelta
              && left.ContinuedOnFoot == right.ContinuedOnFoot;
}
