# N-32 No-Horse Travel Event Exclusion Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace the seed-loop smoke test with a fixed behavior test proving that a traveler without a horse cannot receive the horse-specific Unlucky event when it is the only remaining candidate.

**Architecture:** Test the Domain travel-plan generator at its behavior boundary. This is a test and audit correction only; no production behavior, feature promise, event shape, or architecture change is expected.

**Specification and evidence:** The stable 0.1.0 baseline specification, row 17 roadmap, N-18 test-quality investigation, and `TravelDayPlanGeneratorTests` disposition govern scope. The Domain unslop profile governs event-sourced travel and behavior-test quality. No product ADR or feature-matrix change is expected.

**Governing behavior:** Without a horse, the `Unlucky` category excludes horse-required events. If it has no legal candidate after history and resource filters, generation uses the existing quiet fallback.

**Execution Strategy:** Native inline execution. One fixed context isolates the horse-specific event-candidate path and uses a mutation of its responsible filter to show that the test fails for the missing behavior.

## Global Constraints

- Preserve the user-approved stable 0.1.0 contract and strict event-sourcing, CQRS, and replay boundaries.
- Each merged plan PR advances the single authored application version exactly once; N-32 uses `0.1.0-dev.150`.
- Commit this successor plan/version before implementation; retire the completed N-31 plan in this first substantive commit.
- Do not retain seed searches, loops over arbitrary salts, conditional assertions, or temporary mutation code.
- `HorseTrouble` starts with zero weight whenever the context lacks a horse or mounted travel, and all current weight modifiers preserve that restriction. The later legality guard is redundant for these valid contexts, so do not add a test that only repeats the same zero-weight outcome.
- Do not change production code, feature promises, ADRs, or architecture. Keep unrelated travel-generator and N-18 findings open.
- Obtain a fresh independent whole-branch review for this PR, and require exact-head hosted CI and the post-merge develop-push gate to pass before cleanup.
- Remove the clean worktree before deleting its exact local and upstream branch refs; preserve the separate AWS learning worktree.

## Review Focus

Confirm that the fixed context reaches the Unlucky generator branch, and that disabling only its `RequiresHorse` filter causes the test to fail on the prohibited outcome. Verify it reaches an empty legal candidate set and requires the quiet fallback. The test does not cover the redundant `HorseTrouble` category legality guard, route-specific `TrailEventCatalog` producer, or real travel command consumer.

## Task 1: Successor plan and version

Verify PR #334 source `b90d7ba487852a1bdab9ddbc19ff5100f89d88f7` and squash merge `a885fb73885cd42272f5e19059d0c2326d2ecdfc` have identical tree `feb4b24bdc373396f8290103e4a193468f69e73f`. Verify the corrected independent review approved, exact-head gate `38454249690` and develop-push gate `38455081356` passed, the primary checkout is on clean `develop`, and the N-31 worktree and exact local/upstream refs are absent. Preserve the AWS learning worktree.

Retire `.agents/plans/2026-10-13-row17-n31-spooked-horse-effect-application.md`, make this N-32 plan active in roadmap row 17, append PR #334's verified delivery facts, and advance `Directory.Build.props` to `0.1.0-dev.150`. Validate the plan and roadmap links, single-source version identity, and intended changes. Commit this successor-plan/version update through the normal check-only hook before implementation.

## Task 2: Prove the horse-specific Unlucky candidate exclusion

In `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs`, replace `GenerateDoesNotSelectHorseOnlyEventsWhenTravelingWithoutAHorse` and its 12-seed loop with one fixed-context test. The context has no horse, an active BadLuck delay cooldown, and all other eligible BadLuck event IDs in recent history, leaving only the horse-specific event as an otherwise eligible Unlucky candidate. Require the generator's Quiet encounter with no trail event and exclude `BadLuckSpookedHorse`. Temporarily remove only the event candidate's `RequiresHorse` filter; prove this test fails because the prohibited horse event is selected instead of the quiet fallback, then restore production byte-for-byte.

Run the focused test and the complete `TravelDayPlanGeneratorTests` suite. Do not add a separate `HorseTrouble` category test: the category weight is already zero for a no-horse context before the legality guard, and removing that guard alone does not change observable output. Do not broaden this slice into producer catalog, travel consumer, or other generator behavior. Add dated N-32 dispositions to the N-18 test-quality investigation and the `TravelDayPlanGeneratorTests` row in the .NET dispositions. State the remaining scope and mutation outcome precisely; leave other N-18 travel effects and generator findings open.

## Task 3: Review and deliver

Stage the intended test/audit diff, run `git diff --check`, and commit through the normal staged-snapshot hook. Do not rerun the canonical gate after a successful hooked commit unless a later edit changes that tree. Obtain a fresh independent whole-branch review of the exact committed diff, its two fixed-path behavior proofs and mutation evidence, N-18 wording, applicable ADRs, and feature matrix. Resolve findings and re-review any corrected head.

Push and open a PR to `develop`; mark it ready after local proof and independent review. Require the hosted canonical gate to pass on the exact PR head before squash merge. Verify source/merge tree identity and the develop-push gate, fast-forward the primary checkout, then remove the clean N-32 worktree before deleting the exact local and upstream refs. Verify all three are absent and preserve the AWS learning worktree.
