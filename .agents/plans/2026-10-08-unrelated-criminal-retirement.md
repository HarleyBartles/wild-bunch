# Unrelated-Criminal Retirement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Retire the incomplete unrelated-criminal feature from new hunts and live code while preserving gang/citizen behavior, event-backed retained play, and the applied database migration chain.

**Architecture:** New hunts stop generating unrelated wanted targets. A one-time, explicit pre-alpha data migration invalidates existing playthrough aggregates and their dependent event/cache rows; it does not drop a database, change schema history, or rewrite an applied migration. Once that migration boundary is in place, remove the unrelated ledger, settlement event, replay/projection effects, and persistence codecs while retaining shared warrant, sheriff, bounty, gang, and citizen behavior.

**Tech Stack:** C#/.NET domain and persistence, EF Core/PostgreSQL, GameContent generators, application projectors, xUnit integration and behavior tests, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially history and migration boundary, feature retirement, and retained bounty/investigation behavior; [feature matrix](../../docs/features.md), PG-009 and PG-009-R; [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 05 and per-plan delivery contract.

**Execution Strategy:** `executing-plans` with Native inline execution. The generator, domain, migration, persistence, and projection edits form one feature retirement and are ordered by behavior and data compatibility. One inline context avoids recreating the same aggregate/replay contract across sequential tasks; a single fresh whole-branch review will inspect the resulting vertical slice. The user authorized inline JIT execution for the roadmap.

## Global Constraints

- Start from the current `origin/develop` commit in the fresh row 05 worktree and deliver by PR to `develop`.
- Advance the sole authored application version in `Directory.Build.props` exactly once from `0.1.0-dev.9` to `0.1.0-dev.10` for this PR.
- Preserve the common gang/citizen/nobody saloon scope, gang warrants and payout, culprit secrecy/release rules, public noticeboard, and supported retained event facts.
- Remove unrelated targets as an incomplete 0.1.0 capability; do not implement the separately deferred PG-009-A feature or restore its old roster wholesale.
- Invalidate only existing pre-alpha playthrough aggregate data and dependent rows. Preserve database schema, applied migration history, and upcasters for retained facts. Never use `EnsureDeleted`, drop/recreate the database, or edit an applied migration.
- The SemVer policy is the current `0.y.z` policy: pre-1.0 adoption makes no public API or gameplay compatibility promise and does not define a future `1.0.0` contract. Row 18 will pin the accepted immutable AOM definitions current at that plan's authoring time.
- Tests must establish user-visible or persistence behavior, witness intended RED before implementation, and avoid absence-of-type/file tests or assertions that merely mirror implementation structure.
- Run focused tests at each task and `py -3 tools/run.py ci --check` for the final candidate. Keep PostgreSQL-dependent results explicit.

## Review Focus

- A newly generated case must expose only the generated gang/culprit warrants; reducing the candidate pool must not add filler targets or alter the hidden culprit gate.
- Retiring the ledger must not remove ordinary citizen mistakes, gang bounty settlement, duplicate-payout protection, or the event-derived wallet/journal effects for retained events.
- The data migration must remove all pre-alpha session aggregates and their dependent rows, preserve database schema and `__EFMigrationsHistory`, and permit a new session to be written afterward.
- Common wanted-poster and sheriff surfaces must remain usable with only retained warrants; no UI/API removal is justified solely by shared references to the old target kind.

---

### Task 1: Bootstrap the row 05 plan and retire the completed matrix plan

**Files:** Create this plan; modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-08-authoritative-feature-matrix.md`.

- [x] Verify PR #194 is merged to `develop` at `a22b09fd6951ac752583a69b17c07fa395c7fa24` and hosted canonical gate run `37797433775` passed on reviewed head `03b9763d7bcfaba0ab7cfa030e5abcac2b8f909a`.
- [x] Classify the full authoritative-feature-matrix plan against PR #194 and merged source. Confirm its matrix, lifecycle routing, and plan acceptance shipped; retain the matrix and live playbook, but remove the completed plan and stale roadmap link.
- [x] Mark roadmap row 01 done with PR #194, merge commit `a22b09f`, and `0.1.0-dev.9`; mark row 05 executing and link this plan with `0.1.0-dev.10` reserved. Do not mark broad row 05 done after this one PG-009 slice.
- [x] Advance `Directory.Build.props` exactly once to `0.1.0-dev.10`. Keep the already corrected SemVer statement in the spec and roadmap; do not add a 1.0.0 promise.
- [x] With the plan committed immediately before this task, stage the roadmap, version, and predecessor deletion; inspect the staged diff and commit through the check-only hook. Do not run the canonical full gate immediately before or after this ordinary hooked commit.

### Task 2: Remove unrelated warrants from new-hunt generation

**Files:** Modify `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs` and `src/WildBunch.GameContent/NewGame/CaseCharacterRoster.cs`; modify `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`, `tests/WildBunch.GameContent.Tests/GameSetupResolverTests.cs`, and `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs`; reconcile any generator guardrail that specifically depends on the removed pool.

- [x] In the existing seeded-new-game behavior test, replace the assertion for seven gang plus twenty-one unrelated warrants with an independent contract: generated public warrants correspond to the seven generated gang/culprit identities, and there is no separate non-gang bounty population. Run the focused test and record RED because current generation still adds the unrelated warrants.
- [x] Remove `UnrelatedWantedCriminals`, its salt selector and the unrelated-warrant selection path from `CaseCharacterRoster`; keep retained gang and associated-character pools and their deterministic behavior.
- [x] Remove unrelated warrant creation from `SeedCaseBuilder.CreatePublicWarrants`; retain the gang/culprit warrants and their identity/bounty facts. Do not compensate for the smaller set with filler targets.
- [x] Update setup and roster tests to assert only retained behavior, removing tests whose sole purpose is to freeze the removed candidate pool. Run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj` and verify GREEN.
- [x] Commit this task after reviewing the staged generator and test diff.

### Task 3: Add explicit pre-alpha playthrough invalidation

**Files:** Add one SQL-only migration under `src/WildBunch.Persistence/Migrations/` and any migration metadata required by the repository's EF migration discovery; modify `tests/WildBunch.Integration.Tests/MigrationTests.cs` and use `tests/WildBunch.Integration.Tests/TestInfrastructure/PostgreSqlTestDatabase.cs`.

- **Ruling:** The baseline explicitly permits discarding pre-alpha playthroughs while preserving the database and migration chain. Delete the existing `GameSessions` rows once and rely on their existing foreign-key cascades; the cost if this scope is wrong is loss of any pre-alpha playthrough still present when the migration first runs.
- [x] Add a PostgreSQL integration behavior test that migrates a temporary database only to `20260719061600_AddDiaryDaySchemaVersion`, stores a pre-alpha session, and inserts a travel-diary row. Assert the session, its components, stored events, and diary row are present before advancing the migration; then assert all are gone and `__EFMigrationsHistory` still contains the prior and new migration records.
- [x] Run the focused migration test through `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~PreAlphaPlaythroughsAreDiscardedByMigrationAndNewSessionCanBeStored` and record RED because the current migration chain leaves the seeded session intact.
- [x] Add a one-time EF migration whose `Up` deletes rows from `GameSessions` and relies on the existing foreign-key cascade for session components, stored events, and diary days. Do not drop any table, database, schema, or migration-history row. Make `Down` reject rollback with a clear `NotSupportedException`, because discarded playthroughs cannot be restored and silently marking the purge unapplied would leave false rollback semantics.
- [x] Extend the integration test to write and reload a newly created session after migration; then attempt migration to the prior ID and assert rollback is rejected, the new session remains stored, and the discard migration remains in `__EFMigrationsHistory`.
- [x] Run `dotnet tool restore`, `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`, and `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~PreAlphaPlaythroughsAreDiscardedByMigrationAndNewSessionCanBeStored`; confirm GREEN, inspect generated migration SQL for exact table scope, and commit the migration/test task.

### Task 4: Retire the live ledger, event, projection, and persistence codecs

**Files:** Modify `src/WildBunch.Domain/Cases/CaseWarrants.cs`, `src/WildBunch.Domain/Game/BountyLoop.cs`, `src/WildBunch.Domain/Game/BountyLoopContexts.cs`, `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, `src/WildBunch.Domain/Game/InvestigationLoop.cs`, `src/WildBunch.Application/Projections/HudProjector.cs`, `src/WildBunch.Application/Projections/TravelDiaryDayProjector.cs`, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, `src/WildBunch.Persistence/GameSessions/GameSessionComponentNames.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`, and `src/WildBunch.Persistence/Versioning/PersistedPayloadLoader.cs`; delete `src/WildBunch.Domain/Cases/UnrelatedCriminalLedger.cs`, `src/WildBunch.Domain/Events/UnrelatedCriminalTurnInSettled.cs`, and `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.UnrelatedCriminalLedger.cs`. Modify the affected domain, application, and integration tests named below.

- [x] Remove the ledger from new session initialization, snapshot composition, repository writes/reads, event replay rebuilding, retired-warrant investigation filtering, and bounty settlement side effects. Remove `UnrelatedCriminalTurnInSettled`, its direct aggregate settlement method/context, event deserialization, HUD/diary projection branches, and the now-unused target kind. Do not change common gang settlement or citizen/wrong-declaration behavior.
- [x] Remove the ledger-only tests in `tests/WildBunch.Domain.Tests/UnrelatedCriminalLedgerTests.cs`, `tests/WildBunch.Domain.Tests/GameSessionUnrelatedCriminalLedgerWiringTests.cs`, `tests/WildBunch.Integration.Tests/UnrelatedCriminalLedgerPersistenceTests.cs`, and the ledger-specific full-replay fixture in `tests/WildBunch.Integration.Tests/FullReplayEqualityTests.cs`. Correct generic warrant fixtures in `tests/WildBunch.Domain.Tests/BountySettlementPolicyTests.cs`, `tests/WildBunch.Domain.Tests/CaseInvestigationFoundationTests.cs`, `tests/WildBunch.Domain.Tests/CaseFileTests.cs`, `tests/WildBunch.Domain.Tests/WantedPosterResolverTests.cs`, `tests/WildBunch.Domain.Tests/GameSessionResolverWiringTests.cs`, `tests/WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs`, `tests/WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs`, `tests/WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs`, and `tests/WildBunch.Integration.Tests/GameSessionDifficultyPersistenceTests.cs`. Inspect `tests/WildBunch.Application.Tests/Mappers/JournalMapperTests.cs`, `tests/WildBunch.Application.Tests/Mappers/CaseBoardMapperTests.cs`, `tests/WildBunch.Application.Tests/Handlers/TurnInToSheriffHandlerTests.cs`, and `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs` for target assumptions. Use retained identities and semantics.
- [x] Preserve meaningful behavior coverage for generated gang warrants, public poster/source refresh, citizen mistaken identity, culprit secrecy/release, one-time gang payout, and event replay of retained wallet/journal effects. Reuse or strengthen existing behavior tests only where they lack a real retained outcome; do not add a test asserting that a deleted type or file is absent.
- [x] Update `tests/WildBunch.Integration.Tests/Versioning/ProjectionVersionCompletenessTests.cs`, `tests/WildBunch.Integration.Tests/MigrationTests.cs`, and `tests/WildBunch.Integration.Tests/PostgreSqlPersistenceTests.cs` so component inventories reflect retained live components, while continuing to save and reload a new session after the invalidation migration.
- [x] Search all `src` and `tests` for `UnrelatedCriminalLedger`, `UnrelatedCriminalTurnInSettled`, and `UnrelatedWantedCriminal`; inspect every remaining hit and remove only stale executable branches or test dependencies. Retain authored creative material only in Git history; PG-009-A must revalidate and re-author against future requirements rather than restore the old pool wholesale.
- [ ] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`, `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`, and `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj`; verify retained gang payout and replay outcomes and rerun the migration test. The command bus supplies the configured PostgreSQL test connection. Commit the code and behavior-test task.

### Task 5: Reconcile product truth and deliver the reviewed slice

**Files:** Modify `docs/features.md`, `.agents/investigations/stable-0.1.0/2026-10-07-unrelated-criminal-removal.md`, and only any ADR or test-disposition record that the actual source diff makes stale.

- [ ] Update PG-009 and PG-009-R to record the retired 0.1.0 shell, retained shared gang/citizen/wanted behavior, and PG-009-A as a separate future feature requiring fresh design. State that the migration invalidates all pre-existing pre-alpha playthrough rows and their dependent events/caches while preserving schema and migration history.
- [ ] Record creative-content custody: the executable unrelated roster is removed from current code; prior authored material remains recoverable from Git history and is not a promised reusable candidate pool. The future addition must revalidate/re-author it.
- [ ] Read the decision-record playbook and review ADR-0025 and the ADR catalogue against the final diff. Its existing dated retirement note remains truthful; add or supersede a decision only if the implementation reveals a durable architectural claim that the existing record does not cover.
- [ ] Reconcile relevant test-followup references so no current test plan depends on retired ledger semantics; preserve independent negative/behavioral coverage for retained contracts. Do not introduce empty headings or shape-only checks.
- [ ] Run focused tests, then `py -3 tools/run.py ci --check`. Resolve all failures without weakening the canonical gate; inspect migration output, `git diff --check`, changed paths, complete staged diff, and the final version value.
- [ ] Request one fresh whole-branch review against this plan, the baseline spec, relevant ADRs, the feature matrix, and the review runbook. Fix every Critical/Important finding with a witnessed behavior RED/GREEN cycle and rerun the canonical gate on the resulting candidate.
- [ ] Refresh `origin/develop` immediately before publication and assign the next unique `0.1.0-dev.N` based on its merged version if another PR advanced the line; rebuild/review any changed candidate rather than publish a duplicate checkpoint.
- [ ] Create a Draft PR targeting `develop`, verify the required hosted canonical gate passes on the exact reviewed head, then merge it using the repository's Gitflow procedure. Record actual commit, PR, and hosted-check evidence in the roadmap; leave row 05 executing because other excluded capabilities remain for later JIT slices.

## Completion Conditions

- New hunts expose only the retained gang/culprit bounty identities and no unrelated bounty population.
- The one-time migration explicitly removes pre-alpha playthrough data and dependent rows without database/schema/migration-history destruction; new sessions remain writable and reconstructible.
- No unrelated-criminal producer, settlement path, ledger, projection branch, serializer, or test-only compatibility path remains live. Common retained player behavior and event replay remain tested.
- The feature matrix, removal record, ADR assessment, and roadmap report current truth and actual delivery evidence.
- The canonical gate and fresh whole-branch review pass on the actual PR head, and the PR is merged to `develop` with one development version advancement.
