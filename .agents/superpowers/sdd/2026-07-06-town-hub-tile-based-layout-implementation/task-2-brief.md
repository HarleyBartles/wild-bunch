# Task 2: Create Palette Spec Record

## Task Description

Create PaletteSpec record and PlacementStrategy enum to encode spur configuration and placement strategy for tile-based town hub layouts.

## Files
- Create: `src/WildBunch.GameContent/NewGame/PaletteSpec.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/PaletteSpecTests.cs`

## Interfaces
- Consumes: BuildingLayoutPalette enum (from Task 1)
- Produces: PaletteSpec record for palette configuration

## Steps

### Step 1: Write failing test for PaletteSpec

```csharp
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

public sealed class PaletteSpecTests
{
    [Fact]
    public void PaletteSpec_StoresSpurConfiguration()
    {
        var spec = new PaletteSpec(
            SpurCount: 1,
            SpurRows: new[] { 4 },
            SpurDirections: new[] { SpurDirection.East },
            PlacementStrategy: PlacementStrategy.SpreadEvenly);
        
        Assert.Equal(1, spec.SpurCount);
        Assert.Single(spec.SpurRows);
        Assert.Equal(4, spec.SpurRows[0]);
        Assert.Single(spec.SpurDirections);
        Assert.Equal(SpurDirection.East, spec.SpurDirections[0]);
        Assert.Equal(PlacementStrategy.SpreadEvenly, spec.PlacementStrategy);
    }
}
```

### Step 2: Run test to verify it fails

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "PaletteSpec_StoresSpurConfiguration"`
Expected: FAIL with PaletteSpec not defined

### Step 3: Create PaletteSpec record and supporting types

Create file `src/WildBunch.GameContent/NewGame/PaletteSpec.cs`:

```csharp
namespace WildBunch.GameContent.NewGame;

/// <summary>
/// Palette specification for tile-based town hub layout. Encodes spur configuration
/// and placement strategy for a BuildingLayoutPalette value.
/// </summary>
public sealed record PaletteSpec(
    int SpurCount,
    int[] SpurRows,
    SpurDirection[] SpurDirections,
    PlacementStrategy PlacementStrategy);

/// <summary>
/// Placement strategy for distributing buildings across available tile positions.
/// </summary>
public enum PlacementStrategy
{
    SpreadEvenly,
    ClusterMiddle,
    FavorLeft,
    FavorRight
}
```

Note: SpurDirection enum already exists in BuildingLayoutCatalog.cs (East, West) and will be used from there.

### Step 4: Run test to verify it passes

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "PaletteSpec_StoresSpurConfiguration"`
Expected: PASS

### Step 5: Commit

```bash
git add src/WildBunch.GameContent/NewGame/PaletteSpec.cs tests/WildBunch.GameContent.Tests/NewGame/PaletteSpecTests.cs
git commit -m "feat: add PaletteSpec record for tile-based layout

- Add PaletteSpec record to encode spur configuration and placement strategy
- Add PlacementStrategy enum for building distribution strategies
- Add test to verify PaletteSpec stores configuration correctly"
```
