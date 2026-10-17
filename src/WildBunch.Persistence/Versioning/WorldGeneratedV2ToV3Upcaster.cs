using System.Text.Json;
using System.Text.Json.Nodes;

namespace WildBunch.Persistence.Versioning;

internal sealed class WorldGeneratedV2ToV3Upcaster : IEventUpcaster
{
    public string PayloadType => "WorldGenerated";
    public int FromVersion => 2;

    public string Upcast(string payloadJson)
    {
        var root = JsonNode.Parse(payloadJson) as JsonObject
            ?? throw new JsonException("WorldGenerated payload must be an object.");
        root.Remove("occurredAt");
        return root.ToJsonString();
    }
}
