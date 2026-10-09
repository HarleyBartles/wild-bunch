# Journey Cache Shape Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Reconstruct a malformed persisted active Journey component from its recorded events so command and player reads retain the actual in-progress travel state.

**Architecture:** The present Journey payload is a cache of the `GameSession` state established by `JourneyStarted` and subsequent travel facts. Classify only decode or nested-shape failures in that stored component as cache damage, then use the existing coherent full replay path; event decode, upcast, replay, cancellation and infrastructure failures remain visible. Query recovery stays read-only, while a later normal command save may repair the cache through the existing repository and Unit of Work.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, xUnit integration tests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [PS-04/05 test follow-up](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline. One real PostgreSQL scenario follows an active Journey component from nested cache damage through command/query recovery, read-only preservation and legal-save repair; the same persisted boundary owns the behavior.

## Global Constraints

- Events record facts that occurred; replay reconstructs the exact Journey and never generates a substitute or rerolls randomness.
- A legitimately absent Journey outside active travel remains absent; only an invalid present Journey payload is classified in this slice.
- Query recovery does not write caches, events, envelope positions or diary metadata.
- Corrupt authoritative event history remains visible and is never converted into a plausible Journey or cache default.
- Do not change the current legacy whole-session snapshot boundary, event schema, or migration history.
- Each ordinary PR to `develop` advances `Directory.Build.props` once to the next unique `0.1.0-dev.N` identity.
- Use a fresh worktree from refreshed `origin/develop`, publish a PR to `develop`, and merge only after exact-head review and local and hosted canonical gates.

## Review Focus

- A nested Journey cache shape can deserialize into a plausible but incomplete object without throwing; the test must omit a structurally required route field and compare independently captured journey facts.
- A malformed cache can tempt a broad exception catch that hides event-codec or replay failures; the negative case must corrupt authoritative Journey history and observe that failure.
- Journey absence is a valid state outside travel; retain the normal no-Journey read behavior and do not make absent optional rows globally trigger replay.

---

### Task 1: Save the plan before implementation

**Files:** this plan.

**Consumes:** Refreshed `origin/develop` at PR #207 merge `012dedebc4bc9c11cb162c6aaa2dc6e16a27d977`.

**Produces:** A committed JIT plan for one row 07 restoration gap.

- [x] Read this plan against the live source seam and repository planning guidance; commit only this plan before successor-artifact changes or implementation.
- [x] Record plan-only commit `e0e053c5` and retain the plan through its completing PR.

### Task 2: Retire the completed setup-entropy predecessor and advance the slice

**Files:** `.agents/plans/2026-10-09-setup-entropy-cache-recovery.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`.

**Consumes:** PR #207's exact merged source, merge, matching tree and hosted canonical gate, verified against `develop`.

**Produces:** This plan as row 07's current plan, truthful closure of the prior setup-cache slice, and development identity `0.1.0-dev.23`.

- [x] Verify PR #207 merged to `develop` from reviewed source `2d21aadd4ebc8cdacf3a0dee558ae94b2c55b31e`, merge `012dedebc4bc9c11cb162c6aaa2dc6e16a27d977`, and hosted canonical run `37878390267` passed on that exact source.
- [x] In the first substantive commit, record PR #207's exact source, merge and gate evidence; summarize the now-proven setup-cache recovery; retire its completed predecessor plan and remove its stale link.
- [x] Set this plan as the current row 07 plan and preserve the remaining nested and optional-component gaps as open.
- [x] Advance `Directory.Build.props` once from `0.1.0-dev.22` to `0.1.0-dev.23`; do not hand-edit generated web version output.
- [x] Inspect and commit the intended successor-artifact diff before implementation.

### Task 3: Prove malformed active Journey cache behavior at PostgreSQL boundaries

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` and only directly required fixtures.

**Consumes:** A production repository session with a persisted active Journey and an expected `TravelJourneySnapshot` captured before cache damage.

**Produces:** Red behavior proof for command/query recovery, no-writeback and fail-closed history.

- [x] Start and persist an active Journey through the production aggregate and repository; capture sequence, route endpoints, remaining distance/days, and any pending encounter facts from the accepted Journey event before mutating storage.
- [x] Omit only the current Journey component's nested `routeProfile` while preserving event rows and all unrelated component, envelope and diary data.
- [x] Load through fresh command and player-read repository contexts; assert the independently captured in-progress Journey facts rather than a same-cache equality oracle.
- [x] Assert both reads leave the damaged Journey payload and component version, event rows, snapshot/stream positions and diary metadata unchanged.
- [x] Save after a legal travel command through the existing Unit of Work; assert the persisted Journey row has the current component version and expected event-established state, then load fresh and verify the same result.
- [x] Add a negative where the Journey cache is malformed and its required `JourneyStarted` event payload cannot be decoded; assert the event-history error remains visible.
- [x] Run the new tests before production changes and observe failure at the original nested-deserialization or wrong-state boundary, not at fixture setup. The explicit-null scenario first failed with `NullReferenceException` in `TravelRouteProfileSnapshot.ToDomain`; after changing the case to omitted property, it failed with an empty route trail ID instead of the event-established `trail-preview`. The event-history negative already failed as expected.

### Task 4: Recover a malformed present Journey cache from events

**Files:** `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Travel.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; focused integration tests.

**Consumes:** The real PostgreSQL red scenario and the existing required-component cache recovery boundary.

**Produces:** Exact event-backed Journey state on command and player-read paths, with read-only recovery and normal-save repair.

- [x] Introduce or extend the narrow typed cache-shape classification so omitted or null required `routeProfile` values in present Journey payloads enter the existing replay fallback without broadening it to event, cancellation or infrastructure failures.
- [x] Apply identical classification at command aggregate and player-read model boundaries; keep legitimately absent Journey rows nullable and unchanged.
- [x] Preserve cache/event/envelope no-writeback during recovery, allow a later legal save to repair the component, and keep unreplayable event history explicit.
- [x] Run focused PostgreSQL red/green cases plus existing Journey round-trip and pending-encounter persistence tests; verify an ordinary no-Journey load still returns no active Journey. `Journey|TravelDiary|MalformedJourneyCache` integration filter passed 8/8.

### Task 5: Update evidence and deliver the bounded recovery slice

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `docs/features.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; this plan; changed source and tests.

**Consumes:** Focused red/green PostgreSQL evidence and current feature, decision, persistence and unslop guidance.

**Produces:** A dated PS-04/05 disposition, truthful PLAT-001 assessment, reviewed PR and completed implementation evidence.

- [x] Record the precise nested Journey cache shape, event-established recovery facts, no-writeback boundaries, legal-save repair, corrupt-history negative and preserved valid-absence behavior; retain original audit findings.
- [x] Update PLAT-001 only to the behavior now proven; keep missing optional Journey rows and other malformed or nested components open if not covered.
- [x] Confirm ADR-0028 remains truthful; do not edit it because this enforces its existing cache/event-authority decision.
- [x] Run focused PostgreSQL tests, migration inventory and canonical fail-fast `py -3 tools/run.py ci --check`; confirm generated web identity `0.1.0-dev.23` and no event payload or migration changes unless the plan's bounded evidence finds an explicit necessity. Focused cache recovery tests passed 2/2; Journey/travel-diary integration tests passed 8/8 before the omitted-property extension. The final canonical gate passed on `3e945d92194d78f5a8c73f2657ad596b39577674`; migration inventory contains only the already-pending `20261009001543_AddTravelDiaryProjectionWatermark`, with no event-payload or migration diff.
- [x] Complete whole-branch review against this plan, baseline spec, PS-04/05, persistence doctrine, unslop and code-review runbook; resolve every finding and inspect the final head. The first independent review at `444d98f172b148472afb5a939bab65bf45722c8b` found that omitted `routeProfile` used a plausible empty initializer; the property is now required during domain conversion, the omitted-property regression failed before the fix and passes after it, and the independent correction review at `3e945d92194d78f5a8c73f2657ad596b39577674` found no remaining issue.
- [ ] Publish and attach a Draft PR to `develop`, verify its exact source head and current body, then mark it ready; verify the canonical hosted gate passes on that exact SHA before merging under active epic authorization, fast-forward `Z:\wild-bunch`, and clean only the verified merged worktree and branch; keep this plan until the next row 07 successor classifies it.

The local commit hook runs `py -3 tools/run.py ci --check` against the staged candidate. Confirm generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.23`; inspect migration and event changes with `git diff origin/develop -- src/WildBunch.Persistence/Migrations src/WildBunch.Domain/Events`. For publication, the PR head must equal local `HEAD`, and the hosted canonical gate must pass on that exact SHA before authorized merge.
