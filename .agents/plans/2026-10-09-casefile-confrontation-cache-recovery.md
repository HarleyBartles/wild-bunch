# CaseFile Wanted-Suspect Confrontation Cache Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover a valid current CaseFile cache when its stored wanted-suspect confrontation states contradict the facts established by the event stream.

**Architecture:** Derive the ordered `WantedSuspectConfrontationState` records from `WantedSuspectConfronted` events after the latest `CaseFileGenerated` boundary, using the preceding `TownActionContextEntered` clock facts for each event. Route a mismatch in the command repository and shared player/journal read loader through their existing full replay fallback; retain query no-writeback and ordinary-save repair.

**Tech Stack:** .NET 10, C#, xUnit, EF Core, PostgreSQL, Wild Bunch repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07; `.agents/playbooks/decision-records.md`.

**Execution Strategy:** `executing-plans` — the event comparison, valid-cache mutation proof, shared loader integration, read no-writeback, and ordinary-save repair are one tightly coupled persistence path best handled in one inline TDD context, followed by one independent whole-branch review.

## Global Constraints

- `GameSession` and its typed events remain authoritative; snapshots and components remain reconstructible caches.
- A `WantedSuspectConfronted` fact establishes a CaseFile confrontation state unless its outcome is `Abandoned`; derive `Day` and `Turn` from the last preceding `TownActionContextEntered` fact as replay does.
- Compare every recorded state field and preserve event order; do not infer clock values from the current cached clock or invent a confrontation from the cache.
- Preserve the no-`CaseFileGenerated` compatibility boundary and current fail-closed behavior for invalid authoritative event history.
- Preserve strict query no-writeback, the existing full-replay fallback, and repair through a later ordinary command save; do not change confrontation, surrender, turn-in, payout, journal, or casebook rules.
- Do not change event payloads, event versions, upcasters, migrations, or ADRs; the implementation follows the live decisions in ADR-0028 and ADR-0038.
- Advance the sole authored application version in `Directory.Build.props` from `0.1.0-dev.41` to `0.1.0-dev.42` once for this implementation PR.
- The PR targets `develop`; the required hosted `Canonical tracked commit gate` must pass on the exact final PR head before merge.

## Review Focus

- If a cached confrontation says a suspect surrendered when the recorded event says they fled, a later sheriff turn-in assessment must still reject that suspect; Task 2 mutates `Outcome` and `IsSecured`, then verifies the recovered command result and assessment.
- If the cache has the right result but the wrong action date, the case history must retain the event-time day and turn after multiple context changes; Task 2 uses two real confrontations at different turns and mutates each clock field independently.
- If a cache invents, omits, duplicates, or reorders a confrontation, command and game-read models must reflect only the event-established sequence; Task 2 proves each contradiction through PostgreSQL-backed loads and confirms read no-writeback.

---

### Task 1: Record PR #226 and plan the next cache-recovery slice

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, `docs/features.md` PLAT-001, and `Directory.Build.props`; delete `.agents/plans/2026-10-09-casefile-turnin-settlement-recovery.md`; create this plan.

**Interfaces:** Consume PR #226 source `0d0bd1e6a566beca6b6042fd9aec16c47f2117e9`, merge `f8a8043ca94f80acf7a357ea826e47e57c7d21c6`, hosted run `37967392268`, and version `0.1.0-dev.41`. Record settlement-cache recovery as delivered and select event-established wanted-suspect confrontation states as the next distinct mutable CaseFile cache boundary.

- [x] Verify PR #226 is merged to `develop`, its source is `0d0bd1e6a566beca6b6042fd9aec16c47f2117e9`, its merge is `f8a8043ca94f80acf7a357ea826e47e57c7d21c6`, and hosted run `37967392268` succeeded on that exact source.
- [x] Compare the completed settlement-recovery plan with its merged implementation, PostgreSQL behavior proof, review, hosted gate, and roadmap scope; classify its whole scope as shipped before removing it.
- [x] Record PR #226's source, merge, hosted run, clean review outcome, and `.41` version in row 07 and the persistence follow-up. Record its exact settlement fields, duplicate-payout and casebook behavior, query no-writeback, ordinary-save repair, and no-generation-event compatibility boundary.
- [x] Update PLAT-001 with the settlement evidence and state that confrontation-state consistency, later mutable CaseFile history, malformed-event compatibility, and historical migration questions remain open. Keep learned evidence, confrontation outcome, and sheriff settlement as distinct event-derived facts.
- [x] Point row 07 at this plan, retain its executing state, retire only the completed settlement plan and stale links, and bump `Directory.Build.props` from `.41` to `.42`.
- [x] Stage only the intended successor files, inspect the full staged diff and `git diff --cached --check`, then commit as `docs: plan CaseFile confrontation cache recovery`; let the normal check-only hook validate the staged candidate.

**Expected:** The `.41` slice is recorded against verified delivery evidence, its completed plan is retired in successor history, and row 07 has a bounded `.42` plan for event-established confrontation-state recovery.

### Task 2: Rebuild CaseFile confrontation states from events

**Files:** Create `src/WildBunch.Persistence/GameSessions/CaseFileConfrontationCacheRecovery.cs`; modify `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`, `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`, `docs/features.md` PLAT-001, and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Interfaces:** Consume ordered decoded `IDomainEvent` history and `CaseFile.WantedSuspectConfrontations`. Produce an exact ordered comparison over suspect ID, target name, disposition, outcome, alive state, secured state, day, and turn. For each confrontation, derive its day and turn from the latest preceding `TownActionContextEntered` event, matching `GameSession.Apply`; ignore `Abandoned` outcomes because replay does not record them in CaseFile state. If no `CaseFileGenerated` exists, retain the current compatibility result and do not broaden this slice into historical-event validation.

- [ ] Add PostgreSQL behavior cases using a started session with two distinct wanted suspects confronted through the real saloon flow at different action-context clock values. Persist through the repository and assert the expected `WantedSuspectConfronted` and preceding context events exist before damaging the valid CaseFile JSON; never construct or apply the confrontation event directly in the fixture.
- [ ] Independently mutate the cached confrontation records: remove one, change each recorded field, add an unearned second record, duplicate a suspect record, and reverse the two-record sequence. Keep IDs and enum values valid where the case is intended to test a same-ID altered field. Assert the pre-mutation CaseFile records equal the state produced by full replay.
- [ ] Run the new PostgreSQL cases before production changes and witness each mismatch fail because the fast path returns the damaged confrontation state. With recovery enabled, command loads and the player `GameSessionReadModel` return the exact event-derived ordered records; the fled-suspect case remains ineligible for sheriff turn-in after `Outcome` or `IsSecured` cache mutations.
- [ ] Capture the damaged CaseFile payload/version, envelope snapshot/stream positions, ordered stored event rows, and diary rows before query reads; assert all remain unchanged after the player read and journal read. Do not assert confrontation details in the journal DTO because that projection does not expose this internal CaseFile state.
- [ ] After recovery, make one unrelated legal food purchase through the normal command/unit-of-work route, fresh-load the session, and assert the confrontation records are repaired without another confrontation event.
- [ ] Keep the existing malformed-`WantedSuspectConfronted` fail-closed test passing. Temporarily bypass the new confrontation comparison and rerun the focused PostgreSQL cases; each independent corruption must fail for its intended missing, altered, invented, duplicate, or reordered record. Restore the comparison and run `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~CaseFileConfrontationCache|FullyQualifiedName~CaseFileCache"` with `.\tools\postgres-dev.ps1 ensure` run first.
- [ ] Update PLAT-001 and the persistence follow-up with covered fields, state/order and clock behavior, turn-in consequence, read no-writeback, ordinary-save repair, and compatibility boundary. Leave ADRs, event schemas, upcasters, and migrations unchanged.
- [ ] Stage and make a normal hooked implementation commit; do not bypass the hook or run the complete canonical gate immediately around a successful hooked commit.
- [ ] Complete a fresh whole-branch review against this plan, the baseline spec, event-sourcing doctrine, ADR-0028, ADR-0038, and applicable unslop guidance. Publish a Draft PR to `develop`, verify the PR head equals the reviewed source, promote it after local review and canonical proof, and verify the hosted `Canonical tracked commit gate` succeeds on that exact head. Repair any hosted failure and verify the new final head before merging.
- [ ] Merge the PR to `develop` under the authorized goal, verify the merge and exact-head hosted result, and leave this plan for retirement by the next substantive successor slice.

**Expected:** A stale but valid confrontation cache cannot change a suspect's recorded identity, outcome, custody, or action time, and cannot alter sheriff turn-in eligibility. Reads preserve damaged storage, a later legal save repairs it, and local and hosted canonical gates pass on the exact merged source.
