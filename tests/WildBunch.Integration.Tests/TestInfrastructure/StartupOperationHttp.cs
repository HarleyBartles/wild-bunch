using System.Net.Http.Json;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal static class StartupOperationHttp
{
    public static async Task<HttpResponseMessage> PostAsJsonWithStartupOperationAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        Guid? operationId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(value)
        };
        request.Headers.Add("Idempotency-Key", (operationId ?? Guid.NewGuid()).ToString());
        return await client.SendAsync(request);
    }
}
