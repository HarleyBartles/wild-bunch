# N-37 Replay Tests Use the Original Event History Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make Domain replay tests reconstruct state from the exact event history produced by their command session, with original genesis facts and no manufactured or duplicated setup events.

**Architecture:** `GameSession.AllEvents` is the complete ordered history of committed and uncommitted facts. Replay tests copy that stream once and replay it directly. Test fixtures must not recreate a baseline `CaseFile` or re-run game setup from the player's current mutable state. If a test's direct setup mutations cannot be reconstructed from its event facts, correct the fixture or narrow the replay claim.

**Tech Stack:** C# 14, .NET 10, xUnit, Wild Bunch Domain event sourcing.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), PG-007 and row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), N-18 in the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), and ADR-0028.

**Execution Strategy:** Native inline execution via `executing-plans`. The affected helpers are one shared replay-history seam used by six closely related Domain test classes. Migrating them together allows the full event stream to be checked consistently and keeps the removed reconstruction helper from surviving as a second truth source.

## Global Constraints

- N-36 merged as `0.1.0-dev.154`; advance the sole authored application version in `Directory.Build.props` to `0.1.0-dev.155` exactly once.
- Preserve strict CQRS, event-sourced mutation, immutable event facts, deterministic event application, and event-stream-only replay reconstruction.
- Keep the change within Domain test fixtures, replay tests, and their test-quality audit. Do not change production behavior, event contracts, gameplay, ADRs, or product promises.
- Retire the completed N-36 plan only in this successor's first substantive commit; keep this N-37 plan through its completing PR.
- Use a fresh worktree from `origin/develop`, target the PR at `develop`, pass independent review and exact-head hosted CI before merge, verify the develop-push gate, then remove the worktree and its paired local/upstream refs.

## Task 1: Record N-36 delivery and commit the N-37 successor plan

**Files:**
- Create: `.agents/plans/2026-10-14-row17-n37-original-event-history.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n36-lucky-water-effect.md`

- [x] Verify PR #339 merged as `6eb63ff29aadc779ef0ba2e35f3892894946e12c` from reviewed source `29c6cf76acb64a29dc88e5d525e3e85895371ec8`, both trees match `5845b23bd7e7a6bb3e1f0c312947e295e9fa1565`, fresh review approved without actionable findings, exact-head gate 38479808604 and develop-push gate 38480458740 passed, the primary checkout is current, and the exact N-36 worktree and branch refs are absent.
- [x] Retire the N-36 plan, record PR #339's source/merge/tree identities, review, both gates, and paired cleanup in row 17, name N-37 as the active successor, and set `Directory.Build.props` to `0.1.0-dev.155`.
- [x] Review the staged plan-transition diff and commit it before changing replay fixtures or tests.

## Task 2: Replay the exact event stream from each test session

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelTestFactory.cs`
- Modify: `tests/WildBunch.Domain.Tests/TestSessionFactory.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionDevEntropyTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionDevDifficultyTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/DevTravelOverrideTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/EventSourcing/TravelReplayEqualityTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/Projections/TravelDiaryDayProjectorParityTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelResourceSettlementTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

- [ ] Replace replay setup reconstruction with an array copy of the actual ordered stream, `session.AllEvents.ToArray()`. In tests where only the genesis facts are needed before an action, capture the original `AllEvents` before that action; build the final replay from `AllEvents` once after the command. Do not concatenate a regenerated setup stream with `CommittedEvents` or `UncommittedEvents` because `AllEvents` already contains the complete committed-plus-uncommitted history.
- [ ] Migrate `GameSessionDevEntropyTests` and `GameSessionDevDifficultyTests` to retain the original `GameStarted` fact before mutation, assert its original Classic/Easy value, replay the actual final `AllEvents`, and require the matching dev fact, final changed value, and replayed version to equal the command session version.
- [ ] Migrate `DevTravelOverrideTests`, `TravelReplayEqualityTests`, and `TravelDiaryDayProjectorParityTests` to replay one exact `AllEvents` copy. Remove the `WithSetupEvents` wrapper factories when their only purpose is to call `RecaptureSetupEventsForReplay`; preserve existing field-level command/replay assertions.
- [ ] Migrate all three replay cases in `TravelResourceSettlementTests` to the exact event stream. Preserve each test's resource-loss or theft behavior assertions. If replay exposes test-only inventory or journey mutations that are absent from the original event history, move those initial values into canonical fixture creation or otherwise represent the setup with the existing evented command path; do not manufacture a new `GameStarted` event from the already-mutated player.
- [ ] Delete `TravelTestFactory.RecaptureSetupEventsForReplay`, its `Create...WithSetupEvents` wrappers, and the now-unused `TestSessionFactory.CreateBaselineCaseFileFor` plus public clue/warrant reconstruction helpers. Confirm no callers remain with a repository search; do not add a test that asserts helper/file absence.
- [ ] Prove the replay-history assertion is meaningful: temporarily duplicate the original `GameStarted` fact in the entropy replay input and verify the replay-version equality assertion fails, then restore the test source. Run the focused entropy replay test green afterward.
- [ ] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~GameSessionDevEntropyTests --logger "console;verbosity=quiet"`, the equivalent filters for `GameSessionDevDifficultyTests`, `DevTravelOverrideTests`, `TravelReplayEqualityTests`, `TravelDiaryDayProjectorParityTests`, and `TravelResourceSettlementTests`, then run the full Domain test project through the staged-candidate hook.
- [ ] Update N-18 to say the shared replay-fixture gap is closed by replaying original ordered aggregate history once, while retaining any separate unresolved replay findings. Update the `TestSessionFactory`, `TravelTestFactory`, and replay-test inventory rows. Record why ADR-0028 and PG-007 remain truthful and why no feature-matrix or ADR change is required.
- [ ] Review the test and audit diff, then commit through the staged-candidate hook.

## Task 3: Review and deliver N-37

- [ ] Compare the final diff with ADR-0028, PG-007, N-18, and the feature matrix through their playbooks; apply the backend-architecture and code-review unslop profiles plus testing and writing guidance, and state why product behavior and durable decisions remain unchanged.
- [ ] Build the whole-branch review package and dispatch a fresh-context independent reviewer; resolve material findings with a witnessed failing test and focused correction.
- [ ] Open a Draft PR to `develop`, read back and verify its exact head and description, then mark it ready after independent review so hosted CI runs on the reviewed head.
- [ ] Wait for exact-head hosted CI to pass, squash-merge the N-37 PR, verify its merge tree and successful develop-push gate, then fast-forward the primary checkout.
- [ ] Remove the clean N-37 worktree, delete only its matching local and upstream branch refs, and verify all three are absent before starting another plan.

## Review Focus

- Every replay assertion must consume the original complete fact sequence once, preserving the original `GameStarted` entropy, difficulty, seed and salt instead of synthesizing a plausible new setup.
- Version equality must distinguish the actual stream from a duplicated or dropped setup/change fact; it is meaningful only alongside domain-state equality.
- Tests that mutate aggregate state directly must not claim event replay reconstructs that state unless a recorded fact establishes it.
- Removing test helper code must not remove any independent behavior proof for theft, resource loss, journey progression, dev override consumption, diary projection, or arrival.
- Keep the review scoped to test truth and fixture ownership; do not change runtime game behavior to accommodate a reconstructed test baseline.
