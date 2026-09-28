using System.Text.Json;
using System.Text.Json.Nodes;

namespace WildBunch.Persistence.Versioning;

internal sealed class WorldGeneratedV1ToV2Upcaster : IEventUpcaster
{
    public string PayloadType => "WorldGenerated";
    public int FromVersion => 1;

    public string Upcast(string payloadJson)
    {
        var root = JsonNode.Parse(payloadJson) as JsonObject
            ?? throw new JsonException("WorldGenerated payload must be an object.");
        if (!root.ContainsKey("caseFile"))
        {
            root["caseFile"] = null;
        }

        return root.ToJsonString();
    }
}
