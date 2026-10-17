using System.Text.Json;
using System.Text.Json.Nodes;

namespace WildBunch.Persistence.Versioning;

internal sealed class CaseFileGeneratedV1ToV2Upcaster : IEventUpcaster
{
    public string PayloadType => "CaseFileGenerated";
    public int FromVersion => 1;

    public string Upcast(string payloadJson)
    {
        var root = JsonNode.Parse(payloadJson) as JsonObject
            ?? throw new JsonException("CaseFileGenerated payload must be an object.");
        root.Remove("occurredAt");
        return root.ToJsonString();
    }
}
