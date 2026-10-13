# N-30 Mounted Horse Eligibility Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace the horse-trouble generator's seed-search tests with a deterministic paired-context behavior test proving the encounter is reachable for a mounted horse and excluded on foot without a horse.

**Architecture:** Keep encounter selection and legality in `TravelDayPlanGenerator`. This is test and audit correction only; no production behavior, feature promise, event shape, or architecture changes.

**Specification and evidence:** Stable 0.1.0 baseline specification, row 17 roadmap, N-18 in the test-quality investigation, and the `TravelDayPlanGeneratorTests` disposition. The Domain unslop profile governs event-sourced travel and behavior-test quality. ADR-0028 and ADR-0029 remain applicable; PG-007 is unchanged.

**Governing decisions:** The same seed and route must produce the horse-only `HorseTrouble`/`BadLuckSpookedHorse` encounter when mounted and suppress it when traveling on foot without a horse. A behavior test must require the mounted plan to reach the relevant category and the foot plan to be populated, then prove the no-horse/mounted legality filter by temporarily disabling only that filter.

**Execution strategy:** Native inline execution. The existing paired seed-search tests share one generator seam and should become one paired-input behavior test with one discovered literal seed. A temporary bounded probe may be used only to find that literal; no probe or seed-search loop remains in the committed diff.

## Constraints

- Keep production source unchanged in the committed tree. Temporarily disable only the `HorseTrouble` no-horse/unmounted category filter to discover a fixed seed and prove the committed test fails; restore production byte-for-byte.
- Replace `GenerateDoesNotSelectHorseOnlyEventsWhenTravelingWithoutAHorse` and `GenerateCanStillSelectHorseOnlyEventsWhenAHorseIsPresent` with one deterministic paired-context test using the same literal seed, route, profile and other inputs. The mounted context uses the horse-present/worn/mounted posture; the paired context changes only the horse-presence and travel-mode posture to no-horse/on-foot.
- Require the mounted context to contain a `HorseTrouble` encounter with `BadLuckSpookedHorse`; require the foot context to be populated and contain neither `HorseTrouble` nor `BadLuckSpookedHorse`. The mounted assertion must make the negative case meaningful rather than passing because the encounter was never reachable.
- Find one literal seed while the targeted filter is disabled such that the no-horse context selects `HorseTrouble` under the mutation, while its mounted counterpart reaches the expected encounter. The final test must fail at the foot-context exclusion when the filter is disabled.
- Remove the existing bounded seed searches and terminal seed-search exception from these two test paths. Do not broaden this slice to the separate route-specific `TrailEventCatalog` producer, journey resolver assertions, other generator comparisons, or horse effects/replay.
- Update only `TravelDayPlanGeneratorTests.cs` and the N-18 test-quality/test-disposition records for the implementation. No production code, feature matrix, ADR, or gameplay specification change belongs in the implementation diff.
- The successor planning/version commit retires the completed N-29 plan and updates the row 17 roadmap entry with PR #332 delivery facts; it advances the authored application version once to `0.1.0-dev.148`.

## Review Focus

Check that both contexts retain identical seed, route, difficulty, entropy, history, profile, and all other inputs, with only mounted horse availability changed; the mounted control reaches the exact event; the foot case is populated; and disabling the responsible legality filter makes the foot case fail for the forbidden event. Verify production restoration byte-for-byte and that the N-18 audit closes only the duplicated horse-trouble eligibility seed-search gap. Confirm remaining travel effects, route-specific catalog behavior, completion, replay fixtures and other generator findings remain open, and ADR-0028, ADR-0029, PG-007 and the feature matrix remain truthful.

## Task 1: Successor plan and version

Verify PR #332 source `dcb37184b65a6c4422be490ff9436d1e41e6c115` and squash merge `b8ecc981c3a7bfacda59c86860ab122448d1cae1` have identical tree `f160261f75f1d8897f7764e63d670d8b21c397cb`; verify exact-head hosted gate `38443762523` and develop-push gate `38444555695` passed and fresh independent review approved without findings. Confirm primary `develop` is clean at the merge, N-29's clean worktree and exact local/upstream refs are absent, and the AWS learning worktree remains.

Retire N-29's completed plan, make this plan active in row 17, append PR #332 and its delivery facts, and advance `Directory.Build.props` to `0.1.0-dev.148`. Validate links, version identity, and intended changes. Commit this planning/version successor before editing tests; the normal check-only hook is its canonical local gate.

## Task 2: Prove horse-only encounter eligibility

Inspect the existing horse-trouble tests and `FilterCategoryWeightsForLegality`. Temporarily disable only the `HorseTrouble` horse/mounted exclusion and use a bounded probe to find a literal fixed seed where the mounted control reaches `HorseTrouble`/`BadLuckSpookedHorse` and the no-horse/on-foot context also reaches the forbidden category under the mutation. Restore the production file byte-for-byte before writing the final test.

Replace the two seed-search tests with the paired-context test in the constraints. Run the test with production behavior, disable only the targeted legality filter, run the test and confirm failure at the foot exclusion, then restore production byte-for-byte and confirm the test passes. Do not add unrelated assertions to force a broader generator contract.

Add a dated N-30 disposition to N-18 in the test-quality investigation and update the `TravelDayPlanGeneratorTests` row in the .NET test dispositions. Close only the horse-trouble eligibility oracle gap, record the mutation evidence, and preserve all other open N-18 findings. Run the focused paired test and the complete `TravelDayPlanGeneratorTests` suite.

## Task 3: Review and deliver

Stage the intended implementation/audit diff, run `git diff --check`, and commit through the normal staged-snapshot hook. Do not rerun the full canonical gate after a successful hooked commit unless a later change modifies that committed tree. Obtain a fresh independent whole-branch review of the exact committed diff, the paired contexts and mutation, N-18 wording, ADR-0028/0029, PG-007 and the feature matrix. Resolve findings and re-review any corrected head.

Push and open a PR to `develop`, mark it Ready after local proof and review, and verify the exact-head hosted gate passes before squash merge. Verify source/merge tree identity and the develop-push gate, fast-forward the primary checkout, then remove the clean worktree before deleting its exact local and upstream branch refs. Verify all three are absent and preserve the AWS learning worktree.
