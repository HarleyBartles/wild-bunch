# World Cache Recovery From Generated Facts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Recover a current-version World component whose valid JSON no longer matches the immutable `WorldGenerated` fact, so reloads retain the settled map and ordinary saves repair only the derived cache.

**Architecture:** `WorldGenerated.World` is the event-established world; the current `world` component is a rebuildable cache. The command and player-read loaders may use the component only when its ordered towns, trails and generated town layouts match that fact; otherwise they reconstruct from the event stream. Reads remain write-free, later ordinary saves persist the reconstructed state, and no map generation or random choice runs during loading.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Event history, cache-backed state and recovery”; row 07 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-04 in `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and the persistence test follow-up.

**Execution Strategy:** Use `executing-plans` inline and sequentially under the active roadmap goal. The command/read loaders share one persisted-world invariant and the same PostgreSQL fixture; there are no independent implementation tracks to parallelize. Write a production repository test first, witness that the current-version but incomplete World JSON is accepted before the fix, then add only the cache-vs-event comparison needed to recover it.

## Global Constraints

- Start from `develop` merge `d91b0624305dc73d85f61a7c017af6785ee5b610` in this fresh canonical worktree; target `develop`.
- Commit this JIT plan before implementation. Record PR #237 source `1b91a54fe7c31d3c37f51b8441c8012ef08dab57`, merge `d91b0624305dc73d85f61a7c017af6785ee5b610`, exact-head gate `38016110703`, develop push gate `38016383482`, and delivered `0.1.0-dev.52`. Retire the `.52` plan, point row 07 to this plan, and advance `Directory.Build.props` to `0.1.0-dev.53` in that planning commit.
- Treat the sole `WorldGenerated` event as the source of the world’s town, trail and generated-layout facts. Preserve order and every stored value; do not regenerate a world or layout during loading.
- Compare a decoded current-version `world` component with the recorded WorldGenerated snapshot on both command and player-read paths. A mismatch rebuilds the session from authoritative events; valid matching state stays on the snapshot fast path.
- Query recovery does not write component rows, envelope watermarks, diary rows or events. A later ordinary legal command save repairs the whole current snapshot from the replayed aggregate.
- Do not change event payloads, event schema version, upcasters, projection versions, database schema or migrations. Preserve fail-closed behavior for missing, malformed, unknown or future-version authoritative history.
- Preserve ADR-0028’s existing decision that immutable event history reconstructs state and current caches are rebuildable. No ADR or feature-matrix change is required unless source review discovers a different durable decision.

## Review Focus

- A World JSON payload with valid structure and current component version can still be stale or incomplete: removing an event-established trail must cause command and player-read paths to return that trail from `WorldGenerated`.
- World equality must include generated town layout data and ordered trails, not only town counts or IDs; recovery must retain recorded values without running generation.
- Reads must preserve the damaged cache and history exactly as observed from PostgreSQL, while an ordinary legal save writes the replayed complete World component.
- Invalid authoritative event payloads must remain errors and must never be hidden by cache recovery.

---

### Task 1: Record PR #237 and commit the `.53` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-malformed-diary-cache-recovery.md`; create this plan.

- [x] Confirm PR #237’s exact source and merge SHAs and both hosted gate runs above.
- [x] Record `.52` delivery and this selected PS-04 World-cache consistency slice in row 07 and the roadmap dated delivery history.
- [x] Retire the completed `.52` plan and advance the single authored application version to `0.1.0-dev.53`.
- [x] Commit the planning handoff before editing source or tests (`beaaad9`).

**Expected:** The roadmap points to this committed plan, records verified `.52` delivery, and the plan states a bounded implementation and proof contract.

### Task 2: Recover a valid but incomplete World cache from generated facts

**Files:** Modify `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` and `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`; create `src/WildBunch.Persistence/GameSessions/WorldCacheRecovery.cs` and add a focused behavior test to `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

- [x] Create an event-backed session with four towns, three trails and populated town layouts, then remove one trail from the current-version `world` component JSON while keeping the JSON valid.
- [x] Before production changes, prove the new PostgreSQL regression fails because the player read returns the incomplete cached world.
- [x] Add `WorldCacheRecovery.MatchesGeneratedWorld(IReadOnlyList<IDomainEvent> events, World world, GameSessionJsonSerializer serializer)`. It requires one `WorldGenerated` event and compares its recorded World snapshot with the decoded cache through the serializer’s canonical World JSON shape, including ordered towns and trails, maps, town layouts, nullable layout data, salts and tile grids.
- [x] Route a mismatch through existing event replay in both command and player-read loaders. Event decoding, upcasting and projection failures remain outside cache recovery.
- [x] Assert command, player and journal reads preserve the complete event-established world and leave every component row, diary row, envelope watermark and stored event identity/type/payload/version unchanged.
- [x] Perform one ordinary food purchase and unit-of-work save from the recovered aggregate; a fresh load and stored component confirm the full World cache is repaired and play can continue.
- [x] Add an invalid `WorldGenerated` JSON negative with a damaged World component and prove the authoritative event decode error escapes; both new PostgreSQL tests pass, and the recovery assertion failed before the fix at the missing-world comparison.

**Expected:** The focused PostgreSQL tests fail before the fix for the returned missing trail and pass after it. A structurally valid but inconsistent World cache is read-only recovered from the recorded fact and repaired only by normal save. World equality includes the serializer's complete current payload shape.

### Task 3: Verify, review and publish the slice

**Files:** All implementation and planning paths in Tasks 1-2.

- [x] Run focused `WorldCacheRecovery` and adjacent cache recovery/replay coverage (18 passed). Run `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`; this slice adds no migration. The local database reports two already-pending migrations (`AddTravelDiaryProjectionWatermark`, `RetireUnusedSessionSchemaArtifacts`), which are unchanged by this slice.
- [ ] Review the complete branch against `develop`, event-sourcing doctrine, ADR-0028 and this plan; resolve all Critical and Important findings with witnessed RED/GREEN cycles. If fresh reviewer dispatch remains unavailable, disclose the author self-review fallback.
- [ ] Let the normal check-only pre-commit hook run the canonical staged-candidate gate for each implementation commit; do not rerun the full local gate immediately before or after a successful hooked commit. Hosted CI must pass on the exact PR head and on the develop merge commit.
- [ ] Push and open a PR to `develop`; verify hosted CI passes for the exact PR head before merge, merge as authorized by the goal, then verify the exact develop push gate passes.
- [ ] Record actual source/merge SHAs and CI evidence in the roadmap, fast-forward the main checkout to merged `develop`, then archive this worktree and delete its local/remote branch only after verifying merge containment and clean state.

**Expected:** The PR merges to `develop` at `0.1.0-dev.53` with exact-head and merge hosted gates green; row 07 points to the next JIT outcome.
