# Row 17 N-28 Unlucky Fallback Proof Plan

> REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task.

**Goal:** Replace two exclusion-only travel generator tests with behavior proofs for a lawful non-horse bad-luck result under delay cooldown and the Quiet fallback when no eligible bad-luck candidate remains.

**Architecture:** Preserve TravelDayPlanGenerator as the outcome owner. Build its context through the existing GameSession fixture and call the generator with frozen literal inputs. This is test and audit correction only; no production behavior, ADR, or feature promise is expected to change.

**Spec and evidence:** Stable 0.1.0 baseline, row 17 roadmap, N-18 in the test-quality investigation, and TravelDayPlanGeneratorTests disposition.

**Governing decisions:** ADR-0028 covers deterministic event-derived state and replay; ADR-0029 covers trail pressure. Compare the actual diff against both ADRs and PG-007 before review.

## Constraints

- Preserve production code and use literal deterministic inputs in final tests. Do not keep a seed-search loop or add a test hook for private generator logic.
- The lawful bad-luck case must reach an Unlucky encounter and require its exact legal event with zero delay while cooldown is active.
- The empty-pool case must demonstrate the Unlucky selection path, exclude every eligible candidate through no horse, delay cooldown and recent event IDs, and require Quiet with no trail event.
- Mutate each fallback independently, prove its intended test fails, restore production byte-for-byte, then rerun focused and full generator tests.
- Limit implementation changes to TravelDayPlanGeneratorTests and the N-18 investigation/disposition docs. Advance Directory.Build.props once to 0.1.0-dev.146.

## Task 1: Successor planning

Verify PR #330 source 61db11e65f3cdd3e553fd341722889efe0b123fb and squash merge d23af913cd75d389f7c6993cb68b9a09c6088974 have identical tree 3a6f9bde0753d835a53b253cb9236e7a0215abf3. Record review approval, exact-head gate 38433556252, develop-push gate 38434154169, and paired cleanup. Confirm primary develop is clean at the merge and the AWS learning worktree remains.

Retire N-27's plan, make N-28 the active row-17 plan, append PR #330 and its delivery facts, and advance the authored application version to .146. Validate links and version identity. Commit this planning/version slice before test edits.

## Task 2: Reach and prove both fallback outcomes

In TravelDayPlanGeneratorTests, strengthen GenerateFallsBackToLawfulNonHorseBadLuckWhenNoHorseAndDelayCooldownIsActive to assert a reached Unlucky encounter with its exact non-horse BadLuck event and zero delay. Strengthen GenerateFallsBackToQuietWhenNoUnluckyCandidateRemains to prove the fixed input reaches the Unlucky selection with an empty eligible set and returns a Quiet encounter with no event. Find a bounded deterministic literal during implementation if the existing seed does not reach the branch; retain only the selected literal.

For each test, break the corresponding production fallback while preserving its eligibility setup, observe the focused test fail, restore production byte-for-byte, then run both focused tests and the complete TravelDayPlanGeneratorTests suite. Add dated N-18 and test-disposition entries closing only these two oracle gaps. Keep all other travel and generator findings open. Use the check-only commit hook as the canonical local gate; do not rerun it after a successful commit unless a subsequent correction changes the committed tree.

## Task 3: Review, publication, integration

Obtain a fresh independent whole-branch review of the committed diff, both fallback oracles, input reachability, mutation/restoration, N-18 wording, ADR-0028/0029 and PG-007. Resolve actionable findings and re-review the corrected head. Push and open a PR to develop; verify exact head and passing exact-head hosted gate before squash merge. Verify source/merge tree identity and passing develop-push gate, fast-forward the primary checkout, then remove the clean worktree followed by its exact local and upstream refs. Verify all absences and preserve the AWS worktree.
