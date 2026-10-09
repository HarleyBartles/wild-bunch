# Completed Journey History Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline under the active Stable 0.1.0 goal.

**Goal:** Restore event-established completed-journey history when its persisted cache is missing or invalid, so command loads retain exact journey history and the next journey receives the correct sequence.

**Architecture:** `JourneyArrivalAcknowledged` events already contain the completed journey snapshot and `JourneyLoop.Apply` rebuilds the history during replay. The command loader will replay when an acknowledgement proves a missing or invalid history cache would omit established state. With no acknowledgement, a missing or invalid optional history cache means empty history and must preserve unrelated snapshot-only state. Query repositories, event contracts, database schema, and journey rules remain unchanged.

**Tech Stack:** C#/.NET 10, EF Core, PostgreSQL, xUnit integration tests, repository command bus.

**Spec:** [.agents/specs/2026-10-07-stable-0.1.0-baseline.md](../specs/2026-10-07-stable-0.1.0-baseline.md), row 07 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md).

**Execution Strategy:** `executing-plans` inline. The tests, event classifier, JSON cache classification, and documentation all share one aggregate-load invariant; sequential execution keeps the event history, optional-cache fast path, repair behavior, and version/artifact delivery in one continuous context. The user has authorized inline JIT execution for this roadmap.

## Global Constraints

- Preserve the settled event-sourced cache policy: events record facts; caches are derived and recover from ordered history; replay does not reroll randomness.
- Missing and invalid completed-history caches trigger replay only when an ordered `JourneyArrivalAcknowledged` event establishes completed history; without one, history is empty and the snapshot fast path must preserve unrelated snapshot-only state.
- The first public baseline remains `0.1.0`; this PR advances the develop identity once to `0.1.0-dev.28` in `Directory.Build.props`.
- No database migration, event type/payload/version change, query mutation, direct aggregate mutation, new compatibility path, or unrelated journey behavior is in scope.
- Keep the active plan in `.agents/plans/` through this PR; the next successor slice retires this plan after reconciling its delivered facts.

## Review Focus

- A missing history cache after acknowledgement must restore the acknowledged journey and preserve sequence 2 for the next journey.
- An invalid present history cache with an acknowledgement must enter full event replay; a damaged authoritative acknowledgement event must still fail closed.
- Missing or invalid history caches without an acknowledgement must not trigger unrelated full replay or discard snapshot-only state.

---

### Task 1: Commit the successor slice and development identity

**Files:** `.agents/plans/2026-10-09-suspect-presence-cache-recovery.md` (retire); `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`; this plan.

**Consumes:** Merged PR #212 on `develop`; predecessor plan and its findings; current `0.1.0-dev.27` version authority.

**Produces:** The first substantive implementation commit advances the single version authority to `0.1.0-dev.28`, retires the completed suspect-presence plan, records verified PR #212 delivery and points row 07 at this plan.

- [x] Verify PR #212 targets `develop`, its reviewed source is `a4a326497b3ce623c7bf44473456ea873f74df38`, squash merge is `dd004f5153bab3a24d8befeb44ca43b03b6bcc75`, and hosted canonical gate run `37897619049` passed on the source head.
- [x] Read the completed-artifact doctrine and assess `.agents/plans/2026-10-09-suspect-presence-cache-recovery.md` against its merged implementation; retain its durable cache-recovery findings in the feature matrix and persistence test follow-up, then remove the completed plan and replace its stale roadmap pointer.
- [x] Update row 07 with PR #212's exact source, merge, gate, and `0.1.0-dev.27` delivery facts; identify completed-journey history as the next bounded cache gap and link this plan.
- [x] Advance only `Directory.Build.props` from `0.1.0-dev.27` to `0.1.0-dev.28`; do not commit generated web identity output or duplicate the application version elsewhere.
- [ ] Review and commit this successor-artifact/version diff before adding or changing behavior tests and implementation; let the normal check-only pre-commit hook validate the staged candidate.

### Task 2: Recover acknowledged journey history through command replay

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`; new `src/WildBunch.Persistence/GameSessions/CompletedJourneyHistoryCacheRecovery.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Travel.cs`.

**Consumes:** `JourneyArrivalAcknowledged` with its production `JourneySnapshot`; `CreateJourneyHistorySession`, `CreateJourneyPreview`, `PersistAsync`, and the PostgreSQL integration fixture.

**Produces:** A behavior-first proof that missing and JSON-null `completedJourneyHistory` caches reconstruct acknowledged history, preserve the next journey sequence, remain untouched by reads, repair on the next legal save, and fail closed when authoritative history is undecodable, while an invalid no-ack cache preserves snapshot-only state.

Ruling: Keep the behavior test and implementation in one plan task because committing the intentionally failing recovery test alone would leave the canonical commit gate red. Observe the pre-fix failures first, then commit the test and minimum production recovery together once the focused proof is green.

The red run observed missing-row history as empty and JSON-null as an `InvalidOperationException`; the missing-row behavior assertion reported next journey sequence 1 instead of 2. Temporarily disabling the acknowledgement classifier reproduced sequence 1 for both damage forms. Temporarily allowing the invalid no-acknowledgement cache to use the outer replay fallback made the pending-foe test fail because hidden pressure changed from 1 to 0. Restoring the classifier and no-acknowledgement empty-history handling returned all focused cases to green. The final focused rerun passed all five selected PostgreSQL cases.

- [x] Add `CommandLoad_CompletedJourneyHistoryCacheRecoversFromEventsWithoutWritingBack` as a PostgreSQL theory for `missing-row` and `null-root`. Complete and acknowledge a one-day journey through the public game flow, persist it through the repository/unit of work, capture its `JourneyArrivalAcknowledged` event and cache/envelope/diary metadata, then damage only the `completedJourneyHistory` component.
- [x] Before production changes, run the focused cases and record that the missing row currently reloads empty history and causes the next journey to start at sequence 1 instead of 2, while a JSON-null root fails during deserialization; prove the fixture contains a persisted acknowledgement before attributing either failure to recovery.
- [x] Implement `CompletedJourneyHistoryCacheRecovery` narrowly over ordered events: a missing history row requires full replay when history contains `JourneyArrivalAcknowledged`; with no acknowledgement, preserve the absent-empty fast path. Do not infer acknowledgement from a `JourneyStarted`, `JourneyCompleted`, diary row, or snapshot.
- [x] Wrap malformed/null completed-history cache decoding in `InvalidComponentCacheShapeException` with component identity `completedJourneyHistory`. If that component alone is invalid and no acknowledgement exists, treat history as empty without replaying unrelated state; if an acknowledgement exists, use full replay. Do not catch event loading or replay errors as cache damage.
- [x] Assert the recovered aggregate has no active journey and contains the exact completed sequence, route endpoints, and `Completed` status. Confirm the missing/null cache, envelope positions, ordered event rows, and diary metadata remain unchanged during the recovery read.
- [x] Start the next legal journey from the recovered aggregate and assert sequence 2 before saving. Persist through the normal unit of work, then fresh-load and verify the history remains sequence 1, the active journey is sequence 2, and the repaired component uses the current projection version.
- [x] Add `DamagedCompletedJourneyHistoryCacheDoesNotHideInvalidAcknowledgementEvent`; damage the history cache and the authoritative `JourneyArrivalAcknowledged` payload, then assert command loading surfaces the event decoding failure.
- [x] Add a no-acknowledgement `null-root` cache case with unrelated snapshot-only state by extending `SaveAfterPendingFoeEncounterWithHiddenPressureRoundTripsTheHiddenState`; prove that state survives command load and that forced replay fails this assertion.
- [ ] Ensure `./tools/postgres-dev.ps1 ensure` succeeds, then run: `py -3 tools/run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.CommandLoad_CompletedJourneyHistoryCacheRecoversFromEventsWithoutWritingBack|FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.DamagedCompletedJourneyHistoryCacheDoesNotHideInvalidEventHistory|FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.SaveAfterPendingFoeEncounterWithHiddenPressureRoundTripsTheHiddenState|FullyQualifiedName~WildBunch.Integration.Tests.GameSessionDifficultyPersistenceTests.TownVisitStateWithMultipleTownVisitsRoundTripsThroughRepositoryPersistence"`.
- [ ] Commit focused behavior and implementation after the tests pass through the normal check-only hook.

### Task 3: Record the bounded result and deliver the slice

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; row 07 roadmap; changed implementation and tests.

**Consumes:** Focused PostgreSQL evidence, the approved baseline specification, ADR-0028, event-sourcing integrity doctrine, unslop/backend architecture profile, feature-matrix and decision-record playbooks, and PR/code-review runbooks.

**Produces:** An accurately bounded feature/test disposition and a reviewed implementation PR to `develop`, with merged `0.1.0-dev.28` delivery evidence.

- [ ] Add a dated PS-04/PS-05 and PLAT-001 disposition limited to missing-row and JSON-null completed-history recovery, the no-ack absent-empty fast path, read no-writeback, next-save repair, correct next-sequence behavior, and fail-closed event decoding. State explicitly that this does not prove recovery for every malformed history payload or every optional component.
- [ ] Update PLAT-001 in `docs/features.md` to include the recovered acknowledgement history and next-journey sequence behavior while preserving all previously delivered row 07 facts and open gaps.
- [ ] Compare the diff with ADR-0028 and relevant doctrine. Confirm there is no schema migration, event serializer/upcaster contract, event payload/version, query repository, or durable architecture change; do not modify the ADR when its decision remains true.
- [ ] Confirm generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.28`; do not stage or commit generated build output. The normal hooked commit runs `py -3 tools/run.py ci --check` on its staged candidate and must pass.
- [ ] Complete the fresh whole-branch review against this plan, the accepted spec, PS-04/05, ADR-0028, event-sourcing integrity, selected backend/code-review unslop profiles, feature matrix, and code-review runbook. Resolve actionable findings before publication.
- [ ] Open or update a Draft PR targeting `develop`, verify its exact source head and body, mark it ready once review and local validation pass, and require the hosted canonical gate on that head.
- [ ] Merge the approved PR to `develop`; verify the merge and exact-head hosted gate; fast-forward the primary checkout and clean only the verified merged worktree, local/remote branch, and branch-scoped scratch.
