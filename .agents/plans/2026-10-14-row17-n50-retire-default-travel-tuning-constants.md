# Row 17 N-50: Retire the Default Travel Tuning Inventory

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove tests that freeze every `TravelRulesProfile` tuning literal without proving player-visible travel behavior, while retaining the existing difficulty route and travel-effect behavior tests.

**Architecture:** Test ownership correction only. Travel rules are consumed by route preview, upkeep, encounter resolution and replay. Keep those observable scenarios; do not preserve an initializer inventory as a balance contract, add a replacement inventory, or change production behavior.

**Tech Stack:** C# 10 / .NET 10, xUnit, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially [test design and acceptance](../specs/2026-10-07-stable-0.1.0-baseline.md#test-design-and-acceptance) and the settled travel gameplay boundaries.

**Execution Strategy:** `executing-plans` (native inline). The mutation proof, removal and inventory correction share one test-ownership boundary and depend on the same travel scenario context; splitting them into per-task agents would repeat inspection without creating independent implementation tracks. Use one controlled tuning mutation to prove that the retained travel scenario detects a behavioral regression, restore production byte-for-byte, then remove only direct constant assertions and update the dated inventories. A fresh whole-branch reviewer supplies independent review.

## Global Constraints

- Treat prior audit notes as claims to verify against current source and tests.
- Preserve travel behavior, world/session replay, difficulty selection, ADR decisions, feature promises, and the single canonical development-version source.
- Keep observable travel scenarios. Do not assert tuning values by reading a profile when a test can assert the resulting game behavior.
- Do not add a replacement test unless inspection finds a distinct player-facing invariant without a behavior owner.
- Assign exactly one unique next `0.1.0-dev.N` version for the merged PR, and retain `develop` as the PR base.
- The exact-head hosted gate must pass before merge, and the develop push gate must pass on the exact merge SHA before cleanup.

## Review Focus

- `DefaultProfileKeepsCurrentTravelTuning` pins copied profile literals, including reserved heat fields, rather than checking a gameplay outcome.
- The Easy-profile scenario exercises a real session and route preview; travel scenarios cover upkeep, route pacing, encounter effects and replay.
- Remove scalar profile-value assertions that duplicate construction, retaining assertions on observable route and travel behavior.
- Raising only one Brutal horse-death threshold leaves the other hunger/thirst/exhaustion death paths active, so those mutations did not change the scenario. Raising all three thresholds together should make the retained scenario fail because the horse stays eligible and the Spooked Horse effect applies; restore production byte-for-byte.
- N-49 delivery is fully verified and its plan is retired only after its evidence is promoted in this successor's first substantive commit.

---

### Task 1: Promote N-49 and establish N-50 as its successor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n50-retire-default-travel-tuning-constants.md`
- Delete: `.agents/plans/2026-10-14-row17-n49-retire-map-generator-cluster-count-smoke.md`

**Interfaces:**
- Consumes: N-49 PR #352, its exact-head review and hosted gate, develop merge and push gate, and the live branch/worktree state.
- Produces: promoted N-49 delivery evidence, the active N-50 plan at `0.1.0-dev.168`, and retirement of the completed N-49 plan.

- [x] **Step 1: Verify N-49 delivery and cleanup.** Confirm PR #352 is merged to `develop`, reviewed head `0857a54e7f2fe8b491bc455a1c5a4e50f6326246` equals the PR head, merge SHA `79d65f589462f5ce609e4e571d96b3d5e3fbc8fd` is in current `develop`, exact-head gate run `38569306218` passed, develop push gate run `38570123354` passed on that merge SHA, and the primary checkout plus paired branch/worktree cleanup are complete. Confirm N-49's mutation evidence is in the investigation and test-disposition inventory before retiring its plan.
- [x] **Step 2: Promote delivery, activate N-50, and advance version.** Update roadmap row 17 to link this plan, assign `0.1.0-dev.168`, add merged PR #352 and its exact reviewed/merge identities and hosted gate evidence, and record N-50's bounded travel-tuning disposition. Change only the authored version in `Directory.Build.props` to `0.1.0-dev.168`.
- [x] **Step 3: Retire the completed predecessor.** Delete the N-49 plan after confirming its full scope was delivered and its evidence has current owners. Preserve the baseline specification, roadmap, test-quality investigation, and test-disposition inventory.
- [x] **Step 4: Commit successor planning and predecessor retirement first.** Run `git diff --check`, inspect the complete staged change, and commit the plan, roadmap, version, and N-49 retirement as the first substantive commit using `docs: plan N-50 travel tuning test ownership`.

### Task 2: Prove and retire the tuning-constant inventory

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelRulesProfileTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Inspect only: `src/WildBunch.Domain/Travel/TravelRulesProfile.cs`, travel resolver/upkeep consumers, and applicable ADR/feature records

**Interfaces:**
- Consumes: `DefaultProfileKeepsCurrentTravelTuning`, the Easy-profile route-preview scenario, and existing travel behavior/replay scenarios.
- Produces: test coverage centered on observable travel outcomes and a dated, evidence-backed disposition of direct tuning-constant assertions.

- [x] **Step 1: Reconfirm behavior ownership.** Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~TravelRulesProfileTests` and the post-upkeep scenario filter below. Map the retained Easy route behavior and representative upkeep/encounter/replay outcomes to their production consumers; identify any uncovered player-facing invariant before deciding whether a test is needed.
- [x] **Step 2: Prove behavioral sensitivity.** Temporarily remove only the direct exhaustion-threshold assertion from `AdvanceJourneyDayUsesPostUpkeepHorseEligibilityForBadLuckSpookedHorse`, then raise the Brutal `HorseHungerDeathThreshold`, `HorseThirstDeathThreshold` and `HorseExhaustionDeathThreshold` to 100. Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~AdvanceJourneyDayUsesPostUpkeepHorseEligibilityForBadLuckSpookedHorse`; confirm it fails because the expected horse removal no longer occurs, restore production byte-for-byte, and verify restoration before proceeding.
- [x] **Step 3: Remove configuration inventories.** Delete `DefaultProfileKeepsCurrentTravelTuning`. In the Easy-profile scenario, remove direct assertions of reward/damage profile literals while retaining the session, mounted route mode, expected travel duration, applied route pacing, canteen availability and selected difficulty assertions. Remove the direct threshold-value assertion from the post-upkeep scenario while retaining its event, horse-loss, inventory, clock and replay outcomes. Do not alter production or add a replacement constants test.
- [x] **Step 4: Update the audit records.** Record the mutation and restoration evidence in the test-quality investigation. Update the disposition entry to mark the constant inventory retired, name the retained behavior owners, and leave any uncovered invariant explicitly open rather than claiming a copied profile field proves it.
- [x] **Step 5: Verify focused owners.** Run the Domain test project with filters `FullyQualifiedName~TravelRulesProfileTests`, `FullyQualifiedName~AdvanceJourneyDayUsesPostUpkeepHorseEligibilityForBadLuckSpookedHorse`, and `FullyQualifiedName~AdvanceJourneyDayAppliesHardMountedLuckyWaterSeepToTheCanteenAndReplays`. Confirm expected behavior is asserted without direct profile literal inventories.
- [x] **Step 6: Commit the implementation through the staged-candidate hook.** Stage only the test and investigation edits, inspect the complete staged diff, and commit using `test: retire travel tuning constant inventory`. The tracked hook must pass the canonical fail-fast gate on the staged candidate; do not bypass it or immediately rerun the full gate after a successful hooked commit.

### Task 3: Review, publish, merge, and retire N-50

**Files:**
- Review: the complete diff, N-50 plan, roadmap, investigation/disposition evidence, and applicable decision/feature records.
- Modify: `.agents/plans/2026-10-14-row17-n50-retire-default-travel-tuning-constants.md` and roadmap with final delivery evidence.

**Interfaces:**
- Consumes: Task 2's controlled mutation results and focused tests.
- Produces: N-50 merged to `develop` at `0.1.0-dev.168`, with reviewed evidence and the exact worktree and paired branch refs retired.

- [ ] **Step 1: Validate the final tree.** Confirm the staged-candidate hook's canonical fail-fast gate passed on the committed `0.1.0-dev.168` tree. Run `.\tools\postgres-dev.ps1 ensure` before the implementation commit if PostgreSQL is needed; inspect the generated build identity and confirm it consumes `Directory.Build.props` with no second authored version. Do not repeat the complete gate immediately after a successful hooked commit.
- [x] **Step 2: Reconcile durable truth.** Review ADRs and PG-007 travel promises through the repository playbooks. Record why no ADR/feature-matrix change is warranted if only test ownership changes; if the inspection reveals a durable-truth mismatch, correct its decision/feature owner in this slice.
- [ ] **Step 3: Review and publish.** Build the review package from the exact merge base to current `HEAD`; dispatch a fresh-context reviewer on the complete diff, plan, roadmap, and evidence. Resolve findings and re-review corrections. Publish a Draft PR to `develop`, verify the exact head and body, mark it ready, and require exact-head hosted CI to pass before squash merge.
- [ ] **Step 4: Verify integration and clean up.** Require the develop push gate to pass on the exact merge commit; fast-forward the primary checkout; remove this worktree using the `finishing-a-development-branch` skill script; then delete and verify absence of the matching local and upstream branches.

## Acceptance Criteria

- The retained post-upkeep scenario fails under the controlled combined horse-threshold mutation for a gameplay outcome; production is restored byte-for-byte. Mutating an individual threshold is insufficient for this case because another death axis still causes the horse's removal.
- Direct copied `TravelRulesProfile` tuning inventories are removed without changing gameplay or adding a replacement inventory test.
- Existing Easy route and travel effect/replay behavior owners remain; audit records name their actual behavior ownership and leave uncovered invariants open.
- No gameplay source, event history, ADR decision or feature promise changes unless current inspection demonstrates that durable truth needs correction.
- N-49 delivery evidence is promoted before its completed plan is retired in the first substantive successor commit; this plan remains through its completing PR.
- Focused tests, canonical local gate, fresh independent review, exact-head hosted gate, exact-merge develop gate, primary fast-forward, and paired worktree/ref cleanup are verified.
