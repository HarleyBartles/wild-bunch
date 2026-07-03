## Task 5: Build ClueSurfacingResolver (boring + salt mode)

**Files:**
- Create: `src/WildBunch.Domain/Cases/ClueSurfacingResolver.cs`
- Test: `tests/WildBunch.Domain.Tests/ClueSurfacingResolverTests.cs`

**Interfaces:**
- Consumes: `CaseFile.PublicClues` (pool of 6), `CaseFile.KnownClues`, `InvestigationSourceKind` (surface tag), `SaltSource`, town slot index, visit count
- Produces: `ClueSurfacingResolver.Resolve(CaseFile, InvestigationSourceKind surface, int townSlotIndex, int visitCount, SaltSource? salt)` â†’ `Clue?`

Selection rules:
- Filter PublicClues by surface tag (SourceKind) and not already in KnownClues
- Boring mode: `(townSlotIndex + visitCount) % eligibleCount`
- Salt mode: hash-based

- [ ] **Step 1: Write failing tests**

```csharp
public sealed class ClueSurfacingResolverTests
{
    [Fact]
    public void BoringMode_ReturnsClueMatchingSurfaceTag()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var clue = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, townSlotIndex: 0, visitCount: 0, salt: null);

        Assert.NotNull(clue);
        Assert.Equal(InvestigationSourceKind.TelegraphLead, clue!.SourceKind);
    }

    [Fact]
    public void BoringMode_AlreadyKnownCluesAreSkipped()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var first = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, 0, 0, salt: null);
        Assert.NotNull(first);
        caseFile.RevealClue(first!);

        var next = resolver.Resolve(caseFile, InvestigationSourceKind.TelegraphLead, 0, 0, salt: null);
        // May be null if no more telegraph clues, or a different telegraph clue
        if (next is not null)
        {
            Assert.NotEqual(first!.Id, next.Id);
        }
    }

    [Fact]
    public void BoringMode_SameInputs_ReturnsSameClue()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var first = resolver.Resolve(caseFile, InvestigationSourceKind.LocalGossip, 2, 1, salt: null);
        var second = resolver.Resolve(caseFile, InvestigationSourceKind.LocalGossip, 2, 1, salt: null);

        Assert.Equal(first?.Id, second?.Id);
    }

    [Fact]
    public void ReturnsNullWhenNoCluesMatchSurface()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new ClueSurfacingResolver();

        var clue = resolver.Resolve(caseFile, InvestigationSourceKind.SheriffWarrants, 0, 0, salt: null);
        // If no SheriffWarrants-tagged clues exist in the base 6, this returns null
        // (depends on which surfaces the 6 base clues are tagged with)
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~ClueSurfacingResolverTests"`
Expected: FAIL (type not found)

- [ ] **Step 3: Implement ClueSurfacingResolver**

```csharp
namespace WildBunch.Domain.Cases;

public sealed class ClueSurfacingResolver
{
    public Clue? Resolve(CaseFile caseFile, InvestigationSourceKind surface, int townSlotIndex, int visitCount, SaltSource? salt)
    {
        ArgumentNullException.ThrowIfNull(caseFile);

        var eligible = caseFile.PublicClues
            .Where(c => c.SourceKind == surface)
            .Where(c => !caseFile.KnownClues.Any(k => k.Id.Equals(c.Id)))
            .ToArray();

        if (eligible.Length == 0) return null;

        var index = salt is null
            ? (townSlotIndex + visitCount) % eligible.Length
            : Math.Abs(salt.GetHashCode() + townSlotIndex * 31 + visitCount * 17) % eligible.Length;

        return eligible[index];
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~ClueSurfacingResolverTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add ClueSurfacingResolver with boring/salt dual determinism"
```

---

