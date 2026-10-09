# Snapshot Tail Retirement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Keep snapshot recovery aligned with the repository's selected rule: current coherent snapshots load from cache, stale or invalid snapshots rebuild from immutable event history.

**Architecture:** `EfGameSessionRepository.LoadAsync` holds the envelope, component-presence check, components, event stream and diary rows inside one PostgreSQL `RepeatableRead` transaction. `LoadWithinTransactionAsync` sends a stale snapshot to full replay before the fast path. Therefore a `GameSessionStore` passed to `ToAggregate` has `SnapshotVersion == StreamVersion`, and its post-snapshot event list is necessarily empty. Retire the unreachable tail-replay continuation and keep event upcasters, query full replay, snapshot restoration and projection rebuilds intact.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, xUnit, Wild Bunch Persistence.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07; `.agents/investigations/stable-0.1.0/2026-10-07-cache-state-architecture-spike.md`.

**Execution Strategy:** `executing-plans` - Native inline execution is the best fit because this is one private loader path, its integration behavior proof and a bounded artifact update; the nearest alternative, subagent-driven development, would repeat context setup for tightly coupled changes without gaining an independent test lane. The required fresh whole-branch reviewer still provides independent review.

## Rulings

- Ruling: Retire only command-side snapshot-tail continuation in this slice - the production loader checks snapshot/stream equality and loads its second envelope and components under the same `RepeatableRead` snapshot, so the tail cannot exist on the fast path; stale caches still rebuild from the full event stream. Cost if wrong: a future caller that bypasses the loader gate would need its own explicit recovery route.
- Ruling: Keep event upcasters and defer removal of `PayloadKind.Projection` to a separate JIT slice - event compatibility is used in production while the registry narrowing is an independent extension-contract cleanup. Cost if wrong: inert projection-upcaster scaffolding remains briefly.
- Ruling: Do not add a test for private branch presence or replay arithmetic - strengthen the existing PostgreSQL behavior test so that stale version metadata with already-current component payload cannot apply the event tail twice, and remove the log-duplication test whose claimed prefix-plus-tail projection behavior no longer exists. Cost if wrong: the current command stale-cache behavior would lack a direct regression proof.
- Ruling: Let the check-only commit hook run the complete fail-fast gate rather than invoking `ci --check` immediately before commit - the PR runbook and handoff gate define that hook as the canonical staged-candidate check. Cost if wrong: a failure is first surfaced at commit time, with the same ordered diagnostics and no source mutation.

## Global Constraints

- Immutable typed event history remains authoritative; current caches are an ordinary load optimization and invalid caches rebuild from ordered history.
- Every load observes one coherent stream position. Do not weaken the command or query `RepeatableRead` boundaries.
- Preserve explicit snapshot restoration of seed, phase, action context, dev overrides, travel diary and all current state; preserve full replay recovery and fail-closed history behavior.
- Keep all event types, event payloads, schema versions, migrations and upcasters unchanged. This plan does not remove projection versions or diary rebuild logic.
- Do not change player-facing feature status or dependencies; add only PR #228 delivery evidence for PLAT-001 and state that its feature truth remains unchanged.
- Advance the sole authored application version in `Directory.Build.props` from `0.1.0-dev.43` to `0.1.0-dev.44` once for this implementation PR.
- Target `develop`; require the exact final PR head to pass `Canonical tracked commit gate` and the resulting `develop` merge SHA to pass its push-triggered gate before the next slice.

## Review Focus

- A stale `SnapshotVersion` paired with a current component cache must not apply already represented events a second time; assert wallet, inventory and aggregate version against independently observed committed state.
- After stale-snapshot recovery, a legal command must append once at the persisted stream tail and survive a fresh load.
- A current snapshot still restores action context without spending another turn; retain the existing integration proof rather than changing that contract.
- Query reads still rebuild stale snapshots from full history without writing cache repairs; retain the existing PostgreSQL proof.

---

### Task 1: Record PR #228 and create the `.44` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, `docs/features.md`, and `Directory.Build.props`; delete `.agents/plans/2026-10-09-explicit-persistence-rehydration.md`; create this plan.

**Interfaces:** Record PR #228 source `9aa37a7e40e9fca2b8699442b6c2946e603b2385`, merge `4a659ec5b21466aba2bc6df3d00c9a439cf95a27`, exact-head PR gate run `37981912234`, post-merge push gate run `37982623407`, and delivered version `0.1.0-dev.43`. The roadmap keeps row 07 `executing` and points to this `.44` successor.

- [x] Verify PR #228 is merged to `develop`, its source and merge SHAs match the values above, the PR gate passes on the source SHA, and the post-merge push gate passes on the merge SHA.
- [x] Compare the `.43` plan's scope with the merged diff and behavior tests; classify explicit snapshot rehydration as shipped, retaining current action-context continuation and structured travel-diary round-trip behavior.
- [x] Update the roadmap and persistence follow-up with PR #228, `.43`, both exact hosted gate results, the no-actionable-findings review and the new row 07 successor.
- [x] Resolve PS-08's former external/concurrent-envelope caveat against the current repeatable-read loader boundary; record why command-side post-snapshot replay is unreachable and preserve query full replay.
- [x] Remove the completed `.43` plan and its stale roadmap link in this successor commit. Keep unrelated persistence findings open.
- [x] Advance `Directory.Build.props` from `0.1.0-dev.43` to `0.1.0-dev.44`; inspect the staged planning diff and commit it before production implementation.

**Expected:** The repo records PR #228 accurately, retires only its completed plan, and has this committed `.44` implementation plan as the live row 07 pointer.

### Task 2: Remove unreachable command snapshot-tail replay

**Files:** Modify `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` and `tests/WildBunch.Integration.Tests/EventStorePersistenceTests.cs`.

**Interfaces:** Keep `LoadWithinTransactionAsync` as the only route into `ToAggregate`; it already compares the initial envelope's `SnapshotVersion` and `StreamVersion`, and its enclosing transaction ensures `LoadStoreAsync` observes the same envelope position. `GameSessionStore` retains `Envelope`, `Components`, `TravelDiaryDays` and `AllEvents`, but no `PostSnapshotEvents` member.

- [x] Strengthen `GetByIdAsync_WithStaleSnapshotRecoversCurrentCacheAndAllowsNextCommand` into a behavior test of an already-current player cache paired with deliberately stale snapshot metadata. Assert the recovered wallet and Food quantity equal committed values, the aggregate version equals the stored stream version, and the next legal purchase appends once at the stream tail.
- [x] Before production edits, temporarily bypass only the snapshot-version mismatch full-replay guard and run the focused test. It failed on the intended wallet assertion because the purchase was applied twice; restore the guard and confirm the test passes. Do not commit the temporary mutation.
- [x] Remove `PostSnapshotEvents` calculation from `LoadStoreAsync` and remove that field from `GameSessionStore`.
- [x] In `ToAggregate`, restore `StreamVersion` directly, derive `StartFlowPhase` from the loaded event stream, and remove the conditional snapshot-prefix/tail comments and `ApplyCommittedEvents` branch. Preserve explicit restoration of seed, action context, dev state and committed events.
- [x] Remove `GetByIdAsync_WithLaggingSnapshot_DoesNotDuplicateAggregateLogEntries`; its prefix-plus-tail projection premise is obsolete because journal entries project the complete committed stream once. Keep other behavior tests for actual journal projection and event facts.
- [x] Run the focused PostgreSQL tests for the stale-snapshot command load, query stale-snapshot rebuild and current action-context continuation. These all pass; the full staged-candidate hook still covers event replay, diary reconstruction and missing/invalid-cache recovery.
- [x] Run `dotnet tool restore` and `dotnet ef migrations list --project src\WildBunch.Persistence --startup-project src\WildBunch.Api`; the existing diary watermark migration remains pending. Stage intended changes and use the normal check-only commit hook as the complete fail-fast gate. Do not run `ci --check` immediately before the hooked commit.
- [x] Update the persistence follow-up with the durable test-boundary disposition and remaining scope, without committing transient test-result receipts. Leave ADR-0028 and feature status unchanged because the selected event-authoritative cache policy is unchanged.
- [ ] Complete fresh whole-branch review against this plan, the cache-recovery spec, ADR-0028, ADR-0038, backend architecture and testing profiles. Publish a Draft PR to `develop`, verify its head equals local `HEAD`, make it ready for the hosted gate, and require `Canonical tracked commit gate` success on that exact head before merge.
- [ ] Merge after the exact-head gate succeeds; verify the push-triggered canonical gate passes on the resulting `develop` merge SHA before starting another row 07 slice. Leave this plan for retirement by its next substantive successor.

**Expected:** Command loads use a current coherent snapshot directly and recover stale snapshots through full ordered replay. The aggregate version and state match one stream position, no already represented tail event is applied twice, and a resumed legal command appends once at the stream tail. Query full replay, event upcasting, diary rebuild and existing snapshot state restoration remain intact.
