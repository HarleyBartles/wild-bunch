using System.Text.Json;
using System.Text.Json.Nodes;
using WildBunch.Persistence;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Integration.Tests.Versioning;

public sealed class WorldGeneratedLegacyEventTests
{
    [Fact]
    public void Upcast_AddsOnlyMissingCaseFile_And_RegistryAdvancesToV2()
    {
        var upcaster = new WorldGeneratedV1ToV2Upcaster();
        var registry = new PayloadUpcasterRegistry(DependencyInjection.CreateDefaultUpcasters());
        var legacy = JsonNode.Parse("""{"seedCode":"seed","caseFile":{"suspects":[]}}""")!.AsObject();
        legacy.Remove("caseFile");

        var result = JsonNode.Parse(registry.Upcast("WorldGenerated", 1, legacy.ToJsonString()))!.AsObject();

        Assert.True(result.ContainsKey("caseFile"));
        Assert.Null(result["caseFile"]);
        Assert.Equal(2, registry.CurrentVersion("WorldGenerated"));
        Assert.Equal(1, upcaster.FromVersion);
        Assert.Equal("WorldGenerated", upcaster.PayloadType);
    }

    [Fact]
    public void Upcast_PreservesPopulatedCaseFile_AndRejectsNonObjectPayload()
    {
        var upcaster = new WorldGeneratedV1ToV2Upcaster();
        const string populated = """{"caseFile":{"culpritId":"suspect-1"}}""";

        var result = JsonNode.Parse(upcaster.Upcast(populated))!.AsObject();

        Assert.Equal("suspect-1", result["caseFile"]?["culpritId"]!.GetValue<string>());
        Assert.Throws<JsonException>(() => upcaster.Upcast("[]"));
    }
}
