# CaseFile Genesis State Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Recover the event-established CaseFile accusation and initial killer-release progress when their valid current cache values have changed.

**Architecture:** Extend `CaseFileGenerationCacheRecovery` so both command and player read loaders reject current CaseFile cache values that differ from the latest `CaseFileGenerated` event. The existing full replay path reconstructs the aggregate and read models; reads remain no-writeback, and a later ordinary command save repairs the component.

**Tech Stack:** .NET 10, C#, xUnit, EF Core, PostgreSQL, Wild Bunch repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 07.

**Execution Strategy:** `executing-plans` — the two genesis fields share one current comparison and the same command/read replay boundary; inline TDD keeps their PostgreSQL damage, no-writeback, privacy and legal-save proof in one integration context.

## Global Constraints

- Keep `GameSession` and its typed events authoritative; snapshots/components remain cache and recovery inputs.
- Preserve strict query no-writeback and the existing full-replay failure behavior for invalid authoritative history.
- Treat nullable `AccusationId` as a valid generated value; compare it exactly, including null.
- Do not add gameplay behavior or alter the future five-of-six release rule; this plan validates only accusation and progress already present in `CaseFileGenerated`.
- Do not change event schemas, event upcasters, migrations, ADRs, or duplicate version fields.
- Advance the sole authored application version in `Directory.Build.props` from `0.1.0-dev.39` to `0.1.0-dev.40` once for this implementation PR.
- The PR targets `develop`, carries the eligible predecessor-plan retirement in its first substantive commit, and merges only after the required hosted canonical check passes on the exact final PR head.

## Review Focus

- A valid cache with a different accusation must not change the player's journal or case view; Task 2 mutates to a different valid suspect ID and proves recovery through production loaders.
- A valid cache with progress at the generated release threshold must not make the culprit appear released; Task 2 asserts the event-established progress and release result after recovery.
- Cache recovery must not repair storage during reads or hide malformed authoritative history; Task 2 captures persisted state, proves later legal-save repair, and retains the existing fail-closed replay behavior.

---

### Task 1: Record PR #224 and retire its completed plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`, `docs/features.md` PLAT-001 and `Directory.Build.props`; delete `.agents/plans/2026-10-09-casefile-discovered-suspect-membership-recovery.md`; create this plan.

**Interfaces:** Consumes the merged `develop` implementation at PR #224 and its hosted check. Produces the active row 07 pointer to this plan, version `0.1.0-dev.40`, and durable evidence for the completed discovered-suspect-membership slice plus the next genesis-cache gap.

- [ ] Verify PR #224 is merged to `develop`, its exact source head is `d2f61e641711209ced7e68d555432e4c384b0aba`, and the required hosted `Canonical tracked commit gate` is successful on that head.
- [ ] Compare the entire predecessor plan with its changed production code, PostgreSQL behavior tests, delivery and review evidence; classify its whole scope as shipped before removing it.
- [ ] Record PR #224's source and merge identities, exact-head hosted check, review outcome, and `0.1.0-dev.39` identity in the row 07 roadmap and persistence follow-up. Record only its proven discovered-suspect-membership recovery, read no-writeback, privacy and ordinary-save repair.
- [ ] Update PLAT-001 and the persistence follow-up to identify generated accusation and initial release progress as the next bounded cache gap; preserve confrontation/settlement history, later release-progress behavior and broader event-history compatibility as open work.
- [ ] Point row 07 at this plan and `0.1.0-dev.40`, preserve the row's executing state, and retire only the completed predecessor plan and its stale links.
- [ ] Change the single authored version in `Directory.Build.props` from `0.1.0-dev.39` to `0.1.0-dev.40`; do not commit generated web identity output or duplicate product versions.
- [ ] Stage only the intended successor files, inspect the full staged diff and `git diff --cached --check`, then commit as `docs: record CaseFile membership recovery`; allow the check-only hook to validate the staged candidate.

**Expected:** The completed predecessor plan is retired only after its full scope and delivery are verified, row 07 stays active with this plan as successor, PR #224 evidence is promoted to current owners, and version `.40` is assigned once for this PR.

### Task 2: Recover event-established accusation and release progress

**Files:** Modify `src/WildBunch.Persistence/GameSessions/CaseFileGenerationCacheRecovery.cs` and `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; add focused evidence and limits to `docs/features.md` PLAT-001 and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Interfaces:** Consumes the existing command repository and `GameSessionReadStoreLoader` consistency checks, `CaseFileGenerated.CaseFile.AccusationId`, `KillerReleaseProgress`, the production event decoder/replay path, and PostgreSQL component mutation helpers. Produces a generated-fact match decision that accepts null or matching accusation state and the exact event-recorded initial progress, and rejects valid cache disagreement.

- [ ] Add PostgreSQL mutation cases for the current CaseFile component that independently change a non-null generated `accusationId` to a different suspect ID from the valid generated roster, and change `killerReleaseProgress` to the generated threshold while the event records a lower value. Use a real setup/start event flow and assert the generated event has the expected values before mutating storage.
- [ ] Run the new cases before production changes. Make command aggregate, player read and journal read paths assert the original accusation and release progress from the event; assert `KillerReleaseState.IsReleased` remains false when only the cache was advanced.
- [ ] Capture and compare the damaged component payload/version, envelope snapshot/stream positions, ordered stored event rows and diary rows around all reads; prove the read fallback leaves each stored value unchanged.
- [ ] Extend `CaseFileGenerationCacheRecovery.MatchesEventGeneratedFacts` to compare `AccusationId` and `KillerReleaseProgress` exactly with the latest generated snapshot. Keep null accusation legitimate and do not infer or generate a replacement value.
- [ ] Preserve command and read loaders' single existing fallback to full replay when the new comparisons fail; do not add query writeback or a second cache-repair path.
- [ ] Load each recovered cache through the command repository, make one legal food purchase using the ordinary aggregate/unit-of-work route, fresh-load, and assert the CaseFile component now matches the event-established values without adding an investigation event.
- [ ] Temporarily bypass each new comparison and rerun its focused PostgreSQL case to prove it fails for the intended changed accusation or premature release progress; restore the comparisons and rerun the focused selection alongside existing CaseFile generated-fact and event-replay tests.
- [ ] Run the focused lane with `py -3 tools/run.py dotnet-test --check -- --filter "FullyQualifiedName~CaseFileGenerationCache|FullyQualifiedName~CaseFileCache"`; use the actual test names added by this task and confirm selected PostgreSQL tests execute rather than skip.
- [ ] Update PLAT-001 and the persistence follow-up with the proven fields, production replay paths, read no-writeback, legal-save repair and fail-closed boundary. State explicitly that accusation/release genesis fields are covered while five-of-six progression, confrontation/settlement history and broader event compatibility remain open; leave ADRs, event versions, upcasters and migrations unchanged because this follows the existing event-authority decisions.
- [ ] Stage and make a normal hooked commit so the check-only hook validates the exact staged candidate. Do not bypass it or add a redundant full local gate immediately before or after a successful hooked commit.
- [ ] Complete fresh whole-branch review against this plan, the baseline spec, event-sourcing doctrine and applicable ADRs. Publish a Draft PR to `develop`, verify its final PR head equals the reviewed committed head, promote it only after local review and canonical proof, and wait for the required hosted `Canonical tracked commit gate` to pass on that exact head. Repair any hosted failure and re-verify the new final head before merging.
- [ ] Merge the PR to `develop` under the authorized goal, verify the merge and green exact-head hosted result, then record the actual delivery in row 07 and leave this plan for retirement by the next substantive successor slice.

**Expected:** A changed accusation or release-progress cache cannot alter the loaded player state. Reads preserve damaged storage, a later legal command repairs it, invalid event history still fails explicitly, and the local hooked and required hosted gates pass on the exact merged source.