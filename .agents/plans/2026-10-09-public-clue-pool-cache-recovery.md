# Recover the Public Clue Pool from Event History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover the exact event-established public clue pool when the current `caseFile` cache omits it, so legal clue-surfacing actions continue to work after cache reconstruction.

**Architecture:** `CaseFileGenerated` records the complete `CaseFileSnapshot`, including the public clue pool; the current component is a cache. Treat an absent/null `publicClues` component field as an invalid cache and replay ordered events once through the existing production loader. Reject an authoritative event with no recorded pool explicitly, and keep player/journal projections limited to player-known facts.

**Tech Stack:** C#/.NET 10, EF Core, PostgreSQL, xUnit integration tests, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially cache-backed state and recovery; row 07 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [PLAT-001](../../docs/features.md#plat-001-event-history-saved-state-and-recovery).

**Execution Strategy:** `executing-plans` inline because the serializer guard, event validation, PostgreSQL replay behavior, safe read behavior, and ordinary-save repair are one coupled persistence invariant; separate task handoffs would repeat the same fixture and failure analysis without independent review value.

## Global Constraints

- Preserve immutable event authority, strict CQRS, one coherent replay per invalid-cache load, query no-writeback, and cache repair only through a later legal write.
- Preserve the public clue pool as aggregate-internal source data; player and journal reads may expose only facts established as player-known.
- Limit this slice to absent/null `publicClues` in the current `caseFile` component and absent/null `PublicClues` in `CaseFileGenerated` history.
- Do not change event payload versions, upcasters, migrations, legacy whole-session defaults, clue generation, clue selection, or other CaseFile fields.
- Advance only `Directory.Build.props` from `0.1.0-dev.32` to `0.1.0-dev.33`; do not stage generated web identity output or author another product version.
- Use branch `codex/stable-0.1-public-clue-cache` from merged `develop` PR #217; deliver one PR to `develop`, advance the version once for that merged PR, and preserve this plan through its completing PR.
- Before publication, refresh `develop` and confirm `.33` remains the unique next development version; if another PR merged first, reconcile the version and rerun affected proof and review on the candidate.

## Review Focus

- An absent or null current-cache `publicClues` field must restore the exact pool from `CaseFileGenerated`; prove the pre-fix command reload loses a known `LocalGossip` clue and the post-fix legal gossip action records that clue.
- An omitted/null authoritative event pool must fail with a deliberate diagnostic rather than an incidental null exception; prove all loaders that require replay fail closed.
- Player and journal reads must not expose unlearned public clues or write back cache repair; prove the damaged database state is unchanged until a later legal command is saved.

---

### Task 1: Retire the completed opening-lead plan and prepare this successor

**Files:** Retire `.agents/plans/2026-10-09-casefile-opening-lead-cache-recovery.md`; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `docs/features.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, and `Directory.Build.props`; create this plan.

**Consumes:** PR #217 merged to `develop` from reviewed source `c1fbcec90b1643513814f243b1bcef43e6bd1294` as `60149bdd2b34b3e2b8cb0803bfd4c3e653c4bfaf`; matching source/merge tree `be740cdc572a7247d29ce7fab3fa6a44fc05551a`; hosted canonical gate run `37919771801`; independent whole-branch review with no actionable findings; current version `0.1.0-dev.32`.

**Produces:** Row 07 records PR #217's verified opening-lead delivery and points to this plan; PLAT-001 and the persistence test follow-up record PR #217 and identify the remaining public-clue-pool gap; the completed opening-lead plan is retired only after its full scope is confirmed delivered; the authored version is `.33`.

- [x] Verify PR #217's merged state, exact source/merge/tree identities, hosted run, review evidence, and that this worktree starts at the current `develop` head.
- [x] Compare the predecessor plan's complete scope with PR #217, current source, test follow-up, feature matrix, and review/gate evidence; retire it only after confirming recovery, malformed-event failure, read no-writeback, and legal-save repair all shipped.
- [x] Commit this JIT plan before execution as `bc4d26848b79184e89c643df733357b17f58666c`; the current branch starts from merged develop commit `60149bdd2b34b3e2b8cb0803bfd4c3e653c4bfaf`.
- [x] Update row 07 with PR #217's source, merge, matching tree, hosted gate, review, and `.32` evidence; point the row at this plan and describe the bounded `publicClues` gap without closing broader CaseFile recovery.
- [x] Update PLAT-001 and the dated persistence follow-up with PR #217 delivery evidence and the observed defect: missing current-cache `publicClues` is accepted as an empty pool, so later gossip can no longer surface the recorded clue.
- [x] Advance only `Directory.Build.props` to `0.1.0-dev.33`; leave generated web identity untracked.
- [x] With plan commit `bc4d26848b79184e89c643df733357b17f58666c` already in history, stage the evidence updates, predecessor retirement, and `.33` version; commit this substantive Task 1 before editing tests or production behavior, letting the check-only staged-candidate hook validate it.

### Task 2: Restore public clues and fail closed on incomplete event history

**Files:** Modify `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`, and `src/WildBunch.Domain/Game/GameSession.cs`.

**Consumes:** Task 1's committed plan/version/evidence; production `CaseFileSnapshot` serialization and `DeserializeCaseFile`; `CaseFileGenerated` event application; command, player-read, and journal-read loaders; the PostgreSQL fixture and existing missing-CaseFile-field preservation/repair test pattern.

**Produces:** PostgreSQL behavior proof that an omitted current-cache pool replays the exact recorded public clues, a legal `GatherLocalGossip` surfaces the event-recorded gossip clue, reads do not write repaired cache state or disclose unlearned clues, a later legal save repairs the component, and missing authoritative event data fails explicitly.

- [x] Add `ReadModel_CaseFileCacheMissingOrNullPublicCluesRecoversFromEventsWithoutWritingBack` before production edits. Persist a real started session containing a known `LocalGossip` clue only in `PublicClues`; independently capture its ID and payload from the sole `CaseFileGenerated` event; test both an absent field and an explicit JSON null in the current component; and prove the pre-fix command reload has an empty pool and cannot emit the expected clue.
- [x] In that test, capture component payload/version, snapshot and stream positions, ordered event identifiers/payloads/schema versions, and diary rows before reads; load player and journal projections; assert they remain player-known-only and do not contain the unlearned clue; assert the damaged storage and all captured history metadata are unchanged after reads.
- [x] Start PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure` and run both cases with `py -3 tools/run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.ReadModel_CaseFileCacheMissingOrNullPublicCluesRecoversFromEventsWithoutWritingBack"`; confirm RED is the empty recovered pool / absent gossip event, not fixture setup or a database failure.
- [x] Require command and player-read loaders to restore the exact ordered clue IDs and clue data recorded in `CaseFileGenerated`; assert journal/player DTOs stay knowledge-safe; call `GatherLocalGossip`, assert its `InvestigationPerformed` event names the expected clue and `KnownClues` gains it, then save through the ordinary repository/unit of work and prove a fresh load retains the newly known clue and the exact unconsumed remainder.
- [x] Extend the same test's persistence capture to prove reads alone preserve the malformed component and that the legal save repairs `publicClues` at the current component version; do not use a query-triggered writeback or a test-only repair API.
- [x] Add `DamagedCaseFileCacheDoesNotHideMissingPublicCluesInCaseFileGeneratedEvent`: remove the cache field and set the persisted event pool to null; require command, player-read, and journal-read replay paths to fail with the same clear `InvalidOperationException` identifying missing recorded public clues.
- [x] Validate absent/null `publicClues` as invalid current component shape and add the explicit null-pool guard to `GameSession.Apply(CaseFileGenerated)`; keep explicit empty arrays valid and preserve all event shapes/versions.
- [x] Run both focused PostgreSQL tests and the adjacent CaseFile serializer/event reconstruction tests; temporarily bypass each new guard and confirm the positive recovery test and authoritative-event negative fail for their intended reasons, then restore the guards and confirm GREEN.
- [ ] Commit the behavior tests and minimal guards through the normal check-only hook; do not change schemas, migrations, upcasters, clue generation, projections, or other CaseFile cache fields.

### Task 3: Record evidence and deliver the slice

**Files:** Modify `docs/features.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, and this plan; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` only for factual delivery status.

**Consumes:** Focused PostgreSQL proof; ADR-0028; event-sourcing integrity doctrine; backend-architecture and writing unslop profiles; decision-record, feature-matrix, completing-plans, PR, and review playbooks.

**Produces:** Precise evidence that only absent/null current-cache public clue pools recover, missing authoritative event data fails explicitly, read paths preserve damaged storage and player-knowledge boundaries, and a later legal save repairs the cache; a reviewed PR to `develop` at `.33`.

- [ ] Record the dated persistence disposition for exact pool recovery, gossip event outcome, read preservation, knowledge-safe player/journal projections, legal-save repair, and the malformed-event negative; leave other CaseFile cache contradictions open.
- [ ] Update PLAT-001 with verified behavior and limits while preserving its existing feature promise; compare the diff against ADR-0028 and leave the ADR unchanged because this enforces its existing event-authority/cache decision.
- [ ] Verify generated web identity reports `0.1.0-dev.33` without staging it and run the canonical fail-fast `py -3 tools\run.py ci --check` on the final candidate before publication.
- [ ] Complete a fresh whole-branch review against this plan, the accepted specification, PLAT-001, persistence findings, ADR-0028, event-sourcing doctrine, selected unslop profiles, feature-matrix playbook, and review runbook; resolve actionable findings before publication.
- [ ] Publish a Draft PR to `develop`; verify its title, body, base, exact head, and review evidence; mark it ready only after local validation; require the hosted canonical gate on that exact head.
- [ ] Merge to `develop`; verify merge, source/merge tree, and hosted gate evidence; fast-forward the primary checkout; archive only this verified merged worktree, delete its local/remote branch, and remove only this branch-scoped scratch.
