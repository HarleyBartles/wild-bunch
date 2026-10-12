# Row 17 N-07 Source-Shape Detector Retirement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline.

**Goal:** Remove source-inspection tests that freeze Persistence implementation details, while retaining meaningful PostgreSQL proof that persisted journal reads derive their entries from the event stream.

**Architecture:** Persistence owns the journal read path and derives `LogEntries` from stored domain events through `JournalLogProjector`. The existing `EfGameSessionRepositoryTests.ReadRepositoriesProjectComposedSessionAndJournalViews` exercises command and journal repositories against real PostgreSQL and verifies the journal projection. Application tests must not inspect Persistence source text to assert that implementation. The investigation's cited IL-opcode archive test is absent from the current repository and must not be recreated.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.Application.Tests`, `WildBunch.Integration.Tests`, PostgreSQL, and the repository command bus.

**Specification:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; residual test finding N-07 is recorded in `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`.

**Execution Strategy:** `executing-plans` inline. Native execution fits because the behavior mutation, detector removal, investigation disposition, version bump, roadmap update, and predecessor retirement share one evidence chain and one substantive commit; splitting them among implementers would add handoffs without independent progress. A fresh whole-branch review remains the independent check. This is one bounded test-quality slice; do not change production code, schema, or user behavior.

## Global Constraints

- Start from refreshed `origin/develop` in the dedicated `codex/row17-n07-remove-shape-detectors` worktree at base `5549fb8d7ea3efa57de55463d5c90263c40cbe68`.
- Keep `Directory.Build.props` as the only authored application version. The plan-only commit does not bump it; the first substantive implementation commit advances `0.1.0-dev.121` to `0.1.0-dev.122` exactly once.
- In that first substantive commit, retire `.agents/plans/2026-10-12-row17-discovered-suspect-projection-proof.md` and update the row-17 roadmap with N-06 delivery evidence: PR #306 reviewed head `57aad2cbd2ceba50995c24159023875253571a58`, squash merge `5549fb8d7ea3efa57de55463d5c90263c40cbe68`, matching tree `9afff279d517f2a12d945e6f9adc4863ad584b20`, exact-head gate run `38298581961`, develop-push gate run `38299107567`, and paired worktree/local/upstream ref cleanup verified absent. Mark N-07 as the active `.122` successor; row 17 remains incomplete.
- Remove `tests/WildBunch.Application.Tests/Guardrails/ReadStoreLoaderJournalProjectionGuardrailTests.cs`; its comments can satisfy positive source-string assertions, refactors can break them, and Persistence internals are outside the Application test boundary.
- Preserve the existing PostgreSQL behavior test `ReadRepositoriesProjectComposedSessionAndJournalViews`. Before deleting the guardrail, temporarily make `GameSessionReadStoreLoader` return an empty journal projection and prove that integration test fails on its journal assertions; restore production code and prove the test passes. Do not commit the mutation or add a test whose purpose is to assert source strings, schema absence, or exact implementation shape.
- Update `GameLogEntryLegacyProjectionTests` commentary so the legitimate pure projector test no longer claims the retired source guardrail proves the persistence read path; name the existing PostgreSQL integration behavior test instead.
- Date the N-07 disposition in the investigation: retire the source-string detector, retain the existing PostgreSQL journal-read behavior proof, and record that a repository-wide current-tree search found no cited archive IL-opcode test. Do not infer when or why the cited test disappeared.
- ADR-0028 remains truthful: event history is authoritative and journal views are event-derived projections. No ADR or feature-matrix change is expected.
- Never use `--no-verify`; commit only through the normal staged-snapshot hook and fail-fast repository gate.

## Review Focus

- Removing the source detector does not remove meaningful proof that the persisted journal repository returns the event-derived entries from PostgreSQL.
- No IL-opcode/source-shape detector is reintroduced while reconciling the historical N-07 finding.
- The dated finding distinguishes verified current-tree absence from unknown historical cause.

## Task 1: Record and commit the JIT plan

**Files:**
- Create `.agents/plans/2026-10-12-row17-source-shape-detector-retirement.md`

- [x] Confirm N-07 against the current Application guardrail, PostgreSQL integration test, event-derived loader, ADR-0028, and the current source tree.
- [x] Commit this plan alone without changing product version or implementation.

## Task 2: Retire the source detector and preserve behavioral proof

**Files:**
- Delete `tests/WildBunch.Application.Tests/Guardrails/ReadStoreLoaderJournalProjectionGuardrailTests.cs`.
- Modify `tests/WildBunch.Application.Tests/Projections/GameLogEntryLegacyProjectionTests.cs`.
- Modify `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`.
- Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`.
- Delete `.agents/plans/2026-10-12-row17-discovered-suspect-projection-proof.md`.
- Modify `Directory.Build.props`.

- [x] Run `.\tools\postgres-dev.ps1 ensure`, then run the existing PostgreSQL test `ReadRepositoriesProjectComposedSessionAndJournalViews` and inspect its output.
- [x] Temporarily replace the journal projection in `GameSessionReadStoreLoader` with an empty result; rerun that focused integration test and confirm it fails because the expected journal entries are missing. Restore the production line and rerun the test successfully.
- [x] Delete the source-inspection guardrail and revise only the stale commentary that directed readers to it. Preserve the pure `JournalLogProjector` behavior test and the PostgreSQL integration test.
- [x] Add the dated N-07 disposition without rewriting the original observation or inventing why the IL test is absent.
- [x] Record PR #306's N-06 merge, validation, matching-tree, and paired-cleanup evidence in roadmap row 17; select N-07 as active `.122`, preserve the remaining findings, and do not mark row 17 complete.
- [x] Retire the N-06 plan in this same first substantive commit and advance the version once to `0.1.0-dev.122`.
- [x] Run the focused PostgreSQL integration command and `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`; compare each result with expected behavior. Commit through the normal staged-snapshot hook, which runs the canonical fail-fast `ci --check` gate; do not redundantly run the full gate immediately before or after a successful hooked commit.

## Task 3: Review, publish, merge, and retire the execution unit

**Files:**
- Review the actual branch diff against ADR-0028, the feature matrix, the N-07 disposition, and the Review Focus above.

- [ ] Independently inspect that the integration test still checks the journal result and would fail if event-derived journal projection were removed; resolve any truth discrepancy before publication.
- [ ] Obtain a fresh whole-branch review and resolve actionable findings with focused re-review.
- [ ] Publish a PR to `develop`, confirm the author and reviewer ADR checks, mark it ready, and require the exact-head canonical hosted gate to pass.
- [ ] Squash merge under the authorized epic flow; verify the develop-push gate passes and the merged tree equals the reviewed tree.
- [ ] Fast-forward the primary checkout. Verify the merged PR head, remove the clean worktree first, explicitly delete its exact local and upstream refs, and verify all three are absent before creating another plan worktree.
- [ ] Preserve later row-17 findings for JIT successor plans; do not claim row 17 complete.
