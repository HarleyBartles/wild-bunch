# Malformed Diary Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Recover a current-version diary cache whose metadata looks complete but whose JSON cannot materialize, deriving the full diary from authoritative events without writing during reads and repairing through the next ordinary legal save.

**Architecture:** `PersistedPayloadLoader.LoadDiaryDays` validates diary stream watermark, row count, schema version and contiguous sequence before trusting rows. Rows that pass metadata are deserialized directly; malformed nested snapshot data can currently escape as a raw exception. Convert diary cache decode/materialization failures to `InvalidComponentCacheShapeException`, then rebuild all diary days through `TravelDiaryDayProjector` when that exception occurs. Do not catch failures in authoritative event decoding or projection. Read loaders remain read-only; a normal command load/action/save repairs the projection.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; row 07 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-04 and PS-06 in `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and the persistence test follow-up.

**Execution Strategy:** Use `executing-plans` inline and sequentially under the active roadmap goal. The PostgreSQL behavior proof, decoder boundary and loader fallback share one persistence invariant and one fixture; there are no independent implementation tracks to parallelize. Inline execution preserves the failure-test and recovery reasoning across these tightly coupled changes with less context handoff than subagent-driven development. Develop the PostgreSQL behavior proof first, demonstrate that it fails against the current raw deserialization path, then implement the narrow recovery boundary.

## Global Constraints

- Start from `develop` merge `6a2fe75b8372b33a166381b68a4c77706b47a6b6` in this fresh linked worktree; target `develop`.
- Commit this JIT plan before implementation. Record PR #236 source `7fbb95cff65dcb8ff504a49b4379b316364b4523`, merge `6a2fe75b8372b33a166381b68a4c77706b47a6b6`, exact-head gate `38014331082`, develop push gate `38014636468`, and delivered `0.1.0-dev.51`. Retire the `.51` plan, point row 07 to this plan, and advance `Directory.Build.props` to `0.1.0-dev.52` in that plan commit.
- Preserve the event stream and projection contracts. Diary is derived from ordered authoritative events; the current cache is trusted only when metadata and materialized shape are valid.
- Rebuild all diary days on an invalid cached row. Do not return a partial mixture of stored and projected days.
- Query recovery must not write rows, envelope watermarks or events. A legal ordinary command save must write the complete repaired diary cache.
- Do not swallow missing, malformed, future or otherwise unrecoverable authoritative event history. Catch only the invalid diary-cache shape signal at the cache boundary.
- Limit scope to structurally unmaterializable current-version diary payloads. This does not detect semantically incorrect JSON that materializes to a valid domain value. Do not add a schema migration, new event, event upcaster, ADR, feature promise or semantic cache audit.

## Review Focus

- A cache row with matching stream watermark, expected count, current schema and contiguous sequence must still be rejected if its nested payload cannot become a `TravelDiaryDayState`.
- A malformed cache must recover from exactly the event projector and leave all persisted rows, watermarks and events unchanged on player and journal reads.
- A subsequent legal command save must repair the full ordered diary rows, while corrupt authoritative event history remains a surfaced failure.
- Avoid turning this into a broad exception-catching or semantic cache-validation framework.

---

### Task 1: Record PR #236 and commit the `.52` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-explicit-dev-override-snapshots.md`; create this plan.

- [ ] Confirm PR #236 and both hosted gates against the exact source and merge SHAs above.
- [ ] Record `.51` delivery and the selected malformed-diary follow-up in row 07 and this roadmap's dated delivery history; keep older dated decisions intact.
- [ ] Retire the completed `.51` plan and advance the version to `0.1.0-dev.52`.
- [ ] Commit the planning handoff before editing source or tests.

**Expected:** The roadmap points to this committed plan, records verified `.51` delivery, and the plan contains a bounded implementation and proof contract.

### Task 2: Prove malformed diary cache recovery and ordinary repair

**Files:** `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Travel.cs`; `src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs`; `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`.

- [ ] Extend the real PostgreSQL diary recovery scenario to persist a three-day diary, replace the middle row JSON with `{"entries":null}` while preserving row metadata and stream watermark/count, then read through player and journal repositories.
- [ ] Assert both reads return all event-projected days and leave every diary row, envelope watermark, and stored event unchanged.
- [ ] Load the same session through the command repository, perform the existing legal next-day action and save, then prove a fresh load observes complete, contiguous, repaired rows matching the event projector.
- [ ] Include a negative proving malformed authoritative event history still fails rather than being interpreted as a cache-recovery condition.
- [ ] Confirm the new behavior test fails on the current raw deserialization implementation before adding the recovery boundary.
- [ ] Make `DeserializeTravelDiaryDay` wrap the same targeted JSON/materialization failures already treated as invalid cache shape by neighboring snapshot decoders. In `LoadDiaryDays`, catch only that typed cache exception and rebuild all days; let event/projector failures escape.
- [ ] Update the dated PS-04/PS-06 finding disposition and test follow-up with the exact structural limit: this validates materializability, not semantic correctness of values that successfully materialize.

**Expected:** Focused PostgreSQL tests prove read recovery, no writeback, later ordinary-save repair and fail-closed authoritative-history behavior. No event, schema or migration changes.

### Task 3: Verify, review and publish the slice

**Files:** All implementation and planning paths in Tasks 1-2.

- [ ] Run the focused PostgreSQL diary recovery tests and adjacent projection-version coverage; ensure the test can fail for the malformed payload path it protects.
- [ ] Start/ensure the repository PostgreSQL cluster and run the canonical `py -3 tools/run.py ci --check` fail-fast and cheapest-first. Run applicable migration inventory checks and confirm no migration was introduced.
- [ ] Review the complete branch against `develop`, correct findings, rerun affected checks and commit through the normal check-only hook.
- [ ] Push and open a PR to `develop`; verify hosted CI passes for the exact PR head before merge, merge as authorized by the goal, then verify the exact develop push gate passes.
- [ ] Record actual source/merge SHAs and CI evidence in the roadmap. Fast-forward the main checkout to the merged `develop`, then archive this worktree and delete its local/remote branch only after verifying merge containment and clean state.

**Expected:** PR #237 is merged to `develop` at `0.1.0-dev.52` with exact-head and merge hosted gates green; row 07 points to the next JIT outcome.
