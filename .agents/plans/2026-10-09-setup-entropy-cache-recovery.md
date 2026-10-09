# Setup Entropy Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Recover event-established game entropy when the current persisted setup cache is missing or unusable, rather than silently changing the player's selected randomness mode to `Classic`.

**Architecture:** The event stream remains authoritative and records the selected entropy in normal setup/genesis facts. A current setup-component cache is only a read optimization; missing or invalid current component data is reconstructed from supported history. Query loads do not write back. A later legal command repairs the component through the existing unit of work. Preserve the distinct legacy whole-session snapshot default unless evidence proves it is outside its supported compatibility boundary.

**Tech Stack:** C#/.NET, EF Core, Npgsql/PostgreSQL, xUnit integration tests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery), [row 07 persistence roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md#07-persistence-restoration-and-retries), [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [PS-04/05 test follow-up](../investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md#mapping-every-persistence-finding-to-the-right-proof).

**Execution Strategy:** `executing-plans` inline. One production PostgreSQL scenario exercises the same setup component through command and query loaders, narrow cache-shape classification, replay fallback and same-unit-of-work repair; splitting these would duplicate recovery boundaries and context.

## Global Constraints

- Events record facts that occurred; replay reconstructs the exact selected entropy and never rolls randomness again.
- Do not treat a current event-backed entropy choice as absent merely because the setup cache row or field is absent.
- Query recovery is read-only. A later legal command may repair the cache through the existing unit of work.
- Do not change the documented legacy whole-session snapshot compatibility default without evidence and a separately bounded decision.
- Keep unsupported or incomplete authoritative history visible; cache recovery must not hide replay failure.
- Each ordinary PR to `develop` advances `Directory.Build.props` once to the next unique `0.1.0-dev.N` identity.
- Use a fresh worktree from refreshed `origin/develop`, publish a PR to `develop`, and merge only after exact-head review plus local and hosted canonical gates.

## Review Focus

- A non-`Classic` entropy recorded by normal production events survives missing and unusable current setup cache data through command and applicable query loads.
- Reads leave damaged cache JSON, component version, event rows, stream/snapshot positions and diary metadata unchanged; a later legal save repairs the component and fresh reads return the same entropy.
- Corrupt authoritative history fails explicitly rather than returning `Classic` or another plausible default.
- Legacy whole-session snapshot compatibility remains as supported, with its boundary independently exercised.
- Tests use real PostgreSQL, production serialization/event decoding and independently captured expected entropy; no same-projector-only oracle.

---

### Task 1: Save the plan before implementation

**Files:** this plan.

**Consumes:** Refreshed `origin/develop` at PR #206 merge `2d858c2124d255e044899a0707e5ddb577c321e9`.

**Produces:** A committed, executable JIT plan in the fresh row 07 worktree.

- [ ] Save and review this plan, then commit only this plan before any implementation or successor-artifact changes.
- [ ] Record the plan-only commit in the final PR description and retain the plan through the completing PR.

### Task 2: Establish the next row 07 slice and retire its completed predecessor

**Files:** `.agents/plans/2026-10-09-required-component-cache-recovery.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`.

**Consumes:** PR #206's exact merged source, merge, matching tree and hosted canonical gate, verified against GitHub and `develop`.

**Produces:** This plan as row 07's current plan, truthful closure evidence for its predecessor, predecessor retirement and the unique successor development version.

- [ ] Verify PR #206 merged to `develop` from the reviewed exact source, source and merge trees match, and hosted canonical gate passed on that source.
- [ ] In the first substantive commit, record PR #206's exact source, merge SHA, tree, `0.1.0-dev.21` and hosted gate; summarize current-version required-component null-field recovery; retire the completed predecessor plan and remove stale links.
- [ ] Set this plan as row 07's current plan and record the newly scoped setup-entropy cache gap without claiming it is fixed.
- [ ] Advance `Directory.Build.props` once from `0.1.0-dev.21` to `0.1.0-dev.22`; do not hand-edit generated web version output.
- [ ] Inspect and commit the intended successor-artifact diff before implementation.

### Task 3: Prove setup entropy loss at the persisted repository boundary

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` and only directly required test fixtures.

**Consumes:** A normal setup/game session created and saved through production repositories with a non-`Classic` entropy value captured independently from its accepted event facts.

**Produces:** PostgreSQL red tests for current setup-cache loss and authoritative-history failure.

- [ ] Add a real production-event scenario that independently captures a non-`Classic` selected entropy and then removes only the current `setup` component row or nulls/omits its `gameEntropy` field while preserving events and all unrelated persistence metadata.
- [ ] Exercise fresh command aggregate and player/journal read loads; assert the selected entropy remains exact rather than defaulting to `Classic`.
- [ ] Assert command and query recovery leave the damaged payload/row, component version, envelope positions, event rows and diary metadata unchanged.
- [ ] Execute a legal command after recovery, save through the normal unit of work, and assert a fresh load returns the same selected entropy with the setup cache repaired.
- [ ] Add a negative case that damages the setup cache and removes the required `WorldGenerated` event; assert both command and query loads fail at the authoritative history boundary.
- [ ] Run the focused PostgreSQL tests against the current implementation and observe failure for the entropy-loss behavior before changing production code.

Run RED with `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_CurrentSetupEntropyCacheRecoversFromEvents"`; the expected failure must demonstrate that the cache shortcut returns the wrong entropy or that the recovery path fails while history is intact.

### Task 4: Recover invalid current setup cache from supported events

**Files:** `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Setup.cs`; `src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; focused integration tests.

**Consumes:** The red PostgreSQL cases and the already established typed required-component cache recovery boundary.

**Produces:** Exact event-backed setup entropy on command and query loads, without read writeback or changes to legacy whole-session snapshot compatibility.

- [ ] Treat missing/null or otherwise unusable entropy in a present current-version setup component as invalid cache data; keep any decode/shape classification limited to the persisted component path.
- [ ] Recover event-established setup entropy through the coherent supported replay path in both command and query loaders; preserve truly supported legacy behavior only where the persisted-history boundary proves it applies.
- [ ] Keep event decode, upcast, replay, cancellation and infrastructure failures outside the cache-damage classification.
- [ ] Preserve legacy whole-session snapshot compatibility behavior and prove the existing compatibility test still passes; do not broaden this slice into a generic optional-component policy.
- [ ] Rerun focused PostgreSQL tests and verify the corrupt-history negative fails closed.

### Task 5: Record evidence and deliver the bounded recovery slice

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `docs/features.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; this plan; changed source and tests.

**Consumes:** Focused red-green PostgreSQL evidence and current ADR, persistence doctrine, feature matrix and unslop routes.

**Produces:** Dated PS-04/05 disposition, truthful PLAT-001 assessment, reviewed PR and completed implementation evidence.

- [ ] Record the setup-cache recovery behavior, exact event-established entropy facts, row and metadata preservation, legal-save repair, corrupt-history negative and legacy compatibility boundary; preserve the audit-time findings.
- [ ] Update PLAT-001 to state the precise setup cache behavior now proven and keep all unrelated optional/nested component gaps open.
- [ ] Confirm ADR-0028 remains truthful; change or supersede it only if this implementation changes a durable decision rather than enforcing its existing cache/event-authority decision.
- [ ] Run focused PostgreSQL tests, persistence migration inventory and canonical fail-fast `py -3 tools/run.py ci --check`; confirm generated web identity is `0.1.0-dev.22` and no event payload or migration changes were introduced.
- [ ] Complete whole-branch review against this plan, baseline spec, PS-04/05, persistence doctrine and code-review runbook; apply review fixes and inspect the exact final PR head.
- [ ] Publish and attach a Draft PR to `develop`, verify its exact source head, then mark it ready so hosted validation runs; verify the canonical gate passes on that exact SHA before merging under active epic authorization, fast-forward `Z:\wild-bunch`, and clean only this verified merged worktree and branch; retain this plan until its next row 07 successor classifies it.

The local commit hook runs `py -3 tools/run.py ci --check` against the staged candidate. Confirm generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.22`; verify no migration or event payload file changed with `git diff origin/develop -- src/WildBunch.Persistence/Migrations src/WildBunch.Domain/Events`. For publication, the PR head must equal local `HEAD`; hosted validation must pass on that exact SHA before the authorized merge.
