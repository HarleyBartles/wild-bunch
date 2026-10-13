# N-30 Mounted Horse Trouble Effect Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace the mounted horse-trouble generator's seed-search test with a fixed-seed behavior proof that a reached horse-trouble encounter records its positive horse-exhaustion effect.

**Architecture:** Keep encounter selection and legality in `TravelDayPlanGenerator`. This is test and audit correction only; no production behavior, feature promise, event shape, or architecture changes.

**Specification and evidence:** Stable 0.1.0 baseline specification, row 17 roadmap, N-18 in the test-quality investigation, and the `TravelDayPlanGeneratorTests` disposition. The Domain unslop profile governs event-sourced travel and behavior-test quality. ADR-0028 and ADR-0029 remain applicable; PG-007 is unchanged.

**Governing decisions:** A mounted horse-trouble result must be an actual `HorseTrouble` encounter carrying the `BadLuckSpookedHorse` event and a positive horse-exhaustion effect. The fixed seed must reach that result directly; a search loop or conditional assertion cannot stand in for the behavior.

**Execution strategy:** Native inline execution. The mounted reachability test can use one bounded temporary probe to discover a literal seed, then becomes a direct assertion against that fixed seed. The separate no-horse exclusion test is outside this slice and remains open for its own evidence-backed correction.

## Constraints

- Keep production source unchanged in the committed tree. Temporarily set only the `CreateHorseTroubleEncounter` horse-exhaustion delta to zero to prove the committed test fails; restore production byte-for-byte.
- Preserve `GenerateDoesNotSelectHorseOnlyEventsWhenTravelingWithoutAHorse` unchanged in this slice. Its category weight and legality filter both suppress the outcome, so disabling only one cannot falsify the end behavior. Do not claim this plan proves which independent guard suppresses an ineligible category.
- Replace only `GenerateCanStillSelectHorseOnlyEventsWhenAHorseIsPresent` with a fixed-seed test. Require the generated plan to reach `HorseTrouble`, contain `BadLuckSpookedHorse`, and carry a positive `HorseExhaustionDelta`.
- Find a literal mounted-context seed with a bounded temporary probe, then remove all seed search and early-return control flow from the committed test.
- Do not broaden this slice to the separate route-specific `TrailEventCatalog` producer, journey resolver assertions, the no-horse exclusion test, other generator comparisons, or horse effects/replay.
- Update only `TravelDayPlanGeneratorTests.cs` and the N-18 test-quality/test-disposition records for the implementation. No production code, feature matrix, ADR, or gameplay specification change belongs in the implementation diff.
- The successor planning/version commit retires the completed N-29 plan and updates the row 17 roadmap entry with PR #332 delivery facts; it advances the authored application version once to `0.1.0-dev.148`.

## Review Focus

Check that the test uses one literal mounted context and asserts a populated plan, exact category and event identity, and positive horse-exhaustion effect. Verify zeroing that effect makes the test fail, production is restored byte-for-byte, and the N-18 audit closes only this mounted horse-trouble effect oracle. Confirm the no-horse exclusion test, route-specific catalog behavior, other travel effects, completion, replay fixtures and other generator findings remain open, and ADR-0028, ADR-0029, PG-007 and the feature matrix remain truthful.

## Task 1: Successor plan and version

Verify PR #332 source `dcb37184b65a6c4422be490ff9436d1e41e6c115` and squash merge `b8ecc981c3a7bfacda59c86860ab122448d1cae1` have identical tree `f160261f75f1d8897f7764e63d670d8b21c397cb`; verify exact-head hosted gate `38443762523` and develop-push gate `38444555695` passed and fresh independent review approved without findings. Confirm primary `develop` is clean at the merge, N-29's clean worktree and exact local/upstream refs are absent, and the AWS learning worktree remains.

Retire N-29's completed plan, make this plan active in row 17, append PR #332 and its delivery facts, and advance `Directory.Build.props` to `0.1.0-dev.148`. Validate links, version identity, and intended changes. Commit this planning/version successor before editing tests; the normal check-only hook is its canonical local gate.

## Task 2: Prove mounted horse-trouble effect

Inspect the existing mounted horse-trouble test and `CreateHorseTroubleEncounter`. Use a temporary bounded probe to find a literal seed for which the mounted context reaches `HorseTrouble` with `BadLuckSpookedHorse`; remove the probe before proceeding.

Replace the mounted seed-search test with the fixed-seed behavior test in the constraints. Run the test with production behavior, temporarily set the generated horse-exhaustion effect to zero, run the test and confirm failure at the positive-effect assertion, then restore production byte-for-byte and confirm the test passes. Do not add unrelated assertions to force a broader generator contract.

Add a dated N-30 disposition to N-18 in the test-quality investigation and update the `TravelDayPlanGeneratorTests` row in the .NET test dispositions. Close only the mounted horse-trouble effect oracle gap, record the mutation evidence, and preserve all other open N-18 findings. Run the focused test and the complete `TravelDayPlanGeneratorTests` suite.

## Task 3: Review and deliver

Stage the intended implementation/audit diff, run `git diff --check`, and commit through the normal staged-snapshot hook. Do not rerun the full canonical gate after a successful hooked commit unless a later change modifies that committed tree. Obtain a fresh independent whole-branch review of the exact committed diff, the fixed-seed mounted event and zero-effect mutation, N-18 wording, ADR-0028/0029, PG-007 and the feature matrix. Resolve findings and re-review any corrected head.

Push and open a PR to `develop`, mark it Ready after local proof and review, and verify the exact-head hosted gate passes before squash merge. Verify source/merge tree identity and the develop-push gate, fast-forward the primary checkout, then remove the clean worktree before deleting its exact local and upstream branch refs. Verify all three are absent and preserve the AWS learning worktree.
