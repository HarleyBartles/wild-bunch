# CaseFile Known-Clue Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover a syntactically valid but incomplete `caseFile.knownClues` cache from the authoritative event history so a player's established knowledge is never silently lost on reload.

**Architecture:** Derive the expected known-clue IDs from `CaseFileGenerated`'s initial known clues and subsequent `InvestigationPerformed` events that refer to clues in its recorded public pool. Compare that set with the loaded current cache; a contradiction takes the existing single full-replay path. Keep the aggregate command and player/journal read routes aligned, preserve query no-writeback, and let a later legal save repair the cache.

**Tech Stack:** C#/.NET 10, EF Core, PostgreSQL, xUnit integration tests, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially [cache-backed state and recovery](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery); row 07 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [PLAT-001](../../docs/features.md#plat-001-event-history-saved-state-and-recovery).

**Execution Strategy:** `executing-plans` inline. The event-derived consistency check, PostgreSQL cache damage, command/read recovery, and later legal-save repair form one persistence invariant and share the production event fixtures.

## Global Constraints

- Preserve immutable event authority, strict CQRS, one coherent replay per invalid-cache load, query no-writeback, and cache repair only through a later legal write.
- Compare only player-known clue identity; do not expose public pool contents or hidden case truth through player or journal projections.
- The exact expected known-clue ID set is the `CaseFileGenerated.CaseFile.KnownClues` IDs plus `InvestigationPerformed.ClueId` values present in that generated case's `PublicClues`. If the stream has no `CaseFileGenerated`, leave this check inapplicable rather than inventing an expected set.
- Limit this slice to the valid-but-incomplete or contradictory `knownClues` field in the current `caseFile` cache. Do not broaden into warrants, suspect state, every CaseFile invariant, malformed event facts, migrations, upcasters, or unrelated component recovery.
- Read paths must leave the malformed cache, component version, envelope positions, events, and diary projection untouched; a later legal command save may repair it through the ordinary unit of work.
- Do not change ADR-0028 or event/schema versions; this implements its existing event-authority and rebuildable-cache decision.
- Advance only `Directory.Build.props` from `0.1.0-dev.33` to `0.1.0-dev.34`; do not author or commit generated web identity output.
- Use the fresh canonical branch `codex/stable-0-1-casefile-known-clues` from merged `develop` PR #218. Deliver one PR to `develop`, advance the version once for that merged PR, preserve this plan through its completing PR, and retire the completed public-clue-pool plan in the first substantive commit.
- Before publication, refresh `develop` and reconcile the version against the latest merged development number; if another PR merged first, use the unique next number and rerun affected proof and review.

## Review Focus

- Removing a clue from the cache's valid `knownClues` array while its reveal event remains must previously lose the player's knowledge, and now must recover the exact event-established clue on command and read paths.
- An empty known-clue cache must remain valid when the event history establishes no known clues; do not turn empty state into an unconditional replay trigger.
- Prove read recovery leaves persisted state unchanged, a later legal save repairs the cache, and replay does not reveal unlearned clues or create new events.
- The event-derived set must account for initial known clues as well as later revealed clues, while preserving supported histories without a `CaseFileGenerated` fact.

---

### Task 1: Retire the completed public-clue plan and prepare this successor

**Files:** Retire `.agents/plans/2026-10-09-public-clue-pool-cache-recovery.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `docs/features.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, and `Directory.Build.props`; retain this plan.

**Consumes:** PR #218 merged to `develop` as `7f96730790bc680b95fcae4bad8e58c174251740` from reviewed source `a0a2e9a86d15e6d418a6f8e007976c9872fa9b54`; hosted canonical gate run `37925476572` passed on that exact source; independent whole-branch review found no actionable findings; current version `0.1.0-dev.33`.

**Produces:** Row 07 records the verified public-clue-pool recovery at `.33` and identifies the selected known-clue consistency gap; PLAT-001 and the persistence follow-up retain PR #218's bounded evidence and limits; the completed public-clue plan and its stale links are retired; the authored development version is `.34`.

**Steps:**

1. Run `git status --short --branch`, confirm this branch is a clean worktree from `develop`, and verify `Directory.Build.props` still declares `0.1.0-dev.33`.
2. Read the predecessor plan beside PR #218's merged source, feature evidence, and current roadmap. Classify its full scope: absent/null current-cache `publicClues`, null authoritative `CaseFileGenerated.PublicClues`, command/read recovery, no-writeback, legal-save repair, and hidden-clue projection safety are all delivered; future CaseFile contradictions remain live in the roadmap.
3. Update row 07 with PR #218's source, merge, tree, hosted gate, review, and `.33` evidence. Remove links to the retired plan and its stale future-tense public-clue gap. Identify valid-but-incomplete event-established `knownClues` as this successor's bounded current slice, with other CaseFile contradictions still open.
4. Update PLAT-001 and the persistence test follow-up to include PR #218's evidence, state its exact limits, and route the remaining known-clue consistency gap to this plan/row 07 without claiming broader CaseFile consistency.
5. Remove the predecessor plan only after the complete-scope classification and its durable knowledge are represented in current owners. Do not move it to a tracked archive.
6. Change only `Directory.Build.props` to `0.1.0-dev.34`; inspect the staged diff for generated or duplicated version fields.
7. Run `py -3 tools/run.py ci --check` on the staged candidate, inspect `git diff --cached --check` and the complete staged diff, then commit as `docs: prepare known clue cache recovery slice`.

**Expected:** Planning history shows PR #218 as delivered and the known-clue discrepancy as active; no stale plan link remains; only the canonical authored version advances to `.34`; the staged-candidate gate passes.

### Task 2: Recover event-established known clues when the valid cache is incomplete

**Files:** Add `.cs` under `src/WildBunch.Persistence/GameSessions/` for the narrow event/cache consistency check; update `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` and `GameSessionReadStoreLoader.cs`; extend `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated implementation evidence to PLAT-001 and the persistence test follow-up.

**Consumes:** Task 1's committed planning disposition and version `.34`; production event types `CaseFileGenerated` and `InvestigationPerformed`; existing full-replay behavior and PostgreSQL fixture patterns in `EfGameSessionRepositoryTests`.

**Produces:** A real PostgreSQL regression test fails before the change because a clue in `CaseFile.KnownClues` disappears from the cache and player/journal reads; command and read loaders detect the exact event-derived ID mismatch and recover once through full replay; read storage remains unchanged; a later legal write repairs the cache without inventing an event.

**Steps:**

1. Add a focused PostgreSQL test that starts a real session, legally reveals a public clue, saves its `InvestigationPerformed`, then removes that exact clue from `caseFile.knownClues` while preserving the other cache fields. Assert the command reload and player/journal reads each contain the exact event-established clue; run the test to witness the intended RED because the pre-fix cache path omits it.
2. Add an internal cache-consistency helper that derives expected IDs from the generated CaseFile's initial known clues and the valid public clue IDs referenced by later `InvestigationPerformed` events. Compare with the loaded cache's known-clue IDs; absent `CaseFileGenerated` leaves the check inapplicable. Keep the check read-only and do not reconstruct random state or use hidden truth.
3. Integrate the mismatch with the command loader's existing full-replay fallback and the read loader's existing event-derived read-state fallback. Preserve ordinary cache deserialization, empty-known-clue validity, and the current healthy-cache path.
4. Extend the PostgreSQL test to prove recovered command, player, and journal state contains the exact clue and that query reads preserve cache JSON/version, snapshot and stream positions, event rows, and diary projection metadata.
5. Save a later legal command through the existing unit of work, fresh-load the session, and prove the known clue is retained with no extra clue-reveal event. Add a negative case where history establishes no known clues and the explicit empty cache remains on the cache path; use observable state or a focused load seam that fails if full replay is forced.
6. Run the focused PostgreSQL selection. Temporarily bypass the event/cache mismatch guard and rerun the positive regression to prove it fails for the missing clue; restore the guard and verify the test passes. Ensure the negative case fails if empty state is treated as corruption.
7. Append dated evidence and limits to PLAT-001 and the persistence test follow-up. State that this closes only event-derived `knownClues` completeness/equality, not all CaseFile semantics or event compatibility. Keep ADR-0028 unchanged because its existing rule is implemented.
8. Run `py -3 tools/run.py ci --check` on the staged candidate, inspect the complete diff, and commit as `fix: recover missing known clues from event history`.

**Expected:** A cache with an empty or incomplete known-clue list cannot hide a committed reveal; a truly empty history remains a valid empty cache; the read recovery writes nothing; a normal subsequent save repairs the cache; all focused and canonical checks pass.
