# Geometry-First Map Generation - Plan 3: Integration Test Recalibration

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Recalibrate the integration test scenario fixtures and all topology-dependent tests to work with the real MapGenerator output (Delaunay+MST graph), eliminating ALL hardcoded town name assertions. Tests discover towns dynamically from the session's world and assert on graph properties (connectivity, positive coordinates, 2-6 day distances) — not on game content that may change tomorrow.

**Architecture:** Four tasks. Task 1 redesigns the scenario fixture system to be fully content-agnostic — no town names in descriptors, shape signatures, or assertions. Preview destinations are discovered dynamically. Task 2 makes GameApiTests travel tests topology-agnostic. Task 3 fixes all remaining test files that hardcode town names. Task 4 runs the full integration suite and fixes any remaining failures.

**Tech Stack:** C#/.NET 10, xUnit 2.9.3, PostgreSQL 16 (via scripts/postgres-dev.ps1)

## Global Constraints

- **NO TOWN NAMES in assertions, descriptors, shape signatures, or test logic.** Town names are game content. Game content may change tomorrow. Everything is discovered dynamically from the session's world.
- The canonical seed (SeedWorldResolver.CreateCanonicalSeedCode) with Boring entropy produces a deterministic MST world. The discovered topology at Plan 2 head is:
  - 8 towns, 7 trails (MST), all trails 2-6 days, all terrain=OpenRange, water=Creek
  - Starting town (slot-0) connects to 2 towns in the MST
  - All towns have positive coordinates (clustered placement)
- All 4 scenario fixtures use the same canonical seed code. Difficulty (Standard vs Easy) does NOT affect world generation.
- Integration tests run via `.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests --no-build` (requires PostgreSQL 16 tooling at .local/postgresql16).
- The fixture system's drift detection (AssertCachedFixtureContract) is a good design — keep it, but make contracts assert on properties not content.
- Direct-creation tests (EfGameSessionRepositoryTests, EventStorePersistenceTests, etc.) create their OWN worlds with their OWN town names as test fixture data — these are NOT canonical seed assertions and do NOT need changing. They test persistence, not world generation.

## Design Philosophy

**Assert the right thing:**
- Graph properties: connectivity, positive coordinates, 2-6 day distances, planarity — these catch real bugs.
- State properties: health, wallet, item count, horse/saddle presence, capabilities — these are difficulty-owned, not content-owned.
- Dynamic discovery: travel tests discover connected towns from the session's world.
- Shape signatures capture properties (mode, days, count) not content (town names).

**Fail for the right reasons:**
- Disconnected graph -> connectivity assertion fails.
- Zero coordinates -> positive-coordinate assertion fails.
- Distances outside 2-6 days -> distance-range assertion fails.
- Pipeline changes topology -> shape signature drift detected with clear message.

**What NOT to assert:**
- Specific town names ("hardpan", "quartzsite", "emberfall") — game content.
- Specific town-to-town connections — MapGenerator implementation detail, tested at unit level.
- Case file opening lead text — game content (remove from assertions).
- Town name strings in diary narration — assert on structure, not content.

---

## Prerequisites

- Plan 0 (Clean Slate) complete.
- Plans 1a-1f complete.
- Plan 2 (Wire & Integration) complete — MapGenerator.Generate is wired in, stub CreateWorld is deleted.
- PostgreSQL 16 tooling installed at .local/postgresql16.

## Verified codebase state (Plan 2 head)

- 73 integration tests fail due to ScenarioSeedFixture drift.
- 97 integration tests pass (non-fixture tests + direct-creation tests).
- 134 town name references across 18 integration test files (grep for hardpan|quartzsite|emberfall|...).
- The 4 fixtures all use the same canonical seed code.
- BoringScenarioBuilder wraps the 4 fixtures and is used by 22+ test files.
- Direct-creation tests (EfGameSessionRepositoryTests, etc.) have their own town names as fixture data — NOT affected.

---

## Tasks

### Task 1: Redesign Scenario Fixtures to Be Content-Agnostic

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedDescriptor.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalog.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedFixture.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/BoringScenarioBuilder.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/BoringScenarioBuilderTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedDescriptorTests.cs`

**Interfaces:**
- Consumes: MapGenerator.Generate (via CanonicalStartFlow.StartGame)
- Produces: 4 content-agnostic ScenarioSeedFixture instances with property-based assertions and dynamic preview discovery

**What changes:**

The fixture system currently hardcodes town names everywhere: `WithExactStartingTown(new TownId("hardpan"))`, `WithServicesTown(new TownId("hardpan"))`, `WithConnectedTownIds(new TownId("boulderwash"), ...)`, `PreviewDestinationTownId: "quartzsite"`, shape signatures include town names. All of this must be replaced with property-based assertions and dynamic discovery.

**Step 1: Update ScenarioSeedDescriptor.cs — remove town-name-based fields, add property-based fields**

Add a `ConnectedTownCount` field to replace `RequiredConnectedTownIds`:
```csharp
public int? RequiredConnectedTownCount { get; init; }
```

Add a builder method:
```csharp
public ScenarioSeedDescriptor WithConnectedTownCount(int count)
    => this with { RequiredConnectedTownCount = count, RequiredConnectedTownIds = [] };
```

Add a `ServicesOnStartingTown` boolean flag to replace `ServicesTownId`:
```csharp
public bool? ServicesOnStartingTown { get; init; }
```

Add a builder method:
```csharp
public ScenarioSeedDescriptor WithServicesOnStartingTown()
    => this with { ServicesOnStartingTown = true };
```

Remove `ServicesTownId` and `WithServicesTown` — no longer needed.

Update `FormatRequiredShapeSignature`:
- Replace `start={ExactStartingTownId.Value.Value}` with `start={FormatStartingTownRole(StartingTownRole.Value)}` (already handled — just remove the ExactStartingTownId branch)
- Replace `routes={string.Join(",", RequiredConnectedTownIds.Select(...))}` with `routes=count={RequiredConnectedTownCount}`
- Replace `services={ServicesTownId.Value.Value}` with `services=starting-town` (when ServicesOnStartingTown is true)
- Remove the `RequiredConnectedTownIds` branch

The updated `FormatRequiredShapeSignature` method:
```csharp
public string FormatRequiredShapeSignature()
{
    var parts = new List<string>
    {
        CodecVersion.Value,
        ScenarioName
    };

    if (Entropy is not null)
        parts.Add($"entropy={Entropy}");

    if (Difficulty is not null)
        parts.Add($"difficulty={Difficulty}");

    if (StartingTownRole is not null)
        parts.Add($"start={FormatStartingTownRole(StartingTownRole.Value)}");

    if (HorseCondition is not null)
        parts.Add($"horse={HorseCondition.Value.ToString().ToLowerInvariant()}");

    if (SaddleState is not null)
        parts.Add($"saddle={SaddleState.Value.ToString().ToLowerInvariant()}");

    if (Wallet is not null)
        parts.Add($"wallet={Wallet.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");

    if (ItemCount is not null)
        parts.Add($"items={ItemCount.Value}");

    if (Health is not null)
        parts.Add($"health={Health.Value}");

    if (TownCount is not null)
        parts.Add($"towns={TownCount.Value}");

    if (TravelMode is not null)
        parts.Add($"travel={TravelMode.Value.ToString().ToLowerInvariant()}");

    if (RequiredConnectedTownCount is not null)
        parts.Add($"routes=count={RequiredConnectedTownCount.Value}");

    if (ServicesOnStartingTown is true)
        parts.Add("services=starting-town");

    if (Preview is not null)
        parts.Add($"preview={FormatPreview(Preview)}");

    return string.Join("|", parts);
}
```

Update `ScenarioPreviewExpectation` — remove `DestinationTownId` (discovered dynamically):
```csharp
internal sealed record ScenarioPreviewExpectation
{
    public TravelMode? TravelMode { get; init; }
    public int? BaselineRideDays { get; init; }
    public int? ExpectedDays { get; init; }

    public static ScenarioPreviewExpectation Missing() => new();

    public static ScenarioPreviewExpectation Mounted(int baselineRideDays, int expectedDays)
        => new()
        {
            TravelMode = global::WildBunch.Domain.Travel.TravelMode.Mounted,
            BaselineRideDays = baselineRideDays,
            ExpectedDays = expectedDays
        };

    public bool IsMissing => TravelMode is null
        && BaselineRideDays is null
        && ExpectedDays is null;
}
```

Update `FormatPreview` (remove destination town ID):
```csharp
private static string FormatPreview(ScenarioPreviewExpectation preview)
    => preview.IsMissing
        ? "missing"
        : $"{preview.TravelMode!.Value.ToString().ToLowerInvariant()}:{preview.BaselineRideDays!.Value}/{preview.ExpectedDays!.Value}";
```

Remove `WithExactStartingTown` method (no longer used — all fixtures use role-based starting town).

**Step 2: Update ScenarioSeedFixture.cs — dynamic preview discovery**

The `PreviewDestinationTownId` field is currently a hardcoded string. Replace it with dynamic discovery: the fixture discovers the first connected town from the starting town at assertion time.

Remove `PreviewDestinationTownId` from the record. Add a helper that discovers the first connected town:
```csharp
private TownId DiscoverFirstConnectedTown(GameSession session)
{
    var connectedTownId = session.World.Trails
        .Where(t => t.FromTownId == session.Player.CurrentTownId || t.ToTownId == session.Player.CurrentTownId)
        .Select(t => t.FromTownId == session.Player.CurrentTownId ? t.ToTownId : t.FromTownId)
        .FirstOrDefault();

    if (connectedTownId is null)
        throw new XunitException($"Fixture '{Name}': no connected town found from starting town '{session.Player.CurrentTownId}' for travel preview.");

    return connectedTownId;
}
```

Update `AssertCachedFixtureContract` to discover the preview destination dynamically:
```csharp
public void AssertCachedFixtureContract()
{
    if (!string.Equals(Contract.CodecVersion.Value, SeedWorldResolver.ResolverContractVersion, StringComparison.Ordinal))
        ThrowDrift($"Resolver contract version changed from '{Contract.CodecVersion.Value}' to '{SeedWorldResolver.ResolverContractVersion}'.");

    var session = CreateSession();
    var sessionDto = GameSessionMapper.ToDto(session);

    // Discover preview destination dynamically (first connected town)
    TravelPreviewResultDto? preview = null;
    TownId? previewDestination = null;
    if (AssertTravelPreviewContract is not null)
    {
        previewDestination = DiscoverFirstConnectedTown(session);
        preview = CreatePreview(session, previewDestination.Value.Value);
    }

    var actualShapeSignature = DescribeShapeSignature(sessionDto, preview);
    var requiredShapeSignature = Contract.FormatRequiredShapeSignature();

    if (!string.Equals(actualShapeSignature, requiredShapeSignature, StringComparison.Ordinal))
        ThrowDrift($"Observed required-shape signature '{actualShapeSignature}'.");

    try
    {
        AssertCreatedSessionContract(sessionDto);
    }
    catch (Exception ex)
    {
        ThrowDrift($"The start-session contract failed: {ex.Message}");
    }

    if (AssertTravelPreviewContract is not null)
    {
        if (preview is null)
            ThrowDrift("Expected a travel preview but none was produced.");

        try
        {
            AssertTravelPreviewContract(sessionDto, previewDestination!.Value.Value, preview!);
        }
        catch (Exception ex)
        {
            ThrowDrift($"The travel preview contract failed: {ex.Message}");
        }
    }
}
```

**Step 3: Update ScenarioSeedCatalog.cs — recalibrate all 4 fixtures**

Replace all 4 descriptors. Remove all town names. Use property-based assertions.

CanonicalMountedStandardDescriptor:
```csharp
private static readonly ScenarioSeedDescriptor CanonicalMountedStandardDescriptor = ScenarioSeedDescriptor.Create("CanonicalMountedStandard")
    .WithCodecVersion(ScenarioSeedCodecVersion.Current)
    .WithEntropy(GameEntropy.Boring)
    .WithStartingTownRole(ScenarioStartingTownRole.DefaultPlayableStart)
    .WithHorse(HorseCondition.Healthy)
    .WithSaddle(SaddleState.Present)
    .WithWallet(25m)
    .WithItemCount(8)
    .WithTownCount(8)
    .WithPreview(ScenarioPreviewExpectation.Mounted(4, 4));
```

Note: The preview baseline/expected days are 4 because the first connected town from the starting town in the MST is a 4-day trail (hardpan→emberfall). This is a property of the MST topology, not a town name. If the topology changes, this number changes and drift is detected.

Wait — actually we should NOT hardcode the preview days either, since that's also implementation-specific. The preview should just assert that a preview exists with Mounted mode and positive days. Let me reconsider...

Actually, the shape signature NEEDS specific values to detect drift. If we make everything "just assert it's positive", we can't detect when the pipeline changes the topology. The shape signature is the drift detection mechanism. So we should keep specific values in the descriptor (they're the "cached contract") but the assertion helpers should check properties.

The descriptor says "the preview should be Mounted with 4/4 days" — this is the cached contract. If MapGenerator changes and produces a 3-day trail, the shape signature drifts and the test fails with a clear message. That's the right behavior.

But the 4/4 is specific to the current MST topology. If we're OK with that (it's the drift detection value, not a game content assertion), then keep it. The key distinction is:
- Town names = game content (may change) -> don't assert
- Trail distances = pipeline output (should be stable for same seed) -> assert for drift detection

OK, keeping the preview values. But removing the destination town ID from the preview.

CanonicalPinecrossServicesDescriptor:
```csharp
private static readonly ScenarioSeedDescriptor CanonicalPinecrossServicesDescriptor = ScenarioSeedDescriptor.Create("CanonicalPinecrossServices")
    .WithCodecVersion(ScenarioSeedCodecVersion.Current)
    .WithEntropy(GameEntropy.Boring)
    .WithStartingTownRole(ScenarioStartingTownRole.DefaultPlayableStart)
    .WithHorse(HorseCondition.Healthy)
    .WithSaddle(SaddleState.Present)
    .WithWallet(25m)
    .WithItemCount(8)
    .WithTownCount(8)
    .WithServicesOnStartingTown()
    .WithPreview(ScenarioPreviewExpectation.Mounted(4, 4));
```

HighRiskFoeInterruptRouteDescriptor:
```csharp
private static readonly ScenarioSeedDescriptor HighRiskFoeInterruptRouteDescriptor = ScenarioSeedDescriptor.Create("HighRiskFoeInterruptRoute")
    .WithCodecVersion(ScenarioSeedCodecVersion.Current)
    .WithEntropy(GameEntropy.Boring)
    .WithStartingTownRole(ScenarioStartingTownRole.DefaultPlayableStart)
    .WithHorse(HorseCondition.Healthy)
    .WithSaddle(SaddleState.Present)
    .WithWallet(25m)
    .WithItemCount(8)
    .WithTownCount(8)
    .WithConnectedTownCount(2)
    .WithPreview(ScenarioPreviewExpectation.Missing());
```

NoHorseLightEasyDescriptor:
```csharp
private static readonly ScenarioSeedDescriptor NoHorseLightEasyDescriptor = ScenarioSeedDescriptor.Create("NoHorseLightEasy")
    .WithCodecVersion(ScenarioSeedCodecVersion.Current)
    .WithEntropy(GameEntropy.Boring)
    .WithDifficulty(GameDifficulty.Easy)
    .WithHorse(HorseCondition.Healthy)
    .WithSaddle(SaddleState.Present)
    .WithHealth(1250)
    .WithTownCount(8)
    .WithTravelMode(TravelMode.Mounted)
    .WithPreview(ScenarioPreviewExpectation.Mounted(4, 4));
```

Update all 4 fixture instances — remove `PreviewDestinationTownId` (discovered dynamically now):

CanonicalMountedStandard:
```csharp
public static readonly ScenarioSeedFixture CanonicalMountedStandard = new(
    Name: "CanonicalMountedStandard",
    SeedCode: CanonicalMountedSeedCode,
    GameDifficulty: GameDifficulty.Standard,
    GameEntropy: GameEntropy.Boring,
    Contract: CanonicalMountedStandardDescriptor,
    DescribeShapeSignature: DescribeCanonicalMountedShape,
    AssertCreatedSessionContract: session => AssertCanonicalMountedStartState("CanonicalMountedStandard", session),
    AssertTravelPreviewContract: (session, destinationTownId, preview) => AssertCanonicalMountedTravelPreview("CanonicalMountedStandard", session, destinationTownId, preview));
```

CanonicalPinecrossServices:
```csharp
public static readonly ScenarioSeedFixture CanonicalPinecrossServices = new(
    Name: "CanonicalPinecrossServices",
    SeedCode: CanonicalMountedStandard.SeedCode,
    GameDifficulty: GameDifficulty.Standard,
    GameEntropy: GameEntropy.Boring,
    Contract: CanonicalPinecrossServicesDescriptor,
    DescribeShapeSignature: DescribeCanonicalPinecrossServicesShape,
    AssertCreatedSessionContract: session =>
    {
        AssertCanonicalMountedStartState("CanonicalPinecrossServices", session);
        RequireEqual("CanonicalPinecrossServices", "start-game.inventory.food.quantity", 4, RequireItem("CanonicalPinecrossServices", session, ItemKind.Food).Quantity);
        RequireEqual("CanonicalPinecrossServices", "start-game.inventory.horseFeed.quantity", 3, RequireItem("CanonicalPinecrossServices", session, ItemKind.HorseFeed).Quantity);
    },
    AssertTravelPreviewContract: (session, destinationTownId, preview) => AssertCanonicalMountedTravelPreview("CanonicalPinecrossServices", session, destinationTownId, preview));
```

HighRiskFoeInterruptRoute:
```csharp
public static readonly ScenarioSeedFixture HighRiskFoeInterruptRoute = new(
    Name: "HighRiskFoeInterruptRoute",
    SeedCode: CanonicalMountedStandard.SeedCode,
    GameDifficulty: GameDifficulty.Standard,
    GameEntropy: GameEntropy.Boring,
    Contract: HighRiskFoeInterruptRouteDescriptor,
    DescribeShapeSignature: DescribeHighRiskFoeInterruptRouteShape,
    AssertCreatedSessionContract: session => AssertCanonicalMountedStartState("HighRiskFoeInterruptRoute", session));
```

NoHorseLightEasy:
```csharp
public static readonly ScenarioSeedFixture NoHorseLightEasy = new(
    Name: "NoHorseLightEasy",
    SeedCode: NoHorseLightEasySeedCode,
    GameDifficulty: GameDifficulty.Easy,
    GameEntropy: GameEntropy.Boring,
    Contract: NoHorseLightEasyDescriptor,
    DescribeShapeSignature: DescribeNoHorseLightEasyShape,
    AssertCreatedSessionContract: session =>
    {
        RequireEqual("NoHorseLightEasy", "start-game.GameDifficulty", GameDifficulty.Easy, session.GameDifficulty);
        RequireEqual("NoHorseLightEasy", "start-game.entropy", GameEntropy.Boring, session.GameEntropy);
        RequireEqual("NoHorseLightEasy", "start-game.health", 1250, session.Player.Health);
        Require("NoHorseLightEasy", "start-game.inventory.horseItem", session.Inventory.Items.Any(item => item.Kind == ItemKind.Horse), "expected the starting inventory to include a horse (transitional default).");
        Require("NoHorseLightEasy", "start-game.inventory.saddleItem", session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle), "expected the starting inventory to include a saddle (transitional default).");
    },
    AssertTravelPreviewContract: (session, destinationTownId, preview) =>
    {
        RequireEqual("NoHorseLightEasy", "travel-preview.success", true, preview.Success);
        RequireEqual("NoHorseLightEasy", "travel-preview.travelMode", TravelMode.Mounted, preview.Preview?.TravelMode);
        RequireEqual("NoHorseLightEasy", "travel-preview.mountedTravelAvailable", true, preview.Preview?.MountedTravelAvailable);
    },
    AssertTravelTurnContract: (session, destinationTownId, preview, turn) =>
    {
        RequireEqual("NoHorseLightEasy", "travel-turn.success", true, turn.Success);
        RequireEqual("NoHorseLightEasy", "travel-turn.travelMode", TravelMode.Mounted, turn.CurrentSession.Journey?.TravelMode);
        RequireEqual("NoHorseLightEasy", "travel-turn.baselineRideDays", preview.Preview?.BaselineRideDays, turn.CurrentSession.Journey?.BaselineRideDays);
        RequireEqual("NoHorseLightEasy", "travel-turn.expectedDays", preview.Preview?.ExpectedDays, turn.CurrentSession.Journey?.ExpectedDays);
        RequireEqual("NoHorseLightEasy", "travel-turn.daysTravelled", 0, turn.CurrentSession.Journey?.DaysTravelled);
    });
```

**Step 4: Update DescribeShapeSignature functions — remove town names**

DescribeCanonicalMountedShape:
```csharp
private static string DescribeCanonicalMountedShape(GameSessionDto session, TravelPreviewResultDto? preview)
    => string.Join(
        "|",
        ScenarioSeedCodecVersion.Current.Value,
        "CanonicalMountedStandard",
        $"entropy={session.GameEntropy}",
        "start=default-playable-start",
        $"horse={DescribeHorseState(session.Inventory.HorseState)}",
        $"saddle={DescribePresence(session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle))}",
        $"wallet={session.Inventory.Wallet.Cash.ToString(CultureInfo.InvariantCulture)}",
        $"items={session.Inventory.Items.Count}",
        $"towns={session.World.Towns.Count}",
        $"preview={DescribeMountedPreview(preview)}");
```

DescribeCanonicalPinecrossServicesShape:
```csharp
private static string DescribeCanonicalPinecrossServicesShape(GameSessionDto session, TravelPreviewResultDto? preview)
    => string.Join(
        "|",
        ScenarioSeedCodecVersion.Current.Value,
        "CanonicalPinecrossServices",
        $"entropy={session.GameEntropy}",
        "start=default-playable-start",
        $"horse={DescribeHorseState(session.Inventory.HorseState)}",
        $"saddle={DescribePresence(session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle))}",
        $"wallet={session.Inventory.Wallet.Cash.ToString(CultureInfo.InvariantCulture)}",
        $"items={session.Inventory.Items.Count}",
        $"towns={session.World.Towns.Count}",
        "services=starting-town",
        $"preview={DescribeMountedPreview(preview)}");
```

DescribeHighRiskFoeInterruptRouteShape:
```csharp
private static string DescribeHighRiskFoeInterruptRouteShape(GameSessionDto session, TravelPreviewResultDto? preview)
{
    var connectedCount = session.World.Trails
        .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
        .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
        .Distinct()
        .Count();

    return string.Join(
        "|",
        ScenarioSeedCodecVersion.Current.Value,
        "HighRiskFoeInterruptRoute",
        $"entropy={session.GameEntropy}",
        "start=default-playable-start",
        $"horse={DescribeHorseState(session.Inventory.HorseState)}",
        $"saddle={DescribePresence(session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle))}",
        $"wallet={session.Inventory.Wallet.Cash.ToString(CultureInfo.InvariantCulture)}",
        $"items={session.Inventory.Items.Count}",
        $"towns={session.World.Towns.Count}",
        $"routes=count={connectedCount}",
        $"preview={DescribeMountedPreview(preview)}");
}
```

DescribeNoHorseLightEasyShape:
```csharp
private static string DescribeNoHorseLightEasyShape(GameSessionDto session, TravelPreviewResultDto? preview)
    => string.Join(
        "|",
        ScenarioSeedCodecVersion.Current.Value,
        "NoHorseLightEasy",
        $"entropy={session.GameEntropy}",
        $"difficulty={session.GameDifficulty}",
        $"horse={DescribeHorseState(session.Inventory.HorseState)}",
        $"saddle={DescribePresence(session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle))}",
        $"health={session.Player.Health}",
        $"towns={session.World.Towns.Count}",
        $"travel={preview?.Preview?.TravelMode.ToString().ToLowerInvariant() ?? "missing"}",
        $"preview={DescribeMountedPreview(preview)}");
```

Update DescribeMountedPreview (remove destination town ID):
```csharp
private static string DescribeMountedPreview(TravelPreviewResultDto? preview)
    => preview?.Preview is null
        ? "missing"
        : $"{preview.Preview.TravelMode.ToString().ToLowerInvariant()}:{preview.Preview.BaselineRideDays}/{preview.Preview.ExpectedDays}";
```

Remove `DescribeFootPreview` (unused after changes).

**Step 5: Update AssertCanonicalMountedStartState — remove town names, add graph-property assertions**

Replace the method:
```csharp
private static void AssertCanonicalMountedStartState(string scenarioName, GameSessionDto session)
{
    RequireEqual(scenarioName, "start-game.GameDifficulty", GameDifficulty.Standard, session.GameDifficulty);
    RequireEqual(scenarioName, "start-game.entropy", GameEntropy.Boring, session.GameEntropy);

    // Starting town is whatever StartingTownPolicy resolved — don't assert on the name.
    // Assert that it's one of the world's towns.
    Require(scenarioName, "start-game.currentTownId.inWorld",
        session.World.Towns.Any(t => t.Id == session.Player.CurrentTownId),
        $"expected current town {session.Player.CurrentTownId} to be in the world");

    RequireEqual(scenarioName, "start-game.health", 1000, session.Player.Health);
    RequireEqual(scenarioName, "start-game.wallet.cash", 25m, session.Inventory.Wallet.Cash);
    Require(scenarioName, "start-game.world.towns", session.World.Towns.Count >= 5 && session.World.Towns.Count <= 10, $"expected town count 5-10, got {session.World.Towns.Count}");
    Require(scenarioName, "start-game.world.trails", session.World.Trails.Count > 0, "expected at least one trail");

    // Graph-property assertions: connected, positive coordinates, 2-6 day distances.
    AssertWorldGraphProperties(scenarioName, session);

    // Remove case file opening lead assertion — that's game content.
    RequireEqual(scenarioName, "start-game.caseFile.discoveredSuspects", 0, session.CaseFile.DiscoveredSuspects.Count);
    RequireEqual(scenarioName, "start-game.inventory.items.count", 8, session.Inventory.Items.Count);
    Require(scenarioName, "start-game.inventory.horseState", session.Inventory.HorseState is not null, "expected the player to start mounted.");
    Require(scenarioName, "start-game.capabilities.mountedTravelAvailable", session.Inventory.Capabilities.MountedTravelAvailable, "expected mounted travel to be available.");
    Require(scenarioName, "start-game.capabilities.gunfightCapable", session.Inventory.Capabilities.GunfightCapable, "expected gunfight capability to be available.");
    Require(scenarioName, "start-game.capabilities.rifleUsable", !session.Inventory.Capabilities.RifleUsable, "expected rifles to stay unusable at start.");
    Require(scenarioName, "start-game.logEntries", session.LogEntries.Count > 0, "expected the new game log to be populated.");
}
```

Add the graph-property helper:
```csharp
private static void AssertWorldGraphProperties(string scenarioName, GameSessionDto session)
{
    // All towns have positive coordinates (clustered placement, not placeholder zeros)
    foreach (var town in session.World.Towns)
    {
        Require(scenarioName, $"start-game.world.towns.{town.Id}.mapX", town.X > 0, $"expected positive MapX for town at index, got {town.X}");
        Require(scenarioName, $"start-game.world.towns.{town.Id}.mapY", town.Y > 0, $"expected positive MapY for town at index, got {town.Y}");
    }

    // All trails have ride-day distances in 2-6 day range
    foreach (var trail in session.World.Trails)
    {
        Require(scenarioName, $"start-game.world.trails.{trail.Id}.rideDayDistance",
            trail.RideDayDistance >= 2m && trail.RideDayDistance <= 6m,
            $"expected ride-day distance 2-6 for trail {trail.Id}, got {trail.RideDayDistance}");
    }

    // Trail graph is connected (BFS from starting town reaches all towns)
    var adjacency = new Dictionary<string, HashSet<string>>();
    foreach (var town in session.World.Towns)
        adjacency[town.Id] = new HashSet<string>();
    foreach (var trail in session.World.Trails)
    {
        adjacency[trail.FromTownId].Add(trail.ToTownId);
        adjacency[trail.ToTownId].Add(trail.FromTownId);
    }
    var visited = new HashSet<string>();
    var queue = new Queue<string>();
    var startTown = session.Player.CurrentTownId;
    queue.Enqueue(startTown);
    visited.Add(startTown);
    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        foreach (var neighbor in adjacency[current])
        {
            if (visited.Add(neighbor))
                queue.Enqueue(neighbor);
        }
    }
    Require(scenarioName, "start-game.world.graph.connected",
        visited.Count == session.World.Towns.Count,
        $"expected all {session.World.Towns.Count} towns reachable from starting town, only {visited.Count} reached");

    // Starting town has at least 2 connected towns
    var startConnected = adjacency[startTown].Count;
    Require(scenarioName, "start-game.world.graph.startConnected",
        startConnected >= 2,
        $"expected starting town to have at least 2 connected towns, got {startConnected}");
}
```

**Step 6: Update AssertPinecrossConnectedTownAssumptions — remove town names**

Replace:
```csharp
private static void AssertPinecrossConnectedTownAssumptions(GameSessionDto session)
{
    var connectedCount = session.World.Trails
        .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
        .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
        .Distinct()
        .Count();

    Require("CanonicalPinecrossServices", "start-game.connectedTownIds.count",
        connectedCount >= 2,
        $"expected at least 2 connected towns from starting town, got {connectedCount}");
}
```

**Step 7: Update AssertHighRiskFoeInterruptRoute — dynamic travel destination**

The method currently hardcodes travel to a specific town and checks the diary for "Emberfall". Replace with dynamic discovery — travel to the first connected town and check the diary names whatever town we're traveling to.

This method is called from GameApiTests and uses `dryForkTravel`, `blockedAdvance`, etc. The caller needs to pass the destination town ID. Update the method signature to accept a dynamically discovered destination:

```csharp
public static void AssertHighRiskFoeInterruptRoute(
    this ScenarioSeedFixture fixture,
    GameSessionDto session,
    string destinationTownId,
    string destinationTownName,
    GameTurnResultDto dryForkTravel,
    GameTurnResultDto blockedAdvance,
    GameTurnResultDto resolved,
    GameTurnResultDto resumeAdvance)
{
    RequireEqual("HighRiskFoeInterruptRoute", "scenario.name", "HighRiskFoeInterruptRoute", fixture.Name);
    fixture.AssertCreatedSession(session);

    Require("HighRiskFoeInterruptRoute", "travel-turn.success", dryForkTravel.Success, "expected the journey to start successfully.");
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.journeyStatus", JourneyStatus.Active, dryForkTravel.JourneyStatus);
    Require("HighRiskFoeInterruptRoute", "travel-turn.noEncounter", dryForkTravel.Journey is null || dryForkTravel.Journey.PendingEncounter is null, "expected no pending encounter on journey start.");
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.daysTravelled", 0, dryForkTravel.CurrentSession.Journey?.DaysTravelled);

    Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.success", !blockedAdvance.Success, "expected the first advance to interrupt due to encounter.");
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.journeyStatus", JourneyStatus.Interrupted, blockedAdvance.JourneyStatus);
    Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter", blockedAdvance.Journey is not null && blockedAdvance.Journey.PendingEncounter is not null, "expected a pending public encounter.");
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.kind", "npc", blockedAdvance.Journey!.PendingEncounter!.Kind);
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.choices", 3, blockedAdvance.Journey.PendingEncounter.Choices.Count);
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.choiceIds", "run,fight,bribe", string.Join(",", blockedAdvance.Journey.PendingEncounter.Choices.Select(choice => choice.Id)));
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.clock.day", dryForkTravel.CurrentSession.Clock.Day + 1, blockedAdvance.CurrentSession.Clock.Day);
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.clock.turn", 0, blockedAdvance.CurrentSession.Clock.Turn);
    Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.travelDiary", blockedAdvance.TravelDiary is not null && blockedAdvance.TravelDiary.Days.Count == 1, "expected one diary day for the interrupted first day.");

    // Check diary names the destination town (whatever it is)
    var openingNarration = blockedAdvance.TravelDiary!.Days[0].OpeningNarration;
    Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.openingNarration",
        openingNarration is not null && openingNarration.Contains(destinationTownName, StringComparison.OrdinalIgnoreCase),
        $"expected the diary to name the destination town '{destinationTownName}'.");
    Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.openingNarration",
        openingNarration is not null && openingNarration.Contains("by mounted travel", StringComparison.OrdinalIgnoreCase),
        "expected the diary to reflect mounted travel before the interruption.");

    Require("HighRiskFoeInterruptRoute", "travel-turn.resolved.success", resolved.Success, "expected the public encounter resolution to succeed.");
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.journeyStatus", JourneyStatus.Active, resolved.JourneyStatus);
    Require("HighRiskFoeInterruptRoute", "travel-turn.resolved.pendingEncounter", resolved.CurrentSession.Journey is not null && resolved.CurrentSession.Journey.PendingEncounter is null, "expected the pending encounter to clear after resolution.");
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.clock.day", blockedAdvance.CurrentSession.Clock.Day, resolved.CurrentSession.Clock.Day);
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.clock.turn", 0, resolved.CurrentSession.Clock.Turn);

    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.currentTownId", destinationTownId, resumeAdvance.CurrentSession.Player.CurrentTownId);
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.clock.day", blockedAdvance.CurrentSession.Clock.Day + 1, resumeAdvance.CurrentSession.Clock.Day);
    RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.clock.turn", 0, resumeAdvance.CurrentSession.Clock.Turn);
}
```

**Step 8: Update AssertPinecrossServices — remove hardcoded town name in store-offers query**

The current method queries `/api/games/{gameId}/towns/hardpan/store-offers`. Replace with the session's current town:
```csharp
public static async Task AssertPinecrossServices(this ScenarioSeedFixture fixture, HttpClient client, Guid gameId, GameSessionDto session)
{
    RequireEqual("CanonicalPinecrossServices", "scenario.name", "CanonicalPinecrossServices", fixture.Name);
    fixture.AssertCreatedSession(session);
    AssertPinecrossConnectedTownAssumptions(session);

    var actionsResponse = await client.GetAsync($"/api/games/{gameId}/actions");
    RequireEqual("CanonicalPinecrossServices", "actions.statusCode", HttpStatusCode.OK, actionsResponse.StatusCode);

    var actions = await actionsResponse.Content.ReadFromJsonAsync<AvailableActionDto[]>();
    Require("CanonicalPinecrossServices", "actions.payload", actions is not null, "expected available actions to deserialize.");
    AssertPinecrossActionAvailability(actions!);

    // Query store offers for the current town (not a hardcoded town name)
    var storeOffersResponse = await client.GetAsync($"/api/games/{gameId}/towns/{session.Player.CurrentTownId}/store-offers");
    RequireEqual("CanonicalPinecrossServices", "store-offers.statusCode", HttpStatusCode.OK, storeOffersResponse.StatusCode);

    var storeOffers = await storeOffersResponse.Content.ReadFromJsonAsync<TownStoreOffersDto>();
    Require("CanonicalPinecrossServices", "store-offers.payload", storeOffers is not null, "expected town store offers to deserialize.");
    AssertPinecrossStoreAvailability(storeOffers!, session.Player.CurrentTownId);
}
```

Update AssertPinecrossStoreAvailability to not hardcode town name:
```csharp
private static void AssertPinecrossStoreAvailability(TownStoreOffersDto storeOffers, string currentTownId)
{
    RequireEqual("CanonicalPinecrossServices", "store-offers.available", true, storeOffers.Available);
    RequireEqual("CanonicalPinecrossServices", "store-offers.townId", currentTownId, storeOffers.TownId);
    Require("CanonicalPinecrossServices", "store-offers.generalStore", storeOffers.Offers.Any(offer => offer.VendorType == StoreVendorType.GeneralStore), "expected the starting town to expose a general store.");
    Require("CanonicalPinecrossServices", "store-offers.stable", storeOffers.Offers.Any(offer => offer.VendorType == StoreVendorType.Stable), "expected the starting town to expose a stable.");
}
```

**Step 9: Update BoringScenarioBuilder.cs — remove hardcoded preview destinations**

Remove `PreviewDestinationTownId` from `BoringScenario` record and from all 4 factory methods. The preview destination is discovered dynamically inside `CreateTravelPreview`:

```csharp
internal static class BoringScenarioBuilder
{
    public static BoringScenario MountedTravelReady()
        => new(ScenarioName: "MountedTravelReady", Fixture: ScenarioSeedCatalog.CanonicalMountedStandard);

    public static BoringScenario NoHorseFootTravelReady()
        => new(ScenarioName: "NoHorseFootTravelReady", Fixture: ScenarioSeedCatalog.NoHorseLightEasy);

    public static BoringScenario HighRiskFoeInterruptRoute()
        => new(ScenarioName: "HighRiskFoeInterruptRoute", Fixture: ScenarioSeedCatalog.HighRiskFoeInterruptRoute);

    public static BoringScenario PinecrossServicesOrWantedPosterReady()
        => new(ScenarioName: "PinecrossServicesOrWantedPosterReady", Fixture: ScenarioSeedCatalog.CanonicalPinecrossServices);
}

internal sealed record BoringScenario(
    string ScenarioName,
    ScenarioSeedFixture Fixture)
{
    public string SeedCode => Fixture.SeedCode;
    public GameDifficulty GameDifficulty => Fixture.GameDifficulty;

    public void AssertReady()
        => Fixture.AssertCachedFixtureContract();

    public SetupGameRequest CreateRequest(string playerName)
        => Fixture.CreateRequest(playerName);

    public GameSession CreateSession(string playerName = "Fixture Validator")
    {
        Fixture.AssertCachedFixtureContract();
        return CanonicalStartFlow.StartGame(
            new SeededNewGameFactory(new DeterministicSaltSourceFactory()),
            playerName,
            GameDifficulty,
            SeedCode,
            Fixture.GameEntropy);
    }

    public GameSessionDto CreateSessionDto(string playerName = "Fixture Validator")
        => GameSessionMapper.ToDto(CreateSession(playerName));

    public TravelPreviewResultDto CreateTravelPreview(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Discover the first connected town dynamically
        var connectedTownId = session.World.Trails
            .Where(t => t.FromTownId == session.Player.CurrentTownId || t.ToTownId == session.Player.CurrentTownId)
            .Select(t => t.FromTownId == session.Player.CurrentTownId ? t.ToTownId : t.FromTownId)
            .FirstOrDefault();

        if (connectedTownId is null)
            throw new InvalidOperationException($"Scenario '{ScenarioName}': no connected town found from starting town for travel preview.");

        var previewResult = new TravelResolver().PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            connectedTownId,
            session.Player.Inventory,
            session.TravelRules);

        var preview = new TravelPreviewResultDto(
            previewResult.Success,
            previewResult.Message,
            previewResult.Preview is null ? null : TravelMapper.ToDto(previewResult.Preview, session.TravelRules));

        Fixture.AssertTravelPreview(GameSessionMapper.ToDto(session), connectedTownId.Value, preview);
        return preview;
    }
}
```

**Step 10: Update BoringScenarioBuilderTests.cs — remove town name assertions**

The 4 tests in BoringScenarioBuilderTests call `scenario.AssertReady()` which checks the shape signature. The descriptor updates handle the shape signature changes. But the `HighRiskFoeInterruptRouteUsesTheCachedFixtureAndKeepsTheMountedRouteShape` test has explicit town name assertions — remove them:

```csharp
[Fact]
public void HighRiskFoeInterruptRouteUsesTheCachedFixtureAndKeepsTheMountedRouteShape()
{
    var scenario = BoringScenarioBuilder.HighRiskFoeInterruptRoute();
    scenario.AssertReady();

    var session = scenario.CreateSession();

    // Assert graph properties, not specific town names
    var connectedCount = session.World.Trails
        .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
        .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
        .Distinct()
        .Count();

    Assert.True(connectedCount >= 2, $"expected at least 2 connected towns from starting town, got {connectedCount}");
}
```

**Step 11: Update ScenarioSeedDescriptorTests.cs — update expected shape signature**

The test `TypedDescriptorFormatsAReadableSemanticShape` checks the formatted shape signature. Update it to match the new format (no town names, count-based routes):

```csharp
[Fact]
public void TypedDescriptorFormatsAReadableSemanticShape()
{
    var descriptor = ScenarioSeedDescriptor.Create("CanonicalMountedStandard")
        .WithCodecVersion(new ScenarioSeedCodecVersion("resolver-test"))
        .WithEntropy(GameEntropy.Boring)
        .WithStartingTownRole(ScenarioStartingTownRole.DefaultPlayableStart)
        .WithHorse(HorseCondition.Healthy)
        .WithSaddle(SaddleState.Present)
        .WithWallet(25m)
        .WithItemCount(8)
        .WithTownCount(8)
        .WithPreview(ScenarioPreviewExpectation.Mounted(4, 4));

    var signature = descriptor.FormatRequiredShapeSignature();

    Assert.Equal("resolver-test|CanonicalMountedStandard|entropy=Boring|start=default-playable-start|horse=healthy|saddle=present|wallet=25|items=8|towns=8|preview=mounted:4/4", signature);
}
```

- [x] Implement all changes in ScenarioSeedDescriptor.cs (Step 1)
- [x] Implement changes in ScenarioSeedFixture.cs (Step 2)
- [x] Implement changes in ScenarioSeedCatalog.cs (Steps 3-8)
- [x] Implement changes in BoringScenarioBuilder.cs (Step 9)
- [x] Implement changes in BoringScenarioBuilderTests.cs (Step 10)
- [x] Implement changes in ScenarioSeedDescriptorTests.cs (Step 11)
- [x] Build: `dotnet build tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj`
- [x] Expected: PASS (0 errors)
- [x] Run fixture tests: `.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests --filter "BoringScenarioBuilderTests|ScenarioSeedCatalogTests|ScenarioSeedDescriptorTests" --no-build`
- [x] Expected: PASS
- [x] Commit: `git commit -m "test: redesign scenario fixtures to be content-agnostic, no town names"`

### Task 2: Make GameApiTests Travel Tests Topology-Agnostic

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/GameApiTests.cs`

**What changes:**

GameApiTests has 2 tests with hardcoded topology:

1. `PostGamesReturnsCreatedSession` — asserts "quartzsite" and "emberfall" are connected. Remove specific town assertions, keep dynamic discovery loop.

2. `TravelPreviewStartAndAdvanceFollowTheJourneyLoop` — hardcodes "quartzsite" as destination with specific 5-day values. Dynamically discover first connected town, parameterize the journey loop by actual baseline days.

**Step 1: Fix PostGamesReturnsCreatedSession**

Remove lines 35-36 (specific town assertions). Replace with:
```csharp
Assert.True(connectedTownIds.Length >= 2, $"expected at least 2 connected towns from starting town, got {connectedTownIds.Length}");
```

**Step 2: Fix TravelPreviewStartAndAdvanceFollowTheJourneyLoop**

Replace the hardcoded "quartzsite" destination with dynamic discovery. Replace the 5 hardcoded advance steps with a parameterized loop. Use the preview's actual BaselineRideDays and RideDayDistance for assertions. Use the discovered town's name for diary narration checks.

The full replacement code for this test is large. The key changes:
- Discover `destinationTownId` and `destinationTownName` from `createdSession.World.Trails`
- Get preview for `destinationTownId` (not "quartzsite")
- Use `expectedDays = preview.Preview.BaselineRideDays` and `rideDayDistance = preview.Preview.RideDayDistance` for all assertions
- Replace the 5 hardcoded advance steps with a loop: `for (var day = 1; day < expectedDays; day++)`
- Final advance completes the journey, arriving at `destinationTownId`
- Diary narration check uses `destinationTownName` instead of "Quartzsite"

- [x] Implement changes in GameApiTests.cs
- [x] Build: `dotnet build tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj`
- [x] Expected: PASS
- [x] Run: `.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests --filter "GameApiTests" --no-build`
- [x] Expected: PASS -- all 7 GameApiTests pass
- [x] Commit: `git commit -m "test: make GameApiTests travel tests topology-agnostic"`

### Task 3: Remove Town Names from All Remaining Integration Tests

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/StartingTownMapEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiJournalTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiWantedPostersTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiStoreOffersTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiPurchaseTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiValidationTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevTravelEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevSaloonEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Acceptance/SaloonConfrontationAcceptanceTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Acceptance/StorePurchaseAcceptanceTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Acceptance/WantedPosterAcceptanceTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/AcceptanceTestHarness.cs`
- Modify: `tests/WildBunch.Integration.Tests/WorldMapEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/ProjectionEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiArchiveTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiInvestigationActionsTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiActionsTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/OneActivePlaythroughInvariantTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevEndpointTests.cs`

**What changes:**

Search for ALL hardcoded town names in integration tests and replace with dynamic discovery. The pattern is:

1. **Travel destinations:** Replace hardcoded "quartzsite" with dynamically discovered first connected town from the session's world.
2. **Town ID assertions:** Replace `Assert.Contains("hardpan", townIds)` with property checks (count, uniqueness, non-empty).
3. **Store-offers queries:** Replace hardcoded town ID in URL with `session.Player.CurrentTownId`.
4. **Diary narration checks:** Replace `Assert.Contains("Quartzsite", ...)` with `Assert.Contains(destinationTownName, ...)`.
5. **SeededTownIds array:** Remove the hardcoded array, assert on properties instead.

**Step 1: StartingTownMapEndpointTests.cs — remove SeededTownIds array**

Remove the `SeededTownIds` array (lines 14-24). Update `GetStartingTownMapReturnsAllEightSeededTowns` to assert on properties:
```csharp
[Fact]
public async Task GetStartingTownMapReturnsAllEightTowns()
{
    using var factory = new PostgreSqlApiFactory();
    using var client = factory.CreateClient();
    var sessionId = await CreateSessionAsync(client);

    var response = await client.GetAsync($"/api/games/{sessionId}/starting-town-map");
    var map = await response.Content.ReadFromJsonAsync<StartingTownMapDto>();

    Assert.NotNull(map);
    var townIds = map!.Towns.Select(town => town.Id).ToArray();
    Assert.Equal(8, townIds.Length);
    Assert.Equal(townIds.Length, townIds.Distinct().Count());
    Assert.All(townIds, id => Assert.False(string.IsNullOrWhiteSpace(id)));
}
```

**Step 2: Search and replace all remaining hardcoded town names**

Run: `rg -n "hardpan|quartzsite|emberfall|boulderwash|openpass|holloway|rattleridge|brokenarrow" tests/WildBunch.Integration.Tests/ --glob "*.cs"`

For each match:
- If it's a travel destination, replace with dynamic discovery from the session's world
- If it's a town ID assertion, replace with property checks
- If it's a store-offers query, use `session.Player.CurrentTownId`
- If it's a diary narration check, use the discovered town name

**Step 3: Update AcceptanceTestHarness.cs**

The `SeedCanonicalSessionAsync` method may reference specific town names. Update to use dynamic discovery.

- [x] Update StartingTownMapEndpointTests.cs (Step 1)
- [x] Search and update all remaining files (Step 2)
- [x] Update AcceptanceTestHarness.cs (Step 3)
- [x] Build: `dotnet build tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj`
- [x] Expected: PASS
- [x] Run affected tests: `.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests --no-build`
- [x] Expected: As many tests as possible pass — some may still fail and be fixed in Task 4
- [x] Commit: `git commit -m "test: remove all hardcoded town names from integration tests"`

### Task 4: Run Full Integration Suite and Fix Remaining Failures

**Files:**
- Modify: (whatever files have remaining failures)

- [x] Run full integration suite: `.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests --no-build`
- [x] Expected: All tests pass (0 failed, 0 skipped)
- [x] If any tests fail, investigate and fix them
- [x] Re-run until all pass
- [x] Run Domain.Tests: `dotnet test tests/WildBunch.Domain.Tests/ --no-build`
- [x] Expected: 526 passed, 1 skipped
- [x] Run Application.Tests: `dotnet test tests/WildBunch.Application.Tests/ --no-build`
- [x] Expected: 204 passed
- [x] Run GameContent.Tests: `dotnet test tests/WildBunch.GameContent.Tests/ --no-build`
- [x] Expected: 141 passed
- [x] Commit any remaining fixes: `git commit -m "test: fix remaining integration test failures"`
- [x] Final full-suite run: `.\scripts\postgres-dev.ps1 test -- tests/WildBunch.Integration.Tests --no-build`
- [x] Expected: All tests pass
- [x] Verify zero town name references remain: `rg -n "hardpan|quartzsite|emberfall|boulderwash|openpass|holloway|rattleridge|brokenarrow" tests/WildBunch.Integration.Tests/ --glob "*.cs"`
- [x] Expected: zero matches (excluding direct-creation tests that use their own fixture data)

## Definition of Done

- [x] All 4 scenario fixtures are content-agnostic (no town names in descriptors, shape signatures, or assertions)
- [x] Graph-property assertions added (connectivity, positive coordinates, 2-6 day distances)
- [x] Travel preview destinations discovered dynamically (not hardcoded)
- [x] GameApiTests travel tests are topology-agnostic
- [x] Zero hardcoded town name assertions in integration tests (excluding direct-creation fixture data)
- [x] Full integration suite passes (0 failed)
- [x] Domain.Tests (526+1skip), Application.Tests (204), GameContent.Tests (141) all pass
- [x] No regression in any test suite
