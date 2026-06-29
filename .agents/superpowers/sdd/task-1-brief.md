## Task 1: Fix SeedWorldMapLayout crash on derived town names

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs`
- Test: `tests/WildBunch.Application.Tests/GetStartingTownMapHandlerTests.cs`

**Interfaces:**
- Consumes: `SeedWorldCatalog.CreateCanonicalWorld()` (existing)
- Produces: `SeedWorldMapLayout.GetMapTowns()` / `GetMapTrails()` that no longer crash on derived town names

The current `SeedWorldMapLayout` has a hardcoded `TownCoordinates` dictionary keyed by town ID string ("pinecross", "redmesa", etc.). With seed-derived town selection, town IDs come from the 40-entry name pool and won't all be in that dictionary. We need slot-based coordinates derived from the town's slot index.

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void GetMapTowns_DoesNotCrashWithDerivedTownNames()
{
    var towns = SeedWorldMapLayout.GetMapTowns();
    Assert.NotEmpty(towns);
    Assert.All(towns, town => Assert.True(town.X >= 0 && town.Y >= 0));
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~GetMapTowns_DoesNotCrashWithDerivedTownNames"`
Expected: FAIL with KeyNotFoundException

- [ ] **Step 3: Implement slot-based coordinates**

Replace the hardcoded `TownCoordinates` dictionary with a deterministic coordinate generator based on slot index. Use a simple radial/grid layout: slot 0 at center, remaining slots arranged in a ring or grid pattern.

```csharp
public static class SeedWorldMapLayout
{
    private const int CenterX = 400;
    private const int CenterY = 450;
    private const int RingRadius = 250;

    public static IReadOnlyList<SeedMapTown> GetMapTowns()
    {
        var world = SeedWorldCatalog.CreateCanonicalWorld();
        var towns = world.Towns.ToArray();
        return towns
            .Select((town, index) =>
            {
                var (x, y) = GetCoordinatesForSlot(index, towns.Length);
                return new SeedMapTown(town.Id.Value, town.Name, town.Services, x, y);
            })
            .ToArray();
    }

    private static (int X, int Y) GetCoordinatesForSlot(int slotIndex, int totalTowns)
    {
        if (slotIndex == 0) return (CenterX, CenterY);
        var angle = (slotIndex - 1) * (2.0 * Math.PI / Math.Max(1, totalTowns - 1));
        var x = (int)(CenterX + RingRadius * Math.Cos(angle));
        var y = (int)(CenterY + RingRadius * Math.Sin(angle));
        return (x, y);
    }

    public static IReadOnlyList<SeedMapTrailEdge> GetMapTrails()
    {
        var world = SeedWorldCatalog.CreateCanonicalWorld();
        return world.Trails
            .Select(trail => new SeedMapTrailEdge(
                trail.Id.Value,
                trail.FromTownId.Value,
                trail.ToTownId.Value,
                trail.RideDayDistance))
            .ToArray();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~GetMapTowns_DoesNotCrashWithDerivedTownNames"`
Expected: PASS

- [ ] **Step 5: Fix GetStartingTownMapHandlerTests assertions**

Update tests that assert on specific town counts (8), trail counts (9), and coordinate values to use the canonical world's actual counts and slot-based coordinates.

- [ ] **Step 6: Run all Application.Tests to verify**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.Application.Tests"`
Expected: All GetStartingTownMapHandlerTests pass

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "fix: SeedWorldMapLayout uses slot-based coordinates instead of hardcoded town IDs"
```

---

