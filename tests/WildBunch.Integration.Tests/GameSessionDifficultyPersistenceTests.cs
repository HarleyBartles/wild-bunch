using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using System.Text.Json.Nodes;

namespace WildBunch.Integration.Tests;

public sealed class GameSessionDifficultyPersistenceTests
{
    [Fact]
    public async Task TravelDifficultyRoundTripsThroughJsonPersistence()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var repository = new EfGameSessionRepository(fixture.CreateContext(), new GameSessionJsonSerializer());
        var session = CreateEasySession();

        await repository.SaveAsync(session);
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TravelDifficulty.Easy, reloaded!.TravelDifficulty);
        Assert.Equal(10, reloaded.Player.Inventory.GetCanteenState()!.Capacity);
        Assert.Equal(10, reloaded.Player.Inventory.GetCanteenState()!.Charges);
    }

    [Fact]
    public void CompletedJourneyHistoryRoundTripsThroughFullSessionJsonSnapshot()
    {
        var serializer = new GameSessionJsonSerializer();
        var session = CreateJourneyHistorySession();
        var preview = CreateJourneyPreview(session.Player.CurrentTownId, new TownId("openpass"), "Pinecross", "Open Pass");

        session.StartJourney(preview);
        session.Journey!.MarkCompleted();
        session.AcknowledgeJourneyArrival();

        var json = serializer.Serialize(session);
        var reloaded = serializer.Deserialize(json);

        Assert.Null(reloaded.Journey);
        Assert.Single(reloaded.CompletedJourneyHistory);
        Assert.Equal(1, reloaded.CompletedJourneyHistory[0].JourneySequence);
        Assert.Equal(JourneyStatus.Completed, reloaded.CompletedJourneyHistory[0].Status);
    }

    [Fact]
    public void CaseFileWarrantGangAffiliationFieldsRoundTripThroughJsonPersistence()
    {
        var serializer = new GameSessionJsonSerializer();
        var caseFile = CreateGangAwareCaseFile();

        var json = serializer.SerializeCaseFile(caseFile);
        var reloaded = serializer.DeserializeCaseFile(json);

        Assert.Contains("\"gangAffiliations\"", json, StringComparison.Ordinal);
        Assert.Contains("\"advancesGangPressureFor\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"isGangRelevant\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"advancesGangPressure\"", json, StringComparison.Ordinal);
        Assert.Equal(new[] { OutlawGangIds.WildBunch }, reloaded.PublicWarrants[0].Terms.GangAffiliations);
        Assert.Equal(OutlawGangIds.WildBunch, reloaded.PublicWarrants[0].Terms.AdvancesGangPressureFor);
        Assert.Empty(reloaded.PublicWarrants[1].Terms.GangAffiliations);
        Assert.Null(reloaded.PublicWarrants[1].Terms.AdvancesGangPressureFor);
    }

    [Fact]
    public void LegacyCaseFileWarrantGangBooleansStillDeserializeIntoTypedGangFields()
    {
        var serializer = new GameSessionJsonSerializer();
        var legacySnapshot = JsonNode.Parse(serializer.SerializeCaseFile(CreateGangAwareCaseFile()))!.AsObject();
        var legacyWarrants = legacySnapshot["publicWarrants"]!.AsArray();

        var firstTerms = legacyWarrants[0]!["terms"]!.AsObject();
        firstTerms.Remove("gangAffiliations");
        firstTerms.Remove("advancesGangPressureFor");
        firstTerms["isGangRelevant"] = true;
        firstTerms["advancesGangPressure"] = true;

        var secondTerms = legacyWarrants[1]!["terms"]!.AsObject();
        secondTerms.Remove("gangAffiliations");
        secondTerms.Remove("advancesGangPressureFor");
        secondTerms["isGangRelevant"] = false;
        secondTerms["advancesGangPressure"] = false;

        var reloaded = serializer.DeserializeCaseFile(legacySnapshot.ToJsonString());

        Assert.Equal(new[] { OutlawGangIds.WildBunch }, reloaded.PublicWarrants[0].Terms.GangAffiliations);
        Assert.Equal(OutlawGangIds.WildBunch, reloaded.PublicWarrants[0].Terms.AdvancesGangPressureFor);
        Assert.Empty(reloaded.PublicWarrants[1].Terms.GangAffiliations);
        Assert.Null(reloaded.PublicWarrants[1].Terms.AdvancesGangPressureFor);
    }

    [Fact]
    public void MissingTravelRandomnessInLegacySessionJsonFallsBackToRuntimeSalted()
    {
        var serializer = new GameSessionJsonSerializer();
        var legacySnapshot = JsonNode.Parse(serializer.Serialize(CreateEasySession()))!.AsObject();
        legacySnapshot.Remove("travelRandomness");

        var reloaded = serializer.Deserialize(legacySnapshot.ToJsonString());

        Assert.Equal(TravelRandomnessMode.RuntimeSalted, reloaded.TravelRandomness.Mode);
        Assert.False(string.IsNullOrWhiteSpace(reloaded.TravelRandomness.Salt));
    }

    private static GameSession CreateEasySession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);

        var world = new World(
            new[] { pinecross, holloway },
            new[]
            {
                new Trail(new TrailId("trail-easy"), pinecross.Id, holloway.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 5m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());

        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, new HorseTravelState(3, 2, 3)),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            pinecross.Id,
            Wallet.Starting(25m),
            inventory,
            TravelDifficulty.Easy);
    }

    private static CaseFile CreateGangAwareCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Tessa Wren", new SuspectTraits(true, true, true), SuspectStatus.AtLarge)
        };

        var publicWarrants = new[]
        {
            new Warrant(
                new WarrantId("warrant-gang"),
                "Tessa Wren",
                new WarrantTerms(
                    WarrantDisposition.DeadOrAlive,
                    2500m,
                    new[] { "Red Wren", "Aunt Tess" },
                    new[] { "Pale scar across the left cheek", "Raven-feather pin" },
                    "Dodge City Marshal",
                    InvestigationTargetKind.TrueCulprit,
                    [OutlawGangIds.WildBunch],
                    OutlawGangIds.WildBunch),
                "Wanted for a Wild Bunch robbery and related killings."),
            new Warrant(
                new WarrantId("warrant-unrelated"),
                "Reno Pike",
                new WarrantTerms(
                    WarrantDisposition.AliveOnly,
                    300m,
                    new[] { "The Magpie", "R. Pike" },
                    new[] { "Mismatched spurs", "Black felt hat" },
                    "Silver Creek Sheriff",
                    InvestigationTargetKind.UnrelatedWantedCriminal,
                    Array.Empty<OutlawGangId>(),
                    null),
                "Wanted for cattle theft.")
        };

        return new CaseFile(
            null,
            suspects,
            new SuspectId("suspect-1"),
            CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            Array.Empty<Clue>(),
            publicWarrants: publicWarrants);
    }

    private static TravelPreview CreateJourneyPreview(TownId originTownId, TownId destinationTownId, string originTownName, string destinationTownName)
        => new(
            originTownId,
            destinationTownId,
            originTownName,
            destinationTownName,
            new TravelRouteProfile("trail-preview", TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 1m, 1m, 1m, Array.Empty<string>()),
            TravelMode.Mounted,
            MountedTravelAvailable: true,
            WaterSecure: true,
            RideDayDistance: 1m,
            RemainingRideDayDistance: 1m,
            BaselineRideDays: 1,
            ExpectedDays: 1,
            RemainingDays: 1,
            CanteenChargesPerDay: 0,
            RequiredCanteenCharges: 0,
            AvailableCanteenCharges: 0,
            CanteenReserveCharges: 0,
            DelayMarginDays: 0,
            DelayRisk: false,
            RequiredFood: 1,
            AvailableFood: 6,
            RequiredHorseFeed: 0,
            AvailableHorseFeed: 0,
            HorseState: HorseTravelState.Healthy,
            Warnings: Array.Empty<string>());

    private static GameSession CreateJourneyHistorySession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var openpass = new Town(new TownId("openpass"), "Open Pass", TownServices.None);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);

        var world = new World(
            new[] { pinecross, openpass, dryfork },
            new[]
            {
                new Trail(new TrailId("trail-pine-open"), pinecross.Id, openpass.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m),
                new Trail(new TrailId("trail-open-dry"), openpass.Id, dryfork.Id, TrailRisk.Low, TrailTerrain.OpenRange, WaterFeature.None, 3m)
            });

        var caseFile = new CaseFile(null, Array.Empty<Suspect>(), new SuspectId("suspect-1"), Array.Empty<Clue>());
        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 6),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(6)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1)
        });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            pinecross.Id,
            Wallet.Starting(25m),
            inventory,
            TravelDifficulty.Easy);
    }
}
