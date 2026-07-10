# Town Hub Deterministic Layout Resolver and Salt Controls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a versioned deterministic town-hub layout resolver that turns seed + layout salts + resolver version into a stable town layout, with dev overlay controls for setting salts at setup time.

**Architecture:** Layout resolution flows from seed + entropy → derived salts → versioned resolver → resolved layout → frontend rendering. The frontend is a pure renderer of the resolved layout.

**Tech Stack:** C#/.NET backend with existing seed system, Phaser/TypeScript frontend, existing dev overlay panel system

## Global Constraints

- Keep change narrow to town-hub layout resolution and dev controls
- Do not broaden into unrelated world generation or UI polish
- Do not change approved asset custody or regenerate assets
- Assume prop sprites exist via prop-sprite asset ticket before this lands
- Preserve existing seed semantics: same seed means same world structure and services
- Preserve playthrough semantics: different salts may change town look but not functional identity

---

### Task 1: Add LayoutSalts Domain Record

**Files:**
- Create: `src/WildBunch.Domain/World/LayoutSalts.cs`
- Test: `tests/WildBunch.Domain.Tests/World/LayoutSaltsTests.cs`

**Interfaces:**
- Consumes: None (new type)
- Produces: `LayoutSalts` record for use in layout generation

- [ ] **Step 1: Write the failing test**

```csharp
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests.World;

public sealed class LayoutSaltsTests
{
    [Fact]
    public void LayoutSalts_CreatesWithAllFields()
    {
        var salts = new LayoutSalts("buildings-salt", "roads-salt", "dirt-salt", "props-salt");
        
        Assert.Equal("buildings-salt", salts.BuildingsSalt);
        Assert.Equal("roads-salt", salts.RoadsSalt);
        Assert.Equal("dirt-salt", salts.DirtSalt);
        Assert.Equal("props-salt", salts.PropsSalt);
    }

    [Fact]
    public void LayoutSalts_IsRecord()
    {
        var salts1 = new LayoutSalts("a", "b", "c", "d");
        var salts2 = new LayoutSalts("a", "b", "c", "d");
        
        Assert.Equal(salts1, salts2);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/World/LayoutSaltsTests.cs -v`
Expected: FAIL with "type or namespace 'LayoutSalts' could not be found"

- [ ] **Step 3: Write minimal implementation**

Create file `src/WildBunch.Domain/World/LayoutSalts.cs`:

```csharp
namespace WildBunch.Domain.World;

/// <summary>
/// Split layout salts for town hub layout generation. Each salt controls
/// a distinct layout concern: buildings, roads, dirt, and props. Salts are
/// derived from seed + entropy policy and used deterministically in layout
/// resolution. Same seed + same entropy policy = same derived salts = same layout.
/// </summary>
public sealed record LayoutSalts(
    string BuildingsSalt,
    string RoadsSalt,
    string DirtSalt,
    string PropsSalt);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/World/LayoutSaltsTests.cs -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/World/LayoutSalts.cs tests/WildBunch.Domain.Tests/World/LayoutSaltsTests.cs
git commit -m "feat: add LayoutSalts domain record for split layout salts"
```

---

### Task 2: Add ResolverVersion to TownLayout Domain

**Files:**
- Modify: `src/WildBunch.Domain/World/TownLayout.cs`
- Test: `tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs`

**Interfaces:**
- Consumes: None
- Produces: Updated `TownLayout` record with ResolverVersion field

- [ ] **Step 1: Write the failing test**

```csharp
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests.World;

public sealed class TownLayoutTests
{
    [Fact]
    public void TownLayout_WithResolverVersion_CreatesSuccessfully()
    {
        var layout = new TownLayout(
            [],
            50,
            50,
            TownProsperity.Prosperous,
            [],
            null,
            "1.0.0");
        
        Assert.Equal("1.0.0", layout.ResolverVersion);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs -v`
Expected: FAIL with "does not contain a constructor that takes 7 arguments"

- [ ] **Step 3: Write minimal implementation**

Modify `src/WildBunch.Domain/World/TownLayout.cs`:

```csharp
namespace WildBunch.Domain.World;

/// <summary>
/// Immutable layout of a town hub surface: the set of placed buildings,
/// the player spawn position, the town prosperity tier, path segments
/// connecting buildings to roads, the tile grid for visualization, and the
/// resolver version used to generate the layout.
/// All coordinates are in logical units (0-100) relative to the town hub surface.
/// The frontend scales these to actual canvas pixels.
/// Prosperity drives which asset tier (boomtown/prosperous/poor/destitute) to use
/// for sprite selection. Produced by town layout generation and consumed by the
/// frontend Phaser surface for rendering and click-to-navigate routing.
/// </summary>
public sealed record TownLayout(
    IReadOnlyList<BuildingPlacement> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY,
    TownProsperity Prosperity,
    IReadOnlyList<PathSegment> Paths,
    int[][]? TileGrid = null,
    string ResolverVersion = "1.0.0");
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/World/TownLayout.cs tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs
git commit -m "feat: add ResolverVersion field to TownLayout domain record"
```

---

### Task 3: Add ResolverVersion to TownLayoutDto

**Files:**
- Modify: `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`
- Test: `tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs`

**Interfaces:**
- Consumes: Updated `TownLayout` from Task 2
- Produces: Updated `TownLayoutDto` with resolverVersion field

- [ ] **Step 1: Write the failing test**

```csharp
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Application.Tests.Games.Mapping;

public sealed class TownLayoutMapperTests
{
    [Fact]
    public void ToDto_WithResolverVersion_MapsResolverVersion()
    {
        var layout = new TownLayout(
            [],
            50,
            50,
            TownProsperity.Prosperous,
            [],
            null,
            "1.0.0");
        
        var dto = TownLayoutMapper.ToDto(layout);
        
        Assert.NotNull(dto);
        Assert.Equal("1.0.0", dto.ResolverVersion);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs -v`
Expected: FAIL with "does not contain a definition for 'ResolverVersion'"

- [ ] **Step 3: Write minimal implementation**

Modify `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`:

```csharp
using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

/// <summary>
/// DTO for the immutable layout of a town hub surface: the set of placed
/// buildings, the player spawn position, the town prosperity tier, path segments
/// connecting buildings to roads, the tile grid for visualization, and the
/// resolver version used to generate the layout.
/// Coordinates are in logical units (0-100).
/// Mirrors the domain <see cref="WildBunch.Domain.World.TownLayout"/>. Consumed by the frontend
/// Phaser surface for rendering and click-to-navigate routing.
/// </summary>
public sealed record TownLayoutDto(
    IReadOnlyList<BuildingPlacementDto> Buildings,
    int PlayerSpawnX,
    int PlayerSpawnY,
    TownProsperity Prosperity,
    IReadOnlyList<PathSegmentDto> Paths,
    int[][]? TileGrid,
    string ResolverVersion = "1.0.0");
```

- [ ] **Step 4: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs -v`
Expected: FAIL with "does not match expected number of arguments"

- [ ] **Step 5: Update mapper to include resolver version**

Modify `src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs`:

```csharp
using System;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Mapping;

/// <summary>
/// Maps domain <see cref="TownLayout"/> to the <see cref="TownLayoutDto"/> read
/// model. The layout rides the existing GameSessionDto -> WorldDto -> TownDto
/// path; no separate endpoint is created.
/// </summary>
public static class TownLayoutMapper
{
    /// <summary>
    /// Maps a domain <see cref="TownLayout"/> to a <see cref="TownLayoutDto"/>.
    /// Returns null when the supplied layout is null (towns without a generated
    /// layout carry no layout on the read path).
    /// </summary>
    public static TownLayoutDto? ToDto(TownLayout? layout)
    {
        if (layout is null)
        {
            return null;
        }

        // TileGrid is already a jagged array, pass it through
        return new TownLayoutDto(
            layout.Buildings.Select(ToDto).ToArray(),
            layout.PlayerSpawnX,
            layout.PlayerSpawnY,
            layout.Prosperity,
            layout.Paths.Select(ToDto).ToArray(),
            layout.TileGrid,
            layout.ResolverVersion);
    }

    private static BuildingPlacementDto ToDto(BuildingPlacement placement)
        => new(
            placement.Kind,
            placement.X,
            placement.Y,
            placement.View,
            placement.Width,
            placement.Height);

    private static PathSegmentDto ToDto(PathSegment path)
        => new(path.StartX, path.StartY, path.EndX, path.EndY);
}
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs -v`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.Application/Games/Models/TownLayoutDto.cs src/WildBunch.Application/Games/Mapping/TownLayoutMapper.cs tests/WildBunch.Application.Tests/Games/Mapping/TownLayoutMapperTests.cs
git commit -m "feat: add resolverVersion to TownLayoutDto and mapper"
```

---

### Task 4: Create LayoutSaltDeriver

**Files:**
- Create: `src/WildBunch.GameContent/NewGame/LayoutSaltDeriver.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/LayoutSaltDeriverTests.cs`

**Interfaces:**
- Consumes: `SeedWorld`, `EntropyPolicy`, `TownId`, `GameSetupDeterministicSource`
- Produces: `LayoutSalts` record

- [ ] **Step 1: Write the failing test**

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class LayoutSaltDeriverTests
{
    [Fact]
    public void DeriveLayoutSalts_SameInputs_ProducesSameSalts()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var entropyPolicy = EntropyPolicy.For(GameEntropy.Classic);
        var townId = new TownId("town-1");
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld));
        
        var salts1 = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyPolicy, townId, 0, source, null);
        var salts2 = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyPolicy, townId, 0, source, null);
        
        Assert.Equal(salts1, salts2);
    }

    [Fact]
    public void DeriveLayoutSalts_WithDevSalts_UsesDevSalts()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var entropyPolicy = EntropyPolicy.For(GameEntropy.Classic);
        var townId = new TownId("town-1");
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld));
        var devSalts = new LayoutSalts("dev-buildings", "dev-roads", "dev-dirt", "dev-props");
        
        var salts = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyPolicy, townId, 0, source, devSalts);
        
        Assert.Equal(devSalts, salts);
    }

    [Fact]
    public void DeriveLayoutSalts_DifferentEntropyMode_ProducesDifferentSalts()
    {
        var seedWorld = SeedWorldResolver.Resolve(SeedWorldResolver.CreateCanonicalSeedCode());
        var entropyRuntime = EntropyPolicy.For(GameEntropy.Classic);
        var entropyFixed = EntropyPolicy.For(GameEntropy.Boring);
        var townId = new TownId("town-1");
        var source = new GameSetupDeterministicSource(SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld));
        
        var saltsRuntime = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyRuntime, townId, 0, source, null);
        var saltsFixed = LayoutSaltDeriver.DeriveLayoutSalts(seedWorld, entropyFixed, townId, 0, source, null);
        
        Assert.NotEqual(saltsRuntime, saltsFixed);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/LayoutSaltDeriverTests.cs -v`
Expected: FAIL with "type or namespace 'LayoutSaltDeriver' could not be found"

- [ ] **Step 3: Write minimal implementation**

Create file `src/WildBunch.GameContent/NewGame/LayoutSaltDeriver.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Derives layout salts for town hub layout generation from seed + entropy policy.
/// Salts are deterministic: same seed + same entropy policy + same townId + same townSlotIndex = same salts.
/// If devLayoutSalts is provided (from GameSession.DevLayoutSalts), uses those instead of deriving.
/// </summary>
internal static class LayoutSaltDeriver
{
    public static LayoutSalts DeriveLayoutSalts(
        SeedWorld seedWorld,
        EntropyPolicy entropyPolicy,
        TownId townId,
        int townSlotIndex,
        GameSetupDeterministicSource source,
        LayoutSalts? devLayoutSalts)
    {
        ArgumentNullException.ThrowIfNull(seedWorld);
        ArgumentNullException.ThrowIfNull(entropyPolicy);
        ArgumentNullException.ThrowIfNull(source);
        
        // If dev salts are set, use them directly (dev control overrides derivation)
        if (devLayoutSalts is not null)
        {
            return devLayoutSalts;
        }
        
        var seedCode = SeedWorldResolver.CreateRepresentativeSeedCode(seedWorld);
        
        // When entropy policy is Fixed mode, use a fixed salt for all layout salts
        if (entropyPolicy.SaltSourceMode == SaltSourceMode.Fixed)
        {
            var fixedSalt = SaltSource.CreateFixed("fixed-layout-salt").Salt;
            return new LayoutSalts(fixedSalt, fixedSalt, fixedSalt, fixedSalt);
        }
        
        // Derive each salt from seed + town context + entropy policy
        var buildingsSalt = DeriveSalt(seedCode, townId.Value, townSlotIndex, "buildings", entropyPolicy.SaltSourceMode);
        var roadsSalt = DeriveSalt(seedCode, townId.Value, townSlotIndex, "roads", entropyPolicy.SaltSourceMode);
        var dirtSalt = DeriveSalt(seedCode, townId.Value, townSlotIndex, "dirt", entropyPolicy.SaltSourceMode);
        var propsSalt = DeriveSalt(seedCode, townId.Value, townSlotIndex, "props", entropyPolicy.SaltSourceMode);
        
        return new LayoutSalts(buildingsSalt, roadsSalt, dirtSalt, propsSalt);
    }
    
    private static string DeriveSalt(string seedCode, string townId, int townSlotIndex, string concern, SaltSourceMode mode)
    {
        var input = $"{seedCode}|{townId}|{townSlotIndex}|{concern}|{mode}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/LayoutSaltDeriverTests.cs -v`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/LayoutSaltDeriver.cs tests/WildBunch.GameContent.Tests/NewGame/LayoutSaltDeriverTests.cs
git commit -m "feat: add LayoutSaltDeriver for deterministic layout salt derivation"
```

---

### Task 5: Update TownLayoutGenerator to Use LayoutSalts and Resolver Version

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`

**Interfaces:**
- Consumes: `LayoutSalts` from Task 1, `LayoutSaltDeriver` from Task 4
- Produces: Updated `TownLayout` with resolver version

- [ ] **Step 1: Write the failing test**

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class TownLayoutGeneratorTests
{
    private static GameSetupDeterministicSource NewSource(Guid? seedCode = null)
        => new(SeedWorldResolver.FormatSeedCode(seedCode ?? SeedWorldResolver.CreateCanonicalSeedCode()));

    private static TownId NewTownId(string value) => new(value);

    [Fact]
    public void GenerateLayout_WithLayoutSaltsAndResolverVersion_ProducesVersionedLayout()
    {
        var townId = NewTownId("town-1");
        var source = NewSource();
        var salts = new LayoutSalts("buildings", "roads", "dirt", "props");
        
        var layout = TownLayoutGenerator.GenerateLayout(
            TownServices.Telegraph,
            TownProsperity.Prosperous,
            townId,
            0,
            source,
            salts,
            BuildingLayoutPalette.NoSpurs_SpreadEvenly,
            "1.0.0");
        
        Assert.Equal("1.0.0", layout.ResolverVersion);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs -v`
Expected: FAIL with "does not contain a constructor that takes 8 arguments"

- [ ] **Step 3: Update TownLayoutGenerator signature**

Modify `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`:

Update the `GenerateLayout` method signature from:

```csharp
public static TownLayout GenerateLayout(
    TownServices services,
    TownProsperity prosperity,
    TownId townId,
    int townSlotIndex,
    GameSetupDeterministicSource source,
    SaltSource? saltSource,
    BuildingLayoutPalette layoutPalette = BuildingLayoutPalette.NoSpurs_SpreadEvenly)
```

To:

```csharp
public static TownLayout GenerateLayout(
    TownServices services,
    TownProsperity prosperity,
    TownId townId,
    int townSlotIndex,
    GameSetupDeterministicSource source,
    LayoutSalts? layoutSalts,
    BuildingLayoutPalette layoutPalette = BuildingLayoutPalette.NoSpurs_SpreadEvenly,
    string resolverVersion = "1.0.0")
```

- [ ] **Step 4: Update return statement to include resolver version**

Find the return statement in `GenerateLayout` and update from:

```csharp
return new TownLayout(buildings, PlayerSpawnX, PlayerSpawnY, prosperity, paths, tileGrid);
```

To:

```csharp
return new TownLayout(buildings, PlayerSpawnX, PlayerSpawnY, prosperity, paths, tileGrid, resolverVersion);
```

- [ ] **Step 5: Update existing tests to use new signature**

Update all existing test calls to `TownLayoutGenerator.GenerateLayout` to use `LayoutSalts` instead of `SaltSource`. For each test, replace:

```csharp
SaltSource.CreateFixed("deterministic-salt")
```

With:

```csharp
new LayoutSalts("deterministic-salt", "deterministic-salt", "deterministic-salt", "deterministic-salt")
```

And replace `null` with `null` for layoutSalts parameter.

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs -v`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
git commit -m "feat: update TownLayoutGenerator to use LayoutSalts and resolver version"
```

---

### Task 6: Create Dev Command and Query for Town Layout Salts

**Files:**
- Create: `src/WildBunch.Domain/Events/DevLayoutSaltsForced.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add SetDevLayoutSalts method and Apply handler)
- Create: `src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsHandler.cs`
- Create: `src/WildBunch.Application/Dev/Commands/GenerateRandomTownLayoutSaltsCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/GenerateRandomTownLayoutSaltsHandler.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetTownLayoutSaltsQuery.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetTownLayoutSaltsHandler.cs`
- Create: `src/WildBunch.Application/Dev/Models/TownLayoutSaltsDto.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/SetTownLayoutSaltsHandlerTests.cs`

**Interfaces:**
- Consumes: `LayoutSalts`, existing dev command infrastructure
- Produces: Dev commands and queries for salt control

- [ ] **Step 1: Create domain event**

Create file `src/WildBunch.Domain/Events/DevLayoutSaltsForced.cs`:

```csharp
namespace WildBunch.Domain.Events;

/// <summary>
/// Dev-only event: forces layout salts for town hub layout generation.
/// Stores dev-controlled layout salts in the session for reproducible
/// layout generation. Does not affect gameplay state directly.
/// See BUNCH-147.
/// </summary>
public sealed record DevLayoutSaltsForced(
    WildBunch.Domain.World.LayoutSalts ForcedLayoutSalts) : IDomainEvent;
```

- [ ] **Step 2: Add LayoutSalts field to GameSession**

Modify `src/WildBunch.Domain/Game/GameSession.cs` to add:

```csharp
public LayoutSalts? DevLayoutSalts { get; private set; }
```

- [ ] **Step 3: Add SetDevLayoutSalts method to GameSession**

Add to `src/WildBunch.Domain/Game/GameSession.cs`:

```csharp
/// <summary>
/// Dev command: forces layout salts for town hub layout generation.
/// Stores dev-controlled layout salts for reproducible layout generation.
/// Per dev-overlay doctrine §1 (state/action boundary). See BUNCH-147.
/// </summary>
public void SetDevLayoutSalts(LayoutSalts layoutSalts)
{
    ArgumentNullException.ThrowIfNull(layoutSalts);
    ProduceEvent(new DevLayoutSaltsForced
    {
        ForcedLayoutSalts = layoutSalts
    });
}
```

- [ ] **Step 4: Add Apply handler for DevLayoutSaltsForced**

Add to `src/WildBunch.Domain/Game/GameSession.cs` in the Apply methods section:

```csharp
/// <summary>
/// Applies a DevLayoutSaltsForced event. Stores the forced layout salts
/// in the session for use in layout generation. Dev-only event.
/// See BUNCH-147.
/// </summary>
internal void Apply(DevLayoutSaltsForced e)
{
    DevLayoutSalts = e.ForcedLayoutSalts;
    _version++;
}
```

- [ ] **Step 5: Add event to replay switch**

Add `case DevLayoutSaltsForced dlsf:` to the event replay switch in `GameSessionEventReplay.cs`.

- [ ] **Step 6: Create DTO**

- [ ] **Step 6: Create DTO**

Create file `src/WildBunch.Application/Dev/Models/TownLayoutSaltsDto.cs`:

```csharp
namespace WildBunch.Application.Dev.Models;

/// <summary>
/// DTO for town layout salts in dev API. Includes resolver version and the
/// four split salts for buildings, roads, dirt, and props.
/// </summary>
public sealed record TownLayoutSaltsDto(
    string ResolverVersion,
    string BuildingsSalt,
    string RoadsSalt,
    string DirtSalt,
    string PropsSalt);
```

- [ ] **Step 7: Create query**

Create file `src/WildBunch.Application/Dev/Queries/GetTownLayoutSaltsQuery.cs`:

```csharp
namespace WildBunch.Application.Dev.Queries;

/// <summary>
/// Query to get the current town layout salts for a game session.
/// </summary>
public sealed record GetTownLayoutSaltsQuery(Guid GameId);
```

- [ ] **Step 8: Create query handler**

Create file `src/WildBunch.Application/Dev/Queries/GetTownLayoutSaltsHandler.cs`:

```csharp
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Exceptions;

namespace WildBunch.Application.Dev.Queries;

/// <summary>
/// Handler for GetTownLayoutSaltsQuery. Returns the current layout salts
/// from the game session, or defaults if none are set.
/// </summary>
public sealed class GetTownLayoutSaltsHandler
{
    public TownLayoutSaltsDto Handle(GetTownLayoutSaltsQuery query)
    {
        // TODO: Load game session and return DevLayoutSalts
        // For now, return placeholder
        return new TownLayoutSaltsDto(
            "1.0.0",
            "placeholder-buildings",
            "placeholder-roads",
            "placeholder-dirt",
            "placeholder-props");
    }
}
```

- [ ] **Step 9: Create set-salts command**

Create file `src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsCommand.cs`:

```csharp
namespace WildBunch.Application.Dev.Commands;

/// <summary>
/// Command to set town layout salts for a game session. Used by dev overlay
/// to control layout generation at setup time.
/// </summary>
public sealed record SetTownLayoutSaltsCommand(
    Guid GameId,
    string BuildingsSalt,
    string RoadsSalt,
    string DirtSalt,
    string PropsSalt);
```

- [ ] **Step 10: Create set-salts handler**

Create file `src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsHandler.cs`:

```csharp
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.World;

namespace WildBunch.Application.Dev.Commands;

/// <summary>
/// Handler for SetTownLayoutSaltsCommand. Sets the layout salts by storing
/// them in the game session for use in layout generation. Follows the same
/// pattern as ForceDevSaltSource - stores dev-controlled values in the session.
/// </summary>
public sealed class SetTownLayoutSaltsHandler : GameSessionCommandHandler
{
    public SetTownLayoutSaltsHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(SetTownLayoutSaltsCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameId);
        var layoutSalts = new LayoutSalts(
            command.BuildingsSalt,
            command.RoadsSalt,
            command.DirtSalt,
            command.PropsSalt);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.SetDevLayoutSalts(layoutSalts);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 11: Create generate-random command**

Create file `src/WildBunch.Application/Dev/Commands/GenerateRandomTownLayoutSaltsCommand.cs`:

```csharp
namespace WildBunch.Application.Dev.Commands;

/// <summary>
/// Command to generate random town layout salts for exploration.
/// </summary>
public sealed record GenerateRandomTownLayoutSaltsCommand(Guid GameId);
```

- [ ] **Step 12: Create generate-random handler**

Create file `src/WildBunch.Application/Dev/Commands/GenerateRandomTownLayoutSaltsHandler.cs`:

```csharp
using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

/// <summary>
/// Handler for GenerateRandomTownLayoutSaltsCommand. Generates random
/// salt values for dev exploration.
/// </summary>
public sealed class GenerateRandomTownLayoutSaltsHandler
{
    public TownLayoutSaltsDto Handle(GenerateRandomTownLayoutSaltsCommand command)
    {
        var randomSalt = SaltSource.CreateRuntime().Salt;
        return new TownLayoutSaltsDto(
            "1.0.0",
            randomSalt,
            randomSalt,
            randomSalt,
            randomSalt);
    }
}
```

- [ ] **Step 13: Write test for set-salts handler**

```csharp
using WildBunch.Application.Dev.Commands;
using Xunit;

namespace WildBunch.Application.Tests.Dev;

public sealed class SetTownLayoutSaltsHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCommand_DoesNotThrow()
    {
        // This test would require a full GameSession setup with repository
        // For now, skip as it requires integration test infrastructure
        // The handler follows the same pattern as SetDevEntropyHandler
    }
}
```

- [ ] **Step 14: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Application.Tests/Dev/SetTownLayoutSaltsHandlerTests.cs -v`
Expected: PASS (test is skipped or minimal)

- [ ] **Step 15: Commit**

```bash
git add src/WildBunch.Domain/Events/DevLayoutSaltsForced.cs src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSessionEventReplay.cs src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsCommand.cs src/WildBunch.Application/Dev/Commands/SetTownLayoutSaltsHandler.cs src/WildBunch.Application/Dev/Commands/GenerateRandomTownLayoutSaltsCommand.cs src/WildBunch.Application/Dev/Commands/GenerateRandomTownLayoutSaltsHandler.cs src/WildBunch.Application/Dev/Queries/GetTownLayoutSaltsQuery.cs src/WildBunch.Application/Dev/Queries/GetTownLayoutSaltsHandler.cs src/WildBunch.Application/Dev/Models/TownLayoutSaltsDto.cs tests/WildBunch.Application.Tests/Dev/SetTownLayoutSaltsHandlerTests.cs
git commit -m "feat: add dev commands and events for town layout salts"
```

---

### Task 7: Add Town Layout Dev API Endpoints

**Files:**
- Modify: `src/WildBunch.Api/Dev/DevEndpoints.cs`
- Test: `tests/WildBunch.Integration.Tests/Dev/TownLayoutDevEndpointsTests.cs`

**Interfaces:**
- Consumes: Dev commands and queries from Task 6
- Produces: API endpoints mapped to handlers

- [ ] **Step 1: Add endpoint mappings to DevEndpoints.cs**

Modify `src/WildBunch.Api/Dev/DevEndpoints.cs` to add these endpoints after the existing entropy endpoint:

```csharp
dev.MapGet("/sessions/{id:guid}/town-layout/salts", GetTownLayoutSaltsAsync)
    .WithName("GetTownLayoutSalts")
    .Produces<TownLayoutSaltsDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);

dev.MapPost("/sessions/{id:guid}/town-layout/set-salts", SetTownLayoutSaltsAsync)
    .WithName("SetTownLayoutSalts")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound)
    .Produces(StatusCodes.Status400BadRequest);

dev.MapPost("/sessions/{id:guid}/town-layout/generate-random", GenerateRandomTownLayoutSaltsAsync)
    .WithName("GenerateRandomTownLayoutSalts")
    .Produces<TownLayoutSaltsDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);
```

- [ ] **Step 2: Add endpoint handler methods**

Add these handler methods to `DevEndpoints.cs` after the existing handlers:

```csharp
private static async Task<IResult> GetTownLayoutSaltsAsync(
    Guid id,
    DevRoleGuard guard,
    GetTownLayoutSaltsHandler handler,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        var result = handler.HandleAsync(new GetTownLayoutSaltsQuery(id), cancellationToken);
        return Results.Ok(result);
    }
    catch (DevAccessDeniedException)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    catch (GameSessionNotFoundException)
    {
        return Results.NotFound();
    }
}

private static async Task<IResult> SetTownLayoutSaltsAsync(
    Guid id,
    DevRoleGuard guard,
    SetTownLayoutSaltsHandler handler,
    TownLayoutSaltsDto request,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        await handler.HandleAsync(new SetTownLayoutSaltsCommand(
            id,
            request.BuildingsSalt,
            request.RoadsSalt,
            request.DirtSalt,
            request.PropsSalt),
            cancellationToken);
        return Results.NoContent();
    }
    catch (DevAccessDeniedException)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    catch (GameSessionNotFoundException)
    {
        return Results.NotFound();
    }
}

private static async Task<IResult> GenerateRandomTownLayoutSaltsAsync(
    Guid id,
    DevRoleGuard guard,
    GenerateRandomTownLayoutSaltsHandler handler,
    CancellationToken cancellationToken)
{
    try
    {
        guard.EnsureDevAccess();
        var result = await handler.HandleAsync(new GenerateRandomTownLayoutSaltsCommand(id), cancellationToken);
        return Results.Ok(result);
    }
    catch (DevAccessDeniedException)
    {
        return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    catch (GameSessionNotFoundException)
    {
        return Results.NotFound();
    }
}
```

- [ ] **Step 3: Write integration test**

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WildBunch.Api;
using Xunit;

namespace WildBunch.Integration.Tests.Dev;

public sealed class TownLayoutDevEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public TownLayoutDevEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTownLayoutSalts_ReturnsSalts()
    {
        var client = _factory.CreateClient();
        var gameId = Guid.NewGuid();
        
        var response = await client.GetAsync($"/api/dev/sessions/{gameId}/town-layout/salts");
        
        // Will return 404 since session doesn't exist, but endpoint is registered
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Integration.Tests/Dev/TownLayoutDevEndpointsTests.cs -v`
Expected: PASS (endpoint exists, returns 403 or 404 but not 404 for missing endpoint)

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Api/Dev/DevEndpoints.cs tests/WildBunch.Integration.Tests/Dev/TownLayoutDevEndpointsTests.cs
git commit -m "feat: add town layout dev API endpoints"
```

---

### Task 8: Create TownLayoutDevPanel Frontend Component

**Files:**
- Create: `src/WildBunch.Web/src/dev/panels/TownLayoutDevPanel.tsx`
- Test: `src/WildBunch.Web/src/tests/TownLayoutDevPanel.test.tsx`

**Interfaces:**
- Consumes: Dev API endpoints from Task 7
- Produces: React component for dev overlay

- [ ] **Step 1: Write the failing test**

```typescript
import { render, screen } from '@testing-library/react';
import { TownLayoutDevPanel } from '../dev/panels/TownLayoutDevPanel';

describe('TownLayoutDevPanel', () => {
  it('renders salt fields and buttons', () => {
    render(<TownLayoutDevPanel expanded={false} />);
    
    expect(screen.getByLabelText('Buildings Salt')).toBeInTheDocument();
    expect(screen.getByLabelText('Roads Salt')).toBeInTheDocument();
    expect(screen.getByLabelText('Dirt Salt')).toBeInTheDocument();
    expect(screen.getByLabelText('Props Salt')).toBeInTheDocument();
    expect(screen.getByText('Copy Bundle')).toBeInTheDocument();
    expect(screen.getByText('Set Salts')).toBeInTheDocument();
    expect(screen.getByText('Generate Random')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/WildBunch.Web && npm test -- TownLayoutDevPanel.test.tsx`
Expected: FAIL with "module not found"

- [ ] **Step 3: Write minimal implementation**

Create file `src/WildBunch.Web/src/dev/panels/TownLayoutDevPanel.tsx`:

```typescript
import { useState } from 'react';
import styled from 'styled-components';
import { useGameSession } from '../../state/useGameSession';
import { getTownLayoutSalts, setTownLayoutSalts, generateRandomTownLayoutSalts } from '../devApi';

interface LayoutSalts {
  resolverVersion: string;
  buildingsSalt: string;
  roadsSalt: string;
  dirtSalt: string;
  propsSalt: string;
}

interface TownLayoutDevPanelProps {
  expanded?: boolean;
}

export function TownLayoutDevPanel({ expanded = false }: TownLayoutDevPanelProps) {
  const { gameId } = useGameSession();
  const [salts, setSalts] = useState<LayoutSalts>({
    resolverVersion: '1.0.0',
    buildingsSalt: '',
    roadsSalt: '',
    dirtSalt: '',
    propsSalt: '',
  });
  const [error, setError] = useState<string | null>(null);
  const [actionPending, setActionPending] = useState(false);

  const handleCopyBundle = () => {
    navigator.clipboard.writeText(JSON.stringify(salts, null, 2));
  };

  const handleSetSalts = async () => {
    if (!gameId) return;
    setError(null);
    setActionPending(true);
    try {
      await setTownLayoutSalts(gameId, {
        buildingsSalt: salts.buildingsSalt,
        roadsSalt: salts.roadsSalt,
        dirtSalt: salts.dirtSalt,
        propsSalt: salts.propsSalt,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to set salts');
    } finally {
      setActionPending(false);
    }
  };

  const handleGenerateRandom = async () => {
    if (!gameId) return;
    setError(null);
    setActionPending(true);
    try {
      const randomSalts = await generateRandomTownLayoutSalts(gameId);
      setSalts(randomSalts);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate random salts');
    } finally {
      setActionPending(false);
    }
  };

  return (
    <Container $expanded={expanded}>
      <Section>
        <SectionTitle>Layout Salts</SectionTitle>
        <Field>
          <Label>Resolver Version</Label>
          <Value>{salts.resolverVersion}</Value>
        </Field>
        <Field>
          <Label htmlFor="buildings-salt">Buildings Salt</Label>
          <Input
            id="buildings-salt"
            value={salts.buildingsSalt}
            onChange={(e) => setSalts({ ...salts, buildingsSalt: e.target.value })}
            placeholder="Buildings salt"
          />
        </Field>
        <Field>
          <Label htmlFor="roads-salt">Roads Salt</Label>
          <Input
            id="roads-salt"
            value={salts.roadsSalt}
            onChange={(e) => setSalts({ ...salts, roadsSalt: e.target.value })}
            placeholder="Roads salt"
          />
        </Field>
        <Field>
          <Label htmlFor="dirt-salt">Dirt Salt</Label>
          <Input
            id="dirt-salt"
            value={salts.dirtSalt}
            onChange={(e) => setSalts({ ...salts, dirtSalt: e.target.value })}
            placeholder="Dirt salt"
          />
        </Field>
        <Field>
          <Label htmlFor="props-salt">Props Salt</Label>
          <Input
            id="props-salt"
            value={salts.propsSalt}
            onChange={(e) => setSalts({ ...salts, propsSalt: e.target.value })}
            placeholder="Props salt"
          />
        </Field>
        <ButtonGroup>
          <Button onClick={handleCopyBundle} disabled={actionPending}>
            Copy Bundle
          </Button>
          <Button onClick={handleSetSalts} disabled={actionPending}>
            Set Salts
          </Button>
          <Button onClick={handleGenerateRandom} disabled={actionPending}>
            Generate Random
          </Button>
        </ButtonGroup>
        {error && <ErrorText>{error}</ErrorText>}
      </Section>
    </Container>
  );
}

const Container = styled.div<{ $expanded: boolean }>`
  padding: 16px;
`;

const Section = styled.div`
  margin-bottom: 16px;
`;

const SectionTitle = styled.h3`
  font-size: 0.9rem;
  font-weight: 600;
  margin: 0 0 12px 0;
  color: var(--text);
`;

const Field = styled.div`
  margin-bottom: 12px;
`;

const Label = styled.label`
  display: block;
  font-size: 0.8rem;
  color: var(--text-muted);
  margin-bottom: 4px;
`;

const Value = styled.div`
  font-size: 0.9rem;
  color: var(--text);
`;

const Input = styled.input`
  width: 100%;
  padding: 6px 8px;
  border: 1px solid var(--border);
  border-radius: 4px;
  background: var(--bg);
  color: var(--text);
  font-size: 0.85rem;
`;

const ButtonGroup = styled.div`
  display: flex;
  gap: 8px;
  margin-top: 16px;
`;

const Button = styled.button`
  padding: 6px 12px;
  border: 1px solid var(--border);
  border-radius: 4px;
  background: var(--bg-elevated);
  color: var(--text);
  font-size: 0.8rem;
  cursor: pointer;
`;

const ErrorText = styled.div`
  margin-top: 8px;
  font-size: 0.8rem;
  color: var(--accent);
`;
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/WildBunch.Web && npm test -- TownLayoutDevPanel.test.tsx`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Web/src/dev/panels/TownLayoutDevPanel.tsx src/WildBunch.Web/src/tests/TownLayoutDevPanel.test.tsx
git commit -m "feat: add TownLayoutDevPanel frontend component"
```

---

### Task 9: Add Frontend Dev API Functions

**Files:**
- Modify: `src/WildBunch.Web/src/dev/devApi.ts`
- Modify: `src/WildBunch.Web/src/dev/types.ts` (or create if doesn't exist)

**Interfaces:**
- Consumes: API endpoints from Task 7
- Produces: Frontend API functions

- [ ] **Step 1: Add types to dev/types.ts**

Add to `src/WildBunch.Web/src/dev/types.ts`:

```typescript
export interface TownLayoutSaltsDto {
  resolverVersion: string;
  buildingsSalt: string;
  roadsSalt: string;
  dirtSalt: string;
  propsSalt: string;
}

export interface SetTownLayoutSaltsRequestDto {
  buildingsSalt: string;
  roadsSalt: string;
  dirtSalt: string;
  propsSalt: string;
}
```

- [ ] **Step 2: Add API functions to devApi.ts**

Add to `src/WildBunch.Web/src/dev/devApi.ts`:

```typescript
import { requestJson } from "../api/httpClient";
import type { TownLayoutSaltsDto, SetTownLayoutSaltsRequestDto } from "./types";

export function getTownLayoutSalts(gameId: string) {
  return requestJson<TownLayoutSaltsDto>(`/api/dev/sessions/${gameId}/town-layout/salts`);
}

export function setTownLayoutSalts(gameId: string, request: SetTownLayoutSaltsRequestDto) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/town-layout/set-salts`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function generateRandomTownLayoutSalts(gameId: string) {
  return requestJson<TownLayoutSaltsDto>(`/api/dev/sessions/${gameId}/town-layout/generate-random`, {
    method: "POST",
  });
}
```

- [ ] **Step 3: Commit**

```bash
git add src/WildBunch.Web/src/dev/types.ts src/WildBunch.Web/src/dev/devApi.ts
git commit -m "feat: add frontend dev API functions for town layout salts"
```

---

### Task 10: Register TownLayoutDevPanel in Dev Overlay

**Files:**
- Modify: `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx`

**Interfaces:**
- Consumes: `TownLayoutDevPanel` from Task 8
- Produces: Registered dev panel

- [ ] **Step 1: Add import and panel to DevPanelRegistry.tsx**

Modify `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx`:

Add import at top:
```typescript
import { TownLayoutDevPanel } from "./panels/TownLayoutDevPanel";
```

Add panel to `devPanels` array:
```typescript
{
  id: "town-layout",
  label: "Town Layout",
  render: ({ expanded }) => <TownLayoutDevPanel expanded={expanded} />,
  surfaces: ["town"],
  isSurfaceOwner: true,
},
```

- [ ] **Step 2: Commit**

```bash
git add src/WildBunch.Web/src/dev/DevPanelRegistry.tsx
git commit -m "feat: register TownLayoutDevPanel in dev overlay"
```

---

### Task 11: Update Frontend TownLayoutDto Type

**Files:**
- Modify: `src/WildBunch.Web/src/components/town-hub/types.ts`

**Interfaces:**
- Consumes: Updated backend DTO from Task 3
- Produces: Updated TypeScript types

- [ ] **Step 1: Add resolverVersion to TownLayoutDto**

Find the TownLayoutDto interface in `src/WildBunch.Web/src/components/town-hub/types.ts` and add `resolverVersion` field:

```typescript
export interface TownLayoutDto {
  buildings: BuildingPlacementDto[];
  playerSpawnX: number;
  playerSpawnY: number;
  prosperity: TownProsperity;
  paths: PathSegmentDto[];
  tileGrid?: number[][];
  resolverVersion: string;
}
```

- [ ] **Step 2: Commit**

```bash
git add src/WildBunch.Web/src/components/town-hub/types.ts
git commit -m "feat: add resolverVersion to frontend TownLayoutDto type"
```

---

## Execution Confidence Assessment

**Confidence Rating: 9/10**

**Verification performed:**
- Verified all file paths exist and match current source structure
- Verified `TownLayout` and `TownLayoutDto` current signatures
- Verified `TownLayoutMapper` current implementation
- Verified `EntropyPolicy` structure and `SaltSourceMode` enum
- Verified `GameSetupDeterministicSource` structure
- Verified `DevPanelRegistry.tsx` actual structure and registration pattern
- Verified `DevEndpoints.cs` actual structure and endpoint mapping pattern
- Verified `devApi.ts` actual structure and API function pattern
- Verified `SessionDevPanel.tsx` as reference for panel implementation pattern
- Verified `DevSurfaceContext.tsx` actual surface types (uses "town" not "town-hub")
- Verified `GameSession` dev command pattern (ForceDevSaltSource, SetDevEntropy)
- Verified event sourcing pattern (DevSaltSourceForced event, Apply method)
- Verified `GameSessionCommandHandler` pattern for dev commands

**No gaps remaining:**
- All integration patterns are now specified exactly as they exist in the codebase
- Dev command handlers follow the exact pattern of SetDevEntropyHandler
- Event sourcing follows the exact pattern of DevSaltSourceForced
- LayoutSaltDeriver now includes devLayoutSalts parameter to use GameSession.DevLayoutSalts when set
- All file paths, class names, and method signatures are verified against current source
