using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api;

namespace WildBunch.Api.Tests.Configuration;

public sealed class CorsConfigurationTests
{
    [Fact]
    public void ViteDevClientPolicyAllowsAnyLocalhostPort()
    {
        var services = new ServiceCollection();
        services.AddWildBunchServices(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

        var corsOptions = services.BuildServiceProvider()
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<CorsOptions>>()
            .Value;

        var policy = corsOptions.GetPolicy("ViteDevClient");
        Assert.NotNull(policy);

        Assert.True(policy!.IsOriginAllowed("http://localhost:5173"));
        Assert.True(policy.IsOriginAllowed("http://localhost:5174"));
        Assert.True(policy.IsOriginAllowed("http://localhost:3000"));
        Assert.True(policy.IsOriginAllowed("http://127.0.0.1:5173"));
        Assert.True(policy.IsOriginAllowed("http://127.0.0.1:5174"));

        // Non-local origins must still be rejected
        Assert.False(policy.IsOriginAllowed("http://example.com:5173"));
        Assert.False(policy.IsOriginAllowed("https://evil.com"));
    }
}
