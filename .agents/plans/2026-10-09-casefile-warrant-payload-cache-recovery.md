# CaseFile Warrant Payload Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover current-version CaseFile caches when known or public warrant payloads or their order contradict event-established facts.

**Architecture:** Derive known and remaining public warrant sequences from the latest `CaseFileGenerated` snapshot and subsequent valid `InvestigationPerformed.WarrantId` facts. Compare IDs, order, and all scalar and nested terms against the current cache, then use the existing command/read-model full-replay fallback; preserve the already-covered clue comparisons and query no-writeback boundary.

**Tech Stack:** .NET 10, C#, xUnit, EF Core, PostgreSQL, Wild Bunch repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, cache-backed state and recovery; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07.

**Execution Strategy:** `executing-plans` — both warrant scenarios share one event-derived comparator, the same PostgreSQL fixture and replay path, so inline execution preserves context while one fresh whole-branch review supplies independent scrutiny.

## Global Constraints

- Immutable event history is authoritative; component rows are reconstructible caches.
- Invalid caches recover from ordered, upcasted history through existing load ports; unrecoverable history fails explicitly.
- Query recovery does not issue commands, write caches, emit events, or advance gameplay.
- Serialized player and journal projections expose only known warrants; unrevealed public warrant content remains private.
- No event/schema version, migration, API, gameplay, or durable architecture decision changes in this slice.
- Advance the sole authored version in `Directory.Build.props` from `0.1.0-dev.36` to `0.1.0-dev.37` for this PR; refresh `origin/develop` before publication and resolve any version race before opening the PR.
- Use this fresh worktree based on merged `develop`; publish one reviewed PR to `develop`, verify its hosted canonical gate and merge it as part of the approved epic.
- Retire the public-clue recovery plan only in this successor's first substantive implementation commit, after promoting its exact PR delivery evidence.

## Review Focus

- A same-ID known-warrant cache edit to the bounty terms, aliases/features, or summary must not change the warrant facts used by the command aggregate or player/journal read models.
- Reordering otherwise-valid public warrants must not change which event-established warrant the next wanted-poster action reveals.
- Reads must preserve damaged storage, event history, envelope versions and diary rows; a later legal write repairs the cache through the normal unit of work.
- Empty and already-revealed warrant collections are legitimate states; unrevealed public-warrant content must remain absent from player and journal projections.

---

### Task 1: Retire the public-clue plan and record its delivery

**Files:** Update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `docs/features.md` PLAT-001, and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; delete `.agents/plans/2026-10-09-casefile-public-clue-payload-recovery.md`; update `Directory.Build.props`.

**Consumes:** PR #221 merge `3a8abeb1119ea2681ec0969df6ed2aabf67ad8cc`, source `05a9b9929d90d6f28abe4ad450282588c1178a21`, source/merge/develop tree `cf7604404963decaf3e9d0222df72b16c90b2ae4`, hosted canonical gate run `37940125665`, and this committed successor plan.

**Produces:** The roadmap and evidence documents record PR #221's exact public-clue payload/order scope at `0.1.0-dev.36`, the plan is retired, the row 07 active-plan pointer names this warrant slice, and only `Directory.Build.props` advances to `.37`.

- [x] Record PR #221's merged source, merge commit, matching tree, hosted gate and bounded result in the roadmap, PLAT-001 and persistence follow-up; state that only public-clue payload/order consistency closed and warrant cache semantics remain open.
- [x] Remove the completed public-clue plan and stale links to it; preserve this plan and the live row 07 roadmap.
- [x] Change only `Directory.Build.props` to `0.1.0-dev.37`; do not author or commit generated web identity output or duplicate version fields.
- [x] Inspect the complete staged diff and `git diff --cached --check`, then commit the successor disposition as a substantive Task 1 commit; the check-only staged-candidate hook validates the exact candidate.

**Expected:** PR #221's delivery is durably recorded, its completed plan is retired only after that evidence is promoted, the roadmap points to the warrant recovery slice, and the sole authored version advances once.

### Task 2: Recover event-derived warrant payloads and order

**Files:** Rename `src/WildBunch.Persistence/GameSessions/CaseFileClueCacheRecovery.cs` to a name that accurately owns both clue and warrant evidence collections; update its callers in `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs` and `GameSessionReadStoreLoader.cs`; extend `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated implementation evidence and limits to `docs/features.md` PLAT-001 and the persistence follow-up.

**Consumes:** Task 1's PR #221 delivery disposition and version `.37`; `CaseFileGenerated.CaseFile.KnownWarrants` and `PublicWarrants`; later ordered `InvestigationPerformed.WarrantId` facts; the current clue collection comparison and established aggregate/player/journal replay fallbacks; existing PostgreSQL fixtures and storage-capture helper.

**Produces:** Command, player-read and journal-read loading detect a valid-ID known or public warrant whose complete payload or ordered position differs from event history and recover through the existing full-replay fallback. Query reads do not write storage; the next legal command save repairs the cache through the ordinary unit of work.

- [ ] Add a PostgreSQL regression with one public `GangMember` warrant revealed by a real wanted-poster action. Persist the event-backed known warrant, change its same-ID cached summary and nested terms while preserving its ID, then assert command/player/journal reads recover the exact generated warrant and do not write the component, component version, envelope positions, ordered events or diary rows; perform a legal food purchase, save normally, and prove a fresh load preserves the exact warrant without adding another investigation event. Run before production changes and confirm the test fails on the altered warrant facts.
- [ ] Add a PostgreSQL regression with two eligible public `GangMember` warrants. Reverse only cached `publicWarrants`; assert command and player read models recover the `CaseFileGenerated` order while both unrevealed warrant IDs/details stay absent from serialized player DTO and journal output; then read wanted posters with the fixed salt mode and assert boring mode surfaces the second recorded warrant for Dustvale's slot 0 and visit 1. Persist normally and prove a fresh load has the event-established known warrant and remaining pool. Run before production changes and confirm the test fails on the reversed sequence or wrong surfaced ID.
- [ ] Rename the helper to `CaseFileEvidenceCacheRecovery.MatchesEventEvidenceCollections`. Preserve the existing known/public clue behavior; derive initial warrant sequences with the same ID de-duplication and known-ID exclusion semantics as `CaseFile`, then apply only valid later `InvestigationPerformed.WarrantId` facts in event order, idempotently matching `CaseFile.RevealWarrantById`.
- [ ] Compare warrant sequences structurally by length/order and every `WarrantSnapshot` fact: ID, target name, summary, disposition, bounty, aliases, features, issuing source, target kind, gang affiliations, gang-pressure target and source kind. Do not rely on record equality for nested list members.
- [ ] Keep the established command and player/journal loader mismatch branches and invalid-history behavior intact; do not add query writeback or widen event compatibility.
- [ ] Add direct healthy exact-order and legitimate empty/already-revealed warrant-pool cases; preserve the existing clue healthy/empty coverage and hidden-content assertions.
- [ ] Run the focused PostgreSQL tests with `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_CaseFileCacheSameIdAlteredKnownWarrantPayloadRecoversFromEventsWithoutWritingBack|FullyQualifiedName~ReadModel_CaseFileCacheReorderedPublicWarrantsRecoversFromEvents|FullyQualifiedName~CaseFileEvidenceCacheMatchesExactPayloadAndOrderFromHistory"`; weaken the comparator to skip warrant collection comparison and confirm both warrant regressions fail for their intended mismatch, then restore it and confirm all focused cases pass.
- [ ] Append dated implementation evidence and precise limits to PLAT-001 and the persistence follow-up. State that this closes warrant payload/order consistency only; warrant circulation by town, other CaseFile state contradictions, malformed event compatibility and general event-history compatibility remain open. Do not change ADR-0028, event/schema versions, migrations or gameplay rules.
- [ ] Inspect the full staged diff and commit as `fix: recover altered warrant cache facts`; let the check-only staged-candidate hook validate the exact candidate and do not rerun its full gate manually.

**Expected:** Same-ID warrant payload edits and public-warrant reordering cannot change event-established casefile facts or wanted-poster surfacing; existing clue recovery remains intact; legitimate empty pools remain valid; unrevealed public warrant details remain private; reads preserve storage; legal actions repair through the ordinary unit of work; focused tests and the staged-candidate canonical gate pass.
