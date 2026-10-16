# N-90: Qualify terminal travel death and failed-playthrough recovery

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove that lethal starvation is committed as one terminal event-backed outcome, remains visible through fresh player reads, rejects later travel actions, and can be archived as a failed playthrough.

**Architecture:** Extend the PostgreSQL-backed integration acceptance suite with a deterministic persisted travel session that has no food and 10 health. Advance through the real API, then load the session and player journal through fresh reads. Keep `GameSession` authoritative, use the API/application/persistence path under test, and do not mutate gameplay rules unless the acceptance test exposes a real defect.

**Tech Stack:** C#/.NET integration tests, API, Application, Domain, Persistence, PostgreSQL, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), PG-007 travel and resource/death contract; [feature matrix](../../docs/features.md), PG-007.

**Execution Strategy:** `executing-plans` inline; the scenario is one coupled persistence and API boundary whose test setup, event sequence, fresh reads, retry rejection, and archive outcome need a shared implementation context.

## Global Constraints

- `develop` is the integration branch; deliver this plan through one reviewed PR to `develop`.
- The development version for this PR is `0.1.0-dev.208`, authored only in `Directory.Build.props`; the plan-only commit does not advance the version.
- Use `PostgreSqlApiFactory` and its uniquely named disposable test database; do not mutate or drop the shared development database.
- The event stream records what happened; snapshots/cache and player reads reconstruct or project that history and are not independent authority.
- A lethal unfed travel day consumes only the player's remaining health, records terminal death in the same committed operation, stops travel/rewards/arrival, and rejects later gameplay without another event.
- Preserve the existing 25 HP per unfed day rule, event names and domain outcomes; do not add compatibility or new gameplay behavior.
- Retain N-90's plan and N-89 evidence until this PR completes; retire the completed N-89 plan and record its verified delivery in the roadmap in the first substantive implementation commit.
- Compare the completed diff through the decision-record and feature-matrix playbooks. No ADR change is expected because this qualifies the settled PG-007 death and archive rules.

## Review Focus

- The API call must commit a `TravelDayAdvanced` with the actual remaining-health delta followed by exactly one `PlayerDied`, with failed game and journey status and no journey completion.
- Fresh game and journal reads must show the terminal state and player-facing death entry from persisted history, not the command response's in-memory aggregate.
- A repeated travel advance must be rejected and leave the persisted event-derived state unchanged; archiving the failed playthrough must persist `StatusBeforeArchive == Failed` and remain Archived after a fresh read.
- The acceptance test must fail if the travel command returns a success-shaped result without committing its events.

---

## Settled scope

Domain coverage already proves lethal starvation, actual remaining-health loss, terminal event emission, event replay, action availability, later command rejection, and domain archive behavior. Existing integration coverage round-trips the `PlayerDied` serializer and separately covers active-playthrough archive, but does not prove that a lethal API command commits, that the fresh PostgreSQL-backed game and journal reads expose the death, or that a persisted failed session can be archived. N-90 closes only that persistence/API gap; it does not broaden travel, combat, browser, deployment, or release scope.

## Task 1: Add the PostgreSQL API acceptance scenario and reconcile its evidence

**Files:** Create `tests/WildBunch.Integration.Tests/Acceptance/TerminalTravelDeathAcceptanceTests.cs`; update dated PG-007 evidence in `docs/features.md`; update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; delete `.agents/plans/2026-10-16-row18-n89-saloon-gossip-browser-qualification.md` in the first substantive implementation commit; update `Directory.Build.props` to `0.1.0-dev.208`.

**Consumes:** `PostgreSqlApiFactory`, `AcceptanceTestHarness`, `BoringScenarioBuilder`, the existing `TravelStarvationTests` death contract, the API advance/game/journal/actions/archive routes, and the current PG-007 feature-matrix boundary.

**Produces:** One isolated PostgreSQL acceptance test that persists a legal in-progress journey with no food and 10 health, advances it to terminal death through the API, proves the committed and replayed result through fresh reads, rejects a repeated advance without additional facts, and archives the failed playthrough.

- [ ] **Step 1: Construct and persist a deterministic lethal-travel session**

Create a `PostgreSqlApiFactory` and authenticated client. Build a valid mounted travel-ready session with the shared scenario builder, replace only its `GameStarted` event setup with 10 starting health and zero Food, rehydrate it against the same world, start a legal connected journey through the aggregate, then store and commit it through the repository and unit of work. Assert the initial session is Active, the journey is Active, health is 10, and Food is 0 before exercising the endpoint.

- [ ] **Step 2: Prove the API commits terminal death and exposes fresh player reads**

POST `/api/games/{id}/travel/advance` and assert the real API's expected HTTP and unsuccessful terminal-turn result. Assert the result reports health 0 and Failed game/journey status. Then GET `/api/games/{id}` and `/api/games/{id}/journal` after the command has completed; assert fresh persisted reads show health 0, Failed game/journey status, one player-facing death entry, and no completion or arrival. Inspect the persisted aggregate/event history through the existing test persistence seam and assert the lethal operation records the actual `-10` health delta followed by exactly one `PlayerDied`, without `JourneyCompleted` or arrival facts.

- [ ] **Step 3: Prove retry rejection and failed-session archival survive reload**

Record the persisted event count and death journal entry after the first operation. POST another travel advance and assert the endpoint reports an unsuccessful rejected command; then reload game, journal and persisted history and prove status, health, journey progress, event count and single death entry are unchanged. POST `/api/games/{id}/archive`, assert the response is Archived, then reload again and prove the session remains Archived and the persisted `PlaythroughArchived.StatusBeforeArchive` is Failed.

- [ ] **Step 4: Prove the acceptance test detects a non-committing command**

Temporarily change the real advance command's persistence path so the lethal command can return its in-memory result without committing its events. Run only the new acceptance test and confirm the fresh game or journal read fails to observe persisted death; record the test's failing assertion and restore the production file byte-for-byte. Verify the restored file hash, rerun the focused acceptance test against the real command, and confirm it passes. Do not commit the mutation or its result.

- [ ] **Step 5: Reconcile feature and successor evidence, then validate**

Add dated PG-007 evidence that names the isolated local PostgreSQL/API boundary, the 10-health lethal starvation case, persisted death and fresh game/journal reads, repeat rejection, and archive-from-Failed restoration; retain the explicit limits for other travel outcomes, browser behavior, hosted CI, deployment, and whole-feature qualification. In row 18, record N-89's merged PR #392, reviewed source `a89d35f229a9789aaf5ce9e6700e3810b183a6ae`, squash merge `f291ba8d1e7af0613ad8eb5d0c21a1d7c6b951f0`, exact-head hosted gate 38829632883, develop-push gate 38830240921, independent review outcome, and paired worktree/local/upstream branch cleanup; then point the active plan to N-90. Delete N-89's completed plan in this first substantive implementation commit and change the sole authored application version to `0.1.0-dev.208`. Compare relevant ADRs and state in the PR review notes that no durable decision changed. Ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, run the focused test with `py -3 tools/run.py dotnet-test -- --filter FullyQualifiedName~TerminalTravelDeathAcceptanceTests`, then rely on the canonical `py -3 tools/run.py ci --check` gate through the normal check-only hook at commit time; do not rerun the full canonical gate immediately before or after a successful hooked commit.

## Task 2: Review, publish, merge and clean N-90

**Files:** Review the committed N-90 diff, this plan, the baseline specification, PG-007, row 18, applicable ADRs and the acceptance-test assertions.

**Consumes:** The focused PostgreSQL API acceptance proof, PG-007 evidence update, N-89 successor-plan retirement, development version `0.1.0-dev.208`, and passing canonical local hook.

**Produces:** Independent whole-branch review, a Draft PR to `develop`, passing exact-head hosted CI and develop-push CI, a verified fast-forwarded primary checkout, and paired worktree/branch retirement.

- [ ] **Step 1: Complete readiness and independent review**

Use the `handoff-gates` completion-readiness procedure after implementation and prepare a fresh-context whole-branch review package from current `origin/develop` to `HEAD`. Include this plan, the PG-007 specification and feature-matrix comparison, the relevant ADR check, the non-committing failure witness, focused test output, and the canonical hook result. Follow the user's standing authorization to dispatch the independent reviewer without asking again; use gpt-6-luna. Resolve any findings and repeat review after corrections.

- [ ] **Step 2: Publish and verify the Draft PR**

Run the PR runbook. Push the branch, open a Draft PR targeting `develop`, attach it to this task if the app permits, then read back the PR head/body/state and exact-head hosted checks. Do not merge until exact-head hosted CI passes.

- [ ] **Step 3: Merge and verify integration**

Merge the reviewed PR to `develop` using the repository's accepted method. Wait for and verify the develop-push gate on the exact merge commit. Advance the primary checkout to that merge while preserving unrelated user changes.

- [ ] **Step 4: Retire the worktree and exact branch refs together**

Use the `finishing-a-development-branch` worktree script in check/apply mode. Verify the PR head, merged tree equivalence and clean worktree before removing the exact local and upstream feature branches. Confirm the worktree, local branch and remote tracking refs are all absent; keep this plan through its completing PR for assessment by the next slice.
