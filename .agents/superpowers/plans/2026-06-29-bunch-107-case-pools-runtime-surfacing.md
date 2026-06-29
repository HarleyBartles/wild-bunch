# BUNCH-107: Case File Pool Restructuring and Runtime Surfacing

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Strip town-specific warrants/clues from `SeedCaseBuilder`, restructure case file pools (KnownClues=1 prologue, PublicClues=full surface-tagged pool, KnownWarrants=0, PublicWarrants=7 gang + 21 unrelated), expand the unrelated criminal roster to 21, and build the runtime wanted-poster and clue surfacing system with dual determinism (boring=salt-free deterministic, classic=salt-derived).

**Architecture:** `SeedCaseBuilder` populates the pools at setup time. `GameSession` investigation methods (`ReadWantedPosters`, `FollowTelegraphLeads`, `GatherLocalGossip`, `LookAroundSaloon`) currently peek the next item from the pool in order. We replace the ordered peek with a town+visit+salt-aware selection (`WantedPosterResolver`, `ClueSurfacingResolver`) that picks which warrant/clue from the pool surfaces in the current town on the current visit. Boring mode uses index-based deterministic selection; classic mode uses salt-derived selection.

**Tech Stack:** C#/.NET 10, xUnit, WildBunch.Domain + WildBunch.GameContent + WildBunch.Application

## Global Constraints

- GameSession is the aggregate root — investigation mutations flow through it.
- Hidden culprit truth stays internal. Culprit warrant is gated behind killer release gate.
- Wallet and Inventory are concrete; no generic supplies.
- Do not normalize runtime session state into DB tables — JSON snapshot.
- Boring mode = deterministic (no salt). Classic mode = salt-derived.
- Seed owns the cast/map/culprit. Salt owns the distribution.
- Every town has a sheriff office, saloon, and notice board (always present, not encoded).
- Unrelated criminal roster must be >= 3x max gang size (21 for 7 gang members).
- Killer release threshold gates on clues today (broken, not in scope to fix).

---

## File Structure

### Files to modify

- `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs` — strip town-specific warrant/clue generation, restructure pools
- `src/WildBunch.GameContent/NewGame/CaseCharacterRoster.cs` — expand unrelated criminal pool from 6 to 21
- `src/WildBunch.Domain/Cases/CaseFile.cs` — add `PeekPublicWarrantForTown` / `PeekPublicClueForTown` methods that accept a selector
- `src/WildBunch.Domain/Game/GameSession.cs` — wire `ReadWantedPosters` / `FollowTelegraphLeads` / `GatherLocalGossip` / `LookAroundSaloon` to use resolver-based selection
- `src/WildBunch.GameContent/NewGame/SeedWorldMapLayout.cs` — fix crash on derived town names (slot-based coordinates)
- `src/WildBunch.Application/Games/Commands/ReadWantedPostersHandler.cs` — may need resolver injection
- Tests: `SeedCaseBuilder` pool tests, `SeededNewGameFactoryTests`, `GameSetupResolverTests`, `TravelTestSeedCatalogGuardrailTests`, `GetStartingTownMapHandlerTests`, `GetStartingTownsHandlerTests`, `PurchaseStoreItemHandlerTests`, `GetTownStoreOffersHandlerTests`, integration tests

### Files to create

- `src/WildBunch.Domain/Cases/WantedPosterResolver.cs` — boring/salt warrant selection per town+visit (domain service, stateless)
- `src/WildBunch.Domain/Cases/ClueSurfacingResolver.cs` — boring/salt clue selection per town+visit+surface (domain service, stateless)
- `tests/WildBunch.Domain.Tests/WantedPosterResolverTests.cs` — boring-mode determinism, salt-mode variation, culprit gating
- `tests/WildBunch.Domain.Tests/ClueSurfacingResolverTests.cs` — boring-mode determinism, surface-tag filtering

> **Architecture note:** The resolvers live in `WildBunch.Domain` (not `WildBunch.GameContent`) because `GameSession` (in Domain) calls them directly and Domain cannot reference GameContent. They are stateless domain services that use `CaseFile` + `SaltSource` (both domain types). The selection logic (index-based for boring, hash-based for salt) is pure domain logic.

---

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

## Task 2: Expand unrelated criminal pool from 6 to 21

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/CaseCharacterRoster.cs:290-370`
- Test: `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs` (new or existing)

**Interfaces:**
- Consumes: nothing new
- Produces: `CaseCharacterRoster.UnrelatedWantedCriminalPool` returns 21 entries (up from 6)

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void UnrelatedWantedCriminalPool_HasAtLeast21Entries()
{
    Assert.True(CaseCharacterRoster.UnrelatedWantedCriminalPool.Count >= 21);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~UnrelatedWantedCriminalPool_HasAtLeast21Entries"`
Expected: FAIL (count is 6)

- [ ] **Step 3: Add 15 new unrelated criminal entries**

Add 15 new `Wanted(...)` entries to the `UnrelatedWantedCriminals` array in `CaseCharacterRoster.cs`, following the same pattern as the existing 6 (fictional economy warrants, varied dispositions, bounties, aliases, features, issuing sources). Names should be distinct Western-flavored fictional names.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~UnrelatedWantedCriminalPool_HasAtLeast21Entries"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat: expand unrelated criminal pool from 6 to 21 for parity/respawn coverage"
```

---

## Task 3: Strip town-specific warrants and clues from SeedCaseBuilder

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- Test: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`

**Interfaces:**
- Consumes: `CaseCharacterRoster.UnrelatedWantedCriminalPool` (21 entries after Task 2)
- Produces: `SeedCaseBuilder.CreateCaseFile` / `CreateCanonicalCaseFile` that populate:
  - KnownClues: 1 (prologue)
  - PublicClues: 6 base surface-tagged clues (no town-specific additions)
  - KnownWarrants: 0
  - PublicWarrants: 7 gang warrants + 21 unrelated criminal warrants (28 total)

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void CaseFile_StartsWithOneKnownClueAndZeroKnownWarrants()
{
    var factory = new SeededNewGameFactory();
    var session = factory.Create("Ranger Vale");

    Assert.Single(session.CaseFile.KnownClues);
    Assert.Empty(session.CaseFile.KnownWarrants);
}

[Fact]
public void CaseFile_PublicWarrants_HasSevenGangPlusTwentyOneUnrelated()
{
    var factory = new SeededNewGameFactory();
    var session = factory.Create("Ranger Vale");

    Assert.Equal(28, session.CaseFile.PublicWarrants.Count);
    Assert.Equal(7, session.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.GangMember || w.Terms.TargetKind == InvestigationTargetKind.TrueCulprit));
    Assert.Equal(21, session.CaseFile.PublicWarrants.Count(w => w.Terms.TargetKind == InvestigationTargetKind.UnrelatedWantedCriminal));
}

[Fact]
public void CaseFile_PublicClues_HasSixBaseCluesNoTownSpecificOnes()
{
    var factory = new SeededNewGameFactory();
    var session = factory.Create("Ranger Vale");

    Assert.Equal(6, session.CaseFile.PublicClues.Count);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~CaseFile_StartsWithOneKnownClue|FullyQualifiedName~CaseFile_PublicWarrants_HasSevenGang|FullyQualifiedName~CaseFile_PublicClues_HasSixBase"`
Expected: FAIL

- [ ] **Step 3: Strip town-specific methods from SeedCaseBuilder**

Remove `CreateTownSpecificPublicClues` and `CreateTownSpecificPublicWarrants` methods entirely. Remove the calls to them in `CreatePublicClues` and `CreatePublicWarrants`.

- [ ] **Step 4: Restructure PublicWarrants to include all 7 gang + 21 unrelated**

Replace `CreatePublicWarrants` to build:
- 7 gang member warrants (one per suspect, including true culprit — culprit's warrant has `InvestigationTargetKind.TrueCulprit`)
- 21 unrelated criminal warrants from `CaseCharacterRoster.UnrelatedWantedCriminalPool`
- All tagged with `InvestigationSourceKind.SheriffWarrants`

Remove the `publicWarrant1` / `publicWarrant2` parameters from `BuildCase` and the public entry points.

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~CaseFile_StartsWithOneKnownClue|FullyQualifiedName~CaseFile_PublicWarrants_HasSevenGang|FullyQualifiedName~CaseFile_PublicClues_HasSixBase"`
Expected: PASS

- [ ] **Step 6: Fix SeededNewGameFactoryTests assertions**

Update `CreatesRicherSeedWorldAndCase`:
- `PublicClues.Count` = 6 (not 20)
- `PublicWarrants.Count` = 28 (not 9)
- Remove assertions on specific town-specific warrant names/clue descriptions that no longer exist
- Keep assertions on gang roster, culprit, opening lead, known clues

- [ ] **Step 7: Fix GameSetupResolverTests assertions**

Update `CanonicalTemplateUsesTheExplicitCanonicalPlan`:
- `PublicClues.Count` = 6
- `PublicWarrants.Count` = 28
- Remove assertions on specific public warrant names that were the old 2 base warrants

- [ ] **Step 8: Run all GameContent.Tests**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.GameContent.Tests"`
Expected: PASS

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor: strip town-specific warrants/clues, restructure case file pools"
```

---

## Task 4: Build WantedPosterResolver (boring + salt mode)

**Files:**
- Create: `src/WildBunch.Domain/Cases/WantedPosterResolver.cs`
- Test: `tests/WildBunch.Domain.Tests/WantedPosterResolverTests.cs`

**Interfaces:**
- Consumes: `CaseFile.PublicWarrants` (pool of 28), `CaseFile.KnownWarrants` (already collected), `SaltSource`, town slot index, visit count
- Produces: `WantedPosterResolver.Resolve(CaseFile, int townSlotIndex, int visitCount, SaltSource? salt)` → `Warrant?` (the warrant that surfaces on the wanted poster in this town on this visit, or null if pool exhausted)

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

        // Try all town/visit combos — culprit warrant should never surface
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

## Task 5: Build ClueSurfacingResolver (boring + salt mode)

**Files:**
- Create: `src/WildBunch.Domain/Cases/ClueSurfacingResolver.cs`
- Test: `tests/WildBunch.Domain.Tests/ClueSurfacingResolverTests.cs`

**Interfaces:**
- Consumes: `CaseFile.PublicClues` (pool of 6), `CaseFile.KnownClues`, `InvestigationSourceKind` (surface tag), `SaltSource`, town slot index, visit count
- Produces: `ClueSurfacingResolver.Resolve(CaseFile, InvestigationSourceKind surface, int townSlotIndex, int visitCount, SaltSource? salt)` → `Clue?`

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

## Task 6: Wire resolvers into GameSession investigation methods

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs:2711-2800` (ReadWantedPosters), `3081-3141` (FollowTelegraphLeads), `3143+` (GatherLocalGossip), `2802+` (LookAroundSaloon)
- Test: `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`

**Interfaces:**
- Consumes: `WantedPosterResolver`, `ClueSurfacingResolver` (from Tasks 4-5)
- Produces: `GameSession.ReadWantedPosters()` / `FollowTelegraphLeads()` / `GatherLocalGossip()` / `LookAroundSaloon()` that use resolver-based selection instead of ordered peek

The GameSession needs access to the resolvers. Since GameSession is the aggregate root and is constructed via `GameSession.StartNew`, the resolvers should be injected or constructed internally. Given the current pattern where GameSession doesn't use DI, the resolvers should be created internally as simple stateless services.

- [ ] **Step 1: Write failing test for resolver-based wanted poster selection**

```csharp
[Fact]
public void ReadWantedPosters_InBoringMode_SurfacesDifferentWarrantsInDifferentTowns()
{
    var factory = new SeededNewGameFactory();
    var session = factory.Create("Ranger Vale", GameDifficulty.Standard, seedCode, GameEntropy.Boring);

    // Read posters in starting town
    var firstResult = session.ReadWantedPosters();
    var firstWarrant = session.CaseFile.KnownWarrants.LastOrDefault();
    Assert.NotNull(firstWarrant);

    // Travel to another town and read posters there
    // (use test travel helper to move to a different town)
    // Read posters in new town
    var secondResult = session.ReadWantedPosters();
    var secondWarrant = session.CaseFile.KnownWarrants.LastOrDefault();
    Assert.NotNull(secondWarrant);
    Assert.NotEqual(firstWarrant!.Id, secondWarrant!.Id);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~ReadWantedPosters_InBoringMode_SurfacesDifferentWarrantsInDifferentTowns"`
Expected: FAIL (current implementation peeks in order, not by town)

- [ ] **Step 3: Wire WantedPosterResolver into ReadWantedPosters**

Add resolver fields to GameSession. Replace `CaseFile.PeekNextPublicWarrant(InvestigationSourceKind.SheriffWarrants)` with `WantedPosterResolver.Resolve(CaseFile, CurrentTownSlotIndex, CurrentTownVisitCount, SaltSource)`.

The town slot index can be derived from the town's position in `World.Towns`. The visit count comes from `CurrentTownVisit`.

- [ ] **Step 4: Wire ClueSurfacingResolver into FollowTelegraphLeads, GatherLocalGossip, LookAroundSaloon**

Replace `CaseFile.PeekNextPublicClue(...)` calls with `ClueSurfacingResolver.Resolve(CaseFile, surface, CurrentTownSlotIndex, CurrentTownVisitCount, SaltSource)`.

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test --filter "FullyQualifiedName~ReadWantedPosters_InBoringMode_SurfacesDifferentWarrantsInDifferentTowns"`
Expected: PASS

- [ ] **Step 6: Fix any remaining test failures from the wiring change**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.GameContent.Tests"`
Fix any tests that depended on the old ordered-peek behavior.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: wire WantedPosterResolver and ClueSurfacingResolver into GameSession"
```

---

## Task 7: Fix GetStartingTownsHandlerTests and GetTownStoreOffersHandlerTests

**Files:**
- Modify: `tests/WildBunch.Application.Tests/GetStartingTownsHandlerTests.cs`
- Modify: `tests/WildBunch.Application.Tests/GetTownStoreOffersHandlerTests.cs`
- Modify: `tests/WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs`

These tests fail because they reference old town names (pinecross, redmesa, etc.) or old service flags (Supplies) that no longer exist in the derived town model.

- [ ] **Step 1: Run the failing tests to see current errors**

Run: `dotnet test --filter "FullyQualifiedName~GetStartingTownsHandlerTests|FullyQualifiedName~GetTownStoreOffersHandlerTests|FullyQualifiedName~PurchaseStoreItemHandlerTests"`

- [ ] **Step 2: Fix GetStartingTownsHandlerTests**

Update tests to use the canonical world's actual towns instead of hardcoded names. Use `SeedWorldCatalog.CreateCanonicalWorld().Towns` to get valid town IDs.

- [ ] **Step 3: Fix GetTownStoreOffersHandlerTests**

Update the test that checks for empty catalog when town has no store services — every town now has a store (prosperity-driven). Adjust assertions to check prosperity-based stock instead.

- [ ] **Step 4: Fix PurchaseStoreItemHandlerTests**

Update the test that expects purchase to fail for unknown offers — adjust to use the new prosperity-based store catalog.

- [ ] **Step 5: Run all Application.Tests**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.Application.Tests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "test: fix Application tests for derived town model and prosperity-based stores"
```

---

## Task 8: Fix integration tests

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/**/*.cs` (134 failures)

Integration tests likely fail for the same reasons: hardcoded town names, old trail counts, old warrant/clue counts, old service flags.

- [ ] **Step 1: Run integration tests to categorize failures**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.Integration.Tests" 2>&1 | Select-String "Failed "`
Categorize failures by type (town name, count, service, warrant/clue).

- [ ] **Step 2: Fix town-name-dependent tests**

Replace hardcoded town IDs with canonical world's actual town IDs.

- [ ] **Step 3: Fix count-dependent tests**

Update trail counts (12 for 8 towns), warrant counts (28 public, 0 known at start), clue counts (6 public, 1 known at start).

- [ ] **Step 4: Fix service/store-dependent tests**

Update for prosperity-based stores and always-present sheriff/saloon/noticeboard.

- [ ] **Step 5: Run all integration tests**

Run: `dotnet test --filter "FullyQualifiedName~WildBunch.Integration.Tests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "test: fix integration tests for derived town model and restructured case pools"
```

---

## Task 9: Unrelated criminal parity system (runtime)

**Files:**
- Create: `src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs` (or similar)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (wire in when criminal taken in)
- Test: `tests/WildBunch.Domain.Tests/UnrelatedCriminalLedgerTests.cs`

**Interfaces:**
- Consumes: `CaseFile.PublicWarrants` (unrelated criminal warrants), `CaseFile.KnownWarrants`
- Produces: parity tracking — when a criminal is taken in, spawn replacement if gang count allows; despawn to maintain parity

This is the most complex runtime piece. The parity system tracks:
- Active unrelated criminals (pool for surfacing)
- Taken-in criminals (removed from pool)
- Spawn/despawn rules

- [ ] **Step 1: Write failing tests for parity rules**

```csharp
[Fact]
public void TakingInCriminal_SpawnsReplacement_WhenBelowGangParity()
{
    var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
    var active = ledger.GetActiveCriminalCount();
    Assert.Equal(7, active); // starts at parity

    ledger.RecordTakenIn(criminalId);
    active = ledger.GetActiveCriminalCount();
    Assert.Equal(7, active); // replacement spawned
}

[Fact]
public void TakingInCriminal_DoesNotSpawn_WhenAtGangParity()
{
    var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
    // Take in all 7, each time a replacement spawns
    for (int i = 0; i < 7; i++) ledger.RecordTakenIn($"criminal-{i}");
    Assert.Equal(7, ledger.GetActiveCriminalCount());

    // Take in one more — no replacement since we'd exceed parity
    ledger.RecordTakenIn("criminal-extra");
    Assert.Equal(6, ledger.GetActiveCriminalCount());
}

[Fact]
public void DespawnPrefersCriminalsPlayerHasNotCollectedWarrantFor()
{
    var ledger = new UnrelatedCriminalLedger(gangMemberCount: 7, poolSize: 21);
    // Mark some as warrant-collected
    ledger.MarkWarrantCollected("criminal-0");
    ledger.MarkWarrantCollected("criminal-1");

    // Despawn to reduce by 2
    var despawned = ledger.Despawn(count: 2);
    // Should despawn uncollected ones first
    Assert.DoesNotContain("criminal-0", despawned);
    Assert.DoesNotContain("criminal-1", despawned);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~UnrelatedCriminalLedgerTests"`
Expected: FAIL (type not found)

- [ ] **Step 3: Implement UnrelatedCriminalLedger**

Implement the parity tracking, spawn, and despawn logic per the issue spec.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter "FullyQualifiedName~UnrelatedCriminalLedgerTests"`
Expected: PASS

- [ ] **Step 5: Wire into GameSession**

Hook the ledger into the criminal turn-in flow in GameSession. When a wanted suspect is turned in, record it in the ledger. The ledger adjusts the active pool, which affects what `WantedPosterResolver` can surface.

- [ ] **Step 6: Run full test suite**

Run: `dotnet test`
Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add unrelated criminal parity system with spawn/despawn rules"
```

---

## Task 10: Full validation and commit

**Files:**
- All modified files

- [ ] **Step 1: Run full build**

Run: `dotnet build`
Expected: 0 errors

- [ ] **Step 2: Run full test suite**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 3: Run EF migrations check (if persistence affected)**

Run: `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
Expected: No new migrations needed (JSON snapshot, no table changes)

- [ ] **Step 4: Verify worktree is clean**

Run: `git status`
Expected: clean working tree (all committed)

- [ ] **Step 5: Final commit if any remaining changes**

```bash
git add -A
git commit -m "chore: final validation pass for BUNCH-107"
```

---

## Self-Review

### Spec coverage
- Case file pool restructuring (KnownClues=1, PublicClues=6, KnownWarrants=0, PublicWarrants=28): Task 3 ✓
- Unrelated criminal pool 21 entries: Task 2 ✓
- Runtime wanted-poster system (boring + salt): Task 4 ✓
- Runtime clue surfacing: Task 5 ✓
- Wire resolvers into GameSession: Task 6 ✓
- Parity/respawn/despawn: Task 9 ✓
- SeedWorldMapLayout fix: Task 1 ✓
- Application test fixes: Task 7 ✓
- Integration test fixes: Task 8 ✓
- Full validation: Task 10 ✓

### Placeholder scan
- Task 2 Step 3 says "Add 15 new entries" — the actual names/features need to be written. This is intentional since the names are creative content that should be written during implementation following the existing pattern.
- Task 7 and Task 8 are intentionally high-level because the exact test failures depend on the state after Tasks 1-6 land. The fix patterns are described but exact assertions will be determined at implementation time.

### Type consistency
- `WantedPosterResolver.Resolve` returns `Warrant?` — used in Task 6
- `ClueSurfacingResolver.Resolve` returns `Clue?` — used in Task 6
- `UnrelatedCriminalLedger` methods are consistent across Task 9 steps
- `CaseFile.PublicWarrants` / `KnownWarrants` / `PublicClues` / `KnownClues` are existing properties used consistently
