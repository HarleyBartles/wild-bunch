# Row 17 N-23 Developer Travel Context Mapping Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Replace the thin developer travel-context smoke coverage with behavioral proofs for an interrupted encounter, a custom pending travel override, and the empty context when no journey exists.

**Architecture:** Exercise the existing `GetTravelDevContextHandler` and `TravelDevContextMapper` through their owning query path. Build actual `GameSession` journey states with the domain's event-backed dev override and ordinary travel loop; assert the complete observable DTO so the tests verify real mapping behavior without changing production behavior.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.Application.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), “Finish gaps at their existing behavioral owners”; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `Dev/GetTravelDevContextHandlerTests`.

**Execution Strategy:** `executing-plans`, inline and sequential. Keep the tests at the existing developer query owner; avoid a new mapper-only test class or one-property-per-test sprawl.

## Review Focus

- The interrupted encounter must be produced through a real active journey and the ordinary `AdvanceJourneyDay` path after forcing an event-backed foe override.
- Require the response's complete journey status/day fields, encounter kind/message, and nested foe profile values and descriptive bands.
- A separately pending custom foe override must preserve its forced category, full profile, and message. An override that was consumed by advancing the journey must not be asserted as still pending.
- A session with no journey must return an explicit empty travel context: `HasActiveJourney == false` and all journey, encounter, and override fields null.
- Keep the existing missing-session test; do not introduce a production behavior, API, event, ADR, or feature change.

## Global Constraints

- Start from refreshed `origin/develop` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.141`; it remains the sole authored application version.
- Retire N-22 only after its complete scope is verified in PR #325; preserve this active N-23 plan through its completing PR.
- Do not change production behavior, event payloads, feature promises, ADR decisions, or unrelated mapper behavior.
- Follow the check-only staged-snapshot hook and fail-fast canonical validation; do not use `--no-verify`.
- Before merge, pass fresh whole-branch review and exact-head hosted CI. After merge, verify develop-push CI, fast-forward the primary checkout, then remove this clean worktree and its exact local/upstream branch refs together.
- Preserve unrelated worktrees and branch refs, including the separate AWS learning worktree.

---

### Task 1: Retire N-22 and install N-23

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n22-journal-trail-day.md`
- Add: `.agents/plans/2026-10-13-row17-n23-dev-context-mapping.md`

**Interfaces:**
- Consumes: N-22 PR #325 reviewed source `09d824e5be0574c07c134170f57c8ffef145826a`, squash merge `d0a2f2611450f964d08cb307012c89cbdde20ddc`, identical source/merge tree `1934a096e55f6617cf29ffb857d65e5f88059ff8`, exact-head hosted gate `38393256143`, approved fresh whole-branch review, and verified paired worktree/local/upstream cleanup.
- Produces: active N-23 plan at `0.1.0-dev.141`, N-22 retirement, and dated N-22 delivery evidence in row 17; record the develop-push check result after it completes.

- [x] **Step 1: Record N-22 delivery and select this successor.** Add PR #325, its source/merge/tree identities, exact-head gate, review and paired cleanup evidence to row 17. Point the active row to N-23 and advance the version to `.141`. Record that N-22 closed only the trail-event day projection finding.
- [x] **Step 2: Retire N-22 and commit the plan before test edits.** Confirm N-22's full scope is shipped and its dated disposition remains in the investigation documents. Record the develop-push CI result once available. Delete the completed plan, add this plan, validate version and links, and commit only these planning/version changes before changing tests.

### Task 2: Prove complete developer travel context mapping

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Dev/GetTravelDevContextHandlerTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: `GameSession.ForceDevTravelOverride` emits the event-backed pending override; `AdvanceJourneyDay` consumes it and produces a real interrupted encounter. `GetTravelDevContextHandler` loads the aggregate and maps the state through `TravelDevContextMapper`.
- Produces: complete query-path behavior assertions for interrupted encounter context, custom pending override context, and empty no-journey context; dated closure of the dev-context mapping finding while leaving the Domain arrival/context-refresh finding open.

- [x] **Step 1: Replace smoke assertions with complete observable behavior.** Strengthen the active-journey case to force a foe override with known values and message, advance the actual journey, then require the returned DTO's status, elapsed/remaining days, encounter kind/message, and every nested foe-profile value/band. Replace the category-only override test with one that requires a custom pending profile and message on the DTO. Add one no-journey query scenario requiring the explicit empty context. Preserve missing-session behavior.
- [x] **Step 2: Prove RED and GREEN against mapper behavior.** Temporarily change the mapped foe speed and run the focused interrupted-context test; it failed with expected speed 5 and actual speed 6. Restore the mapper exactly and require the focused suite to pass. Do not commit the mutation.
- [x] **Step 3: Record the finding disposition.** Add a dated disposition naming the complete query-path scenarios, the relevant DTO branches they exercise, and the witnessed mapper mutation. Update the per-file test assessment to retain the strengthened behavior tests. Leave Domain arrival/context refresh open; make no production, feature-matrix, or ADR change.
- [x] **Step 4: Run owning validation and commit.** Discover the focused test target through the command bus; run `GetTravelDevContextHandlerTests`, inspect the focused diff and disposition, and commit. The commit hook runs the full canonical gate; do not redundantly run the full gate immediately before committing.

### Task 3: Review, publish, merge, and close out

**Files:**
- Review: the complete branch diff and staged-candidate validation result.

- [ ] **Step 1: Obtain a fresh whole-branch review.** Build a review package from the refreshed develop merge base to final HEAD and review the code, tests, dispositions, N-22 retirement and roadmap against the plan, ADR-0031, and repository review runbook. Correct actionable findings with witnessed behavior tests and rerun affected validation.
- [ ] **Step 2: Publish and merge.** Push this branch and open one PR to `develop`. Verify the PR head equals local `HEAD`, it targets `develop`, and exact-head hosted CI passes before merging. The next successor records this PR's delivery and paired cleanup evidence in row 17.
- [ ] **Step 3: Verify and retire the paired checkout.** Confirm develop-push CI passed, fast-forward the primary checkout, verify merge identity and resulting tree, and remove the clean worktree before deleting this plan's exact local and upstream branch refs. Confirm the worktree and both refs are absent; preserve unrelated worktrees.
