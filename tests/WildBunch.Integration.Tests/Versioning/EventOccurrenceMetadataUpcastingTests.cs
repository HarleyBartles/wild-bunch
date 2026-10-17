using System.Text.Json;
using System.Text.Json.Nodes;
using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Integration.Tests.Versioning;

public sealed class EventOccurrenceMetadataUpcastingTests
{
    private const string WorldGeneratedV1 = """
        {"seedCode":"seed-42","saltSource":{"mode":1,"salt":"fixed-salt"},"gameEntropy":1,"world":{"towns":[{"id":"dustvale","name":"Dustvale","prosperity":1,"mapX":12,"mapY":34,"isOutlier":false}],"trails":[]},"occurredAt":"2026-10-09T12:00:00+00:00"}
        """;

    private const string WorldGeneratedV2 = """
        {"seedCode":"seed-42","saltSource":{"mode":1,"salt":"fixed-salt"},"gameEntropy":1,"world":{"towns":[{"id":"dustvale","name":"Dustvale","prosperity":1,"mapX":12,"mapY":34,"isOutlier":false}],"trails":[]},"caseFile":{"suspects":[],"trueCulpritId":"suspect-7","openingLead":{"description":"Look for the brass star."},"clues":[],"publicClues":[],"accusationId":null,"discoveredSuspectIds":[],"killerReleaseThreshold":1,"killerReleaseProgress":0,"knownWarrants":[],"publicWarrants":[],"suspectTurfAssignments":[],"wantedSuspectConfrontations":[],"sheriffTurnInSettlements":[]},"occurredAt":"2026-10-09T12:00:00+00:00"}
        """;

    private const string CaseFileGeneratedV1 = """
        {"caseFile":{"suspects":[],"trueCulpritId":"suspect-7","openingLead":{"description":"Look for the brass star."},"clues":[],"publicClues":[],"accusationId":null,"discoveredSuspectIds":[],"killerReleaseThreshold":1,"killerReleaseProgress":0,"knownWarrants":[],"publicWarrants":[],"suspectTurfAssignments":[],"wantedSuspectConfrontations":[],"sheriffTurnInSettlements":[]},"occurredAt":"2026-10-09T12:00:00+00:00"}
        """;

    private const string StartingTownSelectedV1 = """
        {"startingTownId":{"value":"dustvale"},"occurredAt":"2026-10-09T12:00:00+00:00"}
        """;

    [Theory]
    [InlineData(1, WorldGeneratedV1, false)]
    [InlineData(2, WorldGeneratedV2, true)]
    public void LoadWorldGenerated_HistoricalPayloadDropsOnlyOccurrenceMetadata(
        int storedVersion,
        string payloadJson,
        bool hasCaseFile)
    {
        var (registry, loader) = CreateLoader();

        var upgradedJson = JsonNode.Parse(registry.Upcast("WorldGenerated", storedVersion, payloadJson))!.AsObject();
        var expectedJson = JsonNode.Parse(payloadJson)!.AsObject().DeepClone().AsObject();
        expectedJson.Remove("occurredAt");
        if (storedVersion == 1)
        {
            expectedJson["caseFile"] = null;
        }

        Assert.True(JsonNode.DeepEquals(expectedJson, upgradedJson));

        var loaded = Assert.IsType<WorldGenerated>(loader.LoadEvent(Stored("WorldGenerated", storedVersion, payloadJson)));
        Assert.Equal("seed-42", loaded.SeedCode);
        Assert.Equal("fixed-salt", loaded.SaltSource.Salt);
        Assert.Equal("dustvale", loaded.World.Towns.Single().Id);
        Assert.Equal(hasCaseFile, loaded.CaseFile is not null);
        if (hasCaseFile)
        {
            Assert.Equal("suspect-7", loaded.CaseFile!.TrueCulpritId);
        }
    }

    [Fact]
    public void LoadCaseFileGenerated_HistoricalPayloadDropsOnlyOccurrenceMetadata()
    {
        var (registry, loader) = CreateLoader();

        var upgradedJson = JsonNode.Parse(registry.Upcast("CaseFileGenerated", 1, CaseFileGeneratedV1))!.AsObject();
        AssertHistoricalFactsPreserved(CaseFileGeneratedV1, upgradedJson);

        var loaded = Assert.IsType<CaseFileGenerated>(loader.LoadEvent(Stored("CaseFileGenerated", 1, CaseFileGeneratedV1)));
        Assert.Equal("suspect-7", loaded.CaseFile.TrueCulpritId);
        Assert.Equal("Look for the brass star.", loaded.CaseFile.OpeningLead.Description);
    }

    [Fact]
    public void LoadStartingTownSelected_HistoricalPayloadDropsOnlyOccurrenceMetadata()
    {
        var (registry, loader) = CreateLoader();

        var upgradedJson = JsonNode.Parse(registry.Upcast("StartingTownSelected", 1, StartingTownSelectedV1))!.AsObject();
        AssertHistoricalFactsPreserved(StartingTownSelectedV1, upgradedJson);

        var loaded = Assert.IsType<StartingTownSelected>(loader.LoadEvent(Stored("StartingTownSelected", 1, StartingTownSelectedV1)));
        Assert.Equal("dustvale", loaded.StartingTownId.Value);
    }

    [Theory]
    [InlineData("WorldGenerated", 2)]
    [InlineData("CaseFileGenerated", 1)]
    [InlineData("StartingTownSelected", 1)]
    public void UpcastMalformedHistoricalPayload_FailsClosed(string eventType, int storedVersion)
    {
        var (registry, _) = CreateLoader();

        Assert.Throws<JsonException>(() => registry.Upcast(eventType, storedVersion, "[]"));
    }

    private static (PayloadUpcasterRegistry Registry, PersistedPayloadLoader Loader) CreateLoader()
    {
        var registry = new PayloadUpcasterRegistry(DependencyInjection.CreateDefaultUpcasters());
        var serializer = new GameSessionJsonSerializer();
        var loader = new PersistedPayloadLoader(
            registry,
            serializer,
            new TravelDiaryDayProjector(),
            _ => throw new InvalidOperationException("Event loading must not rebuild a session."));
        return (registry, loader);
    }

    private static StoredEventEntity Stored(string eventType, int version, string payloadJson)
        => new()
        {
            EventType = eventType,
            SchemaVersion = version,
            PayloadJson = payloadJson
        };

    private static void AssertHistoricalFactsPreserved(string originalPayload, JsonObject upgradedPayload)
    {
        var expected = JsonNode.Parse(originalPayload)!.AsObject().DeepClone().AsObject();
        expected.Remove("occurredAt");
        Assert.True(JsonNode.DeepEquals(expected, upgradedPayload));
    }
}
