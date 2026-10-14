using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
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
    private static readonly SaltSource DeterministicSaltSource = SaltSource.CreateFixed(string.Empty);
    private static readonly string[] FoePressureSeedCorpus = Enumerable.Range(1, 128)
        .Select(index => $"n16-foe-pressure-{index:D3}")
        .ToArray();

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
        Assert.Equal(WalletBand.Tight, context.WalletBand);
        Assert.True(context.IsMounted);
        Assert.False(context.WaterSecure);
        Assert.Empty(context.RecentTrailEventKinds);
        Assert.Empty(context.RecentEncounterCategories);
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
    public void GenerateCreatesOnlyHostileChoiceEncountersWithAResolvedFoeProfile()
    {
        var session = CreateHighRiskEncounterSession();
        var generatedEncounters = Enumerable.Range(1, 128)
            .Select(index =>
            {
                var context = session.CreateTravelDayGenerationContext(
                    gameSeed: $"retained-encounter-{index}",
                    scenarioProfileId: "friendly-encounter-retirement") with
                {
                    DayNumber = index
                };

                return TravelDayPlanGenerator.Generate(context).Encounters;
            })
            .SelectMany(encounters => encounters)
            .Where(encounter => encounter.PendingEncounter is not null)
            .ToArray();

        Assert.NotEmpty(generatedEncounters);
        Assert.All(generatedEncounters, encounter =>
        {
            Assert.Equal(TravelDayEncounterCategory.Foe, encounter.Category);
            Assert.Equal("foe", encounter.PendingEncounter!.Kind);
            Assert.NotNull(encounter.PendingEncounter.FoeProfile);
        });
    }

    [Fact]
    public void Generate_BrutalDifficultyProducesMoreFoeEncountersThanStandardForFixedHighRiskCorpus()
    {
        var session = CreateHighRiskEncounterSession();
        var baseContext = session.CreateTravelDayGenerationContext(gameSeed: "n16-foe-pressure", scenarioProfileId: "difficulty-pressure");

        var standardFoeCount = CountFoeEncounters(baseContext, GameDifficulty.Standard, GameEntropy.Classic);
        var brutalFoeCount = CountFoeEncounters(baseContext, GameDifficulty.Brutal, GameEntropy.Classic);

        Assert.True(standardFoeCount > 0, "The fixed Standard/Classic corpus must reach generated Foe encounters.");
        Assert.True(brutalFoeCount > 0, "The fixed Brutal/Classic corpus must reach generated Foe encounters.");
        Assert.True(
            brutalFoeCount > standardFoeCount * 1.1m,
            $"Brutal should produce at least 10% more actual Foe encounters than Standard over the fixed corpus. Standard: {standardFoeCount}, Brutal: {brutalFoeCount}.");
    }

    [Fact]
    public void Generate_WildEntropyDoesNotProduceMoreFoeEncountersThanClassicAtStandardDifficulty()
    {
        var session = CreateHighRiskEncounterSession();
        var baseContext = session.CreateTravelDayGenerationContext(gameSeed: "n16-foe-pressure", scenarioProfileId: "entropy-pressure");

        var classicFoeCount = CountFoeEncounters(baseContext, GameDifficulty.Standard, GameEntropy.Classic);
        var wildFoeCount = CountFoeEncounters(baseContext, GameDifficulty.Standard, GameEntropy.Wild);

        Assert.True(classicFoeCount > 0, "The fixed Standard/Classic corpus must reach generated Foe encounters.");
        Assert.True(wildFoeCount > 0, "The fixed Standard/Wild corpus must reach generated Foe encounters.");
        Assert.True(
            wildFoeCount <= classicFoeCount,
            $"Wild entropy must not produce more actual Foe encounters than Classic over the fixed corpus. Classic: {classicFoeCount}, Wild: {wildFoeCount}.");
    }

    [Fact]
    public void AdvanceJourneyDay_BoringEntropyProducesEquivalentStateAcrossThreeReachedDays()
    {
        var firstSession = CreatePressureSession(
            HorseTravelState.Healthy,
            food: 10,
            horseFeed: 10,
            canteenCharges: 20,
            wallet: 25m,
            routeDistance: 6m,
            gameEntropy: GameEntropy.Boring);
        var secondSession = CreatePressureSession(
            HorseTravelState.Healthy,
            food: 10,
            horseFeed: 10,
            canteenCharges: 20,
            wallet: 25m,
            routeDistance: 6m,
            gameEntropy: GameEntropy.Boring);

        Assert.Equal(SaltSourceMode.Fixed, firstSession.SaltSource.Mode);
        Assert.Equal(SaltSourceMode.Fixed, secondSession.SaltSource.Mode);

        for (var day = 1; day <= 3; day++)
        {
            var firstAdvance = firstSession.AdvanceJourneyDay();
            var secondAdvance = secondSession.AdvanceJourneyDay();

            Assert.Equal(firstAdvance.Success, secondAdvance.Success);
            Assert.Equal(firstAdvance.Status, secondAdvance.Status);
            Assert.Equal(firstAdvance.Message, secondAdvance.Message);
            Assert.Equal(firstAdvance.LogMessage, secondAdvance.LogMessage);
            Assert.Equal(firstAdvance.HeatIncrease, secondAdvance.HeatIncrease);
            Assert.Equal(firstAdvance.TrailEvent, secondAdvance.TrailEvent);
            AssertJourneySnapshotsEquivalent(
                Assert.IsType<TravelJourneySnapshot>(firstAdvance.Journey),
                Assert.IsType<TravelJourneySnapshot>(secondAdvance.Journey));

            var firstDayEvent = Assert.Single(firstSession.UncommittedEvents.OfType<TravelDayAdvanced>(), item => item.Day == firstSession.Clock.Day);
            var secondDayEvent = Assert.Single(secondSession.UncommittedEvents.OfType<TravelDayAdvanced>(), item => item.Day == secondSession.Clock.Day);
            AssertTravelDayAdvancedEquivalent(firstDayEvent, secondDayEvent);
            Assert.Equal(day, firstDayEvent.JourneySnapshot.DaysTravelled);
            Assert.Equal(day, secondDayEvent.JourneySnapshot.DaysTravelled);
            AssertSessionStateEquivalent(firstSession, secondSession);

            var encounterResolutions = 0;
            while (firstSession.Journey?.PendingEncounter is not null || secondSession.Journey?.PendingEncounter is not null)
            {
                Assert.NotNull(firstSession.Journey?.PendingEncounter);
                Assert.NotNull(secondSession.Journey?.PendingEncounter);
                Assert.True(ResolutionsAreEquivalent(firstSession.Journey!.PendingEncounter, secondSession.Journey!.PendingEncounter));
                Assert.True(encounterResolutions++ < 10, "A travel day must not require an unbounded number of encounter resolutions.");

                var firstResolution = firstSession.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0UL);
                var secondResolution = secondSession.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0UL);

                Assert.Equal(firstResolution.Success, secondResolution.Success);
                Assert.Equal(firstResolution.SessionChanged, secondResolution.SessionChanged);
                Assert.Equal(firstResolution.Status, secondResolution.Status);
                Assert.Equal(firstResolution.Message, secondResolution.Message);
                Assert.Equal(firstResolution.AdditionalDiaryMessages, secondResolution.AdditionalDiaryMessages);
                AssertJourneySnapshotsEquivalent(
                    Assert.IsType<TravelJourneySnapshot>(firstResolution.Journey),
                    Assert.IsType<TravelJourneySnapshot>(secondResolution.Journey));
                AssertSessionStateEquivalent(firstSession, secondSession);
            }

            Assert.Equal(JourneyStatus.Active, firstSession.Journey!.Status);
            Assert.Equal(JourneyStatus.Active, secondSession.Journey!.Status);
            Assert.Equal(day, firstSession.Journey.DaysTravelled);
            Assert.Equal(day, secondSession.Journey.DaysTravelled);
        }

        Assert.Equal(3, firstSession.Journey!.DaysTravelled);
        Assert.Equal(3, secondSession.Journey!.DaysTravelled);
    }



    [Fact]
    public void GenerateAvoidsRepeatingLuckyTrailEventsOnConsecutiveDays()
    {
        var session = CreateLuckyCooldownSession();
        var context = session.CreateTravelDayGenerationContext(gameSeed: "n29-lucky-22", scenarioProfileId: "profile-lucky") with
        {
            DayNumber = 2,
            GameEntropy = GameEntropy.Adventurous,
            RecentTrailEventKinds = Array.Empty<JourneyTrailEventKind>(),
            RecentTrailEventIds = Array.Empty<JourneyTrailEventId>(),
            RecentEncounterCategories = Array.Empty<TravelDayEncounterCategory>()
        };

        var withoutHistory = TravelDayPlanGenerator.Generate(context);
        var withLuckyHistory = TravelDayPlanGenerator.Generate(context with
        {
            RecentTrailEventKinds = new[] { JourneyTrailEventKind.Lucky }
        });

        Assert.Contains(withoutHistory.Encounters, encounter => encounter.Category == TravelDayEncounterCategory.Lucky);
        Assert.NotEmpty(withLuckyHistory.Encounters);
        Assert.DoesNotContain(withLuckyHistory.Encounters, encounter => encounter.Category == TravelDayEncounterCategory.Lucky);
    }

    [Fact]
    public void GenerateUsesRecentFoeHistoryToBreakUpBackToBackRiderDays()
    {
        var session = CreateHighRiskEncounterSession();
        var baseContext = session.CreateTravelDayGenerationContext(gameSeed: "n46-foe-cooldown-0050", scenarioProfileId: "profile-foe-cooldown");
        var withoutRecentFoe = baseContext with
        {
            DayNumber = 2,
            RecentEncounterCategories = Array.Empty<TravelDayEncounterCategory>()
        };
        var withRecentFoe = withoutRecentFoe with
        {
            RecentEncounterCategories = new[] { TravelDayEncounterCategory.Foe }
        };

        var baselinePlan = TravelDayPlanGenerator.Generate(withoutRecentFoe);
        var cooledPlan = TravelDayPlanGenerator.Generate(withRecentFoe);

        Assert.Equal(TravelDayEncounterCategory.Foe, baselinePlan.Encounters[0].Category);
        Assert.NotEqual(TravelDayEncounterCategory.Foe, cooledPlan.Encounters[0].Category);
    }

    [Fact]
    public void GenerateKeepsFoeSelectionAheadOfUnluckySelectionOnComparableHighRiskRoutes()
    {
        var session = CreateHighRiskEncounterSession();
        var foeCount = 0;
        var unluckyCount = 0;

        foreach (var seed in new[] { "seed-foe-balance", "seed-foe-balance-a", "seed-foe-balance-b", "seed-foe-balance-c" })
        {
            var context = session.CreateTravelDayGenerationContext(gameSeed: seed, scenarioProfileId: "profile-balance") with
            {
                RecentEncounterCategories = Array.Empty<TravelDayEncounterCategory>()
            };

            var plan = TravelDayPlanGenerator.Generate(context);

            if (plan.Encounters.Any(encounter => encounter.Category == TravelDayEncounterCategory.Foe))
            {
                foeCount++;
            }

            if (plan.Encounters.Any(encounter => encounter.Category == TravelDayEncounterCategory.Unlucky))
            {
                unluckyCount++;
            }
        }

        Assert.True(foeCount > 0);
        Assert.True(foeCount >= unluckyCount);
    }

    [Fact]
    public void GenerateFallsBackToLawfulNonHorseBadLuckWhenNoHorseAndDelayCooldownIsActive()
    {
        var session = CreateNoHorseBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        var context = session.CreateTravelDayGenerationContext(gameSeed: "n28-lawful-1", scenarioProfileId: "profile-no-horse") with
        {
            RecentTrailEventKinds = new[] { JourneyTrailEventKind.BadLuck },
            RecentTrailEventIds = new[] { JourneyTrailEventId.BadLuckWashout },
            RecentEncounterCategories = Array.Empty<TravelDayEncounterCategory>()
        };

        var plan = TravelDayPlanGenerator.Generate(context);

        Assert.NotEmpty(plan.Encounters);
        var unluckyEncounters = plan.Encounters
            .Where(encounter => encounter.Category == TravelDayEncounterCategory.Unlucky)
            .ToArray();
        Assert.NotEmpty(unluckyEncounters);
        Assert.All(unluckyEncounters, encounter =>
        {
            Assert.Equal(JourneyTrailEventId.BadLuckDustStorm, encounter.TrailEvent?.Id);
            Assert.Equal(JourneyTrailEventKind.BadLuck, encounter.TrailEvent?.Kind);
            Assert.Equal(0, encounter.TrailEvent?.DelayDays);
        });
    }

    [Fact]
    public void GenerateDoesNotRepeatBadLuckDustStormWhenItsEventIdWasRecentlyRecorded()
    {
        var session = CreateNoHorseBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var context = session.CreateTravelDayGenerationContext(gameSeed: "n29-badluck-8", scenarioProfileId: "profile-badluck") with
        {
            DayNumber = 2,
            RecentTrailEventKinds = new[] { JourneyTrailEventKind.BadLuck },
            RecentTrailEventIds = Array.Empty<JourneyTrailEventId>(),
            RecentEncounterCategories = Array.Empty<TravelDayEncounterCategory>()
        };
        var withoutId = TravelDayPlanGenerator.Generate(context);
        var withDustStormId = TravelDayPlanGenerator.Generate(context with
        {
            RecentTrailEventIds = new[] { JourneyTrailEventId.BadLuckDustStorm }
        });

        var withoutIdUnlucky = withoutId.Encounters.Where(encounter => encounter.Category == TravelDayEncounterCategory.Unlucky).ToArray();
        var withIdUnlucky = withDustStormId.Encounters.Where(encounter => encounter.Category == TravelDayEncounterCategory.Unlucky).ToArray();

        Assert.NotEmpty(withoutIdUnlucky);
        Assert.NotEmpty(withIdUnlucky);
        Assert.Contains(withoutIdUnlucky, encounter => encounter.TrailEvent?.Id == JourneyTrailEventId.BadLuckDustStorm);
        Assert.DoesNotContain(withIdUnlucky, encounter => encounter.TrailEvent?.Id == JourneyTrailEventId.BadLuckDustStorm);
    }

    [Fact]
    public void GenerateFallsBackToQuietWhenNoUnluckyCandidateRemains()
    {
        var session = CreateNoHorseBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var context = session.CreateTravelDayGenerationContext(gameSeed: "seed-no-unlucky", scenarioProfileId: "profile-no-unlucky") with
        {
            RecentTrailEventKinds = new[] { JourneyTrailEventKind.BadLuck },
            RecentTrailEventIds = new[] { JourneyTrailEventId.BadLuckWashout, JourneyTrailEventId.BadLuckDustStorm }
        };

        var plan = TravelDayPlanGenerator.Generate(context);

        Assert.NotEmpty(plan.Encounters);
        var quietEncounters = plan.Encounters
            .Where(encounter => encounter.Category == TravelDayEncounterCategory.Quiet)
            .ToArray();
        Assert.NotEmpty(quietEncounters);
        Assert.All(quietEncounters, encounter =>
        {
            Assert.Null(encounter.TrailEvent);
        });
    }

    [Fact]
    public void GenerateFallsBackWhenOnlyTheHorseSpecificUnluckyEventRemainsWithoutAHorse()
    {
        const string seed = "n32-nohorse-probe-7";
        var context = CreateHorseTroubleContext(seed, HorseConditionBand.None, TravelMode.Foot) with
        {
            ScenarioProfileId = "profile-no-horse-unlucky",
            RecentTrailEventKinds = new[] { JourneyTrailEventKind.BadLuck },
            RecentTrailEventIds = new[]
            {
                JourneyTrailEventId.BadLuckWashout,
                JourneyTrailEventId.BadLuckFoodLoss,
                JourneyTrailEventId.BadLuckDustStorm
            }
        };

        var plan = TravelDayPlanGenerator.Generate(context);

        Assert.Contains(plan.Encounters, encounter => encounter.Category == TravelDayEncounterCategory.Quiet && encounter.TrailEvent is null);
        Assert.DoesNotContain(plan.Encounters, encounter => encounter.TrailEvent?.Id == JourneyTrailEventId.BadLuckSpookedHorse);
    }

    [Fact]
    public void GenerateAppliesHorseExhaustionToMountedHorseTroubleEvent()
    {
        var context = CreateHorseTroubleContext("seed-horse-present-1", HorseConditionBand.Worn, TravelMode.Mounted);
        var plan = TravelDayPlanGenerator.Generate(context);
        var horseTroubleEncounters = plan.Encounters
            .Where(encounter => encounter.Category == TravelDayEncounterCategory.HorseTrouble)
            .ToArray();

        Assert.NotEmpty(horseTroubleEncounters);
        Assert.All(horseTroubleEncounters, encounter =>
        {
            Assert.Equal(JourneyTrailEventId.BadLuckSpookedHorse, encounter.TrailEvent?.Id);
            Assert.True(encounter.TrailEvent!.HorseExhaustionDelta > 0);
        });
    }

    private static TravelDayGenerationContext CreateHorseTroubleContext(string seed, HorseConditionBand horseConditionBand, TravelMode travelMode)
        => new(
            TravelDayPlanGenerator.CurrentVersion,
            seed,
            "profile-horse-trouble",
            "trail-horse-trouble",
            new TownId("pinecross"),
            new TownId("ridgeway"),
            1,
            travelMode,
            TrailRisk.Low,
            TrailTerrain.Hills,
            WaterFeature.River,
            GameDifficulty.Challenging,
            3,
            3m,
            TravelPressureBand.None,
            TravelPressureBand.None,
            TravelPressureBand.None,
            horseConditionBand,
            WalletBand.Steady,
            Array.Empty<JourneyTrailEventKind>(),
            Array.Empty<JourneyTrailEventId>(),
            Array.Empty<TravelDayEncounterCategory>(),
            HasHorse: horseConditionBand != HorseConditionBand.None,
            SaltSourceMode.Fixed,
            seed);

    private static GameSession CreateNoHorseBadLuckSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var holloway = new Town(new TownId("holloway"), "Holloway");
        var world = new DomainWorld(
            new[] { pinecross, holloway },
            new[]
            {
                new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new DomainInventory(new[]
        {
            new DomainInventoryItem(DomainItemKind.Food, 3),
            new DomainInventoryItem(DomainItemKind.Canteen, 1),
            new DomainInventoryItem(DomainItemKind.Knife, 1),
            new DomainInventoryItem(DomainItemKind.Revolver, 1),
            new DomainInventoryItem(DomainItemKind.RevolverAmmo, 2)
        });

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory, GameDifficulty.Standard, saltSource: DeterministicSaltSource);
    }

    private static GameSession CreatePressureSession(
        HorseTravelState horseState,
        int food = 1,
        int horseFeed = 0,
        int canteenCharges = 0,
        decimal wallet = 3m,
        bool withHorse = true,
        bool withSaddle = true,
        decimal routeDistance = 3m,
        GameEntropy gameEntropy = GameEntropy.Classic)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork");
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pressure"), pinecross.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, routeDistance)
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

        var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(wallet), new DomainInventory(items), GameDifficulty.Standard, saltSource: DeterministicSaltSource, gameEntropy: gameEntropy);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, dryfork.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }


    private static GameSession CreateLuckyCooldownSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var creekside = new Town(new TownId("creekside"), "Creekside");
        var world = new DomainWorld(
            new[] { pinecross, creekside },
            new[]
            {
                new Trail(new TrailId("trail-lucky"), pinecross.Id, creekside.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var items = new List<DomainInventoryItem>
        {
            new(DomainItemKind.Food, 4),
            new(DomainItemKind.Canteen, 1, canteenState: new CanteenState(3, 4)),
            new(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new(DomainItemKind.Saddle, 1),
            new(DomainItemKind.Knife, 1)
        };

        var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new DomainInventory(items), GameDifficulty.Easy, saltSource: DeterministicSaltSource);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, creekside.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static GameSession CreateHighRiskEncounterSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross");
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork");
        var world = new DomainWorld(
            new[] { pinecross, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-high"), pinecross.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None, 3m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var items = new List<DomainInventoryItem>
        {
            new(DomainItemKind.Food, 4),
            new(DomainItemKind.Canteen, 1, canteenState: new CanteenState(4, 4)),
            new(DomainItemKind.Horse, 1, HorseTravelState.Healthy),
            new(DomainItemKind.Saddle, 1),
            new(DomainItemKind.Knife, 1)
        };

        var session = TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new DomainInventory(items), GameDifficulty.Challenging, saltSource: DeterministicSaltSource);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId!.Value, dryfork.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static int CountFoeEncounters(
        TravelDayGenerationContext baseContext,
        GameDifficulty difficulty,
        GameEntropy entropy)
        => FoePressureSeedCorpus.Sum(gameSeed => TravelDayPlanGenerator.Generate(baseContext with
        {
            GameSeed = gameSeed,
            GameDifficulty = difficulty,
            GameEntropy = entropy
        }).Encounters.Count(encounter => encounter.Category == TravelDayEncounterCategory.Foe));

    private static void AssertTravelDayAdvancedEquivalent(TravelDayAdvanced expected, TravelDayAdvanced actual)
    {
        Assert.Equal(expected.Day, actual.Day);
        AssertJourneySnapshotsEquivalent(expected.JourneySnapshot, actual.JourneySnapshot);
        Assert.Equal(expected.HealthDelta, actual.HealthDelta);
        Assert.Equal(expected.PursuitHeat, actual.PursuitHeat);
        Assert.Equal(expected.DayOutcome, actual.DayOutcome);
        Assert.Equal(expected.DiaryMessage, actual.DiaryMessage);
        Assert.Equal(expected.HorseLostMessage, actual.HorseLostMessage);
        Assert.Equal(expected.AdditionalDiaryMessages, actual.AdditionalDiaryMessages);
        Assert.Equal(expected.DayEntries, actual.DayEntries);
    }

    private static void AssertJourneySnapshotsEquivalent(TravelJourneySnapshot expected, TravelJourneySnapshot actual)
    {
        Assert.Equal(expected.JourneySequence, actual.JourneySequence);
        Assert.Equal(expected.OriginTownId, actual.OriginTownId);
        Assert.Equal(expected.DestinationTownId, actual.DestinationTownId);
        Assert.Equal(expected.OriginTownName, actual.OriginTownName);
        Assert.Equal(expected.DestinationTownName, actual.DestinationTownName);
        Assert.Equal(expected.RouteProfile.TrailId, actual.RouteProfile.TrailId);
        Assert.Equal(expected.RouteProfile.Risk, actual.RouteProfile.Risk);
        Assert.Equal(expected.RouteProfile.Terrain, actual.RouteProfile.Terrain);
        Assert.Equal(expected.RouteProfile.WaterFeature, actual.RouteProfile.WaterFeature);
        Assert.Equal(expected.RouteProfile.RideDayDistance, actual.RouteProfile.RideDayDistance);
        Assert.Equal(expected.RouteProfile.MountedRideDayProgress, actual.RouteProfile.MountedRideDayProgress);
        Assert.Equal(expected.RouteProfile.FootRideDayProgress, actual.RouteProfile.FootRideDayProgress);
        Assert.Equal(expected.RouteProfile.Warnings, actual.RouteProfile.Warnings);
        Assert.Equal(expected.TravelMode, actual.TravelMode);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.MountedTravelAvailable, actual.MountedTravelAvailable);
        Assert.Equal(expected.WaterSecure, actual.WaterSecure);
        Assert.Equal(expected.RideDayDistance, actual.RideDayDistance);
        Assert.Equal(expected.RemainingRideDayDistance, actual.RemainingRideDayDistance);
        Assert.Equal(expected.ExpectedDays, actual.ExpectedDays);
        Assert.Equal(expected.RemainingDays, actual.RemainingDays);
        Assert.Equal(expected.CanteenChargesPerDay, actual.CanteenChargesPerDay);
        Assert.Equal(expected.RequiredCanteenCharges, actual.RequiredCanteenCharges);
        Assert.Equal(expected.AvailableCanteenCharges, actual.AvailableCanteenCharges);
        Assert.Equal(expected.CanteenReserveCharges, actual.CanteenReserveCharges);
        Assert.Equal(expected.DelayMarginDays, actual.DelayMarginDays);
        Assert.Equal(expected.DelayRisk, actual.DelayRisk);
        Assert.Equal(expected.RequiredFood, actual.RequiredFood);
        Assert.Equal(expected.AvailableFood, actual.AvailableFood);
        Assert.Equal(expected.RequiredHorseFeed, actual.RequiredHorseFeed);
        Assert.Equal(expected.AvailableHorseFeed, actual.AvailableHorseFeed);
        Assert.Equal(expected.HorseState, actual.HorseState);
        Assert.Equal(expected.OpeningNarration, actual.OpeningNarration);
        Assert.Equal(expected.DaysTravelled, actual.DaysTravelled);
        Assert.Equal(expected.DelayDays, actual.DelayDays);
        Assert.Equal(expected.CurrentDayPlan is null, actual.CurrentDayPlan is null);
        if (expected.CurrentDayPlan is not null && actual.CurrentDayPlan is not null)
        {
            Assert.True(PlansAreEquivalent(expected.CurrentDayPlan, actual.CurrentDayPlan));
        }
        Assert.True(ResolutionsAreEquivalent(expected.PendingEncounter, actual.PendingEncounter));
        Assert.Equal(expected.Warnings, actual.Warnings);
    }

    private static void AssertSessionStateEquivalent(GameSession expected, GameSession actual)
    {
        Assert.Equal(expected.Status, actual.Status);
        Assert.Equal(expected.Clock.Day, actual.Clock.Day);
        Assert.Equal(expected.Clock.Turn, actual.Clock.Turn);
        Assert.Equal(expected.Player.Health, actual.Player.Health);
        Assert.Equal(expected.Player.Wallet.Cash, actual.Player.Wallet.Cash);
        Assert.Equal(expected.Player.CurrentTownId, actual.Player.CurrentTownId);
        Assert.Equal(expected.Player.GetCanteenState(), actual.Player.GetCanteenState());
        Assert.Equal(expected.Player.GetHorseState(), actual.Player.GetHorseState());
        Assert.Equal(
            expected.Player.Inventory.Items.OrderBy(item => item.Kind).Select(item => (item.Kind, item.Quantity, item.HorseState, item.CanteenState)),
            actual.Player.Inventory.Items.OrderBy(item => item.Kind).Select(item => (item.Kind, item.Quantity, item.HorseState, item.CanteenState)));
        Assert.True(expected.Journey is not null && actual.Journey is not null);
        AssertJourneySnapshotsEquivalent(expected.Journey!.ToSnapshot(expected.TravelRules), actual.Journey!.ToSnapshot(actual.TravelRules));
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

        if (!Equals(left.FoeProfile, right.FoeProfile) || left.ResolutionAttempts != right.ResolutionAttempts)
        {
            return false;
        }

        if (!HiddenStatesAreEquivalent(left.HiddenState, right.HiddenState))
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

    private static bool HiddenStatesAreEquivalent(JourneyEncounterHiddenState? left, JourneyEncounterHiddenState? right)
        => left is null
            ? right is null
            : right is not null
              && left.BribeOffersMade == right.BribeOffersMade
              && left.CumulativeBribePaid == right.CumulativeBribePaid
              && left.BribeLockedOut == right.BribeLockedOut
              && left.ChaseFatigue == right.ChaseFatigue
              && left.Annoyance == right.Annoyance
              && left.Shaken == right.Shaken;

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




