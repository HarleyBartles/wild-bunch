using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;
using HorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using CanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;
using TrailRisk = WildBunch.Domain.World.TrailRisk;
using TrailTerrain = WildBunch.Domain.World.TrailTerrain;
using WaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Diagnostic probe that sweeps salts for each test factory configuration
/// and records what the day-plan generator produces at Calm heat band (heat=0).
/// Results are written to salt-probe-results.txt in the test output directory.
/// This test is temporary and should be deleted after salts are captured.
/// </summary>
public sealed class DiagnosticSaltProbe
{
    [Fact]
    public void Probe_All_Factories_All_Salts()
    {
        var salts = new[]
        {
            "foe-1", "foe-2", "foe-3", "foe-4", "foe-5",
            "bunch85-1", "bunch85-2", "bunch85-3", "bunch85-4", "bunch85-5",
            "bunch85-6", "bunch85-7", "bunch85-8", "bunch85-9", "bunch85-10",
            "bunch85-11", "bunch85-12", "bunch85-13", "bunch85-14", "bunch85-15",
            "bunch85-16", "bunch85-17", "bunch85-18", "bunch85-19", "bunch85-20",
            "bunch85-21", "bunch85-22", "bunch85-23", "bunch85-24", "bunch85-25",
            "bunch85-26", "bunch85-27", "bunch85-28", "bunch85-29", "bunch85-30",
            "bunch85-31", "bunch85-32", "bunch85-33", "bunch85-34", "bunch85-35",
            "bunch85-36", "bunch85-37", "bunch85-38", "bunch85-39", "bunch85-40",
            "bunch85-41", "bunch85-42", "bunch85-43", "bunch85-44", "bunch85-45",
            "bunch85-46", "bunch85-47", "bunch85-48", "bunch85-49", "bunch85-50",
        };

        var outputPath = Path.Combine(AppContext.BaseDirectory, "salt-probe-results.txt");
        using var writer = new StreamWriter(outputPath, append: false);

        // High-risk mounted (with horse) — need foe encounters
        ProbeFactory(writer, "HighRiskMounted", salts, salt => CreateHighRiskSession(salt, withHorse: true),
            "dryfork", withRules: false);

        // High-risk foot (no horse) — need foe encounters
        ProbeFactory(writer, "HighRiskFoot", salts, salt => CreateHighRiskSession(salt, withHorse: false),
            "dryfork", withRules: false);

        // Bad-luck session (Moderate/Hills/Spring, mounted) — need BadLuckWashout trail event
        ProbeFactory(writer, "BadLuckMounted", salts, salt => CreateBadLuckSession(salt),
            "holloway", withRules: false);

        // No-horse bad-luck session (Moderate/Hills/Spring, foot) — need BadLuckWashout
        ProbeFactory(writer, "BadLuckFoot", salts, salt => CreateNoHorseBadLuckSession(salt),
            "holloway", withRules: false);

        // Easy lucky food (Easy/Low/OpenRange/None, mounted) — need LuckyFoodCache
        ProbeFactory(writer, "EasyLuckyFood", salts, salt => CreateEasyLuckyFoodSession(salt),
            "openpass", withRules: true);

        // Easy lucky water (Easy/Low/Badlands/None, mounted) — need LuckyWaterSeep
        ProbeFactory(writer, "EasyLuckyWater", salts, salt => CreateEasyLuckyWaterSession(salt),
            "dryspring", withRules: true);

        // Hard bad-luck (Hard/Low/Badlands/None, mounted) — need BadLuckSpookedHorse
        ProbeFactory(writer, "HardBadLuck", salts, salt => CreateHardBadLuckSession(salt),
            "hardpan", withRules: true);

        // Hard mounted horse (Hard/Low/Hills/River, mounted) — need BadLuckSpookedHorse
        ProbeFactory(writer, "HardMountedHorse", salts, salt => CreateHardMountedHorseSession(salt),
            "ridgeway", withRules: true);

        // Lucky foot (Low/OpenRange/Creek, foot) — need LuckyCoinCache
        ProbeFactory(writer, "LuckyFoot", salts, salt => CreateLuckyFootSession(salt),
            "silvercreek", withRules: false);

        // Dry mounted (Low/Badlands/None, mounted) — resource test, no encounter needed
        ProbeFactory(writer, "DryMounted", salts, salt => CreateDryMountedSession(salt),
            "dryfork", withRules: false);

        // Dry foot (Low/Badlands/None, foot) — resource test, no encounter needed
        ProbeFactory(writer, "DryFoot", salts, salt => CreateDryFootSession(salt),
            "dryfork", withRules: false);

        writer.Flush();
        writer.Close();

        Assert.True(true);
    }

    private static void ProbeFactory(StreamWriter writer, string label, string[] salts,
        Func<string, GameSession> factory, string destinationTown, bool withRules)
    {
        writer.WriteLine($"=== {label} ===");

        foreach (var salt in salts)
        {
            try
            {
                var session = factory(salt);
                var resolver = new TravelResolver();
                var preview = withRules
                    ? resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId(destinationTown), session.Player.Inventory, session.TravelRules).Preview!
                    : resolver.PreviewJourney(session.World, session.Player.CurrentTownId, new TownId(destinationTown), session.Player.Inventory).Preview!;
                session.StartJourney(preview);
                var result = session.AdvanceJourneyDay();

                var pendingKind = session.Journey?.PendingEncounter?.Kind ?? "none";
                var trailEvent = result.TrailEvent?.Id.ToString() ?? "none";
                var status = result.Status;
                var heat = session.PursuitState.Heat;
                var wallet = session.Player.Wallet.Cash;
                var food = session.Player.Inventory.GetQuantity(DomainItemKind.Food);
                var canteen = session.Player.Inventory.GetCanteenState()?.Charges ?? -1;
                var horse = session.Player.Inventory.GetHorseState()?.ToString() ?? "none";

                writer.WriteLine($"  salt='{salt}': Status={status}, Pending={pendingKind}, TrailEvent={trailEvent}, Heat={heat}, Wallet={wallet}, Food={food}, Canteen={canteen}, Horse={horse}");

                if (pendingKind == "foe")
                    writer.WriteLine($"    >>> FOE ENCOUNTER <<<");
                else if (pendingKind == "npc")
                    writer.WriteLine($"    >>> NPC ENCOUNTER <<<");
                else if (trailEvent != "none")
                    writer.WriteLine($"    >>> TRAIL EVENT: {trailEvent} <<<");
            }
            catch (Exception ex)
            {
                writer.WriteLine($"  salt='{salt}': THREW {ex.Message}");
            }
        }

        writer.WriteLine();
    }

    // === Factory methods ===

    private static CaseFile CreateCaseFile()
    {
        var suspects = new[] { new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge) };
        return new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
    }

    private static GameSession CreateHighRiskSession(string salt, bool withHorse = true)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(new[] { pinecross, dryfork }, new[] { new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.High, TrailTerrain.Badlands, WaterFeature.None) });
        var items = new List<DomainInventoryItem> { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1), new(DomainItemKind.Knife, 1), new(DomainItemKind.Revolver, 1), new(DomainItemKind.RevolverAmmo, 2) };
        if (withHorse) { items.Add(new DomainInventoryItem(DomainItemKind.Horse, 1, HorseTravelState.Healthy)); items.Add(new DomainInventoryItem(DomainItemKind.Saddle, 1)); }
        var inventory = new DomainInventory(items);
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateBadLuckSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
        var world = new DomainWorld(new[] { pinecross, holloway }, new[] { new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1), new(DomainItemKind.Horse, 1, HorseTravelState.Healthy), new(DomainItemKind.Saddle, 1), new(DomainItemKind.Knife, 1), new(DomainItemKind.Revolver, 1), new(DomainItemKind.RevolverAmmo, 2) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateNoHorseBadLuckSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
        var world = new DomainWorld(new[] { pinecross, holloway }, new[] { new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate, TrailTerrain.Hills, WaterFeature.Spring) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1), new(DomainItemKind.Knife, 1), new(DomainItemKind.Revolver, 1), new(DomainItemKind.RevolverAmmo, 2) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateEasyLuckyFoodSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var openpass = new Town(new TownId("openpass"), "Open Pass", TownServices.None);
        var world = new DomainWorld(new[] { pinecross, openpass }, new[] { new Trail(new TrailId("trail-pine-open"), pinecross.Id, openpass.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1), new(DomainItemKind.Horse, 1, new HorseTravelState(1, 1, 1)), new(DomainItemKind.Saddle, 1), new(DomainItemKind.Knife, 1) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Easy, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateEasyLuckyWaterSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryspring = new Town(new TownId("dryspring"), "Dry Spring", TownServices.None);
        var world = new DomainWorld(new[] { pinecross, dryspring }, new[] { new Trail(new TrailId("trail-pine-dryspring"), pinecross.Id, dryspring.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1, canteenState: new CanteenState(1, 2)), new(DomainItemKind.Horse, 1, HorseTravelState.Healthy), new(DomainItemKind.Saddle, 1), new(DomainItemKind.Knife, 1) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Easy, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateHardBadLuckSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var hardpan = new Town(new TownId("hardpan"), "Hardpan", TownServices.None);
        var world = new DomainWorld(new[] { pinecross, hardpan }, new[] { new Trail(new TrailId("trail-pine-hardpan"), pinecross.Id, hardpan.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 3m) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1, canteenState: new CanteenState(3, 4)), new(DomainItemKind.Horse, 1, HorseTravelState.Healthy), new(DomainItemKind.Saddle, 1), new(DomainItemKind.Knife, 1) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Hard, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateHardMountedHorseSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var ridgeway = new Town(new TownId("ridgeway"), "Ridgeway", TownServices.None);
        var world = new DomainWorld(new[] { pinecross, ridgeway }, new[] { new Trail(new TrailId("trail-pine-ridge"), pinecross.Id, ridgeway.Id, TrailRisk.Low, TrailTerrain.Hills, WaterFeature.River, 3m) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1), new(DomainItemKind.Horse, 1, HorseTravelState.Healthy), new(DomainItemKind.Saddle, 1), new(DomainItemKind.Knife, 1) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, TravelDifficulty.Hard, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateLuckyFootSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var world = new DomainWorld(new[] { pinecross, silvercreek }, new[] { new Trail(new TrailId("trail-pine-silver"), pinecross.Id, silvercreek.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.Creek) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateDryMountedSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(new[] { pinecross, dryfork }, new[] { new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None, 2m) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1), new(DomainItemKind.Horse, 1, HorseTravelState.Healthy), new(DomainItemKind.Saddle, 1), new(DomainItemKind.Knife, 1), new(DomainItemKind.HorseFeed, 1) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }

    private static GameSession CreateDryFootSession(string salt)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var world = new DomainWorld(new[] { pinecross, dryfork }, new[] { new Trail(new TrailId("trail-pine-dry"), pinecross.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.Badlands, WaterFeature.None) });
        var inventory = new DomainInventory(new DomainInventoryItem[] { new(DomainItemKind.Food, 3), new(DomainItemKind.Canteen, 1) });
        return GameSession.StartNew("Ranger Vale", world, CreateCaseFile(), pinecross.Id, Wallet.Starting(25m), inventory, travelRandomness: TravelRandomnessState.CreateDeterministic(salt));
    }
}
