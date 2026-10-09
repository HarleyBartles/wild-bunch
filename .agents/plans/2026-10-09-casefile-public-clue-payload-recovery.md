# CaseFile Public-Clue Payload Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover a current-version CaseFile cache when its public-clue payload or order contradicts the event-established pool, while preserving replay, query, and persistence boundaries.

**Architecture:** `CaseFileGenerated.CaseFile.PublicClues` records the ordered initial public pool; each later valid `InvestigationPerformed.ClueId` removes that clue from the pool as `GameSession.Apply` does. The existing persistence recovery check will be renamed to reflect both known and public clue collections, compare both full ordered payloads against event-derived sequences, and reuse the current full-replay fallback. Reads remain no-writeback; ordinary later saves may converge the cache.

**Tech Stack:** .NET 10, C#, xUnit, EF Core, PostgreSQL, Wild Bunch repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, cache-backed state and recovery.

**Execution Strategy:** `executing-plans` — the two behavioral cases and one recovery comparator share one production seam and PostgreSQL fixture; inline execution preserves event/cache context, while a fresh whole-branch review supplies independent scrutiny.

## Global Constraints

- Immutable event history is authoritative; component rows are reconstructible caches.
- Invalid caches recover from ordered, upcasted history through existing load ports; unrecoverable history fails explicitly.
- Query recovery does not issue commands, write caches, emit events, or advance gameplay.
- Player and journal projections do not expose hidden truth or unrevealed clue IDs/descriptions.
- No event/schema version, migration, API, gameplay, or durable architecture decision changes in this slice.
- Advance the sole authored version in `Directory.Build.props` from `0.1.0-dev.35` to `0.1.0-dev.36` for this PR.
- Use the existing fresh worktree `Z:\_agent-worktrees\wild-bunch\codex\stable-0-1-casefile-next-cache`, base it on merged `develop`, and target one PR to `develop`.

## Review Focus

- Same-ID public-clue description or structured-anchor corruption must trigger recovery before a later investigation can report altered facts; prove it with a persisted PostgreSQL test and the next legal investigation event.
- Reordered otherwise-valid public clues must recover their event-established sequence and preserve deterministic surfacing behavior.
- Reads preserve the damaged cache and event/snapshot/diary positions; unrevealed public clues remain absent from player and journal output.
- An empty public pool after all recorded clues are revealed is valid and must not trigger recovery.

---

### Task 1: Retire the known-clue payload plan and record the successor slice

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `docs/features.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, and `Directory.Build.props`; delete `.agents/plans/2026-10-09-casefile-known-clue-payload-recovery.md`; retain this plan.

**Consumes:** PR #220 merged to `develop` at `6e12df5e6f12ce4e3e3a20c0de6275a2a4ed882b` from source `464466f7bdb4c761daa82614655657414609acf5`; source, merge, and develop trees match; hosted canonical gate run `37934167559` passed and independent whole-branch review found no actionable findings; the merged version is `0.1.0-dev.35`.

**Produces:** Row 07 and PLAT-001 record PR #220's exact known-clue payload/order outcome and its limits; this plan becomes the row 07 active plan; the persistence follow-up records PR #220 evidence; the completed plan and stale links are retired; `Directory.Build.props` declares `0.1.0-dev.36`.

- [x] Confirm the clean branch is based on current `origin/develop` PR #220, the source/merge trees match, the hosted canonical gate and review evidence remain available, and `Directory.Build.props` declares `0.1.0-dev.35`.
- [x] Read the complete predecessor plan and compare every criterion with PR #220's merged implementation, tests, hosted gate, and review. Classify exact known-clue payload/order recovery, healthy/empty state, no-writeback, legal-save repair, and unrevealed clue privacy as delivered; retain public-clue payload/order mismatch as the successor scope.
- [x] Update roadmap row 07 with PR #220 merge/source/tree/gate/review/version evidence and link this plan as the active plan; state this slice owns public-clue payload/order consistency while other CaseFile and event-compatibility gaps remain open.
- [x] Update PLAT-001 and the persistence test follow-up with PR #220 evidence and the bounded public-clue successor gap. State ADR-0028 remains unchanged because both slices preserve its existing event-authority and rebuildable-cache decision.
- [x] Remove the completed predecessor plan and its stale links after promoting its durable evidence to the roadmap, feature matrix, and follow-up; do not create a tracked archive.
- [x] Change only `Directory.Build.props` to `0.1.0-dev.36`; do not author or commit generated web identity output or duplicate application version fields.
- [x] Inspect the complete staged diff and `git diff --cached --check`, then commit as `docs: prepare public clue cache recovery`; use the check-only staged-candidate hook and do not rerun its full gate manually.

**Expected:** PR #220 is recorded as delivered at `.35`; this plan exclusively owns exact public-clue pool payload/order recovery; its predecessor and stale links are retired; only the authored version advances.

### Task 2: Recover public-clue pool payload and order from events

**Files:** Rename `src/WildBunch.Persistence/GameSessions/CaseFileKnownClueCacheRecovery.cs` to `src/WildBunch.Persistence/GameSessions/CaseFileClueCacheRecovery.cs`; update its loader callers and direct test references; extend `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated evidence and limits to `docs/features.md` PLAT-001 and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Consumes:** Task 1's committed successor disposition and version `.36`; the latest `CaseFileGenerated.CaseFile.PublicClues` snapshot; subsequent ordered `InvestigationPerformed.ClueId` facts; the existing aggregate/player/journal full-replay mismatch fallbacks and PostgreSQL fixtures.

**Produces:** Command, player-read, and journal-read loading detect either a known or public clue whose ID and membership remain valid while its payload or ordered position differs from event history. Healthy and legitimately empty collections remain on the cache path. Recovery itself does not write storage; the next legal gossip action repairs the cache through its ordinary save and adds exactly that action's `InvestigationPerformed` fact.

- [x] Add a PostgreSQL regression with one eligible `LocalGossip` clue. Persist the game, change only that public clue's description and one nested subject anchor in the current CaseFile JSON while retaining its ID, then assert command and player read models recover the recorded pool while the journal/player DTOs still omit the unrevealed clue ID and description; prove reads leave the damaged component, component version, envelope positions, ordered events, and diary rows unchanged; gather gossip, assert the emitted event and message use the event-recorded clue facts, save normally, and verify a fresh load contains the revealed clue with no extra investigation event. Run this test before production changes and confirm it fails on the altered public clue facts.
- [x] Add a PostgreSQL regression with two public clues in the same deterministic surfacing category. Reverse only the cached `publicClues` order; assert command and player read models restore the exact `CaseFileGenerated` order while the journal projection exposes neither clue; then invoke the next legal gossip action and assert it surfaces the second recorded clue, which boring mode selects for Dustvale's slot 0 and visit 1. Run before production changes and confirm it fails on the reversed sequence or wrong surfaced ID.
- [x] Rename the helper to `CaseFileClueCacheRecovery.MatchesEventClueCollections` and preserve the current event-derived known-clue comparison unchanged. Derive the expected remaining public-clue sequence from the latest generated public pool, filtering clues already known at generation and removing each later valid clue reveal by ID in event order, matching `CaseFile.RevealClue` semantics. Compare sequence length, order, and all existing `ClueSnapshot` scalar and nested-anchor facts; do not compare snapshots by record equality where array members are reference-compared.
- [x] Keep the existing command repository and player/journal loader recovery branches intact; use their established single full-replay fallback on mismatch. Do not add query writeback or alter invalid-authoritative-history handling.
- [x] Add a healthy exact public-pool payload/order case and an empty-pool-after-recorded-reveals case so these legitimate states do not trigger recovery; preserve existing known-clue checks and unrevealed-clue privacy assertions.
- [x] Run the focused cases with `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_CaseFileCacheSameIdAlteredPublicCluePayloadRecoversFromEventsWithoutWritingBack|FullyQualifiedName~ReadModel_CaseFileCacheReorderedPublicCluesRecoversFromEvents|FullyQualifiedName~CaseFileClueCacheMatchesExactPayloadAndOrderFromHistory"`; weaken the comparator to skip public pool comparison and confirm both regressions fail for their intended mismatch, then restore it and confirm all focused cases pass.
- [x] Append dated implementation evidence and exact limits to PLAT-001 and the persistence follow-up. State that this closes exact payload/order consistency for the public-clue pool only; warrant cache semantics, other CaseFile state contradictions, malformed event compatibility, and general event-history compatibility remain open. Do not change ADR-0028, event/schema versions, migrations, or gameplay rules.
- [x] Inspect the full staged diff and commit as `fix: recover altered public clue cache facts`; let the check-only staged-candidate hook validate the exact candidate and do not rerun its full gate manually.

**Expected:** Same-ID public clue edits and reordering cannot alter event-established public clue state or future clue surfacing; existing known-clue recovery remains intact; healthy ordered and legitimately empty collections remain valid; unrevealed clue content remains private; reads preserve storage; the next legal gossip action repairs through the ordinary unit of work and adds exactly one investigation fact; focused tests and the staged-candidate canonical gate pass.
