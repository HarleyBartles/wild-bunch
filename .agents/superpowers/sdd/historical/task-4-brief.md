## Task 4: Build WantedPosterResolver (boring + salt mode)

**Files:**
- Create: `src/WildBunch.Domain/Cases/WantedPosterResolver.cs`
- Test: `tests/WildBunch.Domain.Tests/WantedPosterResolverTests.cs`

**Interfaces:**
- Consumes: `CaseFile.PublicWarrants` (pool of 28), `CaseFile.KnownWarrants` (already collected), `SaltSource`, town slot index, visit count
- Produces: `WantedPosterResolver.Resolve(CaseFile, int townSlotIndex, int visitCount, SaltSource? salt)` â†’ `Warrant?` (the warrant that surfaces on the wanted poster in this town on this visit, or null if pool exhausted)

Selection rules:
- Boring mode (salt is null): `warrants[(townSlotIndex + visitCount) % eligibleCount]` where eligible = all PublicWarrants not already in KnownWarrants, and culprit warrant excluded unless killer released
- Salt mode: hash(salt + townSlotIndex + visitCount) % eligibleCount

- [ ] **Step 1: Write failing tests**

```csharp
public sealed class WantedPosterResolverTests
{
    [Fact]
    public void BoringMode_SameTownSameVisit_ReturnsSameWarrant()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        var first = resolver.Resolve(caseFile, townSlotIndex: 2, visitCount: 0, salt: null);
        var second = resolver.Resolve(caseFile, townSlotIndex: 2, visitCount: 0, salt: null);

        Assert.NotNull(first);
        Assert.Equal(first!.Id, second!.Id);
    }

    [Fact]
    public void BoringMode_DifferentTowns_ReturnDifferentWarrants()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        var town0 = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null);
        var town1 = resolver.Resolve(caseFile, townSlotIndex: 1, visitCount: 0, salt: null);

        Assert.NotNull(town0);
        Assert.NotNull(town1);
        Assert.NotEqual(town0!.Id, town1!.Id);
    }

    [Fact]
    public void BoringMode_CulpritWarrantNotSurfacesUntilReleased()
    {
        var caseFile = BuildTestCaseFile(killerReleased: false);
        var resolver = new WantedPosterResolver();

        // Try all town/visit combos â€” culprit warrant should never surface
        for (int town = 0; town < 8; town++)
        {
            for (int visit = 0; visit < 5; visit++)
            {
                var warrant = resolver.Resolve(caseFile, town, visit, salt: null);
                Assert.NotNull(warrant);
                Assert.NotEqual(InvestigationTargetKind.TrueCulprit, warrant!.Terms.TargetKind);
            }
        }
    }

    [Fact]
    public void BoringMode_AlreadyKnownWarrantsAreSkipped()
    {
        var caseFile = BuildTestCaseFile();
        // Reveal a warrant, then ensure it doesn't surface again
        var resolver = new WantedPosterResolver();
        var first = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null);
        Assert.NotNull(first);
        caseFile.RevealWarrant(first!);

        var next = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null);
        Assert.NotNull(next);
        Assert.NotEqual(first!.Id, next!.Id);
    }

    [Fact]
    public void SaltMode_SameInputsSameSalt_ReturnsSameWarrant()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();
        var salt = new FixedSaltSource(42);

        var first = resolver.Resolve(caseFile, townSlotIndex: 3, visitCount: 1, salt: salt);
        var second = resolver.Resolve(caseFile, townSlotIndex: 3, visitCount: 1, salt: salt);

        Assert.Equal(first!.Id, second!.Id);
    }

    [Fact]
    public void SaltMode_DifferentSalt_ReturnsDifferentWarrant()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        var saltA = new FixedSaltSource(42);
        var saltB = new FixedSaltSource(99);

        var first = resolver.Resolve(caseFile, townSlotIndex: 3, visitCount: 1, salt: saltA);
        var second = resolver.Resolve(caseFile, townSlotIndex: 3, visitCount: 1, salt: saltB);

        Assert.NotEqual(first!.Id, second!.Id);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~WantedPosterResolverTests"`
Expected: FAIL (type not found)

- [ ] **Step 3: Implement WantedPosterResolver**

```csharp
namespace WildBunch.Domain.Cases;

public sealed class WantedPosterResolver
{
    public Warrant? Resolve(CaseFile caseFile, int townSlotIndex, int visitCount, SaltSource? salt)
    {
        ArgumentNullException.ThrowIfNull(caseFile);

        var eligible = caseFile.PublicWarrants
            .Where(w => !caseFile.KnownWarrants.Any(k => k.Id.Equals(w.Id)))
            .Where(w => w.Terms.TargetKind != InvestigationTargetKind.TrueCulprit || caseFile.KillerReleaseState.IsReleased)
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

Run: `dotnet test --filter "FullyQualifiedName~WantedPosterResolverTests"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: add WantedPosterResolver with boring/salt dual determinism"
```

---

