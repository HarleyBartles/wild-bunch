# CaseFile Generated Facts Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover a current CaseFile cache when immutable facts from `CaseFileGenerated` differ from its event-established generated facts.

**Architecture:** Add a focused Persistence consistency helper for generation-time CaseFile facts and call it beside the existing clue/warrant evidence check in command and read-model loading. A mismatch takes the existing full event-replay fallback; queries preserve stored cache/history, and the next ordinary command save repairs the cache.

**Tech Stack:** .NET 10, C#, xUnit, EF Core, PostgreSQL, Wild Bunch repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07.

**Execution Strategy:** `executing-plans` — the documentation successor disposition, the single immutable-fact comparison, and its command/read PostgreSQL proof are sequentially coupled through one recovery boundary; inline TDD keeps the event, cache, and repair assertions in one context while a fresh whole-branch review evaluates the resulting slice.

## Global Constraints

- Immutable event history is authoritative; current component rows are reconstructible caches.
- Invalid derived state uses the existing ordered, upcasted full-replay fallback; invalid authoritative history fails explicitly.
- Query recovery does not write storage, issue commands, emit game events, or advance gameplay.
- Hidden culprit truth remains absent from player and journal projections.
- No event/schema version, migration, API, gameplay rule, durable architecture decision, or historical compatibility boundary changes in this slice.
- Advance the sole authored version in `Directory.Build.props` from `0.1.0-dev.37` to `0.1.0-dev.38` for this PR; refresh `origin/develop` before publication and resolve any version race before opening the PR.
- Use this fresh worktree based on merged `develop`; publish a reviewed PR to `develop`, verify its hosted canonical gate on the exact head, and merge it as part of the authorized epic.
- Retire the completed warrant-recovery plan only in this successor's first substantive commit, after recording its exact PR delivery evidence in current owners.

## Review Focus

- A valid same-ID cache change to the culprit, an ordered suspect or its profile/traits, opening lead, release threshold, or turf must not change facts established by `CaseFileGenerated`; each altered fact has an independent PostgreSQL regression row.
- The comparator must not mistake post-generation state for immutable genesis: discovered suspects, clue/warrant knowledge, release progress, accusation, confrontation records, and sheriff settlements remain governed by their existing event-specific behavior.
- A mismatch must recover through both command and player/journal loaders without query writeback, and a later legal save must repair the component without adding an unrelated event.

---

### Task 1: Retire warrant recovery and record PR #222

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `docs/features.md` PLAT-001, and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; delete `.agents/plans/2026-10-09-casefile-warrant-payload-cache-recovery.md`; update `Directory.Build.props`.

**Consumes:** PR #222 merged to `develop` as `5438c00c176fe33ac925d97d53eaf5df2a6f2e6c` from reviewed source `f05eb17e2ca537c436ab18630f2b65fe7ce87a01`; source, merge, and develop trees `f8b095a2a67fb2b4a752ab57ce2938dde6d37926`; hosted canonical gate run `37946055635` passed on the exact source; independent whole-branch review approved with no actionable findings; development identity `0.1.0-dev.37`.

**Produces:** Current owners describe the exact warrant payload/order recovery and its limits, the completed warrant plan is retired, the row 07 active-plan pointer names this plan, and only `Directory.Build.props` advances to `.38`.

- [x] Record PR #222's source, merge, matching tree, exact-head hosted gate, review outcome, bounded behavior and `.37` identity in the roadmap, PLAT-001, and persistence follow-up.
- [x] Classify the entire warrant plan against its completed Task 1 and Task 2, committed code/tests, focused PostgreSQL proof, mutation proof, review, and hosted gate; preserve the warranted payload/order scope and open town-circulation/other CaseFile/history gaps in current evidence owners, then remove the completed plan and stale links.
- [x] Update the row 07 active-plan pointer and target version to this generated-facts plan and `.38`; preserve row 07 as executing because additional persistence work remains.
- [x] Change only `Directory.Build.props` to `0.1.0-dev.38`; do not commit generated web identity output or duplicate version fields.
- [x] Inspect the complete staged successor disposition and `git diff --cached --check`, then commit it as `docs: retire warrant cache plan and record delivery`; let the normal check-only hook validate the staged candidate.

**Expected:** PR #222's complete, narrowly bounded delivery is durably recorded, its completed plan is retired only after promotion, row 07 points at this plan, and the sole authored version advances once.

### Task 2: Recover immutable generated CaseFile facts

**Files:** Create `src/WildBunch.Persistence/GameSessions/CaseFileGenerationCacheRecovery.cs`; update `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` and `GameSessionReadStoreLoader.cs`; extend `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated implementation evidence and limits to `docs/features.md` PLAT-001 and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Consumes:** Task 1's PR #222 disposition and `.38` identity; the latest applicable ordered `CaseFileGenerated.CaseFile` snapshot; the existing command/read full-replay fallback, required-cache shape handling, clue/warrant consistency helper, PostgreSQL fixture, and `CaptureCaseFileCacheRecoveryStateAsync` evidence helper.

**Produces:** Command, player-read, and journal-read loading reject a current CaseFile cache whose valid generation-time facts differ from the latest applicable `CaseFileGenerated` fact, replay supported history through the existing path, serve recovered facts without query writeback, and repair the component through a later legal command save.

- [ ] Add a PostgreSQL `[Theory]` whose deterministic two-suspect `GameSession` fixture has distinct culprit and suspect IDs, ordered aliases/identity facts/traits, a nondefault opening lead, a nondefault release threshold, and at least one turf assignment; each theory row mutates exactly one valid cached fact while preserving IDs and a deserializable component.
- [ ] Cover independent cache mutations for `trueCulpritId`, suspect ordering, a same-ID suspect profile or trait payload, opening-lead description, release threshold, and turf assignment data. Assert the original `CaseFileGenerated` event facts through command, player-read, and journal-read results; each row must fail before production changes because that fact remains altered or the load fails instead of replaying.
- [ ] Capture component JSON/version, envelope snapshot/stream positions, ordered event rows and diary rows before recovery reads; assert every value is unchanged after all reads. Keep player/journal output free of the hidden culprit ID and other unrevealed truth.
- [ ] Implement `CaseFileGenerationCacheRecovery` in Persistence as a pure comparison against the applicable generation snapshot. Compare culprit ID; suspect count/order and each suspect's ID, name, profile aliases and identity facts, trait tags and status; opening-lead description; release threshold; and ordered turf assignments. Compare nested values structurally, not by record equality over list references.
- [ ] Keep fields that change after generation outside this helper: accusation, discovered-suspect IDs, release progress, known/public clues and warrants, confrontation state, and sheriff settlements. Preserve the existing clue/warrant comparator and do not infer new events or expand event-version compatibility.
- [ ] Invoke the new comparison in both existing command and read-model mismatch guards; a mismatch must use their established full-replay fallback, while a genuine absence of applicable generation history retains the existing behavior.
- [ ] After recovery, perform one legal food purchase through the loaded aggregate, save through the ordinary unit of work, and fresh-load the session; prove the CaseFile cache now matches event-established facts and the save added only its normal purchase event.
- [ ] Run the focused PostgreSQL selection with `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_CaseFileCacheSameIdAlteredGeneratedFactsRecoversFromEventsWithoutWritingBack"`; verify every independent mutation row fails for its intended fact before implementation, then passes after the comparator is wired into both loaders. Run the existing healthy CaseFile evidence comparator test to ensure the current clue/warrant classification is preserved.
- [ ] Append dated feature-matrix and persistence-follow-up evidence limited to these immutable generated facts, command/query recovery, no-writeback, legal-save repair, and hidden-truth safety; state explicitly that mutable CaseFile facts and broader event compatibility remain open. Leave ADR-0028 and migration history unchanged.
- [ ] Inspect and commit the complete implementation slice as `fix: recover altered generated case facts`; let the normal check-only staged-candidate hook run the canonical fail-fast gate, and do not manually rerun the full gate after a successful hooked commit.

**Expected:** Each valid immutable generated-fact cache alteration is detected independently; command and player-facing reads recover from event history without writes or hidden-truth leakage; the existing clue/warrant recovery remains intact; a later legal save repairs the component; focused PostgreSQL tests and the staged-candidate canonical gate pass.
