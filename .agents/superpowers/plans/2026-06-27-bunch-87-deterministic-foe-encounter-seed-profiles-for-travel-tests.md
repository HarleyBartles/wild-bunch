# BUNCH-87: Deterministic Foe Encounter Travel Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the travel tests that need foe encounters deterministic by driving the same backend dev-travel override seam that dev-overlay playthroughs already use, instead of relying on seed-only assumptions or global encounter suppression.

**Architecture:** Keep encounter determinism in the existing backend/domain dev override path. Use seed descriptors only for starting-world and route setup when a test needs them, not as the primary control for the next encounter outcome. The implementation should stay test-layer and test-infrastructure only unless a hidden gap forces a tiny helper in the product seam already in place.

**Tech Stack:** C# / .NET, xUnit, ASP.NET Core integration tests, the existing dev travel endpoints under `/api/dev`, and the current `GameSession.ForceDevTravelOverride` domain command path.

## Global Constraints

- Assumption baseline: current `origin/main` after the BUNCH-103 merge commit `ac683496f462caccd50523c369ae6568737b6ea0`.
- Do not create a separate test-only control path if the dev-overlay seam already provides the right path.
- Do not use frontend-only dev-overlay state as authoritative game truth.
- Do not patch heat, route risk, random rolls, or runtime state behind the normal travel route.
- Do not redesign the encounter system.
- Do not use Boring mode as encounter suppression.
- Heat remains future lawman pressure, not trail danger.
- Seed/profile setup is support-only here: it may establish a route shape, but it is not the primary deterministic-control mechanism for foe outcomes.
- Do not add new seed codec behavior, descriptor fields, or UUID round-trip work for this issue.
- Keep the work narrow enough that a reviewer can tell whether deterministic foe control now comes from the shared dev-travel seam.

---

### Task 1: Move the domain travel foe tests onto the shared dev override seam

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelTestFactory.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelEncounterResolutionCharacterizationTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelReplayEqualityTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalog.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalogGuardrailTests.cs`

**Interfaces:**
- Consumes: `GameSession.StartJourney`, `GameSession.ForceDevTravelOverride`, `DevTravelOverride.ForFoe`, `TravelEncounterResolutionCharacterizationTests`, `TravelReplayEqualityTests`.
- Produces: a reusable forced-foe travel fixture for tests, plus comments that stop describing seed descriptors as the encounter-control mechanism.

- [ ] **Step 1: Add a forced-foe test fixture to `TravelTestFactory`.**

Add a helper that starts a real journey through the normal setup flow, then forces the next travel-day encounter through the existing dev override seam. The helper should return the session, preview, and the forced `JourneyFoeProfile` so tests can assert the exact profile later.

Expected shape:

```csharp
internal static (GameSession session, TravelPreview preview, JourneyFoeProfile foeProfile)
    CreateForcedFoeJourney(string? encounterMessage = null)
```

The helper should:

- call an existing route/setup factory such as `CreateEasyShortJourney()`;
- start the journey with the resolved preview;
- call `session.ForceDevTravelOverride(DevTravelOverride.ForFoe(foeProfile, encounterMessage))`;
- commit the setup events if the test needs a clean replay boundary before advancing;
- leave the test on the same lawful travel route that normal gameplay would use.

- [ ] **Step 2: Rewrite the foe-resolution characterization tests to use the forced-foe fixture.**

Update `TravelEncounterResolutionCharacterizationTests` so the tests no longer loop until a natural encounter interrupts the journey. The tests should instead:

- create the journey with `CreateForcedFoeJourney()`;
- advance once through `AdvanceJourneyDay()`;
- assert that the pending encounter is `foe`;
- assert the forced `JourneyFoeProfile` values exactly;
- resolve the encounter through the normal `ResolveJourneyEncounter(...)` path;
- keep the existing health, wallet, heat, and replay-state assertions.

The important change is that the tests should prove the encounter resolution behavior, not the generator routing path.

- [ ] **Step 3: Rewrite the replay-equality foe test to use the same forced fixture.**

Update `TravelReplayEqualityTests.Replay_ResolveJourneyEncounter_MatchesCommandPath_ExactState` so it uses the forced-foe helper instead of `CreateHighRiskJourney()` plus an interruption loop. Keep the replay comparison intact, but make the test deterministic because the next encounter is forced, not incidentally selected.

- [ ] **Step 4: Reword the stale seed-catalog comments.**

Update the comments in `TravelTestSeedCatalog.cs` and `TravelTestSeedCatalogGuardrailTests.cs` so they describe the descriptors as route/setup guardrails, not as the primary deterministic encounter-control mechanism.

Keep the existing descriptors if they still serve route-shape coverage, but remove stale wording such as "used for foe-encounter tests" if the deterministic foe behavior now comes from the dev override seam.

- [ ] **Step 5: Run the relevant domain tests for this slice.**

Run:

```powershell
dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TravelEncounterResolutionCharacterizationTests|FullyQualifiedName~TravelReplayEqualityTests|FullyQualifiedName~DevTravelOverrideTests"
dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter "FullyQualifiedName~TravelTestSeedCatalogGuardrailTests"
```

Expected:

- the forced-foe tests pass deterministically without a retry loop;
- the dev-override replay-safety tests still pass;
- the seed catalog guardrails still pass after the comment cleanup.

- [ ] **Step 6: Commit.**

Commit the test helper, the rewritten tests, and the comment cleanup together so the plan and the evidence stay causally connected.

---

### Task 2: Prove the same deterministic foe control through the dev travel API

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevTravelEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs` if the new API assertion needs a tighter travel-context guard

**Interfaces:**
- Consumes: `/api/dev/sessions/{id}/travel-context`, `/api/dev/sessions/{id}/travel/force-override`, `/api/games/{id}/travel/advance`, `ForceTravelOverrideRequestDto`, `TravelDevContextDto`.
- Produces: an integration test proving the backend-owned dev route can force a foe encounter and that the override is consumed exactly once.

- [ ] **Step 1: Add an end-to-end forced-foe dev-travel test.**

Start a normal session, start travel, force a foe override through `/api/dev/sessions/{id}/travel/force-override`, advance one travel day through the normal game API, and assert:

- the returned turn result contains a pending `foe` encounter;
- the pending foe profile matches the forced values;
- the dev travel context no longer reports a pending override after consumption;
- the encounter resolves through the normal travel route, not through a special test-only bypass.

Use the same backend path a dev-overlay playthrough would use. Do not introduce any frontend-only state in the test.

- [ ] **Step 2: Preserve the hidden-truth boundary.**

If the new end-to-end assertion needs one more guard, extend the existing hidden-truth test rather than creating a separate route. The dev travel context may expose journey internals and the pending override, but it must not leak culprit identity markers.

- [ ] **Step 3: Run the integration tests that cover the dev travel seam.**

Run:

```powershell
dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~DevTravelEndpointTests|FullyQualifiedName~GameApiHiddenTruthTests"
```

Expected:

- forcing a foe override through `/api/dev` works end-to-end;
- the override is consumed once by the next advance;
- hidden culprit truth is still absent from the dev travel context payload.

- [ ] **Step 4: Commit.**

Keep the API test change small enough that a reviewer can see the same lawful control path being exercised from the outside.

---

### Task 3: Final validation and plan publication

**Files:**
- Read-only: the whole repo for final verification.

**Interfaces:**
- Consumes: the two changed test layers and the repo-local plan record.
- Produces: a plan-only PR-ready branch with a checked-in plan file and a route-state update in Linear.

- [ ] **Step 1: Run the broad repository guardrails that still matter for a test-only slice.**

Run:

```powershell
dotnet build WildBunch.sln
dotnet test WildBunch.sln
```

If full-solution test runtime is too heavy for the plan-only pass, keep the plan narrow but do not skip the targeted tests from Tasks 1 and 2.

- [ ] **Step 2: Confirm the worktree and branch are clean before publication.**

Run:

```powershell
git status --short
git branch --show-current
git log -1 --oneline
```

Expected:

- the worktree is clean after the plan commit;
- the branch matches `harleydbartles/bunch-87-deterministic-foe-encounter-seed-profiles-for-travel-tests`;
- the head commit is the plan commit.

- [ ] **Step 3: Publish the plan-only PR against `main`.**

Push the branch and open a draft PR targeting `main`. The PR body should say:

- the issue now uses the shared dev-travel override seam for deterministic foe tests;
- seed/profile setup is route-only, not encounter-control;
- BUNCH-103 is assumed landed;
- no gameplay redesign or seed-code change was required.

- [ ] **Step 4: Update the Linear route-state comment.**

Post the route-state block back to BUNCH-87 with the actual repo plan path and PR URL.

Expected route state:

```text
Route state: preflight_complete_pending_approval
Plan path: .agents/superpowers/plans/2026-06-27-bunch-87-deterministic-foe-encounter-seed-profiles-for-travel-tests.md
Plan PR: <plan-only draft PR URL>
Plan branch: harleydbartles/bunch-87-deterministic-foe-encounter-seed-profiles-for-travel-tests
Plan base commit: ac683496f462caccd50523c369ae6568737b6ea0
Status: plan-only draft PR open; awaiting approval before execution.
```

---

## Self-Review

**1. Spec coverage:**
- Shared dev-travel override seam as the deterministic control path: Task 1 and Task 2.
- Keep seed descriptors as setup-only and stop claiming they are the encounter-control mechanism: Task 1.
- Preserve replay safety and hidden-truth boundaries: Task 1 and Task 2.
- Avoid Boring-mode suppression and runtime patching: Global Constraints.
- Avoid seed codec or gameplay redesign work: Global Constraints.

**2. Placeholder scan:**
- No TBDs, TODOs, or hand-wavy "handle edge cases" text remain in the task list.

**3. Type consistency:**
- `CreateForcedFoeJourney(...)` is used consistently as the domain-test helper name.
- The integration test uses the existing `ForceTravelOverrideRequestDto` and `TravelDevContextDto` shapes already present in the repo.
