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
        // An upcaster that starts at v2 (skipping v1) — non-contiguous
        var badUpcaster = new TestEventV2ToV3Upcaster();

        Assert.Throws<InvalidOperationException>(() =>
            new PayloadUpcasterRegistry([badUpcaster]));
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
}
