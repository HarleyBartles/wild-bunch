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
    public async Task TravelDifficultyAndEntropyRoundTripThroughJsonPersistence()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        await using var context = fixture.CreateContext();
        var repository = new EfGameSessionRepository(context, new GameSessionJsonSerializer());
        var unitOfWork = new EfGameSessionUnitOfWork(context);
        var session = CreateSession(TravelDifficulty.Easy, AdventureRandomnessPolicy.Wild);

        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(TravelDifficulty.Easy, reloaded!.TravelDifficulty);
        Assert.Equal(AdventureRandomnessPolicy.Wild, reloaded.Entropy);
        Assert.Equal(10, reloaded.Player.Inventory.GetCanteenState()!.Capacity);
        Assert.Equal(10, reloaded.Player.Inventory.GetCanteenState()!.Charges);
    }

    [Fact]
    public void MissingEntropyInLegacySessionJsonDefaultsToStandard()
    {
        var serializer = new GameSessionJsonSerializer();
        var legacySnapshot = JsonNode.Parse(serializer.Serialize(CreateSession(TravelDifficulty.Normal, AdventureRandomnessPolicy.Boring)))!.AsObject();
        legacySnapshot.Remove("entropy");

        var reloaded = serializer.Deserialize(legacySnapshot.ToJsonString());

        Assert.Equal(AdventureRandomnessPolicy.Standard, reloaded.Entropy);
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
    public void WantedSuspectPresenceLedgerRoundTripsThroughFullSessionJsonSnapshot()
    {
        var serializer = new GameSessionJsonSerializer();
        var session = CreateSession(TravelDifficulty.Normal, AdventureRandomnessPolicy.Boring);
        var suspectId = new SuspectId("suspect-1");

        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.SecuredAlive);

        var json = serializer.Serialize(session);
        var reloaded = serializer.Deserialize(json);

        Assert.Equal(WantedSuspectPresenceState.SecuredAlive, reloaded.GetWantedSuspectPresenceState(suspectId));
        Assert.Single(reloaded.WantedSuspectPresenceEntries);
        Assert.Equal(suspectId, reloaded.WantedSuspectPresenceEntries[0].SuspectId);
        Assert.Equal(WantedSuspectPresenceState.SecuredAlive, reloaded.WantedSuspectPresenceEntries[0].State);
    }

    [Fact]
    public void LegacyFullSessionJsonWithoutWantedSuspectPresenceLedgerDefaultsToEmptyLedger()
    {
        var serializer = new GameSessionJsonSerializer();
        var session = CreateSession(TravelDifficulty.Normal, AdventureRandomnessPolicy.Boring);
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.GoneToGround);

        var legacySnapshot = JsonNode.Parse(serializer.Serialize(session))!.AsObject();
        legacySnapshot.Remove("wantedSuspectPresenceLedger");

        var reloaded = serializer.Deserialize(legacySnapshot.ToJsonString());

        Assert.Empty(reloaded.WantedSuspectPresenceEntries);
        Assert.Equal(WantedSuspectPresenceState.Unavailable, reloaded.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
    }

    [Fact]
    public void CaseFileWarrantGangAffiliationFieldsRoundTripThroughJsonPersistence()
    {
        var serializer = new GameSessionJsonSerializer();
        var caseFile = CreateGangAwareCaseFile();

        var json = serializer.SerializeCaseFile(caseFile);
        var reloaded = serializer.DeserializeCaseFile(json);

        Assert.Contains("\"tags\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"isLocal\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"isArmed\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"isDesperate\"", json, StringComparison.Ordinal);
        Assert.Contains("\"gangAffiliations\"", json, StringComparison.Ordinal);
        Assert.Contains("\"advancesGangPressureFor\"", json, StringComparison.Ordinal);
        Assert.Contains("\"sourceKind\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"isGangRelevant\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"advancesGangPressure\"", json, StringComparison.Ordinal);
        Assert.True(reloaded.Suspects[0].Traits.HasTag(SuspectTraitTags.Local));
        Assert.True(reloaded.Suspects[0].Traits.HasTag(SuspectTraitTags.Armed));
        Assert.True(reloaded.Suspects[0].Traits.HasTag(SuspectTraitTags.Desperate));
        Assert.Equal(new[] { OutlawGangIds.WildBunch }, reloaded.PublicWarrants[0].Terms.GangAffiliations);
        Assert.Equal(OutlawGangIds.WildBunch, reloaded.PublicWarrants[0].Terms.AdvancesGangPressureFor);
        Assert.Equal(InvestigationSourceKind.SheriffWarrants, reloaded.PublicWarrants[0].Terms.SourceKind);
        Assert.Empty(reloaded.PublicWarrants[1].Terms.GangAffiliations);
        Assert.Null(reloaded.PublicWarrants[1].Terms.AdvancesGangPressureFor);
        Assert.Equal(InvestigationSourceKind.LocalRecords, reloaded.PublicWarrants[1].Terms.SourceKind);
    }

    [Fact]
    public void CaseFileWantedSuspectConfrontationStateRoundTripsThroughJsonPersistence()
    {
        var serializer = new GameSessionJsonSerializer();
        var caseFile = CreateConfrontationStateCaseFile();

        var json = serializer.SerializeCaseFile(caseFile);
        var reloaded = serializer.DeserializeCaseFile(json);

        Assert.Contains("\"wantedSuspectConfrontations\"", json, StringComparison.Ordinal);
        Assert.Single(reloaded.WantedSuspectConfrontations);
        Assert.Equal(new SuspectId("suspect-1"), reloaded.WantedSuspectConfrontations[0].SuspectId);
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, reloaded.WantedSuspectConfrontations[0].Outcome);
        Assert.False(reloaded.WantedSuspectConfrontations[0].IsSecured);
        Assert.Equal(6, reloaded.WantedSuspectConfrontations[0].Day);
        Assert.Equal(2, reloaded.WantedSuspectConfrontations[0].Turn);
    }

    [Fact]
    public void CaseFileSheriffTurnInSettlementStateRoundTripsThroughJsonPersistence()
    {
        var serializer = new GameSessionJsonSerializer();
        var caseFile = CreateSettlementStateCaseFile();

        var json = serializer.SerializeCaseFile(caseFile);
        var reloaded = serializer.DeserializeCaseFile(json);

        Assert.Contains("\"sheriffTurnInSettlements\"", json, StringComparison.Ordinal);
        Assert.Single(reloaded.SheriffTurnInSettlements);
        Assert.Equal(new SuspectId("suspect-1"), reloaded.SheriffTurnInSettlements[0].SuspectId);
        Assert.Equal("Tessa Wren", reloaded.SheriffTurnInSettlements[0].TargetName);
        Assert.False(reloaded.SheriffTurnInSettlements[0].IsAlive);
        Assert.Equal(2500m, reloaded.SheriffTurnInSettlements[0].BountyAmount);
        Assert.Equal(7, reloaded.SheriffTurnInSettlements[0].Day);
        Assert.Equal(4, reloaded.SheriffTurnInSettlements[0].Turn);
    }

    [Fact]
    public void CaseFileClueAnchorsRoundTripThroughJsonPersistence()
    {
        var serializer = new GameSessionJsonSerializer();
        var caseFile = CreateAnchoredCaseFile();

        var json = serializer.SerializeCaseFile(caseFile);
        var reloaded = serializer.DeserializeCaseFile(json);

        Assert.Contains("\"anchors\"", json, StringComparison.Ordinal);
        Assert.True(reloaded.KnownClues[0].Anchors.HasAnchors);
        Assert.Equal("Grey Jay", reloaded.KnownClues[0].Anchors.Subjects[0].Label);
        Assert.Equal("Red Mesa", reloaded.KnownClues[0].Anchors.Locations[0].Label);
        Assert.Equal("rail spur", reloaded.KnownClues[0].Anchors.Locations[0].Route);
        Assert.Equal(ClueRecency.Recent, reloaded.KnownClues[0].Anchors.Times[0].Recency);
        Assert.Contains("heading", reloaded.KnownClues[0].Anchors.Directions[0].Movement, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacyCaseFileClueSnapshotsWithoutAnchorsBackfillFromLinkedSuspects()
    {
        var serializer = new GameSessionJsonSerializer();
        var legacySnapshot = JsonNode.Parse(serializer.SerializeCaseFile(CreateAnchoredCaseFile()))!.AsObject();
        var knownClue = legacySnapshot["knownClues"]!.AsArray()[0]!.AsObject();
        knownClue.Remove("anchors");

        var reloaded = serializer.DeserializeCaseFile(legacySnapshot.ToJsonString());

        Assert.True(reloaded.KnownClues[0].Anchors.HasAnchors);
        Assert.Single(reloaded.KnownClues[0].Anchors.Subjects);
        Assert.Equal("suspect-1", reloaded.KnownClues[0].Anchors.Subjects[0].Label);
    }

    [Fact]
    public void LegacyCaseFileSuspectTraitBooleansStillDeserializeIntoTraitTags()
    {
        var serializer = new GameSessionJsonSerializer();
        var legacySnapshot = JsonNode.Parse(serializer.SerializeCaseFile(CreateGangAwareCaseFile()))!.AsObject();
        var suspect = legacySnapshot["suspects"]!.AsArray()[0]!.AsObject();
        var traits = suspect["traits"]!.AsObject();

        traits.Remove("tags");
        traits["isLocal"] = true;
        traits["isArmed"] = false;
        traits["isDesperate"] = true;

        var reloaded = serializer.DeserializeCaseFile(legacySnapshot.ToJsonString());

        Assert.True(reloaded.Suspects[0].Traits.IsLocal);
        Assert.False(reloaded.Suspects[0].Traits.IsArmed);
        Assert.True(reloaded.Suspects[0].Traits.IsDesperate);
        Assert.Contains(reloaded.Suspects[0].Traits.Tags, tag => tag.Value == SuspectTraitTags.Local.Value);
        Assert.Contains(reloaded.Suspects[0].Traits.Tags, tag => tag.Value == SuspectTraitTags.Desperate.Value);
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
    public void LegacyCaseFileWithoutWantedSuspectConfrontationsStillDeserializes()
    {
        var serializer = new GameSessionJsonSerializer();
        var legacySnapshot = JsonNode.Parse(serializer.SerializeCaseFile(CreateConfrontationStateCaseFile()))!.AsObject();
        legacySnapshot.Remove("wantedSuspectConfrontations");

        var reloaded = serializer.DeserializeCaseFile(legacySnapshot.ToJsonString());

        Assert.Empty(reloaded.WantedSuspectConfrontations);
        Assert.Equal(new SuspectId("suspect-1"), reloaded.TrueCulpritId);
    }

    [Fact]
    public void MissingTravelRandomnessInLegacySessionJsonFallsBackToRuntimeSalted()
    {
        var serializer = new GameSessionJsonSerializer();
        var legacySnapshot = JsonNode.Parse(serializer.Serialize(CreateSession(TravelDifficulty.Easy, AdventureRandomnessPolicy.Boring)))!.AsObject();
        legacySnapshot.Remove("travelRandomness");

        var reloaded = serializer.Deserialize(legacySnapshot.ToJsonString());

        Assert.Equal(TravelRandomnessMode.RuntimeSalted, reloaded.TravelRandomness.Mode);
        Assert.False(string.IsNullOrWhiteSpace(reloaded.TravelRandomness.Salt));
    }

    [Fact]
    public async Task TownVisitStateWithMultipleTownVisitsRoundTripsThroughRepositoryPersistence()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        await using var context = fixture.CreateContext();
        var repository = new EfGameSessionRepository(context, new GameSessionJsonSerializer());
        var unitOfWork = new EfGameSessionUnitOfWork(context);
        var session = CreateTownVisitSession();

        var firstResult = session.FollowTelegraphLeads();
        Assert.True(firstResult.Success);

        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));

        var afterReturnResult = session.FollowTelegraphLeads();
        Assert.True(afterReturnResult.Success);

        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();

        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(session.Player.CurrentTownId, reloaded!.Player.CurrentTownId);
        Assert.True(reloaded.CurrentTownVisit.TryGetTownState(new TownId("current"), out var currentTownState));
        Assert.True(reloaded.CurrentTownVisit.TryGetTownState(new TownId("connected"), out var connectedTownState));
        Assert.Equal(2, currentTownState!.VisitNumber);
        Assert.Equal(new TownId("connected"), connectedTownState!.TownId);
        Assert.True(connectedTownState.VisitNumber >= 1);
        Assert.True(currentTownState.IsSpent(InvestigationSourceKind.TelegraphLead));
        Assert.Empty(connectedTownState.SpentInvestigationSources);
        Assert.Single(reloaded.CaseFile.KnownClues);
        Assert.Empty(reloaded.CaseFile.PublicClues);
    }

    [Fact]
    public void LegacyTownVisitSnapshotWithoutTownStatesStillDeserializes()
    {
        var serializer = new GameSessionJsonSerializer();
        var session = CreateTownVisitSession();

        session.FollowTelegraphLeads();

        var legacySnapshot = JsonNode.Parse(serializer.Serialize(session))!.AsObject();
        var currentTownVisit = legacySnapshot["currentTownVisit"]!.AsObject();
        currentTownVisit.Remove("townStates");

        var reloaded = serializer.Deserialize(legacySnapshot.ToJsonString());

        Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
        Assert.Equal(session.CurrentTownVisit.TownId, reloaded.CurrentTownVisit.TownId);
        Assert.True(reloaded.CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead));
        Assert.Single(reloaded.CaseFile.KnownClues);
        Assert.Empty(reloaded.CaseFile.PublicClues);
    }

    [Fact]
    public void TownVisitInvestigationStateRoundTripsThroughFullSessionJsonSnapshot()
    {
        var serializer = new GameSessionJsonSerializer();
        var session = CreateTownVisitSession();

        var result = session.FollowTelegraphLeads();
        Assert.True(result.Success);
        Assert.True(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead));

        var json = serializer.Serialize(session);
        var reloaded = serializer.Deserialize(json);

        Assert.Equal(session.Player.CurrentTownId, reloaded.Player.CurrentTownId);
        Assert.Equal(session.CurrentTownVisit.TownId, reloaded.CurrentTownVisit.TownId);
        Assert.True(reloaded.CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead));
        Assert.Single(reloaded.CaseFile.KnownClues);
        Assert.Empty(reloaded.CaseFile.PublicClues);
    }

    [Fact]
    public async Task TownVisitInvestigationStateRoundTripsThroughRepositoryPersistence()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        await using var context = fixture.CreateContext();
        var repository = new EfGameSessionRepository(context, new GameSessionJsonSerializer());
        var unitOfWork = new EfGameSessionUnitOfWork(context);
        var session = CreateTownVisitSession();

        var result = session.FollowTelegraphLeads();
        Assert.True(result.Success);
        Assert.True(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead));

        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();
        var reloaded = await repository.GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(session.Player.CurrentTownId, reloaded!.Player.CurrentTownId);
        Assert.Equal(session.CurrentTownVisit.TownId, reloaded.CurrentTownVisit.TownId);
        Assert.True(reloaded.CurrentTownVisit.IsSpent(InvestigationSourceKind.TelegraphLead));
        Assert.Single(reloaded.CaseFile.KnownClues);
        Assert.Empty(reloaded.CaseFile.PublicClues);
    }

    private static GameSession CreateSession(TravelDifficulty travelDifficulty, AdventureRandomnessPolicy entropy)
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
            travelDifficulty,
            entropy: entropy);
    }

    private static GameSession CreateTownVisitSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.Telegraph | TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);

        var world = new World(
            new[] { currentTown, connectedTown },
            new[]
            {
                new Trail(new TrailId("trail-current-connected"), currentTown.Id, connectedTown.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-public-telegraph"),
                    ClueKind.IdentityFact,
                    "A telegraph clerk filed a name in shorthand.",
                    new[] { new SuspectId("suspect-1") },
                    InvestigationTargetKind.Suspected,
                    InvestigationSourceKind.TelegraphLead,
                    source: "telegraph clerk",
                    context: "Telegraph lead")
            });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            currentTown.Id,
            Wallet.Starting(25m),
            new Inventory(new[]
            {
                new InventoryItem(ItemKind.Food, 4),
                new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
                new InventoryItem(ItemKind.Horse, 1, new HorseTravelState(3, 2, 3)),
                new InventoryItem(ItemKind.Saddle, 1)
            }),
            TravelDifficulty.Easy);
    }

    private static CaseFile CreateGangAwareCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Tessa Wren", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Armed, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
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
                    OutlawGangIds.WildBunch,
                    InvestigationSourceKind.SheriffWarrants),
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
                    null,
                    InvestigationSourceKind.LocalRecords),
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

    private static CaseFile CreateConfrontationStateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Tessa Wren", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            null,
            suspects,
            new SuspectId("suspect-1"),
            CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            Array.Empty<Clue>());

        caseFile.RecordWantedSuspectConfrontationState(new WantedSuspectConfrontationState(
            new SuspectId("suspect-1"),
            "Tessa Wren",
            WarrantDisposition.DeadOrAlive,
            WantedSuspectConfrontationOutcome.Fled,
            IsAlive: true,
            IsSecured: false,
            Day: 6,
            Turn: 2));

        return caseFile;
    }

    private static CaseFile CreateSettlementStateCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Tessa Wren", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            null,
            suspects,
            new SuspectId("suspect-1"),
            CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            Array.Empty<Clue>());

        caseFile.RecordSheriffTurnInSettlementState(new SheriffTurnInSettlementState(
            new SuspectId("suspect-1"),
            "Tessa Wren",
            WarrantDisposition.DeadOrAlive,
            IsAlive: false,
            BountyAmount: 2500m,
            Day: 7,
            Turn: 4));

        return caseFile;
    }

    private static CaseFile CreateAnchoredCaseFile()
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Jonah Pike", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };

        var clues = new[]
        {
            new Clue(
                new ClueId("clue-1"),
                ClueKind.Whereabouts,
                "Local gossip out of Red Mesa says the rider kept to the rail spur after dark.",
                new[] { new SuspectId("suspect-1") },
                InvestigationTargetKind.Suspected,
                InvestigationSourceKind.LocalGossip,
                source: "saloon talk",
                context: "Town gossip",
                anchors: new ClueAnchors(
                    subjects: new[]
                    {
                        new ClueSubjectAnchor("Grey Jay", Alias: "Grey Jay", Feature: "red neckerchief")
                    },
                    locations: new[]
                    {
                        new ClueLocationAnchor("Red Mesa", TownId: new TownId("redmesa"), Place: "Red Mesa", Route: "rail spur")
                    },
                    times: new[]
                    {
                        new ClueTimeAnchor(ClueRecency.Recent)
                    },
                    directions: new[]
                    {
                        new ClueDirectionAnchor("heading north", Movement: "heading north", Route: "rail spur", DestinationTownId: new TownId("redmesa"))
                    }))
        };

        return new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: clues);
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
