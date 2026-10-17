using System.Text.Json.Nodes;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Persistence;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Integration.Tests.Versioning;

/// <summary>
/// Demonstrates the upcaster correctness test pattern.
/// When a real upcaster is written, copy this pattern: seed a vN payload,
/// run the upcaster chain, assert the output matches the expected v(N+1) shape.
/// See the event sourcing integrity policy.
/// </summary>
public sealed class UpcasterCorrectnessTests
{
    /// <summary>
    /// A test-only upcaster that adds a "newField" to a payload at v1 -> v2.
    /// This demonstrates the pattern without needing a real event shape change.
    /// </summary>
    private sealed class TestEventV1ToV2Upcaster : IEventUpcaster
    {
        public string PayloadType => "TestEvent";
        public int FromVersion => 1;
        public string Upcast(string payloadJson)
        {
            // In a real upcaster, this would use JsonNode to transform the payload.
            // Here we just append a field to demonstrate the pattern.
            return payloadJson.Replace("}", ",\"newField\":\"added\"}");
        }
    }

    [Fact]
    public void Upcaster_V1ToV2_ProducesV2Shape()
    {
        var registry = new PayloadUpcasterRegistry([new TestEventV1ToV2Upcaster()]);

        // v1 payload (no newField)
        var v1Json = """{"existingField":"value"}""";

        // Upcast to v2
        var v2Json = registry.Upcast("TestEvent", storedVersion: 1, v1Json);

        // v2 payload has newField
        Assert.Contains("\"newField\":\"added\"", v2Json);
        Assert.Contains("\"existingField\":\"value\"", v2Json);
    }

    [Fact]
    public void CaseFileGeneratedV2Payload_KeepsEqualNameWarrantsUnassociated()
    {
        var serializer = new GameSessionJsonSerializer();
        var warrantTerms = new WarrantTerms(
            WarrantDisposition.DeadOrAlive,
            100m,
            Array.Empty<string>(),
            Array.Empty<string>(),
            "Sheriff",
            InvestigationTargetKind.GangMember,
            Array.Empty<OutlawGangId>(),
            null);
        var caseFile = new CaseFile(
            accusation: null,
            suspects: new[]
            {
                new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
                new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
            },
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            publicWarrants: new[]
            {
                new Warrant(new WarrantId("warrant-1"), "Mira Cline", warrantTerms),
                new Warrant(new WarrantId("warrant-2"), "Mira Cline", warrantTerms)
            });
        var eventJson = serializer.SerializeEvent(new CaseFileGenerated
        {
            CaseFile = CaseFileSnapshot.FromDomain(caseFile)
        });
        var legacyPayload = JsonNode.Parse(eventJson)!.AsObject();
        legacyPayload.Remove("occurredAt");
        var legacyCaseFile = legacyPayload["caseFile"]!.AsObject();
        foreach (var collectionName in new[] { "knownWarrants", "publicWarrants" })
        {
            foreach (var warrant in legacyCaseFile[collectionName]!.AsArray().Select(node => node!.AsObject()))
            {
                warrant.Remove("targetSuspectId");
            }
        }

        var registry = new PayloadUpcasterRegistry(DependencyInjection.CreateDefaultUpcasters());
        var upgradedJson = registry.Upcast("CaseFileGenerated", storedVersion: 2, legacyPayload.ToJsonString());
        var upgradedWarrants = JsonNode.Parse(upgradedJson)!["caseFile"]!["publicWarrants"]!.AsArray();
        Assert.All(upgradedWarrants, warrant =>
        {
            Assert.True(warrant!.AsObject().TryGetPropertyValue("targetSuspectId", out var targetSuspectId),
                "The v2-to-v3 upcaster must explicitly add the unknown identity field.");
            Assert.Null(targetSuspectId);
        });

        var upgradedEvent = Assert.IsType<CaseFileGenerated>(serializer.DeserializeEvent(nameof(CaseFileGenerated), upgradedJson));
        var reconstructed = upgradedEvent.CaseFile.ToDomain();

        Assert.Equal(2, reconstructed.PublicWarrants.Count);
        Assert.Equal("Mira Cline", reconstructed.PublicWarrants[0].TargetName);
        Assert.Equal("Mira Cline", reconstructed.PublicWarrants[1].TargetName);
        Assert.All(reconstructed.PublicWarrants, warrant => Assert.Null(warrant.TargetSuspectId));
    }

    [Fact]
    public void CurrentVersion_WithOneUpcaster_Returns2()
    {
        var registry = new PayloadUpcasterRegistry([new TestEventV1ToV2Upcaster()]);
        Assert.Equal(2, registry.CurrentVersion("TestEvent"));
    }

    [Fact]
    public void Upcast_AtCurrentVersion_ReturnsPayloadUnchanged()
    {
        var registry = new PayloadUpcasterRegistry([new TestEventV1ToV2Upcaster()]);

        var v2Json = """{"existingField":"value","newField":"already_present"}""";
        var result = registry.Upcast("TestEvent", storedVersion: 2, v2Json);
        Assert.Equal(v2Json, result);
    }

    [Fact]
    public void Registry_NonContiguousChain_ThrowsAtConstruction()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new PayloadUpcasterRegistry([
                new TestEventV1ToV2Upcaster(),
                new TestEventV3ToV4Upcaster()
            ]));
    }

    [Fact]
    public void Registry_DuplicateTransition_ThrowsAtConstruction()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new PayloadUpcasterRegistry([
                new TestEventV1ToV2Upcaster(),
                new DuplicateTestEventV1ToV2Upcaster()
            ]));
    }

    [Fact]
    public void Upcast_MultiStepChain_V1ToV3ThroughV2()
    {
        var registry = new PayloadUpcasterRegistry([
            new TestEventV1ToV2Upcaster(),
            new TestEventV2ToV3Upcaster()
        ]);

        var v1Json = """{"existingField":"value"}""";
        var v3Json = registry.Upcast("TestEvent", storedVersion: 1, v1Json);

        Assert.Contains("\"newField\":\"added\"", v3Json);
        Assert.Contains("\"anotherField\":\"added\"", v3Json);
        Assert.Contains("\"existingField\":\"value\"", v3Json);
    }

    [Fact]
    public void CurrentVersion_WithTwoUpcasters_Returns3()
    {
        var registry = new PayloadUpcasterRegistry([
            new TestEventV1ToV2Upcaster(),
            new TestEventV2ToV3Upcaster()
        ]);
        Assert.Equal(3, registry.CurrentVersion("TestEvent"));
    }

    private sealed class TestEventV2ToV3Upcaster : IEventUpcaster
    {
        public string PayloadType => "TestEvent";
        public int FromVersion => 2;
        public string Upcast(string payloadJson)
            => payloadJson.Replace("}", ",\"anotherField\":\"added\"}");
    }

    private sealed class TestEventV3ToV4Upcaster : IEventUpcaster
    {
        public string PayloadType => "TestEvent";
        public int FromVersion => 3;
        public string Upcast(string payloadJson)
            => payloadJson.Replace("}", ",\"v4Field\":\"added\"}");
    }

    private sealed class DuplicateTestEventV1ToV2Upcaster : IEventUpcaster
    {
        public string PayloadType => "TestEvent";
        public int FromVersion => 1;
        public string Upcast(string payloadJson) => payloadJson;
    }
}
