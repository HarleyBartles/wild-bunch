### Task 1: Broaden dev CORS to allow any localhost port

**Files:**
- Modify: `src/WildBunch.Api/DependencyInjection.cs:24-32`
- Test: `tests/WildBunch.Api.Tests/CorsPolicyTests.cs` (create)

**Interfaces:**
- Consumes: `IServiceCollection` from ASP.NET Core
- Produces: A CORS policy named `"ViteDevClient"` that allows any `http://localhost:*` or `http://127.0.0.1:*` origin in development

- [ ] **Step 1: Write the failing test**

Create `tests/WildBunch.Api.Tests/CorsPolicyTests.cs`:

```csharp
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api;

namespace WildBunch.Api.Tests;

public sealed class CorsPolicyTests
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
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Api.Tests --filter CorsPolicyTests`
Expected: FAIL â€” `IsOriginAllowed("http://localhost:5174")` returns false because the current policy only allows port 5173.

- [ ] **Step 3: Implement the fix**

Replace the `WithOrigins(...)` call in `src/WildBunch.Api/DependencyInjection.cs`:

```csharp
        services.AddCors(options =>
        {
            options.AddPolicy("ViteDevClient", policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                        origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase)
                        || origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Api.Tests --filter CorsPolicyTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Api/DependencyInjection.cs tests/WildBunch.Api.Tests/CorsPolicyTests.cs
git commit -m "BUNCH-118: broaden dev CORS to allow any localhost port"
```

---