# Recover Malformed Saloon Identity Cache from Events Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline under the active Stable 0.1.0 goal.

**Goal:** Recover the event-established active saloon person when a present `townVisitState` cache decodes but omits the wanted suspect identity, so a cache cannot turn a suspect into a citizen.

**Architecture:** `GameStarted` and `SaloonPersonOfInterestSpotted` establish the visit and active person in the event stream; the persisted `TownVisitState` is a disposable cache. Treat the impossible current shape `PersonOfInterestKind == WantedSuspect` with no suspect ID as an invalid cache shape, then let existing aggregate and player-read recovery rebuild through ordered events without writeback. Do not add an event, schema, migration, or new recovery authority.

**Tech Stack:** C#/.NET 10, EF Core, PostgreSQL, xUnit integration tests, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially cache-backed state and recovery and PLAT-001; row 07 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md).

**Execution Strategy:** `executing-plans` inline. One PostgreSQL session and one semantically contradictory nested payload exercise the serializer, aggregate loader, player-read loader, immutable-cache boundary, and the existing event-derived recovery path. Keep the test, minimal shape validation, bounded evidence update, fresh branch review, and PR delivery in this single sequential slice.

## Global Constraints

- Preserve the settled event-sourced cache policy: events record facts; caches are derived and recover from ordered history; replay does not reroll randomness.
- Preserve strict CQRS, production event decoding, immutable persisted event rows, and query no-writeback; no migration or event/schema version change is expected.
- The first public baseline remains `0.1.0`; this PR advances the single authored version authority once to `0.1.0-dev.30` in `Directory.Build.props`.
- Do not change saloon rules, feature scope, developer controls, event payloads, player-safe truth, or any persisted data outside the PostgreSQL test fixture.
- Keep this plan through its completing PR; the next substantive slice assesses it against its full scope and delivery evidence.

## Review Focus

- A present cache with wanted-suspect kind, descriptor, and no suspect ID must not deserialize into a state that makes the suspect look like a citizen; the PostgreSQL test must fail on the currently loaded null ID.
- Command and player-read recovery must use the same event-established suspect while leaving the malformed component payload/version, event stream, and envelope untouched during reads.
- A genuine citizen with no suspect ID and a citizen descriptor must remain valid; the shape guard must not classify ordinary citizen state as corruption.

---

### Task 1: Retire the completed persisted-event replay plan and advance the successor

**Files:** `.agents/plans/2026-10-09-persisted-historical-event-replay.md` (retire); `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`; this plan.

**Consumes:** Merged PR #214; its tested supported `WorldGenerated` v1 replay behavior and bounded PLAT-001 evidence; current `0.1.0-dev.29` authority.

**Produces:** The first substantive commit retires the completed predecessor, records PR #214's exact delivery evidence in row 07, points the roadmap at this plan, and advances the product version to `0.1.0-dev.30`.

- [x] Verify PR #214 targets `develop`, reviewed source is `77b1386b2d5e193966cd91157a9e5f8858e35549`, squash merge is `95f41b149d5f6b82176590db6cfd5d62c91d84e5`, source/merge/develop trees match at `daaf882be32adb4ded061c32439e5b5aa5f925dd`, and hosted canonical gate run `37907858474` passed on the source head.
- [x] Assess the whole predecessor plan against PR #214's implementation, passing focused and full local gates, fresh no-finding whole-branch review, and hosted gate; its bounded scope is promoted in the feature matrix and persistence follow-up, so retire the plan and replace the stale roadmap pointer.
- [x] Update row 07 with PR #214's source, merge, tree, hosted gate, review result, and `0.1.0-dev.29` facts; identify the present nested wanted-suspect cache shape as the next bounded gap.
- [x] Advance only `Directory.Build.props` from `0.1.0-dev.29` to `0.1.0-dev.30`; do not commit generated web identity output or add another authored version.
- [x] Review and commit this artifact/version diff before changing behavior tests or implementation; the check-only pre-commit hook must validate the staged candidate.

### Task 2: Recover a semantically invalid nested saloon person cache

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs` only if the red test confirms the missing shape guard.

**Consumes:** `TownVisitStateSnapshot` and `TownVisitTownStateSnapshot`; `DeserializeTownVisitState`; `GameSessionComponentPayloads.GetOptionalPayload`; `TownVisitCacheRecovery`; `EfGameSessionRepository`; `GameSessionReadStoreLoader`; the PostgreSQL fixture and production test repository builders.

**Produces:** PostgreSQL proof that the command and player-read loaders recover an event-established wanted suspect after the nested suspect ID is removed from an otherwise valid cache, preserve that damaged cache during reads, and still accept a genuine citizen shape.

Ruling: A current `SaloonPersonOfInterestSpotted` event with a wanted-suspect identity records the active person; `TownVisitTownStateSnapshot.ToDomain` currently permits `PersonOfInterestKind == WantedSuspect` with a null ID, and the confrontation route branches on the ID, so a missing cache fact can be interpreted as a citizen. Make only this contradictory tagged union invalid and use existing full replay. Do not validate unrelated nested fields in this slice or replay healthy caches on every request.

- [ ] Build a complete started session with one non-culprit suspect, a distinct culprit, deterministic setup, and a normal `LookAroundSaloon` operation that produces a persisted `SaloonPersonOfInterestSpotted` event for the non-culprit; capture its ID, descriptor, and kind independently.
- [ ] Persist through the production repository, then remove only `activeSaloonPersonOfInterestId` from that town's `townVisitState` entry while retaining its descriptor and `WantedSuspect` kind; keep component version and envelope positions current.
- [ ] Add command-load and player-read assertions for the original suspect ID, descriptor, kind, and saloon source fact, plus unchanged malformed JSON/component version, event rows, envelope positions, and diary metadata during reads. Strengthen or reuse the deterministic citizen persistence scenario to assert citizen kind and null suspect ID are still valid under the new guard.
- [ ] Run the focused PostgreSQL test before production changes and confirm it fails because the loaded wanted suspect has no ID; do not accept a test that only checks successful deserialization or cache-shape diagnostics.
- [ ] Add the narrow nested-shape validation that reports the tagged wanted-suspect-without-ID state as `InvalidComponentCacheShapeException`; verify both loaders then recover from `SaloonPersonOfInterestSpotted` and the existing replay path without cache writeback.
- [ ] Prove the regression guard is route-sensitive by temporarily bypassing the invalid-shape validation and confirming the focused behavior assertion fails on the null ID; restore the production code and rerun the focused test.
- [ ] Run `py -3 tools/run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.ReadModel_MalformedTownVisitWantedSuspectShapeRecoversFromEvents"` after ensuring PostgreSQL with `./tools/postgres-dev.ps1 ensure`.
- [ ] Commit the passing behavior test and minimal shape validation through the normal check-only hook.

### Task 3: Record bounded recovery evidence and deliver the slice

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; row 07 roadmap; test and serializer if changed.

**Consumes:** Focused PostgreSQL evidence, the accepted cache-recovery contract, ADR-0028, event-sourcing integrity doctrine, selected backend-architecture and code-review profiles, feature-matrix and decision-record playbooks, and PR/code-review runbooks.

**Produces:** A precise PLAT-001 and persistence-test disposition for this nested cache shape, with no broader claim about all nested or optional component corruption.

- [ ] Add a dated persistence-test disposition limited to the impossible wanted-suspect-without-ID cache shape, aggregate/player-read reconstruction, query no-writeback, retained citizen absence, and fail-closed invalid event history if covered; keep unrelated component shapes open.
- [ ] Update PLAT-001 with the exact event-backed recovery behavior and test limits; do not imply general TownVisitState or optional-component compatibility.
- [ ] Compare the diff with ADR-0028 and the decision-record playbook; leave the ADR unchanged because event authority and cache recovery remain the existing decision.
- [ ] Confirm generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.30`; do not stage generated output. Run the normal check-only hook and canonical `py -3 tools/run.py ci --check` on the implementation candidate.
- [ ] Complete a fresh whole-branch review against this plan, the accepted spec, PS-04/05/14, ADR-0028, event-sourcing integrity, selected backend/code-review unslop profiles, feature matrix, and code-review runbook; resolve actionable findings before publication.
- [ ] Open a Draft PR to `develop`; verify body, base and exact remote head, then mark ready after review and local validation. Require and verify the hosted canonical gate on that exact head.
- [ ] Merge to `develop`; verify merge and hosted gate, fast-forward the primary checkout, and clean only the verified merged worktree, local/remote branch, and branch-scoped scratch.
