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

