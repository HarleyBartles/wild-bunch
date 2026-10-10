# Batch Stale Component Cache Rebuild

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Rebuild one event-derived session per coherent load when multiple stored component versions are stale, then use that same reconstruction to provide every requested stale component payload.

**Architecture:** A component payload read scope is created once for each command-side or player/journal read. Current-version payloads continue to come from the stored cache; the scope lazily reconstructs one `GameSession` from the already-loaded ordered event stream when the first stale component is requested, then serializes requested stale components from that same aggregate. Keep the scope request-local, preserve missing-component and current-version corruption behavior, and leave read paths write-free.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; row 07 of `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-11 and its test disposition in the persistence investigation and test follow-up.

**Execution Strategy:** `executing-plans`, inline and sequential. The command and read loaders must adopt one shared per-load component-payload scope; an independently reviewed task split would add handoff cost without separating behavior or implementation context.

## Global Constraints

- Start from merge `4f96147b92986f88781df7e258b4e5bf5b4634ab` on `develop` in this fresh linked worktree and target `develop`.
- Commit this JIT plan and retire the completed `.48` plan before source edits; record PR #233 and its exact source, merge and hosted gate evidence in row 07.
- Advance `Directory.Build.props` once from `0.1.0-dev.48` to `0.1.0-dev.49`.
- Rebuild from ordered, upcasted event facts only; never re-roll or invent state.
- Preserve healthy-cache fast paths, fail-closed authoritative history, event and payload formats, query no-writeback, and legal-save cache repair.
- Create no ADR or feature-matrix update unless implementation evidence shows a durable decision or player-facing promise changed.

## Review Focus

- **Multiple stale components in one load:** all requested stale payloads come from one event reconstruction, while each later load receives its own reconstruction scope.
- **Healthy components:** current-version JSON is still used without invoking event reconstruction.
- **Read ownership:** player and journal reads may rebuild in memory but leave component rows, envelope versions, and event history unchanged.
- **Mixed optional and required caches:** missing-row fallback, current-version malformed-cache recovery, and cache-version dispatch remain unchanged; Task 2 reruns the existing missing-component, malformed-town-visit read recovery, and current-version direct-load tests.

---

### Task 1: Record PR #233 and commit this `.49` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-simplify-session-rebuilder.md`; create this plan.

**Interfaces:** Record PR #233 source `844a0dd4fcca2501b8c7acd69e9af5ae873cd11c`, merge `4f96147b92986f88781df7e258b4e5bf5b4634ab`, exact-head PR gate run `38006131985`, develop push gate run `38006418496`, and delivered identity `0.1.0-dev.48`. Row 07 remains executing and points to this `.49` plan.

- [x] Verify PR #233 is merged to `develop` at the stated source and merge SHAs and both hosted gates passed on those exact commits.
- [x] Record PR #233's `.48` rebuilder cleanup and disclosed self-review outcome in row 07; append PR #233 to its merged-PR list.
- [x] Select repeated stale-component reconstruction as the next bounded PS-11 slice; leave ownership-bound status queries and historical migration deployment policy with their roadmap owners.
- [x] Advance `Directory.Build.props` to `0.1.0-dev.49`, retire the completed `.48` plan and stale row pointer, and commit this plan before source edits.

**Expected:** Delivery evidence is exact, the `.48` plan is retired, and this committed plan is row 07's live pointer.

### Task 2: Prove and coalesce repeated stale-component reconstruction

**Files:** Modify `src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs`, `src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs`, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, `src/WildBunch.Persistence/GameSessions/GameSessionReadStoreLoader.cs`, and `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated PS-11 dispositions to `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Interfaces:** Add one per-load component-payload read scope to `PersistedPayloadLoader`, with a `GetPayload(componentName)` operation. Both persistence loaders create exactly one scope after loading components and upcasted events, and every required/cache/optional component lookup for that `GameSessionStore` uses it. The scope returns missing as null, returns current-version JSON directly, and lazily memoizes one rebuilt `GameSession` plus the serialized payloads requested from it when a stale component version is first read. Keep any direct single-component convenience API by delegating it to a fresh scope.

- [x] Add a PostgreSQL integration test that persists a real session, marks at least the seven required component rows (`player`, `world`, `caseFile`, `clock`, `pursuitState`, `setup`, `saltSource`) stale, and loads it through `EfGameSessionRepository` with a counting rebuild callback. Assert one rebuild, independently expected player/world/case/clock facts, a successful purchase, and a fresh reload with the purchase persisted. Run it first and confirm it fails because the current code rebuilds once per stale component.
- [x] Add a PostgreSQL read-side test with the same stale component set and separate player/journal loads. Assert one rebuild for each load, correct wallet/status/log facts, and unchanged stale component versions, payloads, envelope watermarks, and event rows after both reads.
- [x] Introduce the per-load read scope and update `EfGameSessionRepository` and `GameSessionReadStoreLoader` so every component lookup within one `GameSessionStore` uses that single scope. Do not place mutable memoization on the singleton `PersistedPayloadLoader`.
- [x] Preserve the existing single-component loader tests by delegating their call to a fresh read scope. Preserve missing-component handling and current-version invalid-cache fallback without changing event, snapshot, diary or schema contracts.
- [x] Run the focused new command/read tests together with `LoadComponentPayload_StaleVersion_TriggersRebuildFromEvents`, `LoadComponentPayload_CurrentVersion_UsesStoredJson`, `LoadComponentPayload_MissingComponent_ReturnsNull`, `ReadModel_MalformedTownVisitWantedSuspectShapeRecoversFromEvents`, fail-closed cache recovery, and full replay tests.
- [x] Append dated investigation/test dispositions that state the repeated-rebuild symptom, one reconstruction per coherent component load, the real cache paths covered, and the preserved write/no-write boundaries.
- [x] Recheck ADR-0028 and `docs/features.md`; leave them unchanged if cache authority and player-facing behavior remain as specified.

**Expected:** One stale version transition reconstructs one `GameSession` per command, player-read, or journal-read load; current payloads remain on the normal cache path and all reads leave durable state unchanged.

### Task 3: Validate, review and publish to `develop`

- [ ] Run the focused behavior and recovery tests and compare their results with the independent persisted-event expectations.
- [ ] Run the canonical fail-fast `py -3 tools/run.py ci --check` gate on the staged candidate.
- [ ] Inspect the full branch diff, stale/current/missing component paths, per-load ownership, command and read consumers, existing behavior tests, PS-11 dispositions, ADR-0028, feature matrix, and backend/code-review unslop profiles. Use and disclose the self-review fallback if reviewer-agent dispatch is unavailable.
- [ ] Publish a Draft PR to `develop`, reconcile its remote head with local `HEAD`, correct and reread the PR body, mark it ready, and require the hosted canonical gate to pass on that exact head before merging.
- [ ] Merge to `develop`, verify the push gate passes on the exact merge SHA, fast-forward the main checkout, and retire this plan in the next substantive successor after verifying delivery evidence.

**Expected:** `.49` is merged to `develop` with exact-head hosted validation; each stale-component load reconstructs once without weakening cache, replay, or read-safety behavior.
