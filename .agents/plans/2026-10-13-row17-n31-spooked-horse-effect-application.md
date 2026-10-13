# N-31 Mounted Spooked-Horse Effect Application Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove that a naturally generated mounted `BadLuckSpookedHorse` event applies its positive exhaustion to the horse, persists the resulting state, and reconstructs it exactly from the event stream.

**Architecture:** Exercise the existing `GameSession` travel command path, with Domain remaining the owner of travel outcomes and event application. This is test and audit correction only; no production behavior, feature promise, event shape, or architecture changes are expected.

**Specification and evidence:** The stable 0.1.0 baseline specification, row 17 roadmap, N-18 test-quality investigation, and `TravelResolverTests` disposition govern scope. The Domain unslop profile governs event-sourced travel and behavior-test quality. ADR-0028 and ADR-0029 remain applicable; PG-007 remains partial and unchanged.

**Governing decisions:** A reached mounted spooked-horse event records positive horse exhaustion; travel applies that recorded effect to the horse state; the committed event and its absolute journey snapshot reconstruct the same state on replay. No reroll or effect inference is part of replay.

**Execution Strategy:** Native inline execution. The two implementation tasks are sequential: the versioned successor plan establishes scope, then one focused Domain test and its audit disposition prove one command/event/replay path. Keeping the cycle inline avoids reconstructing its seed, state and mutation evidence between handoffs; a fresh whole-branch review remains the independent check.

## Global Constraints

- Preserve the user-approved stable 0.1.0 contract and the current strict event-sourcing, CQRS and replay boundaries.
- Each merged plan PR advances the single authored application version exactly once; N-31 uses `0.1.0-dev.149`.
- Commit the successor plan/version before implementation; the successor retires this completed plan in its first substantive commit.
- Keep the test deterministic with runtime salt `n31-horse-trouble-1`; do not commit seed search, conditional assertions, or temporary mutation code.
- Do not add a feature-matrix or ADR change because this test correction makes no product or durable architectural decision change.
- A test-quality slice closes only behavior it directly proves; preserve the no-horse, other trail-effect, completion, replay-fixture and remaining generator findings as open unless this test resolves them.
- Obtain a fresh independent whole-branch review for this PR, and require both the PR exact-head hosted gate and post-merge develop-push gate to pass before cleanup.
- Remove this clean worktree before deleting the exact local and upstream branch refs; preserve the separate AWS learning worktree.

## Review Focus

Check that the fixed runtime salt reaches the mounted spooked-horse event through normal `StartJourney` and `AdvanceJourneyDay`, that its recorded positive exhaustion changes the horse state and causes mounted travel to fall back to foot, and that `TrailEventApplied`, player inventory, journey snapshot and full replay agree. The state assertion includes daily upkeep and the recorded event delta; it does not independently establish ordering between those additive exhaustion changes. Verify the documented mutation suppresses exhaustion application and fails at the expected state assertion. Ensure only the resolver consumer-effect gap is closed; the no-horse generator exclusion and unrelated travel findings remain open, and ADR-0028, ADR-0029, PG-007 and the feature matrix remain truthful.

## Task 1: Successor plan and version

Verify PR #333 source `7768ee8432ee6d815a0663b57c4acc3f38eb903f` and squash merge `22d7289ca05e4b1e8d3198233d4e0a3e4db73c87` have identical tree `7159ad12ca47fe90673d159e9c8963fae3661aac`. Verify fresh review approval, exact-head gate `38448227041`, develop-push gate `38449041681`, clean primary `develop`, and absence of N-30's worktree and exact local/upstream refs; preserve the AWS learning worktree.

Retire `.agents/plans/2026-10-13-row17-n30-mounted-horse-eligibility.md`, make this N-31 plan active in roadmap row 17, append PR #333 and its verified delivery facts, and advance `Directory.Build.props` to `0.1.0-dev.149`. Validate plan/roadmap links, one-source version identity, and intended changes. Commit this successor-plan/version update through the normal check-only hook before implementation.

## Task 2: Prove application of the mounted spooked-horse effect

Add one fixed-salt behavior test to `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`. Extend only the private `CreateProgressionSession` helper with an optional `SaltSource` parameter defaulting to `DeterministicSaltSource`. Create a Challenging session with a healthy horse, saddle, Hills/River route, and runtime salt `n31-horse-trouble-1`; preview and start the normal route, then advance one day.

Require `AdvanceJourneyDay` to return `BadLuckSpookedHorse` of kind BadLuck with a positive `HorseExhaustionDelta`. Require the single `TrailEventApplied` fact to carry that ID and delta and report the event-caused switch to foot travel. Compute the expected horse state by applying `JourneyUpkeepRules.ApplyDailyUpkeep` to the helper's known healthy starting horse and route inputs, then increasing the resulting exhaustion by the event's recorded delta. Require the event's absolute journey snapshot, returned snapshot, live journey, and player inventory to agree on that result. Rehydrate the complete event stream and require its journey snapshot and horse inventory to equal the live command result. Do not claim this distinguishes ordering of the two additive exhaustion changes. Do not force a developer encounter override or inspect generator internals.

Run the focused test against production, temporarily suppress only the positive exhaustion application in `JourneyLoop.ApplyHorseDelta`, and run it again. The mutation must fail because `TrailEventApplied.TravelModeChangedTo` no longer records the event-caused switch to foot travel. Restore production byte-for-byte, rerun the focused test, and run all `TravelResolverTests`.

Add a dated N-31 disposition to the N-18 test-quality investigation and update the `TravelResolverTests` row in the .NET test dispositions. State precisely that the real mounted event consumer, persisted absolute state and full replay agree, and that suppressing the consumer effect failed the state assertion. Keep the no-horse exclusion, other bad-luck/spooked-horse effects, completion and unrelated history/fixture findings open. No production code, feature matrix, ADR, or gameplay specification change belongs in this implementation diff.

## Task 3: Review and deliver

Stage the intended test/audit diff, run `git diff --check`, and commit through the normal staged-snapshot hook. Do not rerun the full canonical gate after a successful hooked commit unless a later change modifies that tree. Obtain a fresh independent whole-branch review of the exact committed diff, fixed-salt behavior and consumer-effect mutation, N-18 wording, ADR-0028/0029, PG-007 and the feature matrix. Resolve findings and re-review any corrected head.

Push and open a PR to `develop`; mark it ready after local proof and independent review. Require the hosted canonical gate to pass on the exact PR head before squash merge. Verify source/merge tree identity and the develop-push gate, fast-forward the primary checkout, then remove the clean N-31 worktree before deleting the exact local and upstream refs. Verify all three are absent and preserve the AWS learning worktree.
