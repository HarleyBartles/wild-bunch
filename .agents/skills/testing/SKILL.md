---
name: testing
description: >
  Use when writing .NET tests, setting up test infrastructure, reviewing test
  coverage, or needing guidance on xUnit, WebApplicationFactory, Testcontainers,
  snapshot testing, the AAA pattern, WireMock, FakeTimeProvider, or brute-force
  tests (thousands-of-combinations invariant and distribution testing).
metadata:
  content_mode: adapted
  adapted_author: Harley Bartles
  adaptation_note: Kept the testing guidance and removed provider-specific load assumptions. Added brute-force test kind guidance.
  source_author: codewithmukesh
  source_license: MIT
  source_repo: https://github.com/codewithmukesh/dotnet-claude-kit
  source_path: sources/third_party/dotnet-claude-kit/upstream/skills/testing/SKILL.md
---

# Testing (.NET 10)

## Core Principles

1. **Integration tests are the highest-value tests** — A single `WebApplicationFactory` test covers routing, binding, validation, business logic, and persistence in one shot. Start here before writing unit tests.
2. **Real databases in tests** — Use Testcontainers to spin up real PostgreSQL/SQL Server instances. In-memory providers hide real bugs (transactions, constraints, SQL generation).
3. **AAA pattern is mandatory** — Every test has three clearly separated sections: Arrange, Act, Assert. No mixing.
4. **Test behavior, not implementation** — Tests should survive refactoring. Test what the system does, not how it does it.

## Patterns

### xUnit v3 Basics

```csharp
public class OrderServiceTests
{
    [Fact]
    public async Task CreateOrder_WithValidItems_ReturnsSuccessResult()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var clock = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 0, 0, 0, TimeSpan.Zero));
        var service = new OrderService(db, clock);
        var request = new CreateOrderRequest("customer-1", [new("product-1", 2)]);

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
        Assert.Equal(clock.GetUtcNow(), result.Value.CreatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateOrder_WithInvalidCustomerId_ReturnsFailure(string? customerId)
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.CreateAsync(new CreateOrderRequest(customerId!, []));

        // Assert
        Assert.False(result.IsSuccess);
    }
}
```

### Integration Tests with WebApplicationFactory

The highest-value test pattern. Tests the full HTTP pipeline.

```csharp
// Fixtures/ApiFixture.cs
public class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace the real DB with Testcontainers
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Apply migrations
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
```

```csharp
// Tests/Orders/CreateOrderTests.cs
public class CreateOrderTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    [Fact]
    public async Task CreateOrder_ReturnsCreated_WithValidRequest()
    {
        // Arrange
        var request = new CreateOrderRequest("customer-1", [new("product-1", 2)]);

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.NotNull(order);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Contains("/api/orders/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task CreateOrder_ReturnsValidationProblem_WithEmptyItems()
    {
        // Arrange
        var request = new CreateOrderRequest("customer-1", []);

        // Act
        var response = await _client.PostAsJsonAsync("/api/orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
```

### Testcontainers for Real Database Testing

```csharp
// For SQL Server
private readonly MsSqlContainer _mssql = new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
    .Build();

// For PostgreSQL
private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
    .WithImage("postgres:17")
    .Build();

// For Redis
private readonly RedisContainer _redis = new RedisBuilder()
    .WithImage("redis:7")
    .Build();
```

### Verify Snapshot Testing

Use Verify for complex response objects where manual assertions would be fragile.

```csharp
[Fact]
public async Task GetOrder_MatchesSnapshot()
{
    // Arrange
    await SeedOrder(fixture);

    // Act
    var response = await _client.GetAsync("/api/orders/known-id");
    var content = await response.Content.ReadAsStringAsync();

    // Assert — compares against a stored .verified.txt file
    await Verify(content);
}
```

On first run, Verify creates a `.verified.txt` file. On subsequent runs, it compares output. If the output changes, the test fails and shows a diff.

### Test Data Builders

```csharp
public class OrderBuilder
{
    private string _customerId = "default-customer";
    private List<OrderItem> _items = [new("product-1", 1, 9.99m)];
    private OrderStatus _status = OrderStatus.Pending;

    public OrderBuilder WithCustomer(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public OrderBuilder WithItems(params OrderItem[] items)
    {
        _items = [..items];
        return this;
    }

    public OrderBuilder WithStatus(OrderStatus status)
    {
        _status = status;
        return this;
    }

    public Order Build() => Order.Create(_customerId, _items, _status);
}

// Usage in tests
var order = new OrderBuilder()
    .WithCustomer("vip-customer")
    .WithStatus(OrderStatus.Confirmed)
    .Build();
```

### Testing Time-Dependent Code

Use `TimeProvider` (built into .NET 8+) and `FakeTimeProvider` from `Microsoft.Extensions.TimeProvider.Testing`.

```csharp
[Fact]
public async Task ExpireOrders_MarksOldPendingOrdersAsExpired()
{
    // Arrange
    var clock = new FakeTimeProvider(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));
    var db = CreateDb();
    var order = Order.Create("customer-1", items, clock.GetUtcNow());
    db.Orders.Add(order);
    await db.SaveChangesAsync();

    // Advance time past expiry threshold
    clock.Advance(TimeSpan.FromDays(31));

    var handler = new ExpireOrders.Handler(db, clock);

    // Act
    await handler.Handle(new ExpireOrders.Command(), CancellationToken.None);

    // Assert
    var updated = await db.Orders.FindAsync(order.Id);
    Assert.Equal(OrderStatus.Expired, updated!.Status);
}
```

### Brute-Force Tests

Brute-force tests are a distinct test kind that iterates over thousands of
seed/salt/parameter combinations in a single test method, asserting invariants
and statistical properties that only become meaningful across many runs. They
are NOT a replacement for unit or integration tests — they are a complement
that catches silent bias, degenerate distributions, and rare anti-patterns that
per-combination tests miss.

**When to write a brute-force test:**
- The system produces deterministic-but-varied output from seed/salt combinations
  (map generation, encounter seeding, item distribution, mystery truth resolution)
- You want to catch silent bias where invariants pass but output is suspiciously
  skewed (e.g., 90% of trails are 2 days, one terrain type never appears)
- You want to catch rare anti-patterns that occur in <1% of runs but should never
  occur at all (self-loops, duplicate IDs, degenerate placement)
- You want to verify that parameter variation (entropy, difficulty, variant)
  actually produces measurably different output distributions

**When NOT to write a brute-force test:**
- The system is not deterministic (use property-based testing instead)
- You're testing a single code path with known inputs (use a unit test)
- You're testing the HTTP pipeline (use an integration test)
- The system has no seed/salt variation surface to iterate over

**Structure of a brute-force test:**

1. **Combination matrix** — enumerate all valid combinations of the system's
   input parameters (seed variants, entropy levels, salts, town counts, cluster
   counts, densities, etc.). Use nested loops, not `[Theory]` data — the point
   is to run thousands of combinations in one test method.

2. **Per-combination invariants** — for each generated output, assert structural
   correctness (counts, bounds, connectivity, uniqueness, determinism). Collect
   failures into a list and assert at the end so you see ALL failures, not just
   the first.

3. **Statistical expectations** — after the loop, assert aggregate distribution
   properties across all generated outputs:
   - Each expected category (terrain type, risk level, distance band) appears
     with reasonable frequency (typically ≥5% for common categories, ≥3% for
     edge categories)
   - No single category dominates excessively (typically ≤50%)
   - Parameter variation produces measurably different distributions (e.g.,
     different `SeedWorldVariant` values produce different terrain mixes)

4. **Anti-pattern detection** — assert that things that should never happen
   occur 0 times, and things that should be rare occur <1% of the time:
   - Self-loops, duplicate IDs, zero/negative values: 0 occurrences
   - Degenerate outputs (all-identical values, all-same-coordinate): <1%
   - Over-connected nodes on small maps: <1%

**Performance considerations:**
- A single brute-force test should complete in under 5 seconds. If it takes
  longer, narrow the combination matrix or reduce the per-combination work.
- If multiple brute-force test methods share the same combination matrix,
  extract a shared data collector that runs the matrix once and caches the
  results (see `BruteForceDataCollector` in `MapGeneratorBruteForceAnalysisTests`
  for the pattern).
- Use `[Fact]`, not `[Theory]` — the loops are inside the test, not in the
  data source.

**Example pattern (from `MapGeneratorTests.cs`):**

```csharp
[Fact]
public void Generate_BruteForce_AllValidCombinations_SatisfyAllInvariants()
{
    var failures = new List<string>();
    var combinationsTested = 0;

    foreach (var variant in Enum.GetValues<SeedWorldVariant>())
    foreach (var density in Enum.GetValues<GraphDensity>())
    foreach (var entropy in Enum.GetValues<GameEntropy>())
    foreach (var salt in new[] { "salt-a", "salt-b", "salt-c" })
    {
        combinationsTested++;
        var world = MapGenerator.Generate(seed, source, entropy, saltSource);

        // Per-combination invariants — collect failures, don't throw
        if (world.Towns.Count != expectedTowns)
            failures.Add($"{label}: town count mismatch");

        // ... more invariants ...
    }

    // Assert all invariants passed
    Assert.True(failures.Count == 0,
        $"Brute-force: {failures.Count} failures / {combinationsTested} combos. " +
        $"First 5: {string.Join(" | ", failures.Take(5))}");
}
```

**Example pattern (statistical + anti-pattern, shared collector):**

```csharp
// Shared collector runs the matrix once, caches for all tests in the class
private static BruteForceDataCollector.CollectorData _data = null!;
private static BruteForceDataCollector.CollectorData Data =>
    _data ??= BruteForceDataCollector.CollectAll();

[Fact]
public void BruteForce_TerrainDiversity_AllTypesAppear()
{
    var data = Data;
    foreach (TrailTerrain terrain in Enum.GetValues<TrailTerrain>())
    {
        var pct = 100.0 * data.TerrainCounts.GetValueOrDefault(terrain) / data.TotalTrails;
        Assert.True(pct >= 5.0, $"{terrain} appears {pct:F1}%, expected >= 5%");
    }
}

[Fact]
public void BruteForce_NoSelfLoopTrails()
{
    Assert.True(Data.SelfLoopCount == 0, "Self-loops should never occur");
}
```

**Existing brute-force tests in this repo:**
- `MapGeneratorTests.Generate_BruteForce_AllValidCombinations_SatisfyAllInvariants`
  — per-combination invariants + ride-day/degree distribution assertions
- `MapGeneratorBruteForceAnalysisTests` — 15 statistical expectation and
  anti-pattern tests sharing a single `BruteForceDataCollector` pass

**Threshold guidance:**
- Distribution floors (≥5%, ≥3%) should be generous enough to allow natural
  variation but strict enough to catch real bias. Tune based on observed
  distributions, not theoretical ideals.
- Anti-pattern ceilings (<1%) should be 0 for things that must never happen,
  and <1% for things that are rare-but-possible (e.g., all-identical distances
  on a 2-town map with a single trail).
- When a threshold fails, investigate the root cause before adjusting the
  threshold. A failing distribution test usually means the generator has a
  real bias that should be fixed, not a threshold that should be loosened.

### Test Naming Convention

Use the pattern: `MethodName_StateUnderTest_ExpectedBehavior`

```csharp
[Fact] public async Task CreateOrder_WithValidItems_ReturnsSuccessResult() { }
[Fact] public async Task CreateOrder_WithEmptyItems_ReturnsValidationError() { }
[Fact] public async Task GetOrder_WithNonExistentId_ReturnsNotFound() { }
[Fact] public async Task CancelOrder_WhenAlreadyShipped_ReturnsConflict() { }
```

## Anti-patterns

### Don't Use In-Memory Database for Integration Tests

```csharp
// BAD — hides real SQL behavior, transactions, constraints
services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));

// GOOD — Testcontainers with real database
services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(testContainer.GetConnectionString()));
```

### Don't Test Implementation Details

```csharp
// BAD — testing that a specific repository method was called
mock.Verify(x => x.AddAsync(It.IsAny<Order>()), Times.Once);
mock.Verify(x => x.SaveChangesAsync(), Times.Once);

// GOOD — test the observable outcome
var order = await db.Orders.FindAsync(orderId);
Assert.NotNull(order);
Assert.Equal(OrderStatus.Created, order.Status);
```

### Don't Share Mutable State Between Tests

```csharp
// BAD — static shared state
private static readonly AppDbContext SharedDb = CreateDb();

// GOOD — fresh state per test (or use IAsyncLifetime for shared fixtures)
private AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>()...);
```

### Don't Write Assertion-Free Tests

```csharp
// BAD — no assertion, only checks it doesn't throw
[Fact]
public async Task CreateOrder_Works()
{
    await service.CreateAsync(request);
    // "it didn't throw, so it works!" — NO
}

// GOOD — assert the expected outcome
[Fact]
public async Task CreateOrder_PersistsOrderToDatabase()
{
    var result = await service.CreateAsync(request);

    var persisted = await db.Orders.FindAsync(result.Value.Id);
    Assert.NotNull(persisted);
    Assert.Equal(request.CustomerId, persisted.CustomerId);
}
```

## Decision Guide

| Scenario | Recommendation |
|----------|---------------|
| Testing an API endpoint | `WebApplicationFactory` integration test |
| Testing business logic in isolation | Unit test with fakes/stubs |
| Database-dependent tests | Testcontainers (real DB) |
| Complex response validation | Verify snapshot testing |
| Time-dependent logic | `FakeTimeProvider` |
| External API dependency | `WireMock.Net` or `HttpMessageHandler` stub |
| Parameterized test cases | `[Theory]` with `[InlineData]` or `[MemberData]` |
| Test data setup | Builder pattern |
| Shared expensive fixture | `IClassFixture<T>` with `IAsyncLifetime` |
