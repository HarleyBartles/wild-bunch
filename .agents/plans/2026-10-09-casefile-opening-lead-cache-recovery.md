# Recover Case Opening Lead from Event History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task.

**Goal:** Recover the exact event-established opening lead when the current `caseFile` cache omits it, instead of displaying generic invented text.

**Architecture:** `CaseFileGenerated` records the opening lead and replay applies that fact to `GameSession`; the persisted `caseFile` component is only a cache. Reject a current component whose `openingLead` is absent, null, or blank so existing ordered replay restores the recorded description. If the authoritative event cannot supply a valid opening lead, fail closed with a clear domain error.

**Tech Stack:** C#/.NET 10, EF Core, PostgreSQL, xUnit integration tests, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially the cache-backed state and recovery contract and the rule that unknown facts remain unknown; row 07 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [PLAT-001](../../docs/features.md#plat-001-event-history-saved-state-and-recovery).

**Execution Strategy:** `executing-plans` inline. The cache decoder, event application, PostgreSQL read paths, and repair proof are sequential parts of one CaseFile invariant; a fresh whole-branch review is more useful than repeated task handoffs.

## Global Constraints

- Preserve immutable event authority, strict CQRS, coherent reads, query no-writeback, and cache repair only through a later legal write.
- Do not invent a generic opening lead, add event types, change event payload schemas/upcasters, alter migrations, or redesign serialization compatibility in this slice.
- Keep the check scoped to current component cache decoding and `CaseFileGenerated` application; leave legacy whole-session snapshot behavior unchanged.
- Advance only the authored version in `Directory.Build.props` from `0.1.0-dev.31` to `0.1.0-dev.32`; generated web identity remains untracked.
- Use this fresh `origin/develop` worktree and dedicated `codex/stable-0.1-casefile-cache-next` branch; deliver the plan by PR to `develop` and advance the development version once for that merged PR.
- Before publication, refresh `develop` and confirm `.32` is still the unique next development version; if another PR merged first, update the authored version and rerun affected checks and review on the new candidate.
- Preserve this plan through its completing PR; the next substantive slice assesses its whole scope before retiring it.

## Review Focus

- A missing current-cache opening lead must recover the exact recorded description rather than the generic fallback; the PostgreSQL test must fail before the fix for that observed difference.
- A missing or blank authoritative `CaseFileGenerated` opening lead must fail clearly when cache recovery requires replay; it must not silently substitute text or escape as an incidental null exception.
- Reads must preserve the damaged cache and persisted event/envelope/diary metadata, while a later legal save repairs the cache through the ordinary unit of work.

---

### Task 1: Retire the completed discovered-identity slice and prepare this successor

**Files:** `.agents/plans/2026-10-09-casebook-discovered-identity-cache-recovery.md` (retire); `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `Directory.Build.props`; this plan.

**Consumes:** PR #216 merged to `develop` from reviewed source `90791dfaea6d0aa93c79f382b1e788e8c0f244ad` as `fb36ad40c29ff9440242f63022d9614f1f8ede2a`; source and merge tree `82655d6d114f1cbaba6ed09000c2eadf8e8085ae`; hosted gate run `37915576877`; current version `0.1.0-dev.31`.

**Produces:** Row 07 records PR #216's verified delivery and retires its completed plan; PLAT-001 and the persistence test follow-up record the delivered discovery-list recovery and identify the remaining opening-lead fallback; the next development version is `.32`.

- [x] Verify PR #216's merged state, exact source/merge/tree identities, successful hosted run, no-finding independent review, and that this worktree starts from the merged `develop` head.
- [x] Compare the predecessor plan's complete scope with PR #216, its current feature-matrix and persistence-test evidence, and the review/gate results; retire it only after confirming every planned exit shipped.
- [x] Update row 07 with PR #216's source, merge, matching tree, hosted gate, review and `0.1.0-dev.31` evidence; point the row at this plan and name the opening-lead cache gap without closing broader CaseFile recovery.
- [x] Update PLAT-001 and the dated persistence test follow-up with PR #216's verified discovered-suspect behavior and limits, then record that a missing current-cache opening lead currently becomes generic text.
- [x] Advance only `Directory.Build.props` to `0.1.0-dev.32`; do not stage generated web identity output or add another authored application version.
- [x] Commit the plan, successor evidence, predecessor retirement and version change before editing behavior tests or production code; the check-only staged-candidate hook validates the staged candidate.

### Task 2: Recover the recorded opening lead and reject invalid history

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; `src/WildBunch.Domain/Game/GameSession.cs`.

**Consumes:** The persisted `caseFile` component and `CaseFileGenerated` event; `DeserializeCaseFile`; `GameSession.Apply(CaseFileGenerated)`; the production command, player-read and journal-read loaders; the existing PostgreSQL fixture and `ReadModel_CaseFileCacheMissingDiscoveredSuspectsRecoversFromEventsWithoutWritingBack` setup/capture pattern.

**Produces:** PostgreSQL proof that an omitted current-cache opening lead recovers the exact recorded description through all applicable loaders without read-time writes, a later legal save repairs it, and malformed authoritative opening-lead history fails closed clearly.

- [ ] Add `ReadModel_CaseFileCacheMissingOpeningLeadRecoversFromEventsWithoutWritingBack` before production edits: persist a real started session, independently capture the `CaseFileGenerated` opening-lead description, remove only `openingLead` from the current `caseFile` JSON, and assert that command, player and journal reads return that exact description.
- [ ] In the same behavior test, capture the damaged component payload/version, snapshot and stream positions, ordered event payloads/metadata, and diary rows before reads; compare them after reads, then issue a legal food purchase, save normally, and verify a fresh load retains the recorded opening lead from the repaired current component.
- [ ] Start PostgreSQL with `.\tools\postgres-dev.ps1 ensure` and run the focused positive test with `py -3 tools\run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.ReadModel_CaseFileCacheMissingOpeningLeadRecoversFromEventsWithoutWritingBack"`; confirm the red result is the generic fallback differing from the event-established description.
- [ ] Add `DamagedCaseFileCacheDoesNotHideMissingOpeningLeadInCaseFileGeneratedEvent`: remove the cache field and set the persisted event's opening lead to null; require command, player and journal loads to fail with the same clear `InvalidOperationException` identifying the missing recorded opening lead.
- [ ] Extend `GameSession.Apply(CaseFileGenerated)` to reject a null or blank recorded opening-lead description with that clear failure; validate absent/null/blank current-cache opening lead as an invalid required component shape and remove only the generic fallback from `CaseFileSnapshot.ToDomain`'s current component path.
- [ ] Rerun both focused PostgreSQL tests and relevant CaseFile serialization/event-application tests; verify the test-only failure reaches the invalid-cache recovery path and the authoritative-event negative reaches the explicit domain guard.
- [ ] Commit the behavior tests and minimal cache/event validation through the normal check-only hook; do not change event JSON, upcasters, migrations, or legacy whole-session defaults.

### Task 3: Record evidence and deliver the slice

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` only if delivery status requires a factual correction.

**Consumes:** Focused PostgreSQL proof, ADR-0028, event-sourcing integrity doctrine, backend-architecture and code-review unslop profiles, feature-matrix and decision-record playbooks, and PR/code-review runbooks.

**Produces:** Precise evidence that only missing/null/blank current-cache opening-lead shapes recover, invalid event history fails clearly, read paths do not write back, and a later legal save repairs the cache, followed by a reviewed PR to `develop`.

- [ ] Record a dated persistence-test disposition for exact opening-lead recovery, read preservation, legal-save repair, and the malformed-event negative; leave other CaseFile fields and general optional/nested cache recovery open.
- [ ] Update PLAT-001 with verified opening-lead behavior and limits; keep the feature promise unchanged because this slice corrects persistence of an existing player-facing fact.
- [ ] Compare the final diff with ADR-0028 and the decision-record playbook; leave the ADR unchanged because this implements its existing event-authority and rebuildable-cache decision.
- [ ] Confirm generated web identity reports `0.1.0-dev.32` without staging it; let the normal staged-candidate hook run the canonical fail-fast gate on the implementation candidate.
- [ ] Complete a fresh whole-branch review against this plan, the accepted specification, PLAT-001, persistence findings, ADR-0028, event-sourcing integrity, selected unslop profiles, feature matrix and review runbook; resolve actionable findings before publication.
- [ ] Publish a Draft PR to `develop`, verify its title/body/base/exact head and review evidence, mark it ready after local validation, and require the hosted canonical gate on that exact head.
- [ ] Merge to `develop`; verify merge, tree and hosted-gate evidence, fast-forward the primary checkout, and clean only the verified merged worktree, local/remote branch and branch-scoped scratch.
