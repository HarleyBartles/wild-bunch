# CaseFile Turn-In Settlement Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task.

**Goal:** Recover event-established sheriff turn-in settlement records when a valid current CaseFile cache disagrees with `SheriffTurnInSettled` history.

**Architecture:** Add a focused persistence consistency comparison for the ordered settlement facts recorded by `SheriffTurnInSettled`, and invoke it on command and player-facing read cache paths. Preserve the existing full replay fallback, query no-writeback, and ordinary unit-of-work repair; do not alter sheriff gameplay rules or event history.

**Tech Stack:** .NET 10, C#, xUnit, EF Core, PostgreSQL, Wild Bunch repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery” and the casebook decision; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07.

**Execution Strategy:** `executing-plans` — cache comparison, PostgreSQL mutation proof, privacy/payout behavior, and save repair share one event-backed recovery path and need one continuous TDD context.

## Global Constraints

- `GameSession` and its typed events remain authoritative; snapshots and components remain reconstructible caches.
- Preserve strict query no-writeback and the current fail-closed behavior for invalid authoritative event history.
- A `SheriffTurnInSettled` fact is the sole source for the corresponding `SheriffTurnInSettlementState`; preserve event order and every recorded field.
- Do not change bounty, surrender, capture, fine, repeat-payout, casebook, or journal rules; do not change event payloads, event versions, upcasters, migrations, or ADRs.
- Advance the sole authored application version in `Directory.Build.props` from `0.1.0-dev.40` to `0.1.0-dev.41` once for this implementation PR.
- The PR targets `develop`, records and retires the completed genesis recovery plan in its first substantive commit, and merges only after the required hosted canonical gate passes on the exact final PR head.

## Review Focus

- A removed or emptied settlement cache must not make the casebook forget that the suspect was captured or allow a second bounty payment; Task 2 mutates a real settled session and proves read recovery, the existing duplicate-turn-in rejection, and payout stability.
- A same-ID settlement with an altered name, bounty, disposition, alive state, day, or turn must recover every event-recorded field; Task 2 mutates each field independently and compares complete player and journal settlement projections.
- A cache-only fake settlement must not block a legitimate target; Task 2 adds an unearned settlement mutation and asserts event-established state after recovery.
- Recovery reads must preserve the damaged component and event metadata; Task 2 captures storage before and after reads, then proves a later legal command save repairs the component.

---

### Task 1: Record PR #225 and retire its completed plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, `docs/features.md` PLAT-001, and `Directory.Build.props`; delete `.agents/plans/2026-10-09-casefile-genesis-state-recovery.md`; create this plan.

**Interfaces:** Consumes PR #225 source `e9a329872f29d293a0848e885035b5da6e3052e3`, merge `8b5796e60132c6ac23f4198c5bdba8e8423bb3b0`, hosted run `37962963866`, and version `0.1.0-dev.40`. Produces row 07's pointer to this plan at version `.41`, records the bounded genesis accusation/release-progress recovery, and identifies turn-in settlement state as the next selected cache gap.

- [x] Verify PR #225 is merged to `develop`, the recorded source is `e9a329872f29d293a0848e885035b5da6e3052e3`, the merge is `8b5796e60132c6ac23f4198c5bdba8e8423bb3b0`, and hosted run `37962963866` succeeded on that exact source.
- [x] Compare the completed genesis-recovery plan with the merged implementation, PostgreSQL behavior proof, review, hosted gate, and roadmap scope; classify its whole scope as shipped before removing it.
- [x] Record PR #225's source, merge, hosted run, clean review outcome and `.40` version in row 07 and the persistence follow-up. Record only generated accusation and initial release-progress recovery, replay across command/player/journal reads, read no-writeback, privacy, and ordinary-save repair.
- [x] Update PLAT-001 with that bounded evidence and state that CaseFile settlement state and other event-history gaps remain open. Preserve the casebook's learned evidence and captured status as separate event-derived facts.
- [x] Point row 07 at this plan, preserve its executing state, retire only the completed genesis plan and stale links, and bump `Directory.Build.props` from `.40` to `.41`.
- [x] Stage only the intended successor files, inspect the full staged diff and `git diff --cached --check`, then commit as `docs: plan CaseFile settlement recovery`; allow the check-only hook to validate the staged candidate.

**Expected:** The `.40` slice is recorded against verified delivery evidence, its completed plan is retired in successor history, and row 07 has a bounded `.41` plan for event-established sheriff settlement recovery.

### Task 2: Rebuild CaseFile settlements from turn-in events

**Files:** Create `src/WildBunch.Persistence/GameSessions/CaseFileSettlementCacheRecovery.cs`; modify `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`, `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`, `docs/features.md` PLAT-001, and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Interfaces:** Consume ordered decoded `IDomainEvent` history and the current `CaseFile.SheriffTurnInSettlements`. Produce a comparison that accepts the event-derived ordered `SheriffTurnInSettlementState` sequence and rejects missing, altered, reordered, or invented cache records. The current command repository and `GameSessionReadStoreLoader` must route mismatches through their existing full replay fallback.

- [x] Add nine PostgreSQL theory cases that create a started session with valid known warrants, establish two encounters and successful sheriff turn-ins through `GameSession` methods, persist the resulting `SheriffTurnInSettled` events, then independently empty the settlement list, alter each same-suspect field (`TargetName`, `Disposition`, `IsAlive`, `BountyAmount`, `Day`, `Turn`), add an unrecorded suspect settlement, or reverse the event-established settlements. Assert the event order and full aggregate/event settlement equality before mutating valid JSON; never construct the event or call `Apply` directly in the fixture.
- [x] Run the new cases before production changes and witness the empty-cache PostgreSQL case fail because command loading returns no settlements. With recovery enabled, all nine cases assert exact event-recorded settlement sequences and fields through command/player/journal loads, event-backed wallet, known warrants and captured casebook status; the empty-list case rejects a repeat bounty without another settlement event.
- [x] Capture the damaged CaseFile component payload/version, envelope snapshot/stream positions, ordered stored event rows, and diary rows before player/journal reads; all nine cases assert they remain unchanged after reads.
- [x] Implement the comparison by deriving settlements from ordered `SheriffTurnInSettled` events after the latest `CaseFileGenerated` boundary and comparing complete settlement records, including target ID/name, warrant disposition, alive state, bounty amount, day, turn, count, and order. If no generated CaseFile event exists, preserve the existing compatibility boundary and do not make this slice a general historical-event validator.
- [x] Add the settlement comparison to command and read cache validation beside the existing generation/evidence checks. Keep the existing single full-replay fallback; do not write cache data during queries or add a second repair mechanism.
- [x] After recovery, perform one unrelated legal purchase through the normal command/unit-of-work route, fresh-load the session, and assert the settlement component is repaired without another sheriff settlement event. Existing malformed-event replay behavior remains unchanged.
- [x] Temporarily bypass both settlement comparisons and rerun the focused PostgreSQL cases; all nine independent mutations fail on their intended missing, altered, invented, or reordered cached settlement. Restore the comparison and run `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~CaseFileSettlementCache|FullyQualifiedName~CaseFileCache"`; 38 selected PostgreSQL cases execute and pass with none skipped.
- [x] Update PLAT-001 and the persistence follow-up with exact covered settlement fields, duplicate-payment and casebook evidence, read no-writeback, ordinary-save repair and the compatibility boundary. State that other mutable CaseFile state, broader malformed-history coverage and historical migration questions remain open. Leave ADRs, event schemas, upcasters and migrations unchanged because this follows existing event-authority decisions.
- [ ] Stage and make a normal hooked implementation commit; do not bypass the hook or repeat a full local gate immediately around a successful hooked commit.
- [ ] Complete a fresh whole-branch review against this plan, the baseline spec, event-sourcing doctrine and applicable ADRs. Publish a Draft PR to `develop`, verify the PR head equals the reviewed source, promote it after local review and canonical proof, and verify the required hosted `Canonical tracked commit gate` succeeds on that exact head. Repair any hosted failure and verify the new final head before merging.
- [ ] Merge the PR to `develop` under the authorized goal, verify the merge and exact-head hosted result, and leave this plan for retirement by the next substantive successor slice.

**Expected:** A stale but valid settlement cache cannot hide a captured suspect, misstate settlement details, invent a prior payment, or permit a duplicate bounty. Reads preserve damaged storage, a later legal save repairs it, and local and hosted canonical gates pass on the exact merged source.
