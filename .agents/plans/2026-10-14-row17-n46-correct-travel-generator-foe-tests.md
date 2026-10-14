# N-46 Correct Conditional Travel Foe Weight Tests Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task.

**Goal:** Remove the misleading high-risk route repetition comparison and make the retained recent-Foe cooldown test deterministic and specific to that cooldown.

**Architecture:** This is a test-only travel-generator correction. `TravelDayPlanGenerator` remains the authority for route-risk and recent-encounter weights; no production behavior, event stream, aggregate ownership, command/query boundary, or player promise changes.

**Tech Stack:** C# 10 / .NET 10, xUnit, Python command bus.

**Spec:** The [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), and current test dispositions.

**Execution Strategy:** Use `executing-plans` inline. The metadata successor and test correction are sequential parts of one focused generator-test slice.

## Global Constraints

- Ordinary development targets `develop`; use a fresh worktree created by `using-git-worktrees/scripts/new_worktree.py` and a dedicated `codex/` branch.
- Assign `0.1.0-dev.164` as the next merged development version.
- Do not change production behavior, event contracts, route-risk rules, difficulty/entropy semantics, or feature promises.
- Do not retain a test whose title claims foe repetition while its assertions compare unrelated route/day/seed streams and only count encounters.
- A retained recent-foe behavior test must hold the generation context constant except for prior Foe history, use a fixed seed, reach Foe without that history, and show that prior Foe history suppresses the repeated category.
- Preserve existing independent difficulty/entropy distribution and lawful category/fallback behavior proof.
- Compare the resulting diff with ADR-0023, ADR-0024, ADR-0028, ADR-0029, ADR-0040 and PG-007; update ADRs or the feature matrix only if the implementation reveals a durable truth change.
- Run the canonical fail-fast gate with `py -3 tools/run.py ci --check` before publication.

## Review Focus

- Confirm `GenerateReducesImmediateFoeRepetitionOnHighRiskRoutes` does not establish the repeated-Foe cooldown or an independently documented high-versus-low risk promise.
- Confirm the fixed paired-context test fails when the recent-Foe category-weight reduction is removed, and that no production mutation remains.
- Keep the test-only disposition accurate about route-risk weighting coverage and the remaining travel-generator findings.

---

### Task 1: Record N-45 delivery and commit the N-46 plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-14-row17-n45-retire-composite-seed-profile-test.md`
- Create: `.agents/plans/2026-10-14-row17-n46-correct-travel-generator-foe-tests.md`

- [x] Record N-45 PR #348, reviewed source `e6cb80a8585357f503a966897722985936e080af`, squash merge `7b115916107b2631b9088d021b449c04f6b9645b`, identical tree `59085514bd97e9cdecf10b4a4e65f375542c159c`, version `0.1.0-dev.163`, clean independent review, exact-head hosted gate `38547058555`, develop-push gate `38547665629`, local Domain and canonical-gate results, checkout fast-forward, and exact worktree/local/upstream-ref cleanup in row 17.
- [x] Select N-46, link this plan, advance the version authority to `0.1.0-dev.164`, and retire N-45's completed plan in this successor.
- [x] Preserve N-45's dated investigation result and the current test behavior owners; do not remove its mutation evidence or convert it into a development receipt.
- [x] Commit the metadata and plan before changing tests; the check-only hook passed the complete canonical gate.

### Task 2: Correct the travel generator Foe comparison tests

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

- [x] Run the current focused `TravelDayPlanGeneratorTests` baseline (17 passed).
- [x] Replace the seed-searching `GenerateUsesRecentFoeHistoryToBreakUpBackToBackRiderDays` scenario with fixed seed `n46-foe-cooldown-0050` and two day-2 contexts; the contexts are identical except for recent Foe history. The no-history context reaches Foe, and the history context does not select Foe.
- [x] Demonstrate the revised test fails when the recent-Foe weight reduction in `BuildCategoryWeights` is temporarily neutralized; restore the production file byte-for-byte. The isolated revised test passes with production restored.
- [x] Remove `GenerateReducesImmediateFoeRepetitionOnHighRiskRoutes` and its now-unused `CreateLowRiskEncounterSession` fixture: its high-risk and low-risk samples use different seeds, day treatment, and route contexts, and it asserts neither consecutive Foe repetition nor a documented route-risk restriction. No replacement was added for that unsupported comparison.
- [x] Retain the separately owned high-risk Foe/category, difficulty, entropy, deterministic-context, legality, cooldown, and fallback behavior tests; do not alter them in this slice.
- [x] Add dated N-46 dispositions to the test-quality investigation and file inventory, stating the test's actual defect, its removal, the fixed cooldown proof and mutation evidence, remaining open findings, and that production behavior, ADRs, and feature promises did not change.
- [x] Run the focused generator tests (16 passed), complete Domain suite (536 passed, 1 skipped), and canonical fail-fast repository gate (all lanes passed).

### Task 3: Independent review and delivery

- [x] Compare the actual diff with ADR-0023, ADR-0024, ADR-0028, ADR-0029, ADR-0040 and PG-007. This test-only correction preserves existing route/category weighting and event replay, establishes no new travel promise, and changes no durable decision; no ADR or feature-matrix update is needed.
- [ ] Push the exact branch and open a Draft PR targeting `develop`; include the plan, mutation evidence, test dispositions, and decision/feature rulings.
- [ ] Obtain fresh-context whole-branch review, resolve actionable findings, verify hosted exact-head CI, then mark the PR ready and merge it to `develop`.
- [ ] Wait for and verify develop-push CI, fast-forward the primary checkout, remove the clean worktree with the finishing skill helper before deleting its exact local/upstream refs, and verify all three are absent.
