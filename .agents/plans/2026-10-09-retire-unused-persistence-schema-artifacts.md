# Retire Unused Persistence Schema Artifacts

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove the unused game-session envelope schema version and the duplicate stored-event sequence index through a forward migration, while preserving event identity/sequence constraints and usable persisted sessions.

**Architecture:** The GameSessions envelope version has no read-side consumer; component, event, and diary-day schema versions remain because their loaders use them. Stored events retain their `(StreamId, Sequence)` primary key and unique `EventId` index. Historical migrations remain immutable; a new migration removes only the dead envelope column and redundant index.

**Tech Stack:** .NET 10, EF Core, PostgreSQL, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery” and “History and migration boundary”; row 07 of `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; persistence finding PS-15 and its test follow-up.

**Execution Strategy:** `executing-plans`, inline and sequential. First commit the JIT plan and retire its completed predecessor; then update the EF model and add a forward migration; prove migration and real repository behavior; run the required persistence and canonical gates; self-review because this runtime does not permit dispatching a separate reviewer agent.

## Rulings

- Remove `GameSessionEntity.SchemaVersion`, its EF mapping, and repository writes. Do not remove component, event, or diary-day schema versions or their upcasting/loading behavior.
- Remove the unique index on `(StreamId, Sequence)` because the same columns and order are the primary key. Preserve the composite primary key and unique `EventId` index.
- Add a new migration that drops the existing duplicate index and envelope column. Preserve every historical migration file and existing session/event data.
- Remove the migration test's assertion that the dead `SchemaVersion` column exists. Do not replace it with an absence assertion or source-shape detector. Keep greenfield migration and the repository save/load behavior assertions.
- No ADR or feature-matrix update is expected: this removes unused persistence metadata and a duplicate constraint representation without changing event authority, supported gameplay, or a durable storage contract.
- Keep unbounded session-load work and `SessionRebuilder` cleanup outside this slice; they remain distinct PS-15 work.
- Advance `Directory.Build.props` once from `0.1.0-dev.46` to `0.1.0-dev.47`.

## Global Constraints

- Work from the clean `develop` merge `59ade964450b8b1498a265cdcd2062a294587006` in the fresh linked worktree and target `develop`.
- Commit this plan before changing implementation code. Retire the completed `.46` plan only after recording PR #231's source/merge identities and hosted gate evidence.
- Never rewrite or delete historical migrations. Do not change event payloads, event ordering, replay, cache recovery, projection versions, or current session behavior.
- Preserve the forward migration's reversibility where EF supports restoring the dropped metadata; no data-bearing state is being discarded by this migration.
- Keep the roadmap live through PR merge. The next substantive successor retires this plan after verifying exact source, merge and hosted gate evidence.

## Review Focus

- No runtime path reads or requires the removed envelope schema version; real repository persistence still stores and reloads a session.
- The final model and applied PostgreSQL schema retain the event sequence primary key and EventId uniqueness while the duplicate sequence index is absent from the model through migration behavior, not a new negative structure test.
- Forward migration works on an existing schema with data and greenfield migration continues to work.
- All historical migrations remain untouched; the snapshot and migration designer reflect the current model.
- Decision-record and feature-matrix review confirms the slice does not alter durable or player-facing truth.

---

### Task 1: Record PR #231 and commit this `.47` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-09-retire-test-only-session-json-snapshot.md`; create this plan.

**Interfaces:** Record PR #231 source `c05cc9a1d7eb24d217969642f586c0ca3b066f59`, merge `59ade964450b8b1498a265cdcd2062a294587006`, hosted PR gate run `37999768372`, develop push gate run `38000263265`, and delivered identity `0.1.0-dev.46`. Row 07 remains executing and points to this `.47` plan.

- [x] Verify PR #231 is merged to `develop` at the stated source and merge SHAs and both hosted gates passed on those exact commits.
- [x] Compare the completed `.46` plan with merged source and evidence; record its scope and review outcome in row 07 and append PR #231 to the merged PR list.
- [x] Select the unused session schema field and duplicate event sequence index as this successor's bounded PS-15 slice; leave load cost and `SessionRebuilder` concerns open.
- [x] Advance `Directory.Build.props` to `0.1.0-dev.47`, retire the completed `.46` plan and its stale row pointer, and commit this plan before source edits.

**Expected:** Delivery evidence is accurate, the completed predecessor is retired, and the committed `.47` plan is the live row 07 pointer.

### Task 2: Remove only the unused schema artifacts from the current model

**Files:** Modify `src/WildBunch.Persistence/GameSessions/GameSessionEntity.cs`, `GameSessionEntityConfiguration.cs`, `EfGameSessionRepository.cs`, and `StoredEventEntityConfiguration.cs`.

- [x] Recheck all repository consumers of `GameSessionEntity.SchemaVersion` and the duplicate `(StreamId, Sequence)` unique index. Confirm other schema versions and both required event constraints have live consumers or behavior.
- [x] Remove the envelope version property, mapping and assignments/constant. Remove only the redundant unique-index configuration.
- [x] Generate a forward EF migration and inspect its Up/Down operations. It drops only the old duplicate index and `GameSessions.SchemaVersion` column, restores them on Down with the former writer's version `1`, and leaves historical migrations unchanged.
- [x] Update the model snapshot and generated migration designer through EF tooling.
- [x] Remove the `Assert.Contains("SchemaVersion", columns)` change-detector assertion from `MigrationTests.MigrationsCreateGameSessionsTableAndRoundTripSession`; retain the actual migration, save, load, player-state and component assertions. Do not add absence checks.

**Expected:** The current EF model has no unused envelope version or duplicate index; event sequence and EventId uniqueness remain protected by their existing constraints.

### Task 3: Verify forward migration and persisted session behavior

**Files:** Update `tests/WildBunch.Integration.Tests/MigrationTests.cs` only as required to preserve meaningful behavior coverage; append dated implementation dispositions to the PS-15 entries in `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

- [x] Run the greenfield migration and repository round-trip test against PostgreSQL after ensuring the repository's shared PostgreSQL service.
- [x] Run the existing destructive-transition migration scenario through the new migration and prove a fresh session is stored and loaded afterward; retain its pre-migration fixture at the schema version where it belongs.
- [x] Run `dotnet tool restore` and `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`; the integration migration scenario applies the new migration and reloads its stored session.
- [x] Record exactly which PS-15 subfindings this slice closes and which remain open. No new structural tests or receipts.
- [x] Recheck applicable ADRs and `docs/features.md`; leave them unchanged because no durable or player-facing truth moved.

**Expected:** Existing data can pass through the additive-history forward migration, and a current session remains usable through the production repository.

### Task 4: Validate, review and publish to `develop`

- [x] Run focused migration and repository behavior tests, then the canonical fail-fast `py -3 tools/run.py ci --check` gate on the exact staged/committed candidate.
- [x] Inspect the full diff, migration Up/Down, generated model snapshot, test evidence, investigation dispositions, ADRs, feature matrix and unslop profile for the actual changed surface. Record the self-review fallback due to runtime subagent restrictions.
- [ ] Publish a Draft PR to `develop` and verify the PR head matches local `HEAD`. PR jobs run only after the PR is marked ready, so require hosted canonical CI to pass on that exact head before merge.
- [ ] After merge, verify the develop push gate passes on the merge SHA. Leave the plan and roadmap for the next successor to retire after verifying evidence.

**Expected:** `.47` is merged to `develop` with exact-head review and passing hosted PR and develop gates; the persisted event stream and session repository remain authoritative and usable.
