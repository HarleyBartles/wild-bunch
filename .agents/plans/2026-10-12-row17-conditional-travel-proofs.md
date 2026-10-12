# Row 17 Conditional Travel Proofs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Correct N-03's conditional travel assertions so tests require the interrupted and completed journeys they claim to prove.

**Architecture:** Keep production behavior and event contracts unchanged. Make the Domain projector test force and assert a real interrupted day before comparing the event projection with the command path; consolidate the two Application completion tests into one deterministic six-day orchestration scenario with successful advances, a completed journey, and acknowledged destination arrival.

**Tech Stack:** C#/.NET 10, xUnit, Domain and Application test projects.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially the test-design, event replay, travel lifecycle and plan-delivery requirements; N-03 in `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md` and its per-file index in `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`.

**Implementation Guide:** `.agents/runbooks/implementing.md`; execute inline with `executing-plans`.

**Execution Strategy:** `executing-plans` inline because both corrections belong to the same N-03 travel-test finding, while each test keeps its distinct Domain projection or Application orchestration owner and focused proof.

## Global Constraints

- Start from refreshed `origin/develop` in the dedicated `codex/row17-n03-conditional-proof` worktree.
- Keep `Directory.Build.props` as the only authored application version and advance exactly once from `0.1.0-dev.117` to `0.1.0-dev.118` in the first substantive implementation commit, not the plan-only commit.
- Do not change production behavior, event payloads, projection policy, or the settled travel contract in this test-only slice.
- A claimed interruption or arrival must be reached through the exercised command path; a conditional branch, bounded loop that silently stops, or quiet-day parity cannot satisfy it.
- Use deterministic travel setup. The existing `DevTravelOverride.ForCategory` seam can force a Foe encounter for the Domain interruption test and Quiet days for the Application completion scenario.
- Prove the corrected tests are sensitive to the interrupted projection and completed-journey behavior with bounded temporary mutations, confirm expected failures, restore production code, and rerun focused tests.
- Do not add development receipts, test-result files, generic test-count targets, source-shape detectors, or one regression test per audit finding.
- This is the next row-17 slice after N-01/N-02, not completion of the test inventory; leave N-04 and all unrelated findings accurately pending.
- Commit this complete plan by itself before execution; the plan-only commit leaves `Directory.Build.props` at `.117` and preserves the `.117` predecessor plan until the first substantive implementation commit.
- In the first substantive implementation commit, reconcile PR #302: source head `fd1716c43abb6467ebcd014513c8bfe168317f85`, squash merge `674fa24f02b1f272d3336b71cf5ab37200ea4d6e`, identical source/merge tree `0b40529485933e928f91be8edc9142e8a4401367`, exact-head hosted gate `38280387077`, and develop-push gate `38280922785`; record that its clean worktree and local/upstream refs were removed together, update row 17 to this active `.118` plan, and retire the completed `.117` plan.
- Keep the active epic spec and roadmap, and this current plan, through their completing PR.

## Review Focus

- The Domain test reaches `JourneyStatus.Interrupted` and retains a pending encounter before it compares command-path diary days with the projector output.
- The Application scenario requires every quiet-day advance to succeed, days one through five to remain active at the origin, day six to complete and move the player to the destination, then acknowledgment to clear the completed Journey without erasing diary history.
- The Application scenario proves all six diary days remain available after acknowledgment; it does not hide success behind an `if` or nullable guard.
- The plan retires only N-03's false conditional proofs and preserves the distinction between Domain projection parity and Application handler orchestration.

---

### Task 1: Require a real interrupted travel day before projector parity

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/Projections/TravelDiaryDayProjectorParityTests.cs`

**Interfaces:**
- Consumes: `TravelTestFactory.CreateHighRiskJourney`, `DevTravelOverride.ForCategory`, and `TravelDiaryDayProjector.Project`.
- Produces: an interrupted-day parity scenario that proves its fixture reaches the `DayCompleted=false` projection branch before comparing complete command-path and event-projection day records.

- [x] Replace seed-dependent high-risk encounter generation with a forced Foe override on the active journey, then advance through the normal aggregate command.
- [x] Assert the command result and aggregate both report `Interrupted`, the journey retains a pending encounter, and the event stream contains the interrupted travel-day fact before invoking the projector.
- [x] Compare the complete diary-day records from the command path and projector; remove the quiet-day fallback and early return.
- [x] Run the focused Domain test and verify a temporary mutation that makes the forced Foe plan quiet fails the required interruption assertion; restore the production code.
- [x] Temporarily break the projector's interrupted-day diary update and verify the exact parity assertion fails; restore the production code and rerun the focused test.

**Acceptance:** The test cannot pass on a quiet-only journey and fails if event replay omits or misprojects the interrupted day.

**Validation:** `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~Projector_InterruptedJourney_MatchesCommandPathDiaryDays`.

### Task 2: Consolidate Application completion and acknowledgment into one reached journey

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Handlers/AdvanceTravelDayHandlerTests.cs`

**Interfaces:**
- Consumes: `CreateSixDayQuietSession`, `ForceTravelOverrideHandler`, `AdvanceTravelDayHandler`, `AcknowledgeJourneyArrivalHandler`, and their current command/result contracts.
- Produces: one deterministic Application scenario covering six successful trail-day commands, destination arrival and completed status on day six before acknowledgment, and complete diary retention after acknowledgment.

- [x] Replace `HandleAsyncKeepsCompletedJourneyUntilArrivalIsAcknowledged` and `HandleAsyncCompletesSixFullTrailDaysBeforeArrival` with one coherent six-day orchestration scenario; remove both conditional completion guards.
- [x] Force one Quiet override through the existing Application dev command before each day advance, then require each handler result to succeed and report `Active` for days one through five and `Completed` for day six.
- [x] On each day, require `pinecross` through day five and `sixmile` when day six completes; require all diary day numbers through the current day in order.
- [x] Before acknowledgment, require a non-null completed journey at `sixmile`; require successful acknowledgment to clear the Journey while retaining all six diary days.
- [x] Run the focused Application test and temporarily make `AdvanceTravelDayHandler` report `Active` when the domain result is `Completed`; verify the test fails at the required completion assertion, restore production code, and rerun the focused test.

**Acceptance:** The Application test proves the actual six-command completion and acknowledgment sequence, not merely a non-null journey after a fixed loop.

**Validation:** `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~HandleAsyncCompletesSixFullTrailDaysBeforeArrivalAcknowledgement`.

### Task 3: Record N-03 disposition, successor delivery, and version

**Files:**
- Modify: `Directory.Build.props`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-12-row17-domain-replay-test-truth.md`

**Consumes:** The corrected N-03 Domain and Application scenarios and merged PR #302 delivery facts listed in Global Constraints.

**Produces:** A dated N-03 disposition, row 17 still in progress at the `.118` plan, and the sole authored application version `0.1.0-dev.118`.

- [x] Record the Domain interruption and Application completion/acknowledgment outcomes as the N-03 disposition in the test-quality investigation and per-file test index; note that destination identity changes on completion (day six) before acknowledgment clears the Journey; do not add a test-result receipt.
- [x] Update row 17 with PR #302's source, squash merge, tree identity, exact-head/develop gates, paired cleanup proof, and this `.118` plan as the next JIT slice; leave N-04 and row-17 completion pending.
- [x] Retire the completed `.117` plan only in this successor's first substantive implementation commit.
- [x] Advance `Directory.Build.props` once from `0.1.0-dev.117` to `0.1.0-dev.118` in that same substantive commit; keep the plan-only commit version-neutral.
- [x] Stage both test corrections, their maintained investigation records, roadmap/version update, and `.117` plan retirement; use the normal check-only hook with no bypass.

**Acceptance:** N-03's two false conditional proofs are corrected and dispositioned; PR #302 is durably reconciled; row 17 remains explicitly incomplete; `.118` is the only authored application version.

**Validation:** Verify the exact authored version, N-03 dispositions, row-17 status/link and PR evidence, `.117` plan retirement from Git history, clean staged/committed tree, and successful normal hook.

**Closeout:** Obtain fresh whole-branch review, publish a Draft PR to `develop`, mark it ready so hosted PR jobs run, require the exact-head hosted canonical gate and develop-push gate to pass, squash merge, fast-forward the primary checkout, then remove this clean worktree before deleting and verifying its local/upstream branch refs. Keep N-04 and all other remaining inventory findings pending for later JIT row-17 plans; do not claim test-inventory completion from this slice.
