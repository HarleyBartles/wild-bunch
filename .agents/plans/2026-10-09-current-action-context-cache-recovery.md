# Recover the CurrentActionContext Cache Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Reconstruct the event-established current town action context when its optional persistence cache is missing or malformed, so the next same-context action does not advance the clock again.

**Architecture:** `TownActionContextEntered` records the context, town and resulting clock. The `currentActionContext` component is a cache; command loading can reconstruct its current value through the existing full-event replay path. A missing row may also represent the legitimate `None` value, so replay is safe and yields the authoritative state without inventing one. Present malformed cache shapes use the existing typed invalid-cache fallback. Reads do not write repaired cache rows; a later normal unit-of-work save may repair them.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit, existing event replay and persistence loaders.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially the event-history authority and rebuildable-cache contract.

**Execution Strategy:** `executing-plans` - the cache and its event-derived clock behavior share one command-load boundary, and a real PostgreSQL test can falsify the missing-cache behavior before production fallback changes.

## Global Constraints

- Preserve event history as the only authority for what happened; cache data is rebuildable state.
- Keep CQRS strict: command loading reconstructs aggregate state, query loading remains read-only, and neither read path writes back.
- Preserve the distinction between entering a different context, which advances the clock, and re-entering the same context in the same town, which is a no-op.
- Do not add domain events, change event payloads or versions, add a database migration, or infer state from cached clock values.
- Repair a damaged component only through an ordinary later aggregate save and unit of work.
- Do not broaden this slice to completed-journey history, suspect-presence state, developer overrides, or unrelated component validation.
- Advance `Directory.Build.props` once for this PR from `0.1.0-dev.25` to `0.1.0-dev.26`; it remains the only authored application version.
- Use the canonical fail-fast `py -3 tools/run.py ci --check` gate and the repository's `develop` PR delivery contract.

## Review Focus

- Removing the current action-context cache after a real `TownActionContextEntered` event must not cause a second clock advance when the same context is entered after reload.
- JSON `null` is syntactically valid but is not a valid context cache; it must use the same replay fallback without rewriting the payload during reads.
- Corrupt authoritative event history must still fail through replay rather than being hidden by cache fallback.
- The cache state after a later legal save must reflect event-established context and clock facts, with contiguous event sequence and no duplicate context event.
- Existing partial diary recovery from PR #203 and TownVisit recovery from PR #210 must remain unchanged; this slice must not claim broader optional-component recovery.

---

### Task 1: Save the JIT plan before implementation

**Files:** `.agents/plans/2026-10-09-current-action-context-cache-recovery.md`.

**Consumes:** Refreshed `origin/develop` at PR #210 merge `148399846f60fd127aade7439d58fe2d8c6967c6`.

**Produces:** A committed plan for missing and malformed `currentActionContext` recovery, before changing predecessor artifacts, version identity, tests or implementation.

- [x] Confirm the exact context and clock event behavior from `GameSession.EnterActionContext`, `TownActionContextEntered`, event replay, command loading and the production PostgreSQL fixture.
- [x] Commit only this plan as the first branch commit; record its SHA in this plan after commit. Plan-only commit: `f245e23df00092dd1d05fef0ee48f5b2d78c13a2`.

### Task 2: Retire the completed predecessor and record verified delivery

**Files:** `.agents/plans/2026-10-09-town-visit-cache-recovery.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `Directory.Build.props`.

**Consumes:** PR #210 merged source `d11a49863c36934b85bbc14fac08c3159f0dc8e5`, merge `148399846f60fd127aade7439d58fe2d8c6967c6`, and hosted canonical gate run `37888683841` passing on that exact source.

**Produces:** A truthful PR #210 delivery record, a narrow completion disposition for the retired predecessor, and `0.1.0-dev.26`.

- [x] In the first substantive commit, record PR #210's target, source head, merge commit and exact-head hosted gate result in row 07; record the delivered TownVisit missing-row/null-root behavior and retire `.agents/plans/2026-10-09-town-visit-cache-recovery.md` with stale links removed.
- [x] Verify and record that PS-06 diary partial-row recovery was already delivered in PR #203 (`66165171b92a9f578d7ee43a2c9a473ed98c9ad7`, merge `2b6472cafac1869b0d94ddbe13891b1364424052`, canonical gate `37865661644`) by `ReadModel_PartialDiaryDayCacheRebuildsFromEventsWithoutWritingBack`; do not plan or implement duplicate diary recovery.
- [x] Keep row 07 open for the current-context gap and other unhandled nested or optional cache cases; do not claim comprehensive optional-cache correctness.
- [x] Confirm ADR-0028 remains truthful because this slice applies its existing event-authority and rebuildable-cache choice; no ADR or feature-matrix change is required.
- [x] Advance `Directory.Build.props` once from `0.1.0-dev.25` to `0.1.0-dev.26`; do not edit generated web identity output.
- [x] Inspect and commit the intended successor-artifact and version diff before adding or changing tests and implementation.

### Task 3: Witness action-context cache loss at PostgreSQL boundaries

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs` and only directly required fixtures.

**Consumes:** A normally started session, a real `TownActionContextEntered` event, the production command repository and PostgreSQL fixture.

**Produces:** Falsifiable missing-row and null-root cases proving that command loads restore context and clock from committed history without reading from a fake database seam or writing back.

- [ ] Add a PostgreSQL theory for `currentActionContext` damage cases `missing-row` and `null-root`; enter `TownActionContext.Saloon` in a live session, persist it, and independently capture context town, day, turn, time of day, pursuit heat and ordered event rows.
- [ ] Name the theory `CommandLoad_CurrentActionContextCacheRecoversFromEventsWithoutWritingBack` and the negative case `DamagedCurrentActionContextCacheDoesNotHideInvalidEventHistory` in `EfGameSessionRepositoryTests.cs`.
- [ ] Delete only the component row for `missing-row`; replace only its JSON payload with `null` and retain its component version for `null-root`.
- [ ] With a fresh command repository, reload the aggregate and assert the context and town equal the event-established values, then call `EnterActionContext(TownActionContext.Saloon)` and prove it returns false without changing clock/heat or appending a `TownActionContextEntered` event.
- [ ] Assert read-only recovery leaves the component absent or byte-for-byte unchanged, keeps its version when present, and preserves envelope versions, event identifiers/payloads/versions and diary rows.
- [ ] Add a negative case that damages the context cache and makes the persisted `TownActionContextEntered` event undecodable; assert command load exposes the event error instead of returning `None` or plausible cached state.
- [ ] Run the new damage and negative cases before the production fix; record the observed wrong extra turn or decode failure and the exact intended negative result.
- [ ] Run the focused PostgreSQL cases with `py -3 tools/run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.CommandLoad_CurrentActionContextCacheRecoversFromEventsWithoutWritingBack|FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.DamagedCurrentActionContextCacheDoesNotHideInvalidEventHistory"`.

### Task 4: Route damaged context cache through event replay

**Files:** `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; focused integration tests.

**Consumes:** The PostgreSQL red cases, existing full replay fallback, `GameSession.RehydrateFromEvents` and typed invalid-component-cache handling.

**Produces:** Correct command aggregate context after a missing or malformed cache, with event failures remaining visible and no read-time persistence mutation.

- [ ] When `currentActionContext` is absent, use the existing full-replay path; replay is valid for both an actual context and the legitimate `None` value, so do not add a second event classifier.
- [ ] Wrap undecodable or null-root context cache shapes in the existing typed invalid-component-cache exception with the `currentActionContext` component identity so the existing full-replay fallback handles them.
- [ ] Leave a valid current cache on the fast path and leave event upcasting, replay and reconstruction exceptions uncaught by the cache-shape fallback.
- [ ] Run the missing/null-root tests before and after the fix, plus the unreplayable-event negative; prove no cache writeback occurs during command load.
- [ ] Extend the scenario through a later legal command and ordinary unit-of-work save; verify the current context component is restored at the current version and a fresh command load has the same context and clock facts.

### Task 5: Record the bounded result and deliver the slice

**Files:** `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; this plan; changed source and tests.

**Consumes:** Focused PostgreSQL proof, the baseline specification, ADR-0028, event-sourcing doctrine, and the decision-record, feature-matrix, unslop and code-review playbooks.

**Produces:** A dated narrow current-context recovery disposition, an honest remaining row 07 gap list, reviewed PR and evidence for the committed development version.

- [ ] Record that current action context and its clock transition reconstruct from the ordered event stream when the component row is missing or null-root; include read no-writeback, later legal save repair and unreplayable-history failure. Retain the original audit finding and leave other optional/nested caches open.
- [ ] Record the already-verified diary partial-row recovery and the PR #210 TownVisit recovery as separate scoped dispositions; do not infer that their fixes prove other cache components.
- [ ] Run focused PostgreSQL tests, inspect the migration and event-version diff, and run `py -3 tools/run.py ci --check`; confirm the generated web identity reports `0.1.0-dev.26` and there is no event or migration change.
- [ ] Re-run the focused command from Task 3 after implementation; after the canonical gate, inspect `src/WildBunch.Web/dist/version.json` for `0.1.0-dev.26` and inspect `git diff -- src/WildBunch.Persistence/Migrations` and event serialization/version files for no schema or event-contract change.
- [ ] Complete the whole-branch review against this plan, baseline specification, PS-04/05, ADR-0028, event-sourcing doctrine, unslop and code-review runbook; resolve actionable findings and inspect the final committed head.
- [ ] Publish the reviewed implementation PR to `develop`, verify its exact source head/body and hosted canonical gate, mark it ready after review and validation pass, and merge it under the active goal authorization.
- [ ] Fast-forward the main checkout to merged `develop`; remove only this verified merged worktree and its branch-scoped scratch after checking exact containment and clean Git state.
