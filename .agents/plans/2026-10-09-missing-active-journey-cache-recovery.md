# Missing Active Journey Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Restore a missing Journey component from event history when that history says a Journey is still present, while preserving a genuinely absent Journey after arrival acknowledgement.

**Architecture:** Journey is an optional cache because ordinary in-town sessions and acknowledged arrivals have no current Journey. `JourneyStarted` establishes the current Journey, travel and completion events retain it, and only `JourneyArrivalAcknowledged` clears it. When the optional cache row is missing, use the ordered Journey start/arrival transition facts to distinguish a valid absence from a lost current cache. If a current Journey should exist, route command and player-read loads through their existing full event replay fallback. Replays never synthesize a Journey, and recovered reads do not write caches or metadata; a later legal command may repair the component through the normal Unit of Work.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, xUnit integration tests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [PS-04/05 test follow-up](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline. Prove the missing-row behavior against real PostgreSQL data after a completed, acknowledged prior Journey and a newly active Journey. Verify both fresh aggregate and player-read recovery, read-only preservation, corrupt-history failure, and normal-save repair. Keep the acknowledged no-current-Journey state as the negative product case.

## Global Constraints

- A Journey exists only when domain events establish it; an absent optional row is not by itself evidence of loss.
- `JourneyStarted` starts the current Journey, `JourneyCompleted` retains it pending acknowledgement, and `JourneyArrivalAcknowledged` clears it. Earlier starts followed by acknowledgement are history, not a current Journey.
- Rebuild exact state from supported recorded events. Do not infer a Journey from player location, diary text, or a default snapshot.
- Command and player-read loaders must agree. Query recovery does not write components, events, envelope positions, or diary metadata.
- Corrupt authoritative event history remains visible; do not translate a replay failure into a plausible absent Journey.
- Keep the generic optional-component policy unchanged. This slice covers only missing `journey` rows; do not expand into other optional components or cache shapes.
- Do not change the legacy whole-session snapshot boundary, event schema, or migration history.
- PR #208 established `0.1.0-dev.23`; this implementation PR advances once to `0.1.0-dev.24` in its first substantive commit.
- Use a fresh worktree from refreshed `origin/develop`, publish a PR to `develop`, and merge only after exact-head review and local and hosted canonical gates.

## Review Focus

- A naïve `Any(JourneyStarted)` check misclassifies a normal missing component after an acknowledged prior Journey; use transition ordering and prove it with a completed-and-acknowledged prior trip.
- A missing Journey row with a still-current Journey must not load as `null` or an empty/default Journey.
- An active Journey cache may be absent in both command and player-read paths; both must return the same event-established route and progress.
- A missing optional row must remain absent across read-only recovery, while a later legal command save can recreate it at the current component version.
- An undecodable Journey event must fail during replay even when the Journey component row is missing.

---

### Task 1: Save the JIT plan before implementation

**Files:** this plan.

**Consumes:** Refreshed `origin/develop` at PR #208 merge `58ee5b2a33d788c16f1463532012453cd1ce422a`.

**Produces:** A committed JIT plan for the missing optional active-Journey cache gap.

- [x] Confirm this plan against current source and repository planning guidance; commit only this plan before changing successor artifacts, versions, tests, or implementation.
- [x] Record plan-only commit `ef65003d7a65ec33cd1181ccd2f67592a8997fce` and retain this plan through its completing PR.

### Task 2: Retire the completed Journey-shape predecessor and advance row 07

**Files:** `.agents/plans/2026-10-09-journey-cache-shape-recovery.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`.

**Consumes:** PR #208's exact merged source, merge, matching tree and hosted canonical gate, verified against `develop`.

**Produces:** This plan as the current row 07 plan, truthful closure of the prior nested-shape slice, the PR #208 delivery evidence, and development identity `0.1.0-dev.24`.

- [x] Verify PR #208 merged to `develop` from source `a4e49d4425ee3515e31b8fd726456637ba940e20`, merge `58ee5b2a33d788c16f1463532012453cd1ce422a`, matching tree `21b99841f9e80edf6948012413fed9b6ea19a078`, and hosted canonical run `37881645016` passing on that exact source.
- [x] In the first substantive commit, record PR #208's exact source, merge, tree and gate evidence; summarize the now-proven omitted `routeProfile` recovery and its still-open missing-row gap; retire `.agents/plans/2026-10-09-journey-cache-shape-recovery.md` and remove its stale link.
- [x] Set this plan as the current row 07 plan; retain other optional-component and projection gaps as open.
- [x] Advance `Directory.Build.props` once from `0.1.0-dev.23` to `0.1.0-dev.24`; do not hand-edit generated web version output.
- [x] Inspect and commit the intended successor-artifact diff before implementation.

### Task 3: Prove missing Journey behavior at PostgreSQL boundaries

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` and only directly required fixtures.

**Consumes:** Production-persisted Journey lifecycle facts and an expected active Journey snapshot captured before deleting its cache row.

**Produces:** A PostgreSQL red proof for missing-row recovery, acknowledged absence, no-writeback, fail-closed history and legal-save repair.

- [x] Create and persist a first Journey, complete it, and acknowledge arrival; verify the normal post-ack state has no current Journey while its completed history remains.
- [x] Start and persist a second Journey, capture its event-established sequence, route, status and remaining distance/days, then delete only its `journey` component row.
- [x] Load from fresh command and player-read repository contexts; assert both expose the captured second Journey rather than `null`, the prior Journey, or a default Journey.
- [x] Assert both reads leave the Journey row absent and preserve all event rows, envelope positions and diary metadata.
- [x] Save after a legal travel command through the existing Unit of Work; assert the Journey row is restored at the current component version and a fresh load retains the event-established state.
- [x] Add a negative with a missing Journey row and an undecodable event required to reconstruct the current Journey; assert both loaders expose the history failure.
- [x] Run the new tests before production changes and observe failure because the absent optional cache currently becomes `null`, not because fixture setup failed. With a missing row and a previously acknowledged Journey followed by a current start, the read returned `null` and failed the independently captured-state assertion; the existing present-malformed and corrupt-history cases passed. The test also directly asserts the classifier is false after acknowledgement and true after the later start. Replacing the ordered classifier with `Any(JourneyStarted)` made the PostgreSQL case fail at the post-ack assertion (`expected false, actual true`); restoring ordered transitions made all four focused cases pass.

### Task 4: Recover only a missing event-established current Journey

**Files:** `src/WildBunch.Persistence/GameSessions/JourneyCacheRecovery.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; focused integration tests.

**Consumes:** The real PostgreSQL red scenario and the existing whole-session replay fallbacks.

**Produces:** Matching command/player-read Journey state when the optional cache is missing, valid null state after acknowledgement, read-only recovery, and normal-save repair.

- [x] Use ordered `JourneyStarted` and `JourneyArrivalAcknowledged` facts to determine whether a missing Journey row represents an active/current cached state; do not trigger loss on a historical acknowledged start.
- [x] Route only a missing row with event-established current Journey state through each existing replay fallback; preserve nullable absence after acknowledgement and when no Journey exists.
- [x] Preserve no-writeback during both reads and let a later legal save persist the reconstructed Journey through the ordinary Unit of Work.
- [x] Keep replay decode/upcast failures visible; do not catch or default them as optional-state absence.
- [x] Run focused recovery tests plus existing Journey lifecycle/round-trip persistence tests and confirm ordinary no-Journey loads remain valid. Focused missing-row, malformed-present and invalid-history cases passed 4/4 against PostgreSQL after the fix.

### Task 5: Update evidence and deliver the bounded recovery slice

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `docs/features.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; this plan; changed source and tests.

**Consumes:** Focused PostgreSQL proof and current feature, decision, persistence and unslop guidance.

**Produces:** A dated PS-04/05 disposition, truthful PLAT-001 assessment, reviewed PR, and completed implementation evidence.

- [x] Record the precise missing optional Journey-row policy, transition ordering, post-ack null behavior, read no-writeback, legal-save repair and invalid-history negative; preserve original audit findings.
- [x] Update PLAT-001 only to the behavior proven here; keep other optional components and malformed/nested shapes open unless this slice directly proves them.
- [x] Confirm ADR-0028 remains truthful; do not edit it because this implements its existing event-history authority and cache-reconstruction decision.
- [x] Run the focused PostgreSQL tests, migration inventory and canonical fail-fast `py -3 tools/run.py ci --check`; confirm generated web identity `0.1.0-dev.24` and no event payload or migration changes absent explicit necessity. Focused cases passed 4/4; replacing ordered classification with `Any(JourneyStarted)` failed at the post-ack assertion; migration inventory shows only the existing `20261009001543_AddTravelDiaryProjectionWatermark` pending; no event or migration diff exists. The full gate passed on implementation HEAD `a53fa431279bd9608170ba8eaaf4e560568dc552` and on the final tracking HEAD `f364ed74264e95c49f197947501812a4532cb66f` via its commit hook.
- [ ] Complete whole-branch review against this plan, baseline spec, PS-04/05, persistence doctrine, unslop and code-review runbook; resolve every finding and inspect the final head. The first review found that post-ack load results alone did not distinguish ordered transitions from an incorrect `Any(JourneyStarted)` predicate. Direct classifier assertions now prove the boundary, and the deliberate mutation failed. The implementation head `a53fa431279bd9608170ba8eaaf4e560568dc552` was re-reviewed with no actionable findings; review of the current plan-tracking commit remains pending.
- [ ] Publish and attach a Draft PR to `develop`, verify its exact source head and current body, then mark it ready; verify the canonical hosted gate passes on that exact SHA before merging under active epic authorization, fast-forward `Z:\wild-bunch`, and clean only the verified merged worktree and branch; keep this plan until the next row 07 successor classifies it.

The local commit hook runs `py -3 tools/run.py ci --check` against the staged candidate. Confirm generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.24`; inspect migration and event changes with `git diff origin/develop -- src/WildBunch.Persistence/Migrations src/WildBunch.Domain/Events`. For publication, the PR head must equal local `HEAD`, and the hosted canonical gate must pass on that exact SHA before authorized merge.
