# CaseFile Known-Clue Payload Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover a current `caseFile.knownClues` cache when a clue keeps its event-established ID but its payload or order contradicts immutable history.

**Architecture:** Derive the ordered known-clue sequence from the latest `CaseFileGenerated` snapshot and later `InvestigationPerformed` events that reference clues in that snapshot's public pool. Compare complete clue facts, including nested anchors, with the current cache; any mismatch uses the existing command and read full-replay paths. Read recovery remains no-writeback, and a later legal save repairs the cache through the ordinary unit of work.

**Tech Stack:** C#/.NET 10, EF Core, PostgreSQL, xUnit integration tests, repository command bus.

**Implementation Guidance:** Follow the repository [planning runbook](../runbooks/planning.md), [backend architecture doctrine](../doctrine/architecture-guardrails.md), [event-sourcing integrity doctrine](../doctrine/event-sourcing-integrity.md), [testing playbook](../playbooks/testing.md), and [decision-record playbook](../playbooks/decision-records.md). Use `py -3 tools/run.py dotnet-test --check --verbose -- --filter <filter>` for the focused PostgreSQL behavior selection; the staged-candidate commit hook runs the full check-only canonical gate.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially [cache-backed state and recovery](../specs/2026-10-07-stable-0.1.0-baseline.md#cache-backed-state-and-recovery); row 07 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [PLAT-001](../../docs/features.md#plat-001-event-history-saved-state-and-recovery).

**Execution Strategy:** `executing-plans` inline. Event-derived clue order and payload, PostgreSQL corruption, command/read replay, no-writeback, and legal-save repair are one persistence invariant using the same production histories.

## Global Constraints

- Preserve immutable event authority, strict CQRS, one coherent replay per invalid-cache load, query no-writeback, and cache repair only through a later legal write.
- The initial ordered known clues come from the latest `CaseFileGenerated.CaseFile.Clues`; later known clues are appended in event order from `InvestigationPerformed.ClueId` values that identify clues in that generated snapshot's `PublicClues`.
- Compare the complete ordered player-known clue payload, including ID, kind, description, linked suspect IDs, target/source kinds, source, context, and every structured anchor field. Compare array contents structurally; record equality on array-backed snapshots is not sufficient.
- Mirror domain replay deduplication by adding a revealed clue only once. A history without `CaseFileGenerated`, or with an absent authoritative clue collection, leaves this consistency check inapplicable; do not invent facts or alter malformed-event handling.
- Limit this slice to same-ID payload and order contradictions in current `caseFile.knownClues`. Do not broaden into public-clue cache validation, other CaseFile state, warrants, general event compatibility, schema or migration changes.
- Read paths must leave the malformed cache, component version, envelope positions, events, and diary projection untouched; a later legal command save may repair it through the ordinary unit of work.
- Player and journal projections must continue to exclude unrevealed public clues and hidden case truth.
- Do not change ADR-0028 or event/schema versions; this implements its existing event-authority and rebuildable-cache decision.
- Advance only `Directory.Build.props` from `0.1.0-dev.34` to `0.1.0-dev.35`; do not author or commit generated web identity output.
- Use the fresh canonical branch `codex/stable-0-1-casefile-known-clue-payload` from merged `develop` PR #219. Deliver one PR to `develop`, preserve this plan through its completing PR, and retire the completed known-clue ID-set plan in the first substantive commit.
- Before publication, refresh `develop` and reconcile the version against the latest merged development number; if another PR merged first, use the unique next number and rerun affected proof and review.

## Review Focus

- A clue whose ID and list membership remain intact but whose description or structured anchors have changed must recover the exact event-recorded payload on command, player, and journal paths.
- Reordering otherwise intact known clues must not change the event-established casebook order.
- Initial known clues and later revealed clues must remain ordered and deduplicated exactly as domain replay applies them; unrevealed public clues must remain absent.
- Cache recovery reads must not write storage, and a later legal save must repair the same clue payload without creating an investigation event.

---

### Task 1: Retire the known-clue ID-set plan and prepare this successor

**Files:** Retire `.agents/plans/2026-10-09-casefile-known-clue-cache-recovery.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `docs/features.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, and `Directory.Build.props`; retain this plan.

**Consumes:** PR #219 merged to `develop` at `e1a73159bab8a1a9c132ab9e455e98444389bc7d` from reviewed source `b6d583d27da57be387ad767871e1ef33ff96a4bb`, matching tree `06600ad2f4b77ca26395415a2bda524d21dc780e`; hosted canonical gate run `37930007381` passed on that exact source at `0.1.0-dev.34`; independent whole-branch review found no actionable findings.

**Produces:** Row 07 and PLAT-001 record PR #219's bounded known-clue ID-set recovery and select same-ID payload/order mismatch as this slice; the persistence test follow-up records its proof and limits; the completed ID-set plan and stale links are retired; the authored development version is `.35`.

**Steps:**

- [x] Confirm the clean branch is based on `develop` PR #219, that `origin/develop` is still `e1a73159bab8a1a9c132ab9e455e98444389bc7d`, and that `Directory.Build.props` declares `0.1.0-dev.34`.
- [x] Read the complete predecessor plan and compare all of its acceptance criteria with PR #219's merged source, exact-tree and hosted-gate evidence, focused PostgreSQL test, negative test, mutation evidence, and independent review report. Classify command/read recovery, no-writeback, empty-set validity, legal-save repair, and private unrevealed clues as delivered; retain same-ID payload/order contradictions as the sole selected successor gap.
- [x] Update row 07 with PR #219's merge, source, tree, hosted gate, review and `.34` evidence; replace the stale known-clue ID-set successor wording with this payload/order slice while keeping other CaseFile and event-compatibility gaps open.
- [x] Update PLAT-001 and the persistence test follow-up with PR #219's exact bounded implementation evidence and the remaining same-ID payload/order gap. State that ADR-0028 remains unchanged because the implementation preserves its decision.
- [x] Remove the predecessor plan only after its whole scope and durable evidence are represented in the current owners; remove its stale links and do not move it into a tracked archive.
- [x] Change only `Directory.Build.props` to `0.1.0-dev.35`; inspect the staged diff for generated or duplicated version values.
- [x] Inspect the full staged diff and `git diff --cached --check`, then commit as `docs: prepare clue payload cache recovery`; rely on the check-only staged-candidate hook for the exact candidate and do not rerun its complete gate manually.

**Expected:** The prior ID-set recovery is recorded as delivered at `.34`; this plan owns only full known-clue payload/order consistency; no stale plan link remains; only the authored version advances; the staged-candidate gate passes.

### Task 2: Recover same-ID known-clue payload and ordering from events

**Files:** Modify `src/WildBunch.Persistence/GameSessions/CaseFileKnownClueCacheRecovery.cs`; extend `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated evidence and limits to PLAT-001 and the persistence test follow-up.

**Consumes:** Task 1's committed planning disposition and version `.35`; `CaseFileGenerated.CaseFile.Clues` and `PublicClues`; ordered `InvestigationPerformed.ClueId` facts; existing `CaseFileKnownClueCacheRecovery` command/read integration and PostgreSQL test fixtures.

**Produces:** The current command and player/journal read loaders recover from a known clue whose ID is still correct but whose full serialized facts or position differs from event-derived state. Healthy ordered caches remain on the cache path. Read recovery does not write back; a later legal save repairs the cache without creating another clue event.

**Steps:**

- [ ] Add PostgreSQL regressions for two known clues: one initial clue in `CaseFileGenerated.CaseFile.Clues`, then one legally revealed clue from its recorded public pool. In the payload case, change the initial clue's description and structured anchor while preserving its ID and list position; assert command, player-read, and journal-read results restore all event-recorded clue fields and keep an unrevealed clue absent. In the order case, reverse two otherwise intact known clues and assert all three paths restore event order. Run both tests before implementation with `py -3 tools/run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~ReadModel_CaseFileCacheSameIdAlteredKnownCluePayloadRecoversFromEventsWithoutWritingBack|FullyQualifiedName~ReadModel_CaseFileCacheReorderedKnownCluesRecoversFromEvents"`; confirm the payload test fails on altered facts and the order test fails on the reversed sequence.
- [ ] Update `CaseFileKnownClueCacheRecovery` to derive the ordered expected `ClueSnapshot` sequence from the latest generated snapshot's initial `Clues` plus subsequent valid public clue reveals, adding each clue ID at most once to match `CaseFile.DiscoverClue` replay behavior.
- [ ] Compare all persisted clue fields and nested anchor arrays structurally, in sequence. Do not rely on generated record equality where array properties compare by reference, and do not compare unrelated CaseFile properties.
- [ ] Keep the existing command repository and player/journal read loader mismatch branches unchanged; route the broader mismatch through their existing single full-replay fallbacks.
- [ ] Extend the PostgreSQL test to capture and compare current component JSON/version, snapshot and stream positions, ordered event rows, and diary metadata before and after all reads. Save a later legal food purchase through the ordinary Unit of Work; verify a fresh load retains exact ordered clue facts and the InvestigationPerformed count is unchanged.
- [ ] Add `CaseFileKnownClueCacheMatchesExactPayloadAndOrderFromHistory` to assert a valid cache with matching payload/order and nested structured anchors stays valid. Retain `EmptyKnownCluesMatchHistoryWhenGeneratedPublicClueRemainsUnrevealed` as proof that an unrevealed public clue does not cause recovery.
- [ ] Run the focused selection with `py -3 tools/run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~ReadModel_CaseFileCacheSameIdAlteredKnownCluePayloadRecoversFromEventsWithoutWritingBack|FullyQualifiedName~ReadModel_CaseFileCacheReorderedKnownCluesRecoversFromEvents|FullyQualifiedName~CaseFileKnownClueCacheMatchesExactPayloadAndOrderFromHistory|FullyQualifiedName~EmptyKnownCluesMatchHistoryWhenGeneratedPublicClueRemainsUnrevealed"`. Weaken the comparison to ID-set-only and verify the payload and order regressions fail; restore the implementation and verify all four tests pass.
- [ ] Append dated implementation evidence and limits to PLAT-001 and the persistence test follow-up. State that this closes only full payload/order equality for known clues; same-ID corruption elsewhere in CaseFile, malformed event compatibility, and general event-history compatibility remain open. Do not change ADR-0028 or event/schema versions.
- [ ] Inspect the full staged diff and commit as `fix: recover altered known clue cache facts`; the check-only staged-candidate hook is the canonical complete gate for this exact candidate.

**Expected:** Same-ID payload edits and reordering cannot alter player-known casebook facts; healthy sequence and empty knowledge remain valid; unrevealed clues stay private; reads preserve storage; later legal writes repair through the ordinary Unit of Work; focused and canonical checks pass.
