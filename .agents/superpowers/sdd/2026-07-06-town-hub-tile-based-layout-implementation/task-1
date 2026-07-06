# Task 1.5: Update SeedWorldResolver for New Palette Range

## Task Description

Update SeedWorldResolver to handle the new 16-palette range (12 functional + 4 reserved) instead of the old 8-palette range. This fixes the codec to prevent values 8-15 from incorrectly wrapping to 0-7.

## Files
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs`
- Test: `tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs`

## Interfaces
- Consumes: Updated BuildingLayoutPalette enum (from Task 1)
- Produces: Updated SeedWorldResolver to handle 16 palette values

## Steps

### Step 1: Write failing test for full palette range

```csharp
[Fact]
public void Resolve_DecodesAll16PaletteValues()
{
    // Test that all 16 palette values (0-15) decode correctly
    for (var i = 0; i < 16; i++)
    {
        var seedCode = Guid.NewGuid();
        var bytes = seedCode.ToByteArray();
        var low = BitConverter.ToUInt64(bytes, 0);
        
        // Encode palette value at bits 29-32
        var encodedLow = (low & ~(0xFUL << 29)) | ((ulong)i << 29);
        var encodedBytes = new byte[16];
        BitConverter.TryWriteBytes(encodedBytes.AsSpan(0), encodedLow);
        BitConverter.TryWriteBytes(encodedBytes.AsSpan(8), BitConverter.ToUInt64(bytes, 8));
        var encodedSeedCode = new Guid(encodedBytes);
        
        var decoded = SeedWorldResolver.Resolve(encodedSeedCode);
        var decodedValue = (int)decoded.BuildingLayoutPalette;
        
        Assert.Equal(i, decodedValue);
    }
}
```

### Step 2: Run test to verify it fails

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "Resolve_DecodesAll16PaletteValues"`
Expected: FAIL (values 8-15 would wrap to 0-7 with current modulo 8)

### Step 3: Update SeedWorldResolver Resolve method to use modulo 16

Find the line in SeedWorldResolver.cs that reads:
```csharp
buildingLayoutPalette = (BuildingLayoutPalette)((int)buildingLayoutPalette % 8);
```

Replace with:
```csharp
// 4-bit buildingLayoutPalette produces 0-15, which maps to 16 palettes (12 functional, 4 reserved).
// Wrap within the current legal range using modulo (16 palettes).
buildingLayoutPalette = (BuildingLayoutPalette)((int)buildingLayoutPalette % 16);
```

### Step 4: Enable BuildingLayoutPalette validation

Find the commented-out validation code in SeedWorldResolver.cs:
```csharp
// TODO: Add BuildingLayoutPalette validation after enum is finalized
// if (!Enum.IsDefined(typeof(BuildingLayoutPalette), seedWorld.BuildingLayoutPalette))
// {
//     return SeedWorldValidationResult.Failed("Building layout palette is invalid.");
// }
```

Replace with:
```csharp
if (!Enum.IsDefined(typeof(BuildingLayoutPalette), seedWorld.BuildingLayoutPalette))
{
    return SeedWorldValidationResult.Failed("Building layout palette is invalid.");
}
```

### Step 5: Update canonical seed world to use NoSpurs_SpreadEvenly

Find the `CreateCanonicalSeedWorldShape` method in SeedWorldResolver.cs and update the BuildingLayoutPalette parameter from `HubAndSpoke` to `NoSpurs_SpreadEvenly`.

### Step 6: Run tests to verify they pass

Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "Resolve_DecodesAll16PaletteValues"`
Run: `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "SeedWorldResolverTests"`

Expected: PASS

### Step 7: Commit

```bash
git add src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs tests/WildBunch.GameContent.Tests/NewGame/SeedWorldResolverCodecTests.cs
git commit -m "fix: update SeedWorldResolver for 16-palette range

- Update modulo from 8 to 16 to handle new palette values
- Enable BuildingLayoutPalette validation
- Update canonical seed world to use NoSpurs_SpreadEvenly
- Add test to verify all 16 palette values decode correctly"
```
