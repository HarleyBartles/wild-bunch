# Persisted Historical Event Replay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline under the active Stable 0.1.0 goal.

**Goal:** Prove that production command and player/journal read repositories reconstruct a stale session from a supported historical `WorldGenerated` v1 event, preserve the stored event row, and fail closed when the later `CaseFileGenerated` fact is missing.

**Architecture:** Full replay must pass every persisted event through `PersistedPayloadLoader`, which applies the registered event upcaster before decoding and rebuilding `GameSession`; snapshots and components remain disposable caches. The v1 `WorldGenerated` upcaster supplies only the missing `caseFile: null` field, and the later event restores case state. Tests will make the cache path observably stale so a snapshot load cannot satisfy the assertions; production code changes only if those behavioral proofs fail.

**Tech Stack:** C#/.NET 10, EF Core, PostgreSQL, xUnit integration tests, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially cache-backed state and recovery; row 07 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md).

**Execution Strategy:** `executing-plans` inline. The command loader, typed read loader, immutable event-row assertion and fail-closed history case share one deliberately downgraded PostgreSQL stream; sequential execution keeps the fixture and replay oracle coherent. A per-task implementer handoff would add context cost without independent source boundaries; a fresh whole-branch reviewer remains the review gate.

## Global Constraints

- Preserve the settled event-sourced cache policy: events record facts; caches are derived and recover from ordered history; replay does not reroll randomness.
- Preserve strict CQRS, production upcasters, existing event/schema versions and immutable persisted event rows; do not add compatibility snapshots or upcast-writeback.
- The first public baseline remains `0.1.0`; this PR advances the single version authority once to `0.1.0-dev.29` in `Directory.Build.props`.
- Do not add migrations, event types or payload changes, alter gameplay rules, expose hidden truth through player reads, or modify data outside the PostgreSQL test fixture.
- Keep the active plan through its completing PR; the next substantive slice assesses and retires it if the full plan scope shipped.

## Review Focus

- A persisted v1 `WorldGenerated` with its `caseFile` field absent must be upcast and replayed through real repository loads; prove this by pairing a stale snapshot version with a valid but wrong cached player name and requiring the event-established name and later `CaseFileGenerated` facts.
- Player and journal queries must take the same event-backed read cut without repairing the stale cache or changing raw v1 event JSON/version; the test must assert both output facts and unchanged stored rows.
- If the legacy `WorldGenerated` has no later `CaseFileGenerated`, command and query loads must surface the existing explicit replay failure rather than return the placeholder case file or cached state.

---

### Task 1: Retire the completed cache-recovery plan and advance the successor

**Files:** `.agents/plans/2026-10-09-completed-journey-history-cache-recovery.md` (retire); `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `Directory.Build.props`; this plan.

**Consumes:** Merged PR #213; completed-journey cache implementation and evidence; `0.1.0-dev.28` version authority.

**Produces:** The first substantive commit for this slice retires the completed predecessor, records PR #213's exact delivery evidence in row 07, points row 07 to this plan, and advances the product identity to `0.1.0-dev.29`.

- [x] Verify PR #213 targets `develop`, reviewed source is `45c923fb9a8dd77caec111bce5d0be08706748a6`, squash merge is `c68b5b0e4c9842cfa06852bc40896b466b32935a`, source/merge/develop trees match at `2321b81e662eb1c6842334150f9bbd1a9468e6be`, and hosted canonical gate run `37903398224` passed on the source head.
- [x] Read the completed-artifact custody doctrine and assess the full completed-journey plan against its merged implementation and delivery evidence; its durable cache-recovery facts are already promoted in `docs/features.md` and the persistence test follow-up, so remove the eligible plan and replace its stale roadmap pointer.
- [x] Update row 07 with PR #213's source, merge, tree, hosted gate and `0.1.0-dev.28` facts; record that missing-row, null-root and empty-array completed history now matches ordered acknowledgement snapshots, and make persisted historical-event replay the next bounded gap.
- [x] Advance only `Directory.Build.props` from `0.1.0-dev.28` to `0.1.0-dev.29`; do not commit generated web identity output or duplicate the authored product version.
- [x] Review and commit this artifact/version diff before changing behavior tests or implementation; the check-only pre-commit hook must validate the staged candidate.

### Task 2: Exercise historical event upcasting through production command and read repositories

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; change production files only if the test exposes a real gap.

**Consumes:** `LegacyWorldGenerated_LoadsFromPersistedEvents_AndCurrentWritesUseV2`, `ReadModel_StaleSnapshotRebuildsPlayerAndJournalFromEvents`, `SessionRebuilder.RebuildFromEvents`, `WorldGeneratedV1ToV2Upcaster`, the PostgreSQL fixture, `CreateSession`, `PersistAsync`, and production repository/read-loader builders.

**Produces:** PostgreSQL behavior proof that current authored v2 history is converted to supported persisted v1 without rewriting it, a deliberately stale snapshot cannot mask event-established aggregate/read facts, and the next legal save converges caches while preserving the immutable historical event.

Ruling: Keep the command path, player/journal read paths and historical-row preservation in one task because they consume the same deliberately downgraded stream and share the event-decoding funnel. No production implementation change is expected if the existing path already satisfies the behavior; tests must still prove the new regression guard can fail. A persisted food-purchase event immediately after the snapshot makes the fast path's one-event catch-up unable to repair the stale player name; only full replay restores it. PostgreSQL `jsonb` normalizes payload whitespace, so compare the database-reloaded representation across reads and writes rather than an in-memory pre-save JSON string; this preserves an exact persistence oracle without mistaking serialization formatting for event mutation.

- [x] Extend the existing legacy-event test to capture the current writer's v2 payload, remove only its `caseFile` property and store the row as schema v1; first persist a food purchase so the last event changes inventory but not the player name, then use a helper that replaces the cached player name with a valid stale sentinel and sets `SnapshotVersion` one event behind `StreamVersion`.
- [x] Fresh-load through `EfGameSessionRepository.GetByIdAsync` and assert the replayed aggregate's player name, true culprit identity and known clue IDs equal values captured from the original session, while current `WorldGenerated` v1 is followed by the persisted `CaseFileGenerated` event. Query assertions remain limited to player-known facts and never expose the true culprit identity.
- [x] Before any legal save, load through `EfGameSessionReadRepository` and `EfGameJournalReadRepository`; assert the event-established player name, opening lead and known-clue facts, and assert that neither query changes the stale player JSON, stale snapshot position, raw `WorldGenerated` payload or its schema version.
- [x] Prove the tests are sensitive to the required replay paths by temporarily bypassing the stale-snapshot branch in `EfGameSessionRepository.LoadWithinTransactionAsync` and `GameSessionReadStoreLoader.CreateReadState`, one disposable edit at a time; the corresponding command or read assertion must fail with the stale sentinel name. Restore each edit before proceeding.
- [x] Execute one legal food purchase through the replayed aggregate and persist with the existing repository/unit of work; fresh-load to prove the player cache/snapshot converged, and verify the original `WorldGenerated` payload as returned from PostgreSQL and its schema version remain unchanged while the purchase is an appended current event.
- [x] Add `LegacyWorldGenerated_WithoutCaseFileEventFailsClosed`: remove the `CaseFileGenerated` row from the downgraded history, force the stale-snapshot path, and assert command and read loads surface `InvalidOperationException` with `Cannot replay a legacy WorldGenerated event without a CaseFileGenerated event.` rather than returning defaults.
- [x] Prove the fail-closed test is sensitive by temporarily bypassing the legacy missing-case-file guard in a disposable edit; the expected explicit failure assertion must fail, then restore the guard.
- [x] Ensure `./tools/postgres-dev.ps1 ensure` succeeds, then run: `py -3 tools/run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.LegacyWorldGenerated_LoadsFromPersistedEvents_AndCurrentWritesUseV2|FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.ReadModels_LegacyWorldGeneratedEventUpcastsThroughProductionLoaderWithoutWriteback|FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.LegacyWorldGenerated_WithoutCaseFileEventFailsClosed"`.
- [x] Commit the focused behavior test and any minimal required production correction after the focused proof passes through the normal check-only hook.

### Task 3: Record bounded evidence and deliver the slice

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; row 07 roadmap; changed test and any implementation file.

**Consumes:** Focused PostgreSQL evidence, the accepted baseline specification, ADR-0028, event-sourcing integrity doctrine, selected backend-architecture and code-review profiles, feature-matrix and decision-record playbooks, and PR/code-review runbooks.

**Produces:** A truthful PLAT-001/test disposition and a reviewed implementation PR to `develop`, with merged `0.1.0-dev.29` delivery evidence.

- [x] Add a dated persistence-test disposition limited to production repository and typed read reconstruction of a supported v1 `WorldGenerated`, immutable stored event rows, stale-cache no-writeback, legal-save convergence and the missing-`CaseFileGenerated` failure; keep unrelated historical formats and all other recovery gaps open.
- [x] Update PLAT-001 with the production full-replay/upcaster result and exact evidence limits; preserve prior row 07 outcomes and do not claim general historical-data compatibility beyond the supported v1 fixture.
- [x] Compare the diff with ADR-0028 and the decision-record playbook; state that event history/upcasting/cache decisions are unchanged and leave the ADR untouched unless implementation reveals a genuine durable divergence.
- [x] Confirm generated `src/WildBunch.Web/dist/version.json` reports `0.1.0-dev.29`; do not stage generated build output. The normal hooked commit runs `py -3 tools/run.py ci --check` on its staged candidate and must pass.
- [ ] Complete a fresh whole-branch review against this plan, the accepted spec, PS-11/12/16, ADR-0028, event-sourcing integrity, selected backend/code-review unslop profiles, feature matrix, and code-review runbook; resolve actionable findings before publication.
- [ ] Open a Draft PR targeting `develop`; verify its body, base and exact remote head, then mark it ready after review and local validation. Require and verify the hosted canonical gate on that exact head.
- [ ] Merge to `develop`; verify the merge and hosted gate, fast-forward the primary checkout, and clean only the verified merged worktree, local/remote branch and branch-scoped scratch.
