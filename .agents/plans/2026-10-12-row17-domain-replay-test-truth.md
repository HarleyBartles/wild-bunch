# Domain Replay and Travel Test Truth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Domain event-replay and travel-heat tests prove their named behavior from real setup and reached gameplay states.

**Architecture:** Preserve the production event model and correct only test fixtures, assertions, and shared test helpers. Replay starts from the actual canonical setup events and the original public clue state; one travel scenario proves the same session reaches positive heat, resets on departure, advances real trail days without heat, and arrives in its destination.

**Tech Stack:** C#/.NET 10, xUnit, Wild Bunch Domain and its test project.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially its test-design and lifecycle/replay acceptance; finding dispositions N-01 and N-02 in `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md` and the per-file index in `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`.

**Implementation Guide:** `.agents/runbooks/implementing.md`; execute inline with `executing-plans`.

**Execution Strategy:** `executing-plans` inline because each test correction shares the Domain test project, fixture conventions and mutation-proof loop, and the user has specified JIT planning followed by inline execution for this epic.

## Global Constraints

- Start from refreshed `origin/develop` in the dedicated `codex/row17-test-inventory` worktree.
- Keep `Directory.Build.props` as the only authored application version and advance exactly once from `0.1.0-dev.116` to `0.1.0-dev.117` in the first substantive implementation commit, not the plan-only commit.
- Do not change production behavior, event payloads, projection policy, or the settled game contract in this test-only slice.
- A replay fixture must use the original canonical setup facts and actual events emitted by the command under test; do not reconstruct a baseline from post-action state or infer vanished facts from mutated aggregates.
- Every test must establish its named precondition, require the named branch to be reached, and assert an independently meaningful outcome; no empty-result, non-null-only, or conditional-branch escape may satisfy it.
- Prove each corrected test is sensitive to the behavior it protects by applying a bounded temporary mutation, observing failure, restoring production code, and rerunning the focused test.
- Do not add development receipts, test result files, generic test-count targets, source-shape detectors, or one regression test per audit finding.
- This is an initial row-17 slice, not completion of the complete test/finding inventory; leave all unrelated and unimplemented row-17 findings accurately pending.
- Commit this complete plan by itself before execution; the plan-only commit leaves `Directory.Build.props` at `.116` and preserves the `.116` predecessor plan until the first substantive implementation commit.
- In the first substantive implementation commit, record PR #301 source head `dbe9688477e923b8cb1ffafd593587cb61a666db`, squash merge `59a4e080a2866fb54dfac6ccbd10488c80fdb422`, exact-head hosted gate run `38271955001`, develop-push gate run `38272670829`, clean paired worktree/ref removal, and retire the completed `.116` plan.
- Update row 16 as complete with PR #301 delivery evidence and make row 17 active with this `.117` plan; do not claim the remaining test audit is complete.
- Keep the active spec and roadmap, and this current plan, through their completing PR.

## Review Focus

- Investigation replay must begin with a `CaseFileGenerated` event that contains the still-public clue; the command-produced `InvestigationPerformed` must be necessary for replay to reveal the clue and spend its source.
- Heat reset must be observed on the same aggregate that first reaches positive heat and then starts the journey.
- The combined travel scenario must prove heat gain in town, its reset on departure, real successful trail-day events without heat increase, completed arrival, and the changed town identity.

---

### Task 1: Replay investigation from actual canonical setup history

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TestSessionFactory.cs`
- Modify: `tests/WildBunch.Domain.Tests/Events/GameSessionEventSourcingTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: `TestSessionFactory.CreateWithPublicClue(InvestigationSourceKind, string)` and `GameSession.UncommittedEvents`.
- Produces: `TestSessionFactory.CreateUncommittedWithPublicClue(InvestigationSourceKind, string)`, which returns the same public-clue scenario with canonical setup events still uncommitted; existing callers retain their current committed-setup behavior.

- [x] Add a test-helper entry point that creates the existing public-clue scenario without committing its canonical setup events, factoring shared construction so the committed helper remains behaviorally unchanged.
- [x] Change `RehydrateFromEvents_Reconstructs_Investigation_State` to assert its pre-command state has the expected public clue and no known clue, invoke `GatherLocalGossip`, and replay the complete actual event stream without fabricating `GameStarted` or constructing `CaseFileGenerated` from the mutated case file.
- [x] Assert the setup `CaseFileGenerated` event contains the clue in the public pool and assert the command/replay outcomes independently: the same clue ID is known, absent from public clues, the local-gossip source is spent, and replayed day/turn match the command result.
- [x] Run the focused test and inspect the captured source history to confirm that removing the `InvestigationPerformed` application makes the known/public/source assertions fail.
- [x] Temporarily disable the production application of `InvestigationPerformed`, run the focused test and confirm failure for the expected clue/source behavior, then restore the production file and rerun the test successfully.
- [x] Record a dated test-design disposition that replaces N-01's false proof while retaining the N-01 historical audit and updating the exact per-file disposition; record no raw test output or result as a durable receipt.

**Acceptance:** The replay receives the unmodified clue as an event-backed public fact and cannot pass if investigation application is removed. Existing helper callers still receive their prior committed setup state.

**Validation:** `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~RehydrateFromEvents_Reconstructs_Investigation_State`.

### Task 2: Consolidate heat reset and completed travel into one reached scenario

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/Guardrails/HeatSemanticGuardrailTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelTestFactory.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: deterministic `TravelTestFactory` session and travel-preview construction.
- Produces: `TravelTestFactory.ResolvePreviewFor(GameSession, TownId)`, a test helper for resolving a current-session preview after its town clock has been advanced; no production API changes.

- [x] Expose a narrowly named test-helper operation to resolve a preview from the supplied session and destination, reusing the existing private resolver and preserving current factory behavior.
- [x] Rewrite `StartingJourney_ResetsHeatToZero` to prepare a deterministic easy journey, build heat to 1 in that same session, obtain a fresh preview, then start the journey and prove the reset on that aggregate.
- [x] Fold `AdvancingTrailDays_DoesNotIncreaseHeat` and `ArrivalInNewTown_DoesNotRetroactivelyAddTravelHeat` into that same scenario, asserting successful day events with zero heat after each, a completed journey, successful acknowledgment, and the player's destination identity.
- [x] Remove the redundant old trail/arrival tests after their distinct claims are represented in the consolidated scenario; retain separate town-day heat-resumption coverage.
- [x] Run the focused scenario and confirm removing the heat-reset application, suppressing successful day advancement, and preventing acknowledgment each fail at their corresponding independent assertion.
- [x] Temporarily apply those bounded mutations one at a time, verify each failure, restore production code, and rerun the focused scenario successfully; include a nonzero travel-day heat mutation to prove the per-day no-increase assertion is sensitive.
- [x] Record dated N-02 dispositions in the test-quality investigation and per-file index; do not add a test result receipt.

**Acceptance:** One scenario proves positive heat on the same session before departure, zero heat immediately after journey start and after actual successful trail days, then proves completion and destination arrival before retaining the no-retroactive-heat assertion.

**Validation:** `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~StartingJourney_ResetsHeatToZero`.

### Task 3: Record successor delivery and retire completed planning

**Files:**
- Modify: `Directory.Build.props`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Delete: `.agents/plans/2026-10-12-row16-dn15-building-inputs.md`

- Consumes: merged PR #301 and exact hosted gate evidence listed in Global Constraints.
- Produces: row 16 marked complete, row 17 linked to this active `.117` plan, and the single authored version `0.1.0-dev.117`.

- [x] Verify PR #301 is merged into `develop`, its exact-head and develop-push gates passed, the primary checkout contains merge `59a4e080a2866fb54dfac6ccbd10488c80fdb422`, and its clean worktree and local/upstream refs were removed together.
- [x] Update row 16 with PR #301 source/merge/gate evidence and paired cleanup, mark row 16 complete, and activate row 17 with this plan as the first test-reconciliation slice.
- [x] Make the spec and roadmap explicit that a plan worktree and its local/upstream branch refs are cleaned as one unit after merge; verify the branch inventory and preserve active PR #185 rather than deleting its unmerged branch.
- [x] Retire the completed `.116` plan only in this successor's first substantive implementation commit.
- [x] Advance `Directory.Build.props` once from `0.1.0-dev.116` to `0.1.0-dev.117` in that same substantive commit; keep the plan-only commit version-neutral.
- [x] Stage both test tasks, their maintained investigation records, roadmap/version update, and `.116` plan retirement; use the normal check-only hook with no bypass.

**Acceptance:** The merged `.116` delivery and paired cleanup are durably reconciled, row 17 remains explicitly incomplete, and `.117` is the only authored application version.

**Validation:** Verify the exact authored version, the row-16/row-17 states and links, plan retirement from Git history, clean staged/committed tree, and successful normal hook.

**Closeout:** Obtain fresh whole-branch review, publish a Draft PR to `develop`, mark it ready so hosted PR jobs run, require the exact-head hosted canonical gate and develop-push gate to pass, squash merge, fast-forward the primary checkout, then remove this clean worktree before deleting and verifying its local/upstream branch refs. Leave N-03 and all other remaining inventory findings pending for later JIT row-17 plans; do not claim test-inventory completion from this slice.
