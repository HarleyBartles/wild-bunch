using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests;

/// <summary>
/// Integration-level hidden-truth guard for the prologue endpoint. Hits the actual
/// HTTP endpoint <c>GET /api/games/prologue</c> and asserts the response body exposes
/// no hidden culprit internals (<c>trueCulpritId</c>, <c>isTrueCulprit</c>,
/// <c>linkedSuspectIds</c>, internal <c>suspect-</c> ids, or the unsubstituted
/// <c>{trueCulpritMainIdentifier}</c> placeholder), and that the public-facing fields
/// (<c>heading</c>, <c>body</c>, <c>primaryAction</c>, <c>variantId</c>) are present
/// and non-empty. The application-level guard lives in
/// <c>PrologueHandlerTests.HiddenTruthGuard_NoCulpritInternalsExposed</c>; this test
/// proves the boundary holds end-to-end through the HTTP layer. See BUNCH-102.
/// </summary>
public sealed class PrologueHiddenTruthTests
{
    private static readonly string[] HiddenCulpritMarkers =
    [
        "trueCulpritId",
        "isTrueCulprit",
        "IsTrueCulprit",
        "linkedSuspectIds",
        "suspect-",
        "{trueCulpritMainIdentifier}"
    ];

    [Fact]
    public async Task PrologueEndpoint_DoesNotLeakHiddenCulpritInternals()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/games/prologue");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(payload));

        // The response body must not contain any hidden culprit internals, in any case
        // where the marker itself is case-sensitive (the JSON serializer emits camelCase
        // property names, so we check both the camelCase and PascalCase spellings).
        foreach (var marker in HiddenCulpritMarkers)
        {
            Assert.DoesNotContain(marker, payload, StringComparison.Ordinal);
        }

        // Also assert case-insensitively for the structural field names, so a future
        // serializer config change (e.g. PascalCase) cannot silently leak the truth.
        Assert.DoesNotContain("trueCulpritId", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("isTrueCulprit", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("linkedSuspectIds", payload, StringComparison.OrdinalIgnoreCase);

        // Deserialize and assert the public-facing fields are present and non-empty.
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("heading", out var headingElement), "response must contain 'heading'");
        var heading = headingElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(heading), "'heading' must be a non-empty string");

        Assert.True(root.TryGetProperty("body", out var bodyElement), "response must contain 'body'");
        var body = bodyElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(body), "'body' must be a non-empty string");

        Assert.True(root.TryGetProperty("primaryAction", out var primaryActionElement), "response must contain 'primaryAction'");
        var primaryAction = primaryActionElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(primaryAction), "'primaryAction' must be a non-empty string");

        Assert.True(root.TryGetProperty("variantId", out var variantIdElement), "response must contain 'variantId'");
        var variantId = variantIdElement.GetString();
        Assert.False(string.IsNullOrWhiteSpace(variantId), "'variantId' must be a non-empty string");

        // The body must contain a public-safe descriptor (the substituted true-culprit
        // descriptor), not an empty string, a raw GUID, or an internal suspect id.
        // The prologue copy always references "the Wild Bunch", so its presence confirms
        // the variant body was emitted and not truncated to a bare descriptor.
        Assert.Contains("Wild Bunch", body, StringComparison.OrdinalIgnoreCase);

        // A GUID-shaped descriptor would indicate the placeholder was substituted with
        // an internal id rather than a public-safe SaloonPersonOfInterestDescriptor.
        Assert.False(Guid.TryParse(body, out _), "'body' must not be a bare GUID");
        Assert.DoesNotContain("suspect-", body, StringComparison.OrdinalIgnoreCase);
    }
}
