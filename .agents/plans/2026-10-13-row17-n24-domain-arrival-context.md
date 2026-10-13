# Row 17 N-24 Domain Arrival Context Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Prove town-scoped action context resets through a real outbound and return journey so re-entering a familiar location after arrival advances time.

**Architecture:** Extend the existing Domain clock/action-context behavior test at its current owner. Drive both legs through `GameSession.StartJourney`, `AdvanceJourneyDay`, `JourneyCompleted`, and `AcknowledgeJourneyArrival`; never mutate Player, TownVisitState, or ActionContextTracker directly. This is a test-only slice unless the actual command/replay path violates the already-recorded decision.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.Domain.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md); [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `ClockTurnCorrectionTests`.

**Execution Strategy:** `executing-plans`, inline and sequential. The slice is one coupled Domain lifecycle test, one witnessed mutation, and its finding disposition; parallel task execution would split the same fixture and behavior context without reducing review risk.

**Execution ruling:** Measure the Saloon re-entry turn against the clock immediately before re-entry, not the first Saloon entry. Actual travel advances days and may reset the turn counter; the regression is specifically that re-entry advances one turn after arrival. The initial absolute comparison failed with expected turn 2 and actual turn 1 because the journey legitimately reset the turn counter.

## Review Focus

- The journey must use actual session commands and event application for both departure and arrival; manually calling `TravelTo`, `TownVisitState.Reset`, or `ResetActionContextForTownChange` does not cover this invariant.
- The behavior must include returning to the original town after a saloon context was entered there, then re-entering that saloon and observing one additional turn. A different-town-only test cannot prove same-town return behavior.
- Keep the existing direct tracker and clock tests only where they prove their own narrow contracts; do not change production code or add replay coverage unless the actual behavior exposes a distinct missing invariant.

---

### Task 1: Retire N-23 and install N-24

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n23-dev-context-mapping.md`
- Add: `.agents/plans/2026-10-13-row17-n24-domain-arrival-context.md`

**Interfaces:**
- Consumes: N-23 PR #326, reviewed source `db961edd44815d75ee284141a7c4489c3e1a586f`, squash merge `68697be9f881428f037dea77541123647f5e46d9`, identical source/merge tree `b59642fff3899755b0f3b09a663d7e5f76edc799`, exact-head gate `38396502186`, approved whole-branch review, and paired checkout/ref cleanup. The N-23 develop-push gate `38397246834` must pass before this delivery record is committed.
- Produces: active N-24 plan at `0.1.0-dev.142`, N-23 plan retirement, and a dated N-23 delivery record that closes only the developer travel-context mapper finding.

- [x] **Step 1: Confirm the successor boundary.** Verify N-23 source/merge identities, tree equality, exact-head gate, independent review, primary checkout, and paired worktree/local/upstream ref cleanup. Confirm develop-push gate `38397246834` passes. Preserve the Domain arrival/context-refresh finding as open.
- [x] **Step 2: Record N-23 and install N-24.** Add PR #326, the required source/merge/tree/review/gate/cleanup evidence, and its narrow finding disposition to row 17. Point the row to this plan, add PR #326 to the delivered PR list, and advance `Directory.Build.props` once to `0.1.0-dev.142`.
- [x] **Step 3: Retire N-23 and commit planning/version changes.** Delete the completed N-23 plan, preserve this active N-24 plan, validate its links and version ownership, and commit only planning/version changes before editing tests.

### Task 2: Prove same-town return through the journey lifecycle

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/EventSourcing/ClockTurnCorrectionTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: `TravelTestFactory.CreateEasyShortJourney`, `TravelTestFactory.ResolvePreviewFor`, `GameSession.StartJourney`, `AdvanceJourneyDay`, `AcknowledgeJourneyArrival`, `LookAroundSaloon`, and the ordinary generated event application in `GameSession`.
- Produces: one behavioral test proving saloon entry, a completed and acknowledged outbound journey, a completed and acknowledged return journey, and saloon re-entry in the original town each follow the real aggregate path; a dated disposition closing only the Domain arrival/context-refresh finding.

- [x] **Step 1: Write the failing round-trip behavior test.** In `ClockTurnCorrectionTests`, enter Saloon in the starting town. Complete and acknowledge travel to the connected town using actual `GameSession` commands. Resolve and complete the return route using `TravelTestFactory.ResolvePreviewFor(session, originalTownId)`, acknowledge arrival, then capture `Clock.Day` and `Clock.Turn` immediately before calling `LookAroundSaloon` again. Assert the player is back in the original town, the action context is Saloon, and re-entry advances exactly one turn, including the day rollover from turn 3 to turn 0. Do not directly mutate child state or reset context in the test.
- [x] **Step 2: Run the focused test and correct fixture setup only.** Ran the test alone with `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~JourneyRoundTrip_ReturningToSameTownResetsSaloonContextAndAdvancesTime`. Used the event-backed `DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet)` before each real day advance to isolate town-context behavior from generated encounters; both journeys still complete and are acknowledged through the ordinary aggregate path.
- [x] **Step 3: Prove the test detects the missing reset.** Temporarily removed the `ResetActionContextForTownChange()` call from `RefreshTownVisit`; the new test failed with expected `None` and actual `Saloon` after outbound arrival. Restored production code and reran the focused test green. The mutation was not committed.
- [x] **Step 4: Record the finding disposition.** The test-quality investigation now records that the old tests simulated town changes by directly mutating location and resetting context; it records the real round-trip path and witnessed missing-reset mutation. Other Domain findings remain open. No ADR, feature-matrix, or production behavior changed.
- [x] **Step 5: Run owning validation and commit.** Ran all `ClockTurnCorrectionTests` (12 passed), inspected the test and disposition diff, and committed the slice. The commit hook's canonical fail-fast gate passed.

### Task 3: Review, publish, merge, and close out

**Files:**
- Review: complete branch diff and staged-candidate validation result.

- [ ] **Step 1: Obtain a fresh whole-branch review.** Review the complete diff from refreshed `develop` against this plan, ADR-0028, relevant Domain architecture and event-sourcing doctrine, the unslop backend-architecture and code-review profiles, and the repository review runbook. Correct actionable findings with behavior tests and rerun affected checks.
- [ ] **Step 2: Publish and merge.** Push this branch and open one PR to `develop`. Verify PR head equals local `HEAD`, the base is `develop`, the author decision-record review is resolved, and exact-head hosted CI passes before merge. Record N-24 delivery and paired checkout/ref cleanup in the next slice's first substantive planning commit.
- [ ] **Step 3: Verify and retire the paired checkout.** Confirm develop-push CI passes, fast-forward the primary checkout, verify merge identity and source/merge tree equality, then remove this clean worktree before deleting this exact local and upstream branch ref. Verify all three are absent and preserve unrelated worktrees.
