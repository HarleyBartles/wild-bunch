# CaseFile Discovered-Suspect Membership Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover a current CaseFile cache when its discovered-suspect membership differs from the facts established by `CaseFileGenerated` and later event-recorded clue reveals.

**Architecture:** Extend the Persistence CaseFile event-consistency check to derive discovered-suspect IDs from the latest generated CaseFile snapshot plus valid later `InvestigationPerformed` clue reveals. Compare membership as an ordinal ID set because the player projection enumerates suspects in roster order; route a mismatch through the existing command and read full-replay fallback without query writeback.

**Tech Stack:** .NET 10, C#, xUnit, EF Core, PostgreSQL, Wild Bunch repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07.

**Execution Strategy:** `executing-plans` — successor delivery evidence and predecessor retirement must land before the persistence proof; the event-derived membership cases share one CaseFile replay boundary, so inline TDD keeps recovery, privacy, no-writeback and ordinary-save repair together for one fresh whole-branch review.

## Global Constraints

- Immutable event history is authoritative; current component rows are reconstructible caches.
- Discovered-suspect membership starts with `CaseFileGenerated.DiscoveredSuspectIds`; a later successfully revealed generated public clue adds only linked IDs present in the generated suspect roster.
- Discovered-suspect membership is a set; its stored array order does not change the player-facing casebook, which enumerates suspects in roster order.
- Invalid derived state uses the existing ordered, upcasted full-replay fallback; invalid authoritative history fails explicitly.
- Query recovery does not write storage, issue commands, emit game events, or advance gameplay.
- Player and journal projections do not reveal the hidden culprit or backend-only identifiers.
- No event/schema version, migration, API, gameplay rule, durable architecture decision, or historical compatibility boundary changes in this slice.
- Advance the sole authored application version in `Directory.Build.props` from `0.1.0-dev.38` to `0.1.0-dev.39` for this PR; refresh `origin/develop` before publication and resolve any version race before opening the PR.
- Use this fresh worktree based on merged `develop`; publish a reviewed PR to `develop`, verify its hosted canonical gate on the exact head, and merge it as part of the authorized epic.
- Retire the completed generated-facts recovery plan only in this successor's first substantive commit, after recording PR #223 delivery evidence in current owners.

## Review Focus

- Removing an event-discovered suspect from a valid cache must not erase a clue-linked person from the player's or journal's casebook; adding an undiscovered hidden culprit must not expose that identity. Both are independent PostgreSQL theory rows and must fail before the recovery comparison is added.
- The expected ID set must follow actual `Apply(InvestigationPerformed)` behavior: only a newly revealed clue found in the generated public pool adds roster-linked suspects; initial generated known clues do not independently add discovery state.
- Reordering the same discovered IDs must remain a healthy cache because `GetDiscoveredSuspects()` enumerates the suspect roster; a missing authoritative generated discovery list must continue to fail closed through replay.
- Command, player and journal reads must recover without writing storage, and a later legal purchase must repair the component through the normal unit of work without another investigation event.

---

### Task 1: Retire generated-facts recovery and record PR #223

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, `docs/features.md` PLAT-001, and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; delete `.agents/plans/2026-10-09-casefile-generated-facts-cache-recovery.md`; update `Directory.Build.props`.

**Consumes:** PR #223 merged to `develop` as `283bcdbb30ce7a534bafdb12c6f4b5ca9d68f955` from reviewed source `614248a90d6ffe19e6f262eb7a98cdaa15336f8b`; source, merge and develop trees match `e423e0a8470788aeeeac7f17f60de01219217997`; hosted canonical gate run `37952676399` passed on the exact source; independent whole-branch review found no actionable findings; development identity `0.1.0-dev.38`.

**Produces:** Current owners record the exact immutable generation-fact recovery, its remaining mutable-state and compatibility limits, the completed plan is retired, row 07 points at this plan and `.39`, and only `Directory.Build.props` advances the application version.

- [ ] Record PR #223's source, merge, matching tree, exact-head hosted gate, review outcome, bounded behavior and `.38` identity in the roadmap, PLAT-001 and persistence follow-up.
- [ ] Classify the entire generated-facts plan against its code, eleven PostgreSQL mutation rows, focused healthy comparator, read no-writeback, privacy, ordinary-save repair, review and hosted gate; preserve mutable CaseFile and broader history gaps in current owners, then remove the completed plan and stale links.
- [ ] Add the settled discovered-suspect membership rule to the spec and update PLAT-001/follow-up to identify valid same-ID membership drift as the next open cache gap; preserve the limit that only generated genesis IDs and successfully revealed generated public clues establish membership.
- [ ] Update the row 07 active-plan pointer and target version to this plan and `.39`; preserve row 07 as executing because other persistence and compatibility gaps remain.
- [ ] Change only `Directory.Build.props` to `0.1.0-dev.39`; do not commit generated web identity output or duplicate version fields.
- [ ] Inspect the complete staged successor disposition and `git diff --cached --check`, then commit it as `docs: retire generated facts plan and record delivery`; let the normal check-only hook validate the staged candidate.

**Expected:** PR #223's completed and reviewed scope is durably recorded; its plan is retired only after promotion; the spec states how event-established suspect membership is derived; row 07 points at this plan; and the sole authored application version advances once.

### Task 2: Recover event-established discovered-suspect membership

**Files:** Modify `src/WildBunch.Persistence/GameSessions/CaseFileEvidenceCacheRecovery.cs` and `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated evidence and limits to `docs/features.md` PLAT-001 and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Consumes:** Task 1's PR #223 disposition and `.39` identity; the latest applicable `CaseFileGenerated.CaseFile` snapshot; `InvestigationPerformed.ClueId`; existing ordered known/public clue reconstruction; command and read-store mismatch guards that already call `CaseFileEvidenceCacheRecovery`; the PostgreSQL fixture and `CaptureCaseFileCacheRecoveryStateAsync` helper.

**Produces:** Existing command, player-read and journal-read loading reject a valid CaseFile cache whose discovered-suspect ID set differs from genesis plus successfully revealed generated clues, replay supported history through the established fallback, preserve storage during reads, and repair the component on the next legal command save.

- [ ] Add a PostgreSQL `[Theory]` with two independent valid component mutations: remove a suspect discovered by a persisted `InvestigationPerformed` clue, and add the hidden culprit ID when no generated or revealed clue established it. Build a deterministic three-suspect case with one initially discovered suspect, a public LocalGossip clue linked to a second suspect, and an undiscovered culprit as the third; call `GatherLocalGossip()` before persistence so the event stream records the clue reveal.
- [ ] In each theory row, mutate only `discoveredSuspectIds` while retaining valid roster IDs and all other component facts. Assert command, player and journal reads restore the expected ID set, the player and journal retain the clue-linked suspect, and neither DTO contains the hidden culprit ID or name. Capture and compare component JSON/version, envelope snapshot/stream positions, ordered event rows and diary rows around all reads.
- [ ] Run the new PostgreSQL theory before production changes and witness the missing-suspect row fail because the casebook omits the learned suspect and the hidden-culprit row fail because the player projection exposes that identity.
- [ ] Extend the existing event-derived CaseFile comparison using the same generated clue reconstruction already used for known clues. Begin with the generated `DiscoveredSuspectIds`; only when a later `InvestigationPerformed.ClueId` resolves to a not-yet-known clue in the generated public pool, add its linked IDs that exist in the generated suspect roster. Do not infer discovery from generation-time known clues, unknown event clue IDs, or an unrecorded action.
- [ ] Compare the current `DiscoveredSuspectIds` to the expected IDs using ordinal set equality. Keep ordering non-semantic because the player projection returns suspects in roster order. A null/missing authoritative generated discovery list must not be interpreted as empty; retain the existing full-replay failure for invalid history.
- [ ] Add a focused healthy comparator test that reverses the same valid discovered IDs and remains a match. Keep existing clue/warrant exact-payload/order behavior unchanged; the current command and read-store loader calls should use the extended comparator without a second recovery path.
- [ ] Confirm the new PostgreSQL rows fail for their intended user-visible behavior before implementation; after green, temporarily remove only the discovered-set comparison and rerun the rows to witness both regressions fail, then restore the comparison and rerun the focused selection.
- [ ] Load the damaged component through the command path, make one legal food purchase using `Purchase(new StoreOffer(DomainItemKind.Food, "Food", 2m), 1)`, save through the ordinary unit of work and fresh-load the session. Assert the event count increases only by the normal store context and purchase facts, no new investigation event appears, and the repaired component retains the event-established discovered set.
- [ ] Run the focused PostgreSQL selection with `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~ReadModel_CaseFileCacheSameIdAlteredDiscoveredSuspectsRecoversFromEventsWithoutWritingBack|FullyQualifiedName~CaseFileEvidenceCacheMatchesDiscoveredSuspectIdsAsSet|FullyQualifiedName~CaseFileEvidenceCacheMatchesExactPayloadAndOrderFromHistory|FullyQualifiedName~CaseFileClueCacheMatchesExactPayloadAndOrderFromHistory"`; ensure the new healthy and mutation cases pass and existing clue/warrant coverage remains green.
- [ ] Append dated feature-matrix and persistence-follow-up evidence limited to discovered-suspect membership, its genesis/reveal sources, command/read recovery, no-writeback, culprit privacy and ordinary-save repair. State that accusation, release progress, confrontation/settlement consistency and broader event-history compatibility remain open. Leave ADRs and schema/upcaster history unchanged.
- [ ] Inspect and commit the implementation slice as `fix: recover altered discovered suspects`; let the normal check-only staged-candidate hook run the canonical fail-fast gate. Do not manually rerun the full gate after a successful hooked commit.

**Expected:** Removing a learned suspect or adding an unearned culprit identity from the current component cannot change the player-visible casebook; membership derives from genesis plus event-established clue reveals, order-only changes remain valid, reads do not write, and a later legal command repairs the component. Focused PostgreSQL proofs and the staged-candidate canonical gate pass.
