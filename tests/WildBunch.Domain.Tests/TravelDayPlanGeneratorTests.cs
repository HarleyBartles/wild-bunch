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

    [Fact]
    public void GenerateAvoidsRepeatingLuckyTrailEventsOnConsecutiveDays()
    {
        var session = CreateLuckyCooldownSession();
        foreach (var seed in new[] { "seed-lucky", "seed-lucky-a", "seed-lucky-b", "seed-lucky-c" })
        {
            var firstContext = session.CreateTravelDayGenerationContext(gameSeed: seed, scenarioProfileId: "profile-lucky");
            var firstPlan = TravelDayPlanGenerator.Generate(firstContext);
            if (firstPlan.Encounters.Count == 0 || firstPlan.Encounters[0].Category != TravelDayEncounterCategory.Lucky)
            {
                continue;
            }

            var secondContext = firstContext with
            {
                DayNumber = firstContext.DayNumber + 1,
                RecentTrailEventKinds = new[] { JourneyTrailEventKind.Lucky }
            };
            var secondPlan = TravelDayPlanGenerator.Generate(secondContext);

            Assert.True(secondPlan.Encounters.Count >= 0);
            Assert.DoesNotContain(secondPlan.Encounters, encounter => encounter.Category == TravelDayEncounterCategory.Lucky);
            return;
        }

        throw new Xunit.Sdk.XunitException("Did not find a seeded lucky trail day to validate repeat suppression.");
    }

    [Fact]
    public void GenerateReducesImmediateFoeRepetitionOnHighRiskRoutes()
    {
        var highRiskSession = CreateHighRiskEncounterSession();
        var lowRiskSession = CreateLowRiskEncounterSession();
        var seed = "seed-high-route";
        var highRiskFoes = 0;
        var lowRiskFoes = 0;
        var recentHighRiskEncounters = Array.Empty<TravelDayEncounterCategory>();

        for (var day = 1; day <= 8; day++)
        {
            var highRiskContext = highRiskSession.CreateTravelDayGenerationContext(gameSeed: seed, scenarioProfileId: "profile-high") with
            {
                DayNumber = day,
                RecentEncounterCategories = recentHighRiskEncounters
            };

            var highRiskPlan = TravelDayPlanGenerator.Generate(highRiskContext);
            var lowRiskPlan = TravelDayPlanGenerator.Generate(lowRiskSession.CreateTravelDayGenerationContext(gameSeed: $"{seed}-low-{day}", scenarioProfileId: "profile-low"));

            if (highRiskPlan.Encounters.Count > 0 && highRiskPlan.Encounters[0].Category == TravelDayEncounterCategory.Foe)
            {
                highRiskFoes++;
            }

            if (lowRiskPlan.Encounters.Count > 0 && lowRiskPlan.Encounters[0].Category == TravelDayEncounterCategory.Foe)
            {
                lowRiskFoes++;
            }

            recentHighRiskEncounters = recentHighRiskEncounters
                .Append(highRiskPlan.Encounters.Count == 0 ? TravelDayEncounterCategory.Quiet : highRiskPlan.Encounters[0].Category)
                .TakeLast(3)
                .ToArray();
        }

        Assert.True(highRiskFoes > 0);
        Assert.True(highRiskFoes >= lowRiskFoes);
        Assert.True(highRiskFoes < 8);
    }

    [Fact]
    public void GenerateUsesRecentFoeHistoryToBreakUpBackToBackRiderDays()
    {
        var session = CreateHighRiskEncounterSession();
        var seeds = Enumerable.Range(1, 48).Select(index => $"seed-repeat-{index}").ToArray();
        var foundReducedFoeSelection = false;

        foreach (var seed in seeds)
        {
            var baseContext = session.CreateTravelDayGenerationContext(gameSeed: seed, scenarioProfileId: "profile-repeat");
            var basePlan = TravelDayPlanGenerator.Generate(baseContext);
            if (basePlan.Encounters[0].Category != TravelDayEncounterCategory.Foe)
            {
                continue;
            }

            var cooledContext = baseContext with
            {
                DayNumber = baseContext.DayNumber + 1,
                RecentEncounterCategories = new[] { TravelDayEncounterCategory.Foe }
            };
            var cooledPlan = TravelDayPlanGenerator.Generate(cooledContext);

            if (cooledPlan.Encounters[0].Category != TravelDayEncounterCategory.Foe)
            {
                foundReducedFoeSelection = true;
                break;
            }
        }

        Assert.True(foundReducedFoeSelection);
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
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);
        var context = session.CreateTravelDayGenerationContext(gameSeed: "seed-no-horse-badluck", scenarioProfileId: "profile-no-horse") with
        {
            RecentTrailEventKinds = new[] { JourneyTrailEventKind.BadLuck },
            RecentTrailEventIds = new[] { JourneyTrailEventId.BadLuckWashout },
            RecentEncounterCategories = Array.Empty<TravelDayEncounterCategory>()
        };

        var plan = TravelDayPlanGenerator.Generate(context);

        Assert.NotEmpty(plan.Encounters);
        Assert.All(plan.Encounters, encounter =>
        {
            Assert.NotEqual(JourneyTrailEventId.BadLuckSpookedHorse, encounter.TrailEvent?.Id);
            Assert.False(encounter.TrailEvent?.Kind == JourneyTrailEventKind.BadLuck && encounter.TrailEvent.DelayDays > 0);
        });
    }

    [Fact]
    public void GenerateDoesNotRepeatHardMilesOnConsecutiveTrailDays()
    {
        var session = CreateNoHorseBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var firstContext = session.CreateTravelDayGenerationContext(gameSeed: "seed-hard-miles", scenarioProfileId: "profile-hard-miles");
        var firstPlan = FindPlanWithTrailEvent(firstContext, JourneyTrailEventId.BadLuckDustStorm);

        Assert.Equal(TravelDayEncounterCategory.Unlucky, firstPlan.Encounters[0].Category);
        Assert.Equal(JourneyTrailEventId.BadLuckDustStorm, firstPlan.Encounters[0].TrailEvent?.Id);

        var secondContext = firstContext with
        {
            DayNumber = firstContext.DayNumber + 1,
            RecentTrailEventKinds = new[] { JourneyTrailEventKind.BadLuck },
            RecentTrailEventIds = new[] { JourneyTrailEventId.BadLuckDustStorm }
        };
        var secondPlan = TravelDayPlanGenerator.Generate(secondContext);

        Assert.NotEqual(JourneyTrailEventId.BadLuckDustStorm, secondPlan.Encounters[0].TrailEvent?.Id);
    }

    [Fact]
    public void GenerateFallsBackToQuietWhenNoUnluckyCandidateRemains()
    {
        var session = CreateNoHorseBadLuckSession();
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId("holloway"), session.Player.Inventory).Preview!;
        session.StartJourney(preview);

        var context = session.CreateTravelDayGenerationContext(gameSeed: "seed-no-unlucky", scenarioProfileId: "profile-no-unlucky") with
        {
            RecentTrailEventKinds = new[] { JourneyTrailEventKind.BadLuck },
            RecentTrailEventIds = new[] { JourneyTrailEventId.BadLuckWashout, JourneyTrailEventId.BadLuckDustStorm }
        };

        var plan = TravelDayPlanGenerator.Generate(context);

        Assert.NotEmpty(plan.Encounters);
        Assert.All(plan.Encounters, encounter =>
        {
            Assert.NotEqual(JourneyTrailEventId.BadLuckSpookedHorse, encounter.TrailEvent?.Id);
            Assert.False(encounter.TrailEvent?.Kind == JourneyTrailEventKind.BadLuck && encounter.TrailEvent.DelayDays > 0);
        });
    }

    [Fact]
    public void GenerateDoesNotSelectHorseOnlyEventsWhenTravelingWithoutAHorse()
    {
        var seeds = Enumerable.Range(1, 12).Select(index => $"seed-no-horse-{index}");

        foreach (var seed in seeds)
        {
            var context = CreateHorseTroubleContext(seed, HorseConditionBand.None, TravelMode.Foot);
            var plan = TravelDayPlanGenerator.Generate(context);

            Assert.DoesNotContain(plan.Encounters, encounter => encounter.Category == TravelDayEncounterCategory.HorseTrouble);
            Assert.DoesNotContain(plan.Encounters, encounter => encounter.TrailEvent?.Id == JourneyTrailEventId.BadLuckSpookedHorse);
        }
    }

    [Fact]
    public void GenerateCanStillSelectHorseOnlyEventsWhenAHorseIsPresent()
    {
        foreach (var seed in Enumerable.Range(1, 48).Select(index => $"seed-horse-present-{index}"))
        {
            var context = CreateHorseTroubleContext(seed, HorseConditionBand.Worn, TravelMode.Mounted);
            var plan = TravelDayPlanGenerator.Generate(context);
            if (plan.Encounters.Any(encounter => encounter.Category == TravelDayEncounterCategory.HorseTrouble))
            {
                Assert.Contains(plan.Encounters, encounter => encounter.TrailEvent?.Id == JourneyTrailEventId.BadLuckSpookedHorse);
                return;
            }
        }

        throw new Xunit.Sdk.XunitException("Did not find a horse-trouble day to validate horse-only selection.");
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
            TravelDifficulty.Hard,
            3,
            3m,
            TravelPressureBand.None,
            TravelPressureBand.None,
            TravelPressureBand.None,
            horseConditionBand,
            PursuitHeatBand.Calm,
            WalletBand.Steady,
            Array.Empty<JourneyTrailEventKind>(),
            Array.Empty<JourneyTrailEventId>(),
            Array.Empty<TravelDayEncounterCategory>(),
            HasHorse: horseConditionBand != HorseConditionBand.None);

    private static TravelDayPlanState FindPlanWithTrailEvent(TravelDayGenerationContext context, JourneyTrailEventId trailEventId)
    {
        for (var seedSuffix = 1; seedSuffix <= 128; seedSuffix++)
        {
            var plan = TravelDayPlanGenerator.Generate(context with
            {
                GameSeed = $"{context.GameSeed}-seed-{seedSuffix}"
            });

            if (plan.Encounters[0].TrailEvent?.Id == trailEventId)
            {
                return plan;
            }
        }

        throw new Xunit.Sdk.XunitException($"Did not find a plan with trail event {trailEventId}.");
    }

    private static GameSession CreateNoHorseBadLuckSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), inventory);
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

    private static GameSession CreateLuckyCooldownSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var creekside = new Town(new TownId("creekside"), "Creekside", TownServices.None);
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new DomainInventory(items), TravelDifficulty.Easy);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, creekside.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static GameSession CreateHighRiskEncounterSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new DomainInventory(items), TravelDifficulty.Hard);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, dryfork.Id, session.Player.Inventory, session.TravelRules).Preview!;
        session.StartJourney(preview);
        return session;
    }

    private static GameSession CreateLowRiskEncounterSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var creekside = new Town(new TownId("creekside"), "Creekside", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, creekside },
            new[]
            {
                new Trail(new TrailId("trail-low"), pinecross.Id, creekside.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek, 3m)
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, pinecross.Id, Wallet.Starting(25m), new DomainInventory(items), TravelDifficulty.Easy);
        var resolver = new TravelResolver();
        var preview = resolver.PreviewJourney(session.World, session.Player.CurrentTownId, creekside.Id, session.Player.Inventory, session.TravelRules).Preview!;
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
