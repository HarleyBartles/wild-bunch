# Prove Projector Input Nonmutation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan inline, task by task.

**Goal:** Make the surviving Application projection test prove that projecting a populated `GameStarted` event leaves both the event sequence and its nested starting-inventory collection unchanged.

**Architecture:** `HudProjector` remains a pure Application read projection over typed Domain events. This slice changes test evidence only and does not alter production behavior.

**Tech Stack:** .NET 10, xUnit, repository command bus.

**Spec:** [Stable 0.1.0 test-quality investigation, N-10](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md#n-10-nonmutation-and-order-claims-need-independent-values)

**Execution Strategy:** `executing-plans` - one mutable nested event input and its targeted mutation proof form a single tightly coupled test-quality slice; a fresh implementer per task adds handoff cost without an independent review boundary.

## Global Constraints

- Start from current `origin/develop` in this fresh canonical worktree and deliver one PR to `develop`.
- Keep the plan-only commit at `0.1.0-dev.124`; advance the sole authored version in `Directory.Build.props` to `0.1.0-dev.125` in the first substantive implementation commit.
- Retire the N-09 plan in the first substantive implementation commit; retain this plan through delivery for its next substantive successor to assess.
- Replace the misleading HUD repeat-output assertions in `ProjectionTests.Projectors_DoNotMutateInputEvents` with direct before/after assertions over a populated event array and mutable starting-inventory list. Include food, horse, and canteen values so the snapshot is nontrivial.
- Prove the assertion fails when a temporary projector mutation changes an inventory entry, restore `HudProjector.cs` exactly, and rerun the focused test successfully.
- The named `DevSaltSourceTests` was removed in commit `290b9cf4` as part of retiring developer salt controls; do not restore it. `TravelDiaryTextRenderer` and its tests were removed in PR #247 / commit `8bcf9991` when travel narration moved to the shared journal. Check whether current mapper coverage proves the retained ordering contract; do not recreate the retired renderer.
- The specification already requires paired worktree/local/upstream-branch cleanup before the next plan. Clarify that it applies only to this plan's exact refs and is not permission to sweep other branches. No ADR or product feature-matrix change is expected.
- Before review, compare the actual diff with applicable ADRs and the feature matrix. After merge, require passing exact-head and develop-push gates, fast-forward the primary checkout, then remove the clean worktree and its exact local/upstream refs as one verified closeout.

## Review Focus

- The test must fail if the projector changes, replaces, reorders, or removes an event in the supplied event sequence, or changes an item in the supplied starting inventory list.
- Snapshot values must be captured independently before projection, not derived from the post-projection input.
- The N-10 disposition must distinguish retired test/code paths from current order coverage in the travel mapper; do not claim that deleting an old test proves its behavior is covered.
- Do not change projector output, game behavior, ADR decisions, feature promises, or stale branches unrelated to this plan.

---

### Task 1: Prove the projector preserves populated event inputs

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row17-n09-salt-selection-proof.md`
- Modify: `.agents/plans/2026-10-12-row17-n10-projector-nonmutation.md`

**Interfaces:**
- Consumes: `HudProjector.Project(IReadOnlyList<IDomainEvent>)` and the `GameStarted.StartingInventoryItems` read-only interface backed by a mutable list.
- Produces: an xUnit behavior test that independently snapshots the supplied event sequence and populated inventory values and verifies that both remain unchanged after projection.

- The test body should follow this concrete setup and assertion shape, retaining the existing event's required setup values:

```csharp
var startingItems = new List<DomainInventoryItem>
{
    new(DomainItemKind.Food, 3),
    new(DomainItemKind.Horse, 1),
    new(DomainItemKind.Canteen, 1)
};
var gameStarted = new GameStarted
{
    PlayerName = "Ranger Vale",
    StartingTownId = new TownId("pinecross"),
    StartingTownName = "Pinecross",
    StartingHealth = 100,
    StartingWallet = 25m,
    StartingInventoryItems = startingItems,
    GameDifficulty = GameDifficulty.Standard,
    SaltSource = SaltSource.CreateFixed(string.Empty),
    GameEntropy = GameEntropy.Classic
};
IDomainEvent[] events =
[
    gameStarted,
    new StoreItemPurchased
    {
        TownId = new TownId("pinecross"),
        ItemKind = DomainItemKind.Food,
        DisplayName = "Trail Biscuits",
        Quantity = 2,
        UnitPrice = 2m,
        TotalPrice = 4m,
        WalletAfter = 21m
    }
];
var eventsBefore = events.ToArray();
var itemsBefore = startingItems.ToArray();

new HudProjector().Project(events);

Assert.Equal(eventsBefore, events);
Assert.Same(eventsBefore[0], events[0]);
Assert.Same(eventsBefore[1], events[1]);
Assert.Equal(itemsBefore, startingItems);
```

- [x] Rename the existing test to describe the observed nonmutation contract, supply an `IDomainEvent[]` containing a `GameStarted` whose starting inventory is a mutable `List<InventoryItem>` with food, a horse and a canteen, and capture independent event-array and inventory-array snapshots before invoking the projector.
- [x] Assert that the event sequence and ordered inventory values equal their pre-projection snapshots, including reference identity at both positions so value-equal replacement cannot pass. Remove the unrelated repeat-projection output comparisons from this test.
- [x] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter FullyQualifiedName~HudProjector_DoesNotMutateInputEventSequenceOrStartingInventory` and confirm it passes on current production code.
- [x] Temporarily mutate `HudProjector.Project` to replace one entry in the supplied `StartingInventoryItems` list before projecting it (for example, replace index 0 with `new InventoryItem(ItemKind.Food, 4)`); rerun the focused test and confirm it fails at the before/after inventory assertion. Restore `HudProjector.cs` exactly, confirm its diff is empty, and rerun the focused test successfully.
- [x] Run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`; 161 tests passed and none were skipped.
- [x] Add a dated N-10 disposition that the existing Application projector test was strengthened; `DevSaltSourceTests` was deliberately retired by commit `290b9cf4`; and the old diary renderer/order test was retired with the separate renderer in PR #247. Confirm current `TravelDiaryMapperTests.ToDtoPreservesRecordedLatestJourneyEntriesWithSharedDayAndMessageValues` compares the ordered deduplicated current journey entries; preserve it without adding a duplicate test.
- [x] Update roadmap row 17 with N-09 delivery facts: PR #309, reviewed source `dd1d61d32c81c891eea01f9f015ae91165daffa5`, merge `254df9639e6fc327f66dd42a6a4b227b0e94dd29`, matching source and merge tree `4e67548dbdb63d23717ec4ee3b1102809df02c76`, exact-head gate `38310892376` passed, develop-push gate `38311538108` passed, and clean worktree/local/upstream ref removal verified. Keep row 17 incomplete and set N-10 active at `0.1.0-dev.125`.
- [x] Clarify the spec's paired cleanup paragraph: only the current plan's exact dedicated branch refs are removed, after its clean worktree, and that closeout must finish before another plan worktree is created. Preserve the instruction to leave unrelated, active, unmerged, protected, shared or uncertain branches untouched.
- [x] Set the sole authored version to `0.1.0-dev.125`, retire the N-09 plan in this first substantive implementation commit, and retain this plan for successor assessment. Use the normal staged-snapshot hook; do not run a redundant full gate immediately around a successful hooked commit.

### Task 2: Review, publish, merge and clean the execution unit

**Files:**
- Review the full branch diff against the N-10 investigation, specification, ADRs, feature matrix and Review Focus.

- [ ] Verify the temporary input-mutation proof failed for the intended inventory assertion and the restored source passed; verify the two historical test dispositions and current journey-order coverage against Git history and live tests.
- [ ] Obtain a fresh whole-branch review, resolve actionable findings with focused red/green evidence, and rerun the canonical gate through a normal hooked commit if the tree changes.
- [ ] Publish PR `test: prove projector input nonmutation` to `develop`, independently confirm the author's ADR and feature-matrix checks, mark it ready, and require the exact-head canonical hosted gate to pass.
- [ ] Squash merge under the authorized epic flow; verify the develop-push gate passes and the merged tree equals the reviewed source tree.
- [ ] Fast-forward the primary checkout. Verify the merged PR head, remove this clean worktree first, explicitly delete its exact local and upstream refs, and verify all three are absent before creating the next plan worktree.
- [ ] Keep this plan for its next substantive successor to assess; do not claim row 17 complete while later findings remain.
