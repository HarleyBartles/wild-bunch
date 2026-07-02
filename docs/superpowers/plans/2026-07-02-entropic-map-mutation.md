# Entropic Map Mutation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement entropic map variation with hidden outlier slots, replacing the complex outlier removal logic with simple slot activation and redesigning layouts for better mutation support.

**Architecture:** Separate layout variety (trail removal) from outlier creation (hidden slot activation). Redesign 8 mutation-friendly layouts with built-in redundancy. Remove complex budget accounting and outlier selection logic.

**Tech Stack:** C#/.NET 10, xUnit, existing seed codec system

## Global Constraints

- Seed bit expansion: add 1 bit for "has outlier slot" (from 104 reserved bits)
- Town count: seed-encoded base count + 1 if outlier slot activated
- Trail distances: normal trails 2-5 days, outlier trail exactly 6 days
- Connectivity: all towns must remain reachable after trail removal
- No crossing trails: trails only meet at towns
- Deterministic: same seed + same entropy = same result
- Playability: maintain over all else

---

## File Structure

**Files to modify:**
- `src/WildBunch.GameContent/NewGame/SeedWorld.cs` - Add HasOutlierSlot property
- `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs` - Update seed encoding/decoding for outlier bit
- `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs` - Redesign layout palette and trail definitions
- `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs` - Simplify to remove complex outlier logic, add outlier slot activation
- `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs` - Update tests for new layouts and outlier behavior
- `tests/WildBunch.GameContent.Tests/GeometryCanonicalDistanceTests.cs` - Update tests for simplified outlier logic

**Files to create:**
- `src/WildBunch.GameContent/NewGame/OutlierNamePool.cs` - Remote-themed outlier name pool (deferred: use regular pool for now)

---

### Task 1: Add HasOutlierSlot Property to SeedWorld

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorld.cs`

**Interfaces:**
- Consumes: None
- Produces: Updated SeedWorld record with HasOutlierSlot property

- [ ] **Step 1: Add HasOutlierSlot property to SeedWorld record**

```csharp
public sealed record SeedWorld(
    Guid SeedCode,
    SeedWorldVariant WorldVariant,
    int TownCount,
    int AccusationIndex,
    int DefaultCulpritIndex,
    int CashBonus,
    ProsperityPalette ProsperityPalette,
    ServicesPalette ServicesPalette,
    MapLayoutPalette MapLayoutPalette,
    bool HasOutlierSlot) // New property
{
    public string SeedCodeText => SeedCode.ToString("D");
}
```

- [ ] **Step 2: Update canonical seed world creation to set HasOutlierSlot to false**

```csharp
private static SeedWorld CreateCanonicalSeedWorldShape()
{
    var variant = SeedWorldVariant.Canonical;
    var townCount = 8;
    var accusationIndex = 1;
    var defaultCulpritIndex = 3;
    var cashBonus = 0;
    var prosperityPalette = ProsperityPalette.UniformProsperous;
    var servicesPalette = ServicesPalette.HubTelegraph;
    var mapLayoutPalette = MapLayoutPalette.HubAndSpoke;
    var hasOutlierSlot = false; // New parameter

    // ... rest of method
    return new SeedWorld(
        seedCode, variant, townCount, accusationIndex, defaultCulpritIndex,
        cashBonus, prosperityPalette, servicesPalette, mapLayoutPalette,
        hasOutlierSlot, // Add to constructor call
        selectedTownIds, townServices, townProsperity, trails);
}
```

- [ ] **Step 3: Run existing tests to verify no breaking changes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --configuration Release`
Expected: All tests pass (new property has default value)

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorld.cs
git commit -m "feat: add HasOutlierSlot property to SeedWorld"
```

---

### Task 2: Update Seed Encoding/Decoding for Outlier Bit

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs`

**Interfaces:**
- Consumes: SeedWorld with HasOutlierSlot property
- Produces: Updated CreateRepresentativeSeedCode to encode outlier bit

- [ ] **Step 1: Update CreateRepresentativeSeedCode to encode HasOutlierSlot bit**

```csharp
public static Guid CreateRepresentativeSeedCode(SeedWorld seedWorld)
{
    ArgumentNullException.ThrowIfNull(seedWorld);

    var validation = Validate(seedWorld);
    if (!validation.Success)
    {
        throw new ArgumentException(validation.ErrorMessage ?? "Seed world is invalid.", nameof(seedWorld));
    }

    ulong low = 0;
    low |= (ulong)(uint)((int)seedWorld.WorldVariant & 0x3);
    low |= (ulong)(seedWorld.AccusationIndex & 0xF) << 2;
    low |= (ulong)(seedWorld.DefaultCulpritIndex & 0xF) << 6;
    low |= (ulong)(seedWorld.CashBonus & 0xF) << 10;
    low |= (ulong)((seedWorld.TownCount - TownCountOffset) & 0xF) << 14;
    low |= (ulong)((int)seedWorld.ProsperityPalette & 0x7) << 18;
    low |= (ulong)((int)seedWorld.ServicesPalette & 0x7) << 21;
    low |= (ulong)((int)seedWorld.MapLayoutPalette & 0x7) << 24;
    low |= (ulong)(seedWorld.HasOutlierSlot ? 1u : 0u) << 27; // New bit at position 27

    ulong high = 0UL;

    var bytes = new byte[16];
    BitConverter.TryWriteBytes(bytes.AsSpan(0), low);
    BitConverter.TryWriteBytes(bytes.AsSpan(8), high);
    return new Guid(bytes);
}
```

- [ ] **Step 2: Update Resolve to decode HasOutlierSlot bit**

```csharp
private static SeedWorld Resolve(Guid seedCode)
{
    var bytes = seedCode.ToByteArray();
    var low = BitConverter.ToUInt64(bytes, 0);
    var high = BitConverter.ToUInt64(bytes, 8);

    var variant = (SeedWorldVariant)(low & 0x3);
    var accusationIndex = (int)((low >> 2) & 0xF);
    var defaultCulpritIndex = (int)((low >> 6) & 0xF);
    var cashBonus = (int)((low >> 10) & 0xF);
    var townCount = (int)((low >> 14) & 0xF) + TownCountOffset;
    var prosperityPalette = (ProsperityPalette)((low >> 18) & 0x7);
    var servicesPalette = (ServicesPalette)((low >> 21) & 0x7);
    var mapLayoutPalette = (MapLayoutPalette)((low >> 24) & 0x7);
    var hasOutlierSlot = ((low >> 27) & 0x1) == 1; // New bit decoding

    // ... rest of method
    return new SeedWorld(
        seedCode, variant, townCount, accusationIndex, defaultCulpritIndex,
        cashBonus, prosperityPalette, servicesPalette, mapLayoutPalette,
        hasOutlierSlot, // Add to constructor call
        selectedTownIds, townServices, townProsperity, trails);
}
```

- [ ] **Step 3: Update Validate to check HasOutlierSlot is within valid range**

```csharp
private static (bool Success, string? ErrorMessage) Validate(SeedWorld seedWorld)
{
    // ... existing validation ...
    
    if (seedWorld.TownCount < MinTownCount || seedWorld.TownCount > MaxTownCount)
        return (false, $"Town count must be between {MinTownCount} and {MaxTownCount}, got {seedWorld.TownCount}");

    // Add validation for HasOutlierSlot
    if (seedWorld.HasOutlierSlot && seedWorld.TownCount >= MaxTownCount)
        return (false, "Cannot have outlier slot when town count is at maximum");

    return (true, null);
}
```

- [ ] **Step 4: Run tests to verify encoding/decoding round-trip**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --configuration Release --filter "SeedWorldResolver"`
Expected: All seed resolver tests pass

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs
git commit -m "feat: encode/decode HasOutlierSlot bit in seed codec"
```

---

### Task 3: Redesign MapLayoutPalette Enum

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs`

**Interfaces:**
- Consumes: None
- Produces: Updated MapLayoutPalette enum with 8 new layouts

- [ ] **Step 1: Replace MapLayoutPalette enum with 8 new layouts**

```csharp
/// <summary>
/// Map layout palette defines how towns are positioned and connected.
/// All layouts are designed with redundancy to support trail removal while maintaining connectivity.
/// Trails only meet at towns - no crossing trails between towns.
/// </summary>
public enum MapLayoutPalette
{
    HubAndSpoke = 0,        // Central hub with outer ring towns connected via spokes
    DoubleLine = 1,          // Two parallel lines of towns, connected at endpoints
    XShaped = 2,             // Four arms meeting at central town, each arm is a line of towns
    Tree = 3,                // Hierarchical structure with main trunk and branches
    Star = 4,                // Central hub with many dead-end spokes
    Cluster = 5,             // Multiple mini-hubs (2-3 towns each) connected together
    Mesh = 6,                // Fully connected network with lots of redundancy
    Grid = 7                 // 2D grid structure (3x3 max) with trails along grid lines
}
```

- [ ] **Step 2: Update enum references in code to use new layout names**

Search and replace in `SeedWorldCatalog.cs`:
- `MapLayoutPalette.Ring` → remove (no longer exists)
- `MapLayoutPalette.LinearChain` → remove (no longer exists)
- `MapLayoutPalette.DoubleLine` → keep (same name)
- `MapLayoutPalette.HubAndSpoke` → keep (same name)

- [ ] **Step 3: Run tests to verify enum changes don't break existing code**

Run: `dotnet build src/WildBunch.GameContent/WildBunch.GameContent.csproj --configuration Release`
Expected: Build succeeds (enum changes are backward compatible for existing layouts)

- [ ] **Step 4: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs
git commit -m "refactor: redesign MapLayoutPalette with 8 mutation-friendly layouts"
```

---

### Task 4: Implement New Layout Trail Definitions

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs`

**Interfaces:**
- Consumes: Updated MapLayoutPalette enum
- Produces: Trail definitions for 8 new layouts

- [ ] **Step 1: Add trail definitions for XShaped layout**

```csharp
// In BuildTrails method, add XShaped case:
MapLayoutPalette.XShaped => new[]
{
    // Central hub (slot 0) to four arms
    new SlotTrailDefinition(0, 1, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 2, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 3, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 4, TrailRisk.Low, canonical, variant),
    // Arm extensions (if town count > 5)
    new SlotTrailDefinition(1, 5, TrailRisk.Moderate, canonical, variant),
    new SlotTrailDefinition(2, 6, TrailRisk.Moderate, canonical, variant),
    new SlotTrailDefinition(3, 7, TrailRisk.Moderate, canonical, variant),
    new SlotTrailDefinition(4, 8, TrailRisk.Moderate, canonical, variant),
}
```

- [ ] **Step 2: Add trail definitions for Tree layout**

```csharp
MapLayoutPalette.Tree => new[]
{
    // Main trunk
    new SlotTrailDefinition(0, 1, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(1, 2, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(2, 3, TrailRisk.Moderate, canonical, variant),
    // Branches from trunk
    new SlotTrailDefinition(1, 4, TrailRisk.Moderate, canonical, variant),
    new SlotTrailDefinition(2, 5, TrailRisk.Moderate, canonical, variant),
    new SlotTrailDefinition(3, 6, TrailRisk.Moderate, canonical, variant),
}
```

- [ ] **Step 3: Add trail definitions for Star layout**

```csharp
MapLayoutPalette.Star => new[]
{
    // Central hub (slot 0) to all other towns
    new SlotTrailDefinition(0, 1, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 2, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 3, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 4, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 5, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 6, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 7, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(0, 8, TrailRisk.Low, canonical, variant),
}
```

- [ ] **Step 4: Add trail definitions for Cluster layout**

```csharp
MapLayoutPalette.Cluster => new[]
{
    // Mini-hub groups (0-1, 2-3, 4-5)
    new SlotTrailDefinition(0, 1, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(2, 3, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(4, 5, TrailRisk.Low, canonical, variant),
    // Inter-cluster connections
    new SlotTrailDefinition(1, 2, TrailRisk.Moderate, canonical, variant),
    new SlotTrailDefinition(3, 4, TrailRisk.Moderate, canonical, variant),
    new SlotTrailDefinition(5, 0, TrailRisk.Moderate, canonical, variant),
}
```

- [ ] **Step 5: Add trail definitions for Mesh layout**

```csharp
MapLayoutPalette.Mesh => Enumerable.Range(0, 9)
    .SelectMany(i => Enumerable.Range(i + 1, 9 - i)
        .Select(j => new SlotTrailDefinition(i, j, TrailRisk.Low, canonical, variant)))
    .ToArray()
```

- [ ] **Step 6: Add trail definitions for Grid layout**

```csharp
MapLayoutPalette.Grid => new[]
{
    // 3x3 grid: rows and columns
    // Row 0
    new SlotTrailDefinition(0, 1, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(1, 2, TrailRisk.Low, canonical, variant),
    // Row 1
    new SlotTrailDefinition(3, 4, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(4, 5, TrailRisk.Low, canonical, variant),
    // Row 2
    new SlotTrailDefinition(6, 7, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(7, 8, TrailRisk.Low, canonical, variant),
    // Columns
    new SlotTrailDefinition(0, 3, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(3, 6, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(1, 4, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(4, 7, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(2, 5, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(5, 8, TrailRisk.Low, canonical, variant),
}
```

- [ ] **Step 7: Remove Ring and LinearChain cases from BuildTrails**

Delete the cases for `MapLayoutPalette.Ring` and `MapLayoutPalette.LinearChain`.

- [ ] **Step 8: Update DoubleLine to remove crossing trails**

```csharp
MapLayoutPalette.DoubleLine => new[]
{
    // Line 1: 0-1-2
    new SlotTrailDefinition(0, 1, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(1, 2, TrailRisk.Low, canonical, variant),
    // Line 2: 3-4-5
    new SlotTrailDefinition(3, 4, TrailRisk.Low, canonical, variant),
    new SlotTrailDefinition(4, 5, TrailRisk.Low, canonical, variant),
    // Connections between lines (at endpoints only)
    new SlotTrailDefinition(2, 3, TrailRisk.Moderate, canonical, variant),
    new SlotTrailDefinition(0, 5, TrailRisk.Moderate, canonical, variant),
}
```

- [ ] **Step 9: Run tests to verify new layout definitions**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --configuration Release --filter "SeedWorldCatalog"`
Expected: All catalog tests pass

- [ ] **Step 10: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldCatalog.cs
git commit -m "feat: add trail definitions for 8 new layouts"
```

---

### Task 5: Simplify SeedWorldBuilder - Remove Complex Outlier Logic

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`

**Interfaces:**
- Consumes: Updated SeedWorld with HasOutlierSlot
- Produces: Simplified builder without complex outlier removal

- [ ] **Step 1: Remove ApplyOutlierClamping method entirely**

Delete the entire `ApplyOutlierClamping` method and its call from `DeriveDistancesAndAdjustCoordinates`.

- [ ] **Step 2: Remove VerifyOutlierInvariant method entirely**

Delete the entire `VerifyOutlierInvariant` method.

- [ ] **Step 3: Simplify DeriveDistancesAndAdjustCoordinates to remove outlier phase**

```csharp
private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, Dictionary<int, (int X, int Y)> AdjustedCoordinates, int? OutlierSlot) DeriveDistancesAndAdjustCoordinates(
    IReadOnlyList<SeedWorldTrail> trails,
    Dictionary<int, (int X, int Y)> townCoordinates,
    MapLayoutPalette layout,
    GameEntropy entropy,
    GameSetupDeterministicSource source,
    SaltSource? saltSource)
{
    // Pass 1: Derive raw distances from geometry
    var trailsWithRawDistances = trails.Select(trail =>
    {
        var parts = trail.Id.Split('-');
        var fromSlot = int.Parse(parts[1]);
        var toSlot = int.Parse(parts[2]);
        var fromCoords = townCoordinates[fromSlot];
        var toCoords = townCoordinates[toSlot];
        var dx = toCoords.X - fromCoords.X;
        var dy = toCoords.Y - fromCoords.Y;
        var coordinateDistance = Math.Sqrt(dx * dx + dy * dy);
        var rawRideDays = Math.Round(coordinateDistance / CoordinateScale, 1);
        var clampedRaw = Math.Max(MinDays, Math.Min(MaxDays, (decimal)rawRideDays));
        return (trail, clampedRaw);
    }).ToArray();

    // Clamp all 6-day trails to 5 days (no outlier special case here)
    var trailsAfterClamp = trailsWithRawDistances.Select(t =>
    {
        var distance = t.clampedRaw == 6m ? 5m : t.clampedRaw;
        return t.trail with { RideDayDistance = distance };
    }).ToArray();

    // Apply layout-specific trail removal by salt
    var (trailsAfterRemoval, _) = ApplyLayoutSpecificTrailRemoval(trailsAfterClamp, layout, entropy, source, saltSource, townCoordinates.Count, null);

    // Pass 2: Adjust coordinates to match final ride days
    var adjustedCoordinates = AdjustCoordinatesToMatchRideDays(trailsAfterRemoval, townCoordinates, CoordinateScale, MinDays, MaxDays, entropy);

    return (trailsAfterRemoval, adjustedCoordinates, null);
}
```

- [ ] **Step 4: Remove layout-specific outlier protection from SelectRandomTrails**

Simplify `SelectRandomTrails` to not take `outlierSlot` parameter:

```csharp
private static List<SeedWorldTrail> SelectRandomTrails(
    List<SeedWorldTrail> trails,
    int count,
    Random random)
{
    if (count == 0 || trails.Count == 0)
        return new List<SeedWorldTrail>();

    var result = new List<SeedWorldTrail>();
    var available = trails.ToList();

    while (result.Count < count && available.Count > 0)
    {
        var index = random.Next(available.Count);
        result.Add(available[index]);
        available.RemoveAt(index);
    }

    return result;
}
```

- [ ] **Step 5: Update all ApplyLayoutSpecificTrailRemoval calls to pass null for outlierSlot**

Update calls in `ApplyHubAndSpokeTrailRemoval`, `ApplyRingTrailRemoval`, `ApplyDoubleLineTrailRemoval` to pass `null` instead of `outlierSlot`.

- [ ] **Step 6: Run tests to verify simplified builder works**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --configuration Release --filter "SeedWorldBuilder"`
Expected: Tests may fail (expected - we removed outlier logic, will add back in next task)

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs
git commit -m "refactor: remove complex outlier removal logic from SeedWorldBuilder"
```

---

### Task 6: Implement Hidden Outlier Slot Activation

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`

**Interfaces:**
- Consumes: SeedWorld with HasOutlierSlot
- Produces: Outlier slot activation logic

- [ ] **Step 1: Add ActivateOutlierSlot method**

```csharp
private static (IReadOnlyList<SeedWorldTrail> Trails, int? OutlierSlot) ActivateOutlierSlot(
    IReadOnlyList<SeedWorldTrail> trails,
    Dictionary<int, (int X, int Y)> townCoordinates,
    GameSetupDeterministicSource source,
    SaltSource? saltSource,
    GameEntropy entropy,
    int outlierSlotIndex)
{
    // Determine if outlier should be activated based on entropy
    var shouldActivate = entropy switch
    {
        GameEntropy.Boring => false,
        GameEntropy.Classic => true, // If HasOutlierSlot is true, activate
        GameEntropy.Adventurous => true,
        GameEntropy.Wild => true,
        _ => false
    };

    if (!shouldActivate)
        return (trails, null);

    // Select connection target using deterministic hash
    var connectionTargetSlot = SelectOutlierConnectionTarget(townCoordinates, source, saltSource, entropy);

    // Create outlier town coordinates (6 days away from target)
    var targetCoords = townCoordinates[connectionTargetSlot];
    var angle = ComputeStableHash(source.SeedCode, "outlier-angle", entropy.ToString(), saltSource?.Salt ?? "default") % 360;
    var angleRad = angle * Math.PI / 180;
    var outlierX = targetCoords.X + (int)(6 * CoordinateScale * Math.Cos(angleRad));
    var outlierY = targetCoords.Y + (int)(6 * CoordinateScale * Math.Sin(angleRad));

    townCoordinates[outlierSlotIndex] = (outlierX, outlierY);

    // Create outlier trail
    var targetTownId = new TownId(connectionTargetSlot.ToString());
    var outlierTownId = new TownId(outlierSlotIndex.ToString());
    var outlierTrail = new SeedWorldTrail(
        $"trail-{connectionTargetSlot}-{outlierSlotIndex}",
        targetTownId,
        outlierTownId,
        TrailRisk.High,
        TrailTerrain.Mountain,
        WaterFeature.None,
        6m); // Exactly 6 days

    var result = new List<SeedWorldTrail>(trails) { outlierTrail };
    return (result, outlierSlotIndex);
}

private static int SelectOutlierConnectionTarget(
    Dictionary<int, (int X, int Y)> townCoordinates,
    GameSetupDeterministicSource source,
    SaltSource? saltSource,
    GameEntropy entropy)
{
    var slots = townCoordinates.Keys.ToList();
    var hash = ComputeStableHash(source.SeedCode, "outlier-target", entropy.ToString(), saltSource?.Salt ?? "default");
    return slots[Math.Abs(hash) % slots.Count];
}
```

- [ ] **Step 2: Update CreateWorld to call ActivateOutlierSlot**

```csharp
public static SeedWorld CreateWorld(SeedWorld seedWorld, GameSetupDeterministicSource source, GameEntropy entropy, SaltSource? saltSource)
{
    // ... existing code up to DeriveDistancesAndAdjustCoordinates ...

    var (trails, adjustedCoordinates, _) = DeriveDistancesAndAdjustCoordinates(
        trails, townCoordinates, seedWorld.MapLayoutPalette, entropy, source, saltSource);

    // Activate outlier slot if needed
    var outlierSlotIndex = seedWorld.TownCount; // Outlier is at the next slot
    var (finalTrails, outlierSlot) = seedWorld.HasOutlierSlot
        ? ActivateOutlierSlot(trails, adjustedCoordinates, source, saltSource, entropy, outlierSlotIndex)
        : (trails, (int?)null);

    // Convert slot-based coordinates to TownId-based coordinates
    var townIdCoordinates = adjustedCoordinates.ToDictionary(
        kvp => new TownId(kvp.Key.ToString()),
        kvp => (kvp.Value.X, kvp.Value.Y));

    // ... rest of method
}
```

- [ ] **Step 3: Update CreateWorld to handle outlier town name**

```csharp
// In CreateWorld, after getting town names:
var townNames = SeedWorldCatalog.DeriveTownNames(
    seedWorld.WorldVariant,
    seedWorld.HasOutlierSlot ? seedWorld.TownCount + 1 : seedWorld.TownCount, // Include outlier slot
    seedWorld.AccusationIndex,
    seedWorld.DefaultCulpritIndex,
    seedWorld.CashBonus,
    seedWorld.ProsperityPalette,
    seedWorld.ServicesPalette,
    seedWorld.MapLayoutPalette);
```

- [ ] **Step 4: Update CreateWorld to mark outlier town with IsOutlier**

```csharp
// When building Town records:
var towns = townNames.Select((name, i) =>
{
    var isOutlier = outlierSlot.HasValue && i == outlierSlot.Value;
    return new Town(
        name.Id,
        name.Name,
        townIdCoordinates[name.Id],
        name.Prosperity,
        townServices[name.Id],
        isOutlier); // Add IsOutlier parameter
}).ToList();
```

- [ ] **Step 5: Run tests to verify outlier activation works**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --configuration Release --filter "SeedWorldBuilder"`
Expected: Tests may need updates for new outlier behavior

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs
git commit -m "feat: implement hidden outlier slot activation"
```

---

### Task 7: Simplify Layout-Specific Trail Removal

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs`

**Interfaces:**
- Consumes: Simplified builder without outlier logic
- Produces: Simple trail removal based on entropy level

- [ ] **Step 1: Simplify ApplyHubAndSpokeTrailRemoval**

```csharp
private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyHubAndSpokeTrailRemoval(
    IReadOnlyList<SeedWorldTrail> trails,
    GameEntropy entropy,
    GameSetupDeterministicSource source,
    SaltSource? saltSource,
    int townCount,
    int? outlierSlot)
{
    var spokes = trails.Where(t => t.Id.StartsWith("trail-0-")).ToList();
    var edgeTrails = trails.Where(t => !t.Id.StartsWith("trail-0-")).ToList();

    var trailsToRemove = entropy switch
    {
        GameEntropy.Boring => 0,
        GameEntropy.Classic => 1,
        GameEntropy.Adventurous => 2,
        GameEntropy.Wild => 3,
        _ => 0
    };

    if (trailsToRemove == 0)
        return (trails, null);

    if (saltSource == null)
        return (trails, null);

    var salt = saltSource.Salt;
    var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));

    // Split removal between spokes and edges
    var spokesToRemove = Math.Min(trailsToRemove / 2, spokes.Count - 1);
    var edgesToRemove = trailsToRemove - spokesToRemove;

    var spokesToRemoveList = SelectRandomTrails(spokes, spokesToRemove, random);
    var edgesToRemoveList = SelectRandomTrails(edgeTrails, edgesToRemove, random);

    var removedIds = new HashSet<string>(spokesToRemoveList.Concat(edgesToRemoveList).Select(t => t.Id));
    var result = trails.Where(t => !removedIds.Contains(t.Id)).ToList();

    if (!VerifyConnectivity(townCount, result))
        return (trails, null);

    return (result, null);
}
```

- [ ] **Step 2: Remove ApplyRingTrailRemoval entirely**

Delete the entire method (Ring layout no longer exists).

- [ ] **Step 3: Simplify ApplyDoubleLineTrailRemoval**

```csharp
private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyDoubleLineTrailRemoval(
    IReadOnlyList<SeedWorldTrail> trails,
    GameEntropy entropy,
    GameSetupDeterministicSource source,
    SaltSource? saltSource,
    int townCount,
    int? outlierSlot)
{
    var trailsToRemove = entropy switch
    {
        GameEntropy.Boring => 0,
        GameEntropy.Classic => 1,
        GameEntropy.Adventurous => 2,
        GameEntropy.Wild => 3,
        _ => 0
    };

    if (trailsToRemove == 0 || trails.Count <= 4)
        return (trails, null);

    if (saltSource == null)
        return (trails, null);

    var salt = saltSource.Salt;
    var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));

    var trailsToRemoveList = SelectRandomTrails(trails.ToList(), trailsToRemove, random);
    var removedIds = new HashSet<string>(trailsToRemoveList.Select(t => t.Id));
    var result = trails.Where(t => !removedIds.Contains(t.Id)).ToList();

    if (!VerifyConnectivity(townCount, result))
        return (trails, null);

    return (result, null);
}
```

- [ ] **Step 4: Add simple removal methods for new layouts**

```csharp
private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyXShapedTrailRemoval(
    IReadOnlyList<SeedWorldTrail> trails,
    GameEntropy entropy,
    GameSetupDeterministicSource source,
    SaltSource? saltSource,
    int townCount,
    int? outlierSlot)
{
    var trailsToRemove = entropy switch
    {
        GameEntropy.Boring => 0,
        GameEntropy.Classic => 1,
        GameEntropy.Adventurous => 2,
        GameEntropy.Wild => 3,
        _ => 0
    };

    if (trailsToRemove == 0)
        return (trails, null);

    if (saltSource == null)
        return (trails, null);

    var salt = saltSource.Salt;
    var random = new Random(ComputeStableHash(source.SeedCode, entropy.ToString(), salt));

    var trailsToRemoveList = SelectRandomTrails(trails.ToList(), trailsToRemove, random);
    var removedIds = new HashSet<string>(trailsToRemoveList.Select(t => t.Id));
    var result = trails.Where(t => !removedIds.Contains(t.Id)).ToList();

    if (!VerifyConnectivity(townCount, result))
        return (trails, null);

    return (result, null);
}

// Similar methods for Tree, Star, Cluster, Mesh, Grid...
```

- [ ] **Step 5: Update ApplyLayoutSpecificTrailRemoval to handle new layouts**

```csharp
private static (IReadOnlyList<SeedWorldTrail> TrimmedTrails, int? OutlierSlot) ApplyLayoutSpecificTrailRemoval(
    IReadOnlyList<SeedWorldTrail> trails,
    MapLayoutPalette layout,
    GameEntropy entropy,
    GameSetupDeterministicSource source,
    SaltSource? saltSource,
    int townCount,
    int? outlierSlot)
{
    return layout switch
    {
        MapLayoutPalette.HubAndSpoke => ApplyHubAndSpokeTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
        MapLayoutPalette.DoubleLine => ApplyDoubleLineTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
        MapLayoutPalette.XShaped => ApplyXShapedTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
        MapLayoutPalette.Tree => ApplyTreeTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
        MapLayoutPalette.Star => ApplyStarTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
        MapLayoutPalette.Cluster => ApplyClusterTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
        MapLayoutPalette.Mesh => ApplyMeshTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
        MapLayoutPalette.Grid => ApplyGridTrailRemoval(trails, entropy, source, saltSource, townCount, outlierSlot),
        _ => (trails, outlierSlot)
    };
}
```

- [ ] **Step 6: Run tests to verify simplified removal works**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --configuration Release --filter "SeedWorldBuilder"`
Expected: Tests may need updates for new layouts

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldBuilder.cs
git commit -m "refactor: simplify layout-specific trail removal"
```

---

### Task 8: Update Tests for New Layouts and Outlier Behavior

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/SeedWorldBuilderTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/GeometryCanonicalDistanceTests.cs`

**Interfaces:**
- Consumes: Updated builder and layouts
- Produces: Updated tests for new behavior

- [ ] **Step 1: Update SeedWorldBuilderTests to test new layouts**

Add tests for XShaped, Tree, Star, Cluster, Mesh, Grid layouts. Remove tests for Ring and LinearChain.

- [ ] **Step 2: Update GeometryCanonicalDistanceTests for simplified outlier logic**

Remove tests that expect complex outlier selection. Add tests for outlier slot activation.

- [ ] **Step 3: Add test for outlier slot activation by entropy**

```csharp
[Fact]
public void OutlierSlot_ActivatesBasedOnEntropy()
{
    var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
    var seedWorldWithOutlier = seedWorld with { HasOutlierSlot = true };
    var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorldWithOutlier.SeedCode));

    // Boring should not activate outlier
    var boringWorld = SeedWorldBuilder.CreateWorld(seedWorldWithOutlier, source, GameEntropy.Boring);
    Assert.Equal(seedWorld.TownCount, boringWorld.Towns.Count);

    // Wild should activate outlier
    var wildWorld = SeedWorldBuilder.CreateWorld(seedWorldWithOutlier, source, GameEntropy.Wild);
    Assert.Equal(seedWorld.TownCount + 1, wildWorld.Towns.Count);
    Assert.Single(wildWorld.Towns.Where(t => t.IsOutlier));
}
```

- [ ] **Step 4: Add test for outlier trail being exactly 6 days**

```csharp
[Fact]
public void OutlierTrail_IsExactly6Days()
{
    var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
    var seedWorldWithOutlier = seedWorld with { HasOutlierSlot = true };
    var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorldWithOutlier.SeedCode));

    var wildWorld = SeedWorldBuilder.CreateWorld(seedWorldWithOutlier, source, GameEntropy.Wild);
    var outlier = wildWorld.Towns.First(t => t.IsOutlier);
    var outlierTrail = wildWorld.Trails.First(t => t.FromTownId == outlier.Id || t.ToTownId == outlier.Id);

    Assert.Equal(6m, outlierTrail.RideDayDistance);
}
```

- [ ] **Step 5: Add test for normal trails being 2-5 days**

```csharp
[Fact]
public void NormalTrails_Are2To5Days()
{
    var seedWorld = SeedWorldResolver.CreateCanonicalSeedWorld();
    var source = new GameSetupDeterministicSource(SeedWorldResolver.FormatSeedCode(seedWorld.SeedCode));

    var wildWorld = SeedWorldBuilder.CreateWorld(seedWorld, source, GameEntropy.Wild);
    var normalTrails = wildWorld.Trails.Where(t => !wildWorld.Towns.Any(town => t.IsOutlier && (t.FromTownId == town.Id || t.ToTownId == town.Id)));

    Assert.All(normalTrails, t => Assert.InRange(t.RideDayDistance, 2m, 5m));
}
```

- [ ] **Step 6: Run all tests to verify updates**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --configuration Release`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add tests/WildBunch.GameContent.Tests/
git commit -m "test: update tests for new layouts and simplified outlier logic"
```

---

### Task 9: Update Spec Document with Implementation Details

**Files:**
- Modify: `docs/superpowers/specs/2026-07-02-entropic-map-mutation-design.md`

**Interfaces:**
- Consumes: Implementation decisions from brainstorming
- Produces: Updated spec with implementation details

- [ ] **Step 1: Add implementation details section to spec**

Add a section documenting:
- Final bit allocation (1 bit for HasOutlierSlot)
- Outlier activation rules by entropy level
- Trail distance rules (normal: 2-5, outlier: 6)
- Outlier name pool approach (deferred, use regular pool for now)
- Layout-specific removal patterns

- [ ] **Step 2: Commit**

```bash
git add docs/superpowers/specs/2026-07-02-entropic-map-mutation-design.md
git commit -m "docs: update spec with implementation details"
```

---

### Task 10: Final Verification and CI Check

**Files:**
- None (verification only)

**Interfaces:**
- Consumes: Complete implementation
- Produces: Verification that all tests pass and CI is green

- [ ] **Step 1: Run full test suite**

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --configuration Release`
Expected: All tests pass

- [ ] **Step 2: Run full solution build**

Run: `dotnet build src/WildBunch.sln --configuration Release`
Expected: Build succeeds

- [ ] **Step 3: Push changes and verify CI**

Run: `git push`
Expected: CI passes all checks

- [ ] **Step 4: Commit final verification**

```bash
git add .
git commit -m "chore: final verification - all tests pass and CI green"
```
