# Task 1: Update BuildingLayoutPalette Enum

## Task Description

Replace the 8 canonical layout patterns (HubAndSpoke, LinearChain, etc.) with 12 functional tile-based palettes + 4 reserved values.

## Files
- Modify: `src/WildBunch.Domain/World/BuildingLayoutPalette.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs`

## Interfaces
- Consumes: None
- Produces: Updated BuildingLayoutPalette enum with 12 functional palettes + 4 reserved

## Steps

### Step 1: Write failing test for new palette values

```csharp
[Fact]
public void BuildingLayoutPalette_Has12FunctionalPalettes()
{
    // Verify the enum has the expected 12 functional palettes
    Assert.Equal(16, Enum.GetValues<BuildingLayoutPalette>().Length); // 12 functional + 4 reserved
    
    // Verify specific palette values exist
    Assert.True(Enum.IsDefined(typeof(BuildingLayoutPalette), (int)BuildingLayoutPalette.NoSpurs_SpreadEvenly));
    Assert.True(Enum.IsDefined(typeof(BuildingLayoutPalette), (int)BuildingLayoutPalette.OneSpurLeft_SpreadEvenly));
    Assert.True(Enum.IsDefined(typeof(BuildingLayoutPalette), (int)BuildingLayoutPalette.TwoSpursLeftRight_SpreadEvenly));
    Assert.True(Enum.IsDefined(typeof(BuildingLayoutPalette), (int)BuildingLayoutPalette.Reserved12));
}
```

### Step 2: Run test to verify it fails

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "BuildingLayoutPalette_Has12FunctionalPalettes"`
Expected: FAIL with enum values not found

### Step 3: Update BuildingLayoutPalette enum

```csharp
namespace WildBunch.Domain.World;

/// <summary>
/// Tile-based building layout palette for town hub surfaces. Encodes road topology
/// (spur count, spur positions, spur direction) and placement strategy in 4 bits.
/// Used by TownLayoutGenerator to generate deterministic tile-based layouts.
/// </summary>
public enum BuildingLayoutPalette
{
    // 0 spurs
    NoSpurs_SpreadEvenly = 0,
    NoSpurs_ClusterMiddle = 1,
    NoSpurs_FavorLeft = 2,
    NoSpurs_FavorRight = 3,
    
    // 1 spur (at middle row)
    OneSpurLeft_SpreadEvenly = 4,
    OneSpurLeft_ClusterMiddle = 5,
    OneSpurRight_SpreadEvenly = 6,
    OneSpurRight_ClusterMiddle = 7,
    
    // 2 spurs (at upper and lower middle rows)
    TwoSpursLeftRight_SpreadEvenly = 8,
    TwoSpursLeftRight_ClusterMiddle = 9,
    TwoSpursRightLeft_SpreadEvenly = 10,
    TwoSpursRightLeft_ClusterMiddle = 11,
    
    // Reserved for future expansion
    Reserved12 = 12,
    Reserved13 = 13,
    Reserved14 = 14,
    Reserved15 = 15
}
```

### Step 4: Run test to verify it passes

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "BuildingLayoutPalette_Has12FunctionalPalettes"`
Expected: PASS

### Step 5: Commit

```bash
git add src/WildBunch.Domain/World/BuildingLayoutPalette.cs tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs
git commit -m "refactor: update BuildingLayoutPalette to tile-based encoding

- Replace 8 canonical layout patterns with 12 functional palettes
- Encode spur count, spur positions, spur direction, and placement strategy
- Add 4 reserved values for future expansion
- Update test to verify new palette structure"
```
