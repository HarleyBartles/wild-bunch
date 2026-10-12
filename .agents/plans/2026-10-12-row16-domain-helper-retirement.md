# Domain Dormant Helper Retirement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retire confirmed unused Domain convenience APIs, the unreachable duplicate travel branch, and stale task comments without changing supported game behavior.

**Architecture:** Keep `GameSession`, `JourneyUpkeepRules`, and current travel/event paths as the behavior owners. Remove only APIs and branches whose live production callers do not exist; retain town refresh policy behavior and all current travel upkeep, encounter, and inventory semantics.

**Tech Stack:** C#/.NET 10, xUnit, Git, repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; DN-14 in `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline. The retired methods, tests, unreachable branch, and dated DN-14 disposition form one bounded Domain-maintenance slice; one implementation context avoids splitting small removals, while one fresh whole-branch review independently checks the complete set.

## Global Constraints

- Preserve every supported player-facing result, event, event order, replay result, travel upkeep rule, inventory rule, and persisted shape.
- Do not add a behavior test for code absence or a structural inventory test; remove only tests whose sole behavior is an API explicitly retired here.
- Preserve `JourneyUpkeepRules.ApplyDailyUpkeep`, trail-event horse deltas, and their existing behavior tests as the current travel-condition paths.
- Leave `TownSourceRefreshPolicy` and `TownSourceVisitState.RefreshForVisit` unchanged; their policy semantics are outside this confirmed-dead-code slice.
- Keep the `.109` version until the successor implementation is complete; then advance `Directory.Build.props` once to `0.1.0-dev.110` and retire the `.109` predecessor plan in this PR's first substantive commit.
- Deliver by reviewed PR to `develop`; this plan uses a fresh worktree from refreshed `origin/develop`, retires the clean worktree before its paired local/upstream branch refs, and verifies all three absences.

## Review Focus

- Confirm all removed APIs have no production caller and no remaining test relies on them; deleting legacy horse advancement must not alter `JourneyUpkeepRules` or trail-event horse-state changes.
- Confirm the removed duplicate `encounterCount == 0` branch was unreachable after the first branch returns and did not affect generator version or deterministic output.
- Confirm citizen encounter selection and concealment still use `CitizenCast` and its live descriptor path after deleting unused reveal helpers and their helper-only tests.

---

### Task 1: Commit the JIT plan before implementation

**Files:**
- Add: `.agents/plans/2026-10-12-row16-domain-helper-retirement.md`

- [x] Commit this plan by itself before changing Domain code, tests, investigation, roadmap, or version.

### Task 2: Retire dormant Domain APIs and complete their successor disposition

**Files:**
- Modify: `src/WildBunch.Domain/Inventory/Inventory.cs`
- Modify: `src/WildBunch.Domain/Inventory/HorseTravelState.cs`
- Modify: `src/WildBunch.Domain/Game/CitizenCast.cs`
- Modify: `src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs`
- Modify: `src/WildBunch.Domain/Game/JourneyLoop.cs`
- Modify: `tests/WildBunch.Domain.Tests/InventoryTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/CitizenCastTests.cs`
- Verify: `tests/WildBunch.Domain.Tests/JourneyUpkeepRulesTests.cs`
- Verify: `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-domain-event-dispatcher.md`

**Interfaces:**
- Consumes: The confirmed current call map for the listed Domain APIs, retained travel-upkeep behavior, PR #294's merge/review/gate/cleanup evidence, and the completed `.109` plan's full scope.
- Produces: The same supported game behavior with dead helpers retired, only the addressed DN-14 findings closed, the `.109` plan semantically retired, and `0.1.0-dev.110` routed from row 16.

- [x] Run targeted `rg` searches across `src` and `tests` for `GetHorseStateOrNull`, `CanAddItem`, `AdvanceHorseState`, `AdvanceTravelDay(bool horseFed)`, `ResolveRevealName`, and `ResolveRevealNarration`; confirm only the inventoried declaration/test sites remain before editing.
- [x] Remove `Inventory.GetHorseStateOrNull` because it is an exact forwarding alias with no caller, and remove `Inventory.CanAddItem` because it has no caller and validates shape without guaranteeing that `AddItem` will accept a duplicate nonstackable item.
- [x] Remove `Inventory.AdvanceHorseState` and `HorseTravelState.AdvanceTravelDay(bool horseFed)` because their only use is a helper-specific inventory test; current daily condition changes are owned by `JourneyUpkeepRules.ApplyDailyUpkeep`, and encounter pressure is owned by trail/gameplay event paths.
- [x] Update `HorseAndCanteenStateCanBeExplicitlySeededAndMutated` to set a concrete horse state through `Inventory.SetHorseState` and a concrete canteen state through `Inventory.SetCanteenState`; retain assertions about stored inventory state and do not test the removed daily-advancement convenience.
- [x] Remove `CitizenCast.ResolveRevealName`, `CitizenCast.ResolveRevealNarration`, and only their two helper-only tests; preserve `ResolveDescriptor`, encounter selection, role lookup, and all live saloon behavior.
- [x] Remove the second identical zero-encounter early return in `TravelDayPlanGenerator.Generate`; preserve the first return, generator version, seed composition, roll sequence, and all encounter generation behavior.
- [x] Remove the obsolete numbered-task comments from `JourneyLoop`; do not rewrite current architecture documentation or unrelated comments.
- [x] Add a dated DN-14 disposition closing only the unused Inventory APIs, unused citizen reveal helpers and their helper-only tests, unreachable duplicate encounter branch, stale JourneyLoop task comments, and legacy horse daily-advancement wrapper; leave refresh-policy semantics and all unrelated DN-14 findings open.
- [x] Correct only the per-file rows for `Inventory.cs`, `HorseTravelState.cs`, `CitizenCast.cs`, `TravelDayPlanGenerator.Context.cs`, and `JourneyLoop.cs`; preserve the original audit text and dated dispatcher disposition.
- [x] Record PR #294's reviewed source `685268ebce7fb77d49ef437937f672649489f5a8`, merge `75990e011b02da94fdf108a832a1a4772be56049`, clean whole-branch review, passing exact-head gate `38227578330`, passing develop push gate `38228260904`, primary fast-forward, and verified worktree/local/upstream branch cleanup in row 16.
- [x] Retire the completed `.109` dispatcher plan, route this `.110` successor from row 16, and advance `Directory.Build.props` exactly once from `0.1.0-dev.109` to `0.1.0-dev.110`.
- [x] Keep the baseline specification, ADRs, and feature matrix unchanged because the slice removes unused implementation surfaces without changing a product promise, dependency, event contract, or durable architecture decision.
- [x] Run `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~CitizenCastTests|FullyQualifiedName~InventoryTests|FullyQualifiedName~JourneyUpkeepRulesTests|FullyQualifiedName~TravelDayPlanGeneratorTests"`; expect all matching Domain tests to pass.
- [x] Inspect the full diff; confirm no remaining caller references the retired APIs, no current daily-upkeep or saloon behavior changed, `git diff --check` passes, and the predecessor plan and stale link are removed in this first substantive commit.
- [x] Review and stage the intended tree, then commit through the normal check-only hook; do not run the full CI gate immediately before a normal commit and do not bypass the hook.

**Validation:** Existing independent travel-upkeep, generator, inventory-state, and CitizenCast-selection tests cover retained behavior. This slice changes no supported game outcome and adds no new behavior test; the two deleted CitizenCast tests asserted only unused helper output. Run the focused Domain suite above, then use the normal commit hook and hosted exact-head gate as the canonical delivery checks.
