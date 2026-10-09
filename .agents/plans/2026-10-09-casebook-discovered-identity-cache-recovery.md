# Recover Discovered Casebook Identity from Events Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline under the active Stable 0.1.0 goal.

**Goal:** Recover a player's event-established discovered-suspect list when a current `caseFile` cache omits `discoveredSuspectIds`, rather than presenting an empty casebook.

**Architecture:** `CaseFileGenerated` records the generated case and its discovered identities; later `InvestigationPerformed` events record clue discoveries that can reveal suspects. The composed `caseFile` component is a derived cache. Reject the current component shape when its required `discoveredSuspectIds` field is absent or null, then use existing ordered full replay. A valid empty list remains valid when it is explicitly present. Reads must not write back; a later legal command repairs the cache through the existing unit of work. If the authoritative event payload cannot reconstruct its required case facts, fail closed.

**Tech Stack:** C#/.NET 10, EF Core, PostgreSQL, xUnit integration tests, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially the event-history and cache-recovery contract and the rule that unknown facts remain unknown; row 07 of the [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [PLAT-001](../../docs/features.md#plat-001-event-history-saved-state-and-recovery).

**Execution Strategy:** `executing-plans` inline. Use PostgreSQL through production command and player/journal loaders. The core behavior test removes one required field from an otherwise current `caseFile` payload and independently asserts the known suspect identity returns from persisted history. Keep schema/version work out of scope unless inspection shows that current event history cannot truthfully reconstruct the fact; if that consequential boundary appears, stop and return to the user rather than inventing compatibility.

## Global Constraints

- Preserve immutable event authority, strict CQRS, coherent reads, query no-writeback, and cache repair only through a later legal write.
- Do not invent casebook facts, add event types, change event payload schemas/upcasters, alter migrations, or redesign serialization compatibility in this slice.
- Keep explicitly present empty `discoveredSuspectIds` valid; only the absent/null required field is the selected malformed current-cache shape.
- Keep legacy whole-session snapshot defaults and their tests unchanged; the new validation belongs only to current component cache deserialization.
- Advance only the authored version in `Directory.Build.props` from `0.1.0-dev.30` to `0.1.0-dev.31`; generated web identity remains untracked.
- Preserve this plan through its completing PR; the next substantive slice will assess its whole scope before retiring it.

## Review Focus

- The PostgreSQL behavior test must fail before the fix because the discovered suspect is absent from loaded player/journal casebook state, not because fixture setup or JSON mutation failed.
- Recovery must restore the exact event-established suspect and leave the missing-field payload, component version, event rows, snapshot/stream positions, and diary projection metadata unchanged during reads.
- The next legal command must repair the component through the normal unit of work, and a fresh load must retain the discovered identity.
- An explicitly present empty discovery list must not be classified as malformed; an authoritative event payload that cannot establish the casebook must still fail closed.

---

### Task 1: Retire the completed saloon-cache plan and advance the successor

**Files:** `.agents/plans/2026-10-09-town-visit-saloon-cache-recovery.md` (retire); `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; `Directory.Build.props`; this plan.

**Consumes:** Merged PR #215, source `a1bbeb765456ba19ab74ae185ca4844636e8c46c`, merge `bf871c152b1cbf753e14fea58c382906b6e2d3ed`, matching tree `2026c2c014cc1d8ac63f1ca2736d1d6c10f4cb5d`; hosted gate run `37911946265`; current version `0.1.0-dev.30`.

**Produces:** The first substantive commit records PR #215's exact delivery evidence, retires its completed plan, points row 07 at this successor, advances the single product version to `.31`, and narrows the next PLAT-001 risk to an omitted current-cache discovery field.

- [x] Verify PR #215 is merged to `develop`, the reviewed source and merge/tree evidence match the roadmap receipt, and hosted run `37911946265` passed on the reviewed source.
- [x] Assess the predecessor plan against the actual merged source, feature-matrix/test-follow-up promotion, fresh whole-branch no-finding review, and hosted result; retire it only after confirming the entire bounded saloon cache scope shipped.
- [x] Update row 07 with PR #215's source, merge, matching tree, hosted run, review and `0.1.0-dev.30` evidence; point to this plan and state that other recovery shapes remain open.
- [x] Update PLAT-001 with the exact saloon tagged-cache behavior and its limits, then record this next gap without claiming its recovery before tests prove it.
- [x] Advance only `Directory.Build.props` to `0.1.0-dev.31`; do not commit generated web identity output or introduce another authored version.
- [x] Stage and commit the plan/version/successor-evidence change before modifying behavior tests or production code; the normal check-only hook validates the staged candidate.

### Task 2: Recover omitted discovered-suspect identity from event history

**Files:** `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; `tests/WildBunch.Integration.Tests/GameSessionDifficultyPersistenceTests.cs`; `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`; `src/WildBunch.Domain/Game/GameSession.cs` only if authoritative event validation is needed for the fail-closed test.

**Consumes:** `CaseFileSnapshot` and `CaseFileGenerated`; `InvestigationPerformed` event application; `DeserializeCaseFile`; `GameSessionComponentPayloads`; `PersistedPayloadLoader`; aggregate, player-read and journal-read loaders; existing PostgreSQL test helpers.

**Produces:** PostgreSQL proof that a current-version casebook cache missing `discoveredSuspectIds` recovers the exact discovered identities from replay in each path that consumes the case file, preserves storage during reads, and remains repairable through an ordinary legal command.

Ruling: `DeserializeCaseFile` currently converts an omitted `DiscoveredSuspectIds` payload property into an empty list, which suppresses reconstruction. Current component JSON and legacy whole-session snapshots use separate deserialization routes, so the cache guard stays scoped to `DeserializeCaseFile`; independently tested snapshot defaults remain unchanged. Case generation records discovery state in `CaseFileGenerated`, and later clue discoveries replay from `InvestigationPerformed`. `GameSession.Apply(CaseFileGenerated)` explicitly rejects a missing discovery list so malformed history fails with a descriptive invalid-operation error. If this guard were omitted, the current incidental `ArgumentNullException` would still fail closed but obscure the invalid event field and leave rejection dependent on LINQ internals. Never manufacture an empty fact when history is invalid.

- [x] Persist a real started session with an independently captured discovered suspect, using the fixture's casefile-known identity and event stream; assert the generated event itself carries that discovered ID before damaging the cache.
- [x] Remove only `discoveredSuspectIds` from the persisted current `caseFile` component JSON while retaining the row, component version, envelope positions, events, and diary rows.
- [x] Add a PostgreSQL test that loads via the production command repository and player/journal read repositories, then asserts the exact discovered suspect ID/name (and any casebook field needed to make the assertion player-visible) is present.
- [x] Capture and compare the missing-field payload, component version, snapshot/stream positions, ordered event payloads/metadata, and diary rows before and after all read paths to prove reads do not write back.
- [x] Run the focused PostgreSQL test before production changes and confirm it fails specifically because the loaded casebook lacks the discovered identity; reject failures caused by setup or event decoding.
- [x] Add the minimal current-component shape validation for absent/null `discoveredSuspectIds`; explicitly present empty arrays remain valid. Confirm normal command and player/journal paths use existing typed invalid-cache recovery and full replay.
- [x] Add explicit validation in `GameSession.Apply(CaseFileGenerated)` only if the malformed authoritative-event test otherwise fails with an incidental null collection error; retain legacy whole-session snapshot behavior and the original event JSON/schema version.
- [x] Prove the positive test is sensitive by temporarily bypassing the guard and confirming it fails on the absent identity; restore the implementation and rerun.
- [x] Perform a legal purchase or other settled legal command after recovery, save normally, then verify a fresh aggregate load sees the repaired current component and exact discovered identity.
- [x] Damage the authoritative `CaseFileGenerated` payload's discovered-ID field while the component cache is malformed and prove command/player/journal loads fail closed instead of returning an empty or invented casebook.
- [x] Preserve the existing legacy whole-session snapshot defaults and prove an explicitly present empty discovery list still deserializes as empty; do not expand the change into general case-file validation.
- [x] Run focused integration coverage with `.\tools\postgres-dev.ps1 ensure` and `py -3 tools/run.py dotnet-test --check --verbose -- --filter "FullyQualifiedName~WildBunch.Integration.Tests.EfGameSessionRepositoryTests.<new-test-name>"`.
- [x] Commit the behavior proof and minimal production guard through the normal check-only hook.

### Task 3: Record bounded evidence and deliver the slice

**Files:** `docs/features.md`; `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`; this plan; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; test and serializer if changed.

**Consumes:** Focused PostgreSQL proof, ADR-0028, event-sourcing integrity guidance, backend architecture and code-review unslop profiles, feature-matrix and decision-record playbooks, and PR/code-review runbooks.

**Produces:** A precise PLAT-001 assessment for the omitted discovered-identity cache field and its exercised read/replay/repair/fail-closed boundaries, followed by a reviewed develop PR.

- [x] Add a dated persistence-test disposition limited to omitted/null current-cache `discoveredSuspectIds`, event-backed discovery restoration, all tested command/player/journal paths, read no-writeback, legal-save repair, explicit-empty validity, and fail-closed invalid history.
- [x] Update PLAT-001 with the verified behavior and clear limits; other CaseFile fields and optional/nested cache recovery remain open unless directly tested.
- [x] Compare the final diff with ADR-0028 and the decision-record playbook; leave the ADR unchanged if this remains implementation of its existing event-authority/cache-recovery decision.
- [ ] Confirm generated web identity reports `0.1.0-dev.31` without staging it, then run the canonical `py -3 tools/run.py ci --check` on the implementation candidate if the normal staged hook has not already validated that exact candidate.
- [ ] Complete a fresh whole-branch review against the plan, accepted specification, PLAT-001, relevant persistence findings, ADR-0028, event-sourcing integrity, selected backend/code-review unslop profiles, feature matrix and code-review runbook; resolve actionable findings before publication.
- [ ] Open a Draft PR to `develop`; verify body, base and exact remote head, mark ready after review and local validation, and require the hosted canonical gate on that exact head.
- [ ] Merge to `develop`; verify merge and hosted gate, fast-forward the primary checkout, and clean only the verified merged worktree, local/remote branch, and branch-scoped scratch.
