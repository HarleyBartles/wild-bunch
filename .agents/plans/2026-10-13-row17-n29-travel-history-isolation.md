# Row 17 N-29 Travel History Isolation Plan

> REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task.

**Goal:** Replace two confounded repeat-suppression tests with deterministic paired-context proofs that isolate Lucky-category cooldown and repeated BadLuck event-ID suppression.

**Architecture:** Keep `TravelDayPlanGenerator` as the owner of deterministic encounter selection. This is a test-quality correction only; production behavior, event facts, feature promises, and architecture remain unchanged.

**Spec and evidence:** Stable 0.1.0 baseline specification, row 17 roadmap, N-18 in the test-quality investigation, and the `TravelDayPlanGeneratorTests` disposition.

**Governing decisions:** ADR-0028 governs deterministic event-derived state and replay; ADR-0029 governs the separate meaning of trail pressure. The PR author and reviewer must compare the actual diff with both ADRs and PG-007; no durable decision or feature-matrix update is expected.

**Execution Strategy:** Native inline execution. The two tests share one deterministic generator/context seam and require coordinated input discovery, mutation proof, and one audit update; continuity is more valuable than separate implementer handoffs.

## Constraints

- Keep production source unchanged in the committed tree. Temporarily mutate only the relevant history filter to discover literals and prove each final test can fail; restore production byte-for-byte after each mutation.
- The Lucky-category test compares two populated day-2 plans with identical route, literal game seed, profile, and other inputs. Only the prior Lucky-kind history differs. Require the no-history context to reach a Lucky result and the cooldown context to produce no Lucky result. A mutation disabling the category cooldown must make this test fail for that outcome.
- The BadLuck event-ID test compares two day-2 contexts with identical inputs, including the same recent BadLuck kind. Only the prior `BadLuckDustStorm` event ID differs. Require both contexts to reach an Unlucky result, require the no-ID context to select `BadLuckDustStorm`, and require the ID-history context not to repeat it. A mutation disabling event-ID exclusion must make this test fail for that outcome.
- Find bounded deterministic literal inputs while each targeted filter is temporarily disabled, then retain one literal per behavior. No seed-search loop, `Count >= 0`, or helper that loses the selected seed may remain in these test paths.
- Remove `FindPlanWithTrailEvent` only if this is its final call site. Do not expand scope to other generator, travel-effect, completion, replay-fixture, map, or feature findings.
- Update only `TravelDayPlanGeneratorTests.cs` and the N-18 test-quality and test-disposition records for implementation. No production code, ADR, specification, or feature-matrix change belongs in the committed diff.
- Advance the single authored application version to `0.1.0-dev.147` in the successor planning commit.

## Task 1: Successor planning and version

Verify PR #331 source `7bcde4e80b973a5dcb2d4daddc620b4072b265d8` and squash merge `d9fc6dffe03af24f90d12ce6bb60447b41d84f46` have identical tree `a7d4aa3b0ebe72eb075348c1d0067546398b8e73`. Record independent review approval without findings, exact-head gate `38439139315`, develop-push gate `38439986956`, and verified paired worktree/branch cleanup. Confirm primary `develop` is clean at the merge and the AWS learning worktree remains.

Retire N-28's plan, make this N-29 plan active in row 17, append PR #331 and its delivery facts, and advance `Directory.Build.props` to `0.1.0-dev.147`. Validate links, version identity, and the intended diff. Commit this successor planning/version slice before editing tests; the normal check-only hook is its canonical local gate.

## Task 2: Isolate and prove repeat suppression

Inspect `TravelDayPlanGeneratorTests.GenerateAvoidsRepeatingLuckyTrailEventsOnConsecutiveDays` and `GenerateDoesNotRepeatHardMilesOnConsecutiveTrailDays` against `TravelDayPlanGenerator.Context.cs`. Replace their seed-search/changed-day comparisons with the paired day-2 contexts in the constraints, and rename the event-ID test to name `BadLuckDustStorm` rather than the stale Hard Miles label. Use temporary bounded probes only to find literals that make each corresponding disabled-filter mutation select the repeated outcome; remove every probe before committing.

For each final test, run it with production behavior, temporarily disable only its responsible filter, run the test and confirm it fails at the repeated-outcome assertion, then restore production byte-for-byte and confirm the test passes. Keep the event-ID test from passing merely because category selection stopped being Unlucky. Remove the tautological assertion and dead seed helper if no longer used.

Add a dated N-29 disposition to the N-18 test-quality investigation and update the `TravelDayPlanGeneratorTests` row in the .NET dispositions. Close only the two repeat-suppression oracle gaps, name the independently failing mutations, and preserve all remaining N-18 generator, effects, completion, and replay-fixture findings as open. Run the focused pair and the complete `TravelDayPlanGeneratorTests` suite.

## Task 3: Review and deliver

Stage the intended implementation/audit diff, run `git diff --check`, and commit normally through the staged-snapshot hook. Do not rerun the full canonical gate after a successful hooked commit unless a later change modifies that committed tree. Obtain a fresh independent whole-branch review of the exact committed diff, both paired-input oracles and mutations, N-18 wording, ADR-0028/0029 and PG-007. Resolve findings and re-review any corrected head.

Push and open a PR to `develop`, promote it to Ready after local proof and review, and verify the exact PR-head hosted gate passes before squash merge. Verify source/merge tree identity and the develop-push gate, fast-forward the primary checkout, then remove the clean worktree before deleting its exact local and upstream branch refs. Verify all three are absent and preserve the AWS learning worktree.
