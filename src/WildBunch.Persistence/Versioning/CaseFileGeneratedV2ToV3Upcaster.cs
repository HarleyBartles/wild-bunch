using System.Text.Json;
using System.Text.Json.Nodes;

namespace WildBunch.Persistence.Versioning;

internal sealed class CaseFileGeneratedV2ToV3Upcaster : IEventUpcaster
{
    public string PayloadType => "CaseFileGenerated";
    public int FromVersion => 2;

    public string Upcast(string payloadJson)
    {
        var root = JsonNode.Parse(payloadJson) as JsonObject
            ?? throw new JsonException("CaseFileGenerated payload must be an object.");
        var caseFile = root["caseFile"] as JsonObject
            ?? throw new JsonException("CaseFileGenerated payload must contain a caseFile object.");

        AddUnknownIdentityToWarrants(caseFile, "knownWarrants");
        AddUnknownIdentityToWarrants(caseFile, "publicWarrants");

        return root.ToJsonString();
    }

    private static void AddUnknownIdentityToWarrants(JsonObject caseFile, string collectionName)
    {
        if (caseFile[collectionName] is not JsonArray warrants)
        {
            throw new JsonException($"CaseFileGenerated payload must contain a {collectionName} array.");
        }

        foreach (var warrantNode in warrants)
        {
            if (warrantNode is not JsonObject warrant)
            {
                throw new JsonException($"CaseFileGenerated {collectionName} entries must be objects.");
            }

            warrant["targetSuspectId"] = null;
        }
    }
}
