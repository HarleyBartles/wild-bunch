# Row 17 N-16 Travel Entropy and Difficulty Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace enum-only travel-pressure claims and the one-day Boring comparison with deterministic behavioral evidence for travel difficulty, entropy, and repeated Boring travel progression.

**Architecture:** `TravelDayPlanGenerator` in Domain owns encounter-count and category selection; `GameSession` owns authoritative travel-day advancement and resource settlement. This plan changes tests and test-fixture descriptions only, preserving the current gameplay algorithm and its event/state boundaries.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.Domain.Tests`, and `WildBunch.GameContent.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), N-16; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `TravelEntropyVarianceTests` and `TravelDayPlanGeneratorTests`.

**Execution Strategy:** `executing-plans`, inline and sequential. The Domain generator outcomes, aggregate progression fixture, and GameContent disposition are coupled to one travel-entropy claim and need one shared integration context; separate plans would pay repeated PR/version/review overhead without independent product outcomes.

## Review Focus

- Entropy and difficulty must be varied independently on otherwise paired generator contexts; Task 2 uses the same fixed seed corpus and compares actual Foe encounters across complete generated day plans.
- The Boring fixture must reach at least three real travel days, and paired sessions must agree on generated plan facts, outcomes, and settled resources after each day; Task 3 owns the reached-day and state assertions.
- Encounter-choice interruptions must not be mistaken for failed travel or extra days; Task 3 resolves any generated pending encounter through the existing deterministic internal roll path and checks the day count after settlement.

## Global Constraints

- Start from `develop` in this fresh worktree and deliver one PR back to `develop`; advance `Directory.Build.props` exactly once to `0.1.0-dev.132`.
- In the first substantive commit, record N-16 seed-code delivery evidence for PR #316 in the roadmap, replace the active-plan pointer and version, and retire `.agents/plans/2026-10-13-row17-n16-seed-codec.md`.
- Difficulty owns challenge/consequence pressure; randomness owns variation and volatility. Preserve ADR-0023's surviving axis-separation decision and ADR-0040's current vocabulary/defaults.
- Keep deterministic seed inputs fixed and paired; do not search until a desired outcome appears, compare unrelated route contexts, freeze private weight constants, or add source-shape tests.
- Do not change production gameplay behavior, public feature promises, seed-code compatibility, event payloads, or persistence. Boring determinism does not imply encounter suppression.
- Before review, compare the test disposition with ADR-0023, ADR-0024, ADR-0040, the feature matrix and the current implementation; state that no durable decision or product capability changes.
- Require focused Domain and GameContent lanes, the canonical repository gate, a fresh whole-branch review, and passing exact-head hosted CI before merge. After merge, require the develop-push gate and remove the clean worktree with its exact local/upstream refs as one verified closeout.

### Task 1: Record PR #316 and install this successor plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n16-seed-codec.md`
- Add: `.agents/plans/2026-10-13-row17-n16-entropy-behavior.md`

**Interfaces:**
- Consumes: PR #316 source head `533a5dfc7fff7d04b27d66ac2efce6f73e0c97be`, squash merge `4324c6edc043aafd9ddb34f0e21f457756708492`, exact-head CI run `38341505071`, develop-push CI run `38341997340`, the corrected review finding and scoped fix review, and verified removal of the N-16 seed-code worktree plus its local/upstream refs.
- Produces: row 17 linked to this N-16 successor at `0.1.0-dev.132`, a truthful PR #316 delivery record, and retirement of the completed seed-code plan.

- [x] **Step 1: Record N-16 seed-code delivery in row 17.** Add PR #316 to the merged PR links and record source/merge SHAs, both passing hosted runs, the corrected roadmap review finding, primary-checkout fast-forward, and verified paired worktree/ref cleanup. State that seed-code findings alone are closed and the entropy, roster, generator, graph and map findings remain open.

- [x] **Step 2: Install this plan and advance the sole version authority.** Change `<Version>` in `Directory.Build.props` from `0.1.0-dev.131` to `0.1.0-dev.132`; update the row 17 plan/version pointer; delete only the completed seed-code predecessor plan. Do not add another authored version value.

- [x] **Step 3: Commit the planning handoff before test changes.** Check the row pointer, delivery evidence, predecessor retirement, active plan and single version authority. Run `git diff --check`, inspect the staged diff, and commit the planning artifacts before Task 2.

### Task 2: Prove difficulty and entropy through actual generated categories

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs`
- Remove misleading pressure assertions from: `tests/WildBunch.GameContent.Tests/TravelEntropyVarianceTests.cs`

**Interfaces:**
- Consumes: internal `TravelDayPlanGenerator.Generate(TravelDayGenerationContext)`, existing high-risk Domain session/context fixtures, and paired `GameDifficulty`/`GameEntropy` context fields.
- Produces: deterministic assertions over actual Foe encounters proving Brutal produces at least 10% more Foe selections than Standard on the fixed corpus and Wild does not produce more Foe selections than Classic at the same Standard difficulty.

- [x] **Step 1: Replace the enum-only assertions with fixed-corpus behavior tests.** In `TravelDayPlanGeneratorTests`, use the deterministic ordinal corpus `n16-foe-pressure-001` through `n16-foe-pressure-128` and one high-risk base context. For every seed, generate the same context with only difficulty changed (`Standard` versus `Brutal`) and only entropy changed (`Classic` versus `Wild`). Count every actual Foe encounter across the full generated plan, so entropy's encounter-count effect remains part of the observed player outcome; assert each variant produces at least one encounter across the corpus, the Standard/Classic baseline has at least one Foe, Brutal has at least 10% more Foe encounters than Standard, and Wild has no larger Foe count than Classic. Do not stop early or choose seeds based on observed results.

- [x] **Step 2: Run the focused Domain tests and confirm the result observes the intended behavior.** Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~TravelDayPlanGeneratorTests`. Confirm the new tests pass against the current algorithm and report the deterministic corpus counts in test failure messages only when useful for diagnosis; do not persist a development receipt.

- [x] **Step 3: Prove the tests detect a behavior regression.** Temporarily add a Wild-only Foe weight increase inside `BuildCategoryWeights`, run both new tests, and confirm the entropy test fails for the actual Foe-count comparison. Restore the generator byte-for-byte and rerun the focused Domain tests. Separately remove the Brutal Foe weight increase, confirm the difficulty test fails, restore, and rerun; retain no production diff.

- [x] **Step 4: Remove the vacuous copies.** Delete `WildEntropy_DoesNotIncreaseFoePressure`, `Difficulty_BrutalIncreasesFoePressureMoreThanWildEntropy`, the unused `Ratio` helper and the obsolete stochastic-test comment from the GameContent suite, since the pressure claims compare labels rather than generated outcomes. Update its class summary to describe the retained seed/salt-mode setup behaviors and point pressure behavior to the Domain generator owner.

- [x] **Step 5: Verify the focused suites and commit the behavior tests.** Run the focused Domain generator selection and focused GameContent entropy selection. Inspect the source diff to confirm no production or event code changed, then commit the completed test corrections.

### Task 3: Prove Boring determinism across reached aggregate travel days

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/TravelDayPlanGeneratorTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelEntropyVarianceTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalog.cs`

**Interfaces:**
- Consumes: `GameSession.AdvanceJourneyDay`, `TravelDayAdvanced` events, `TravelJourneySnapshot`, the existing complete `PlansAreEquivalent` and encounter-equivalence helpers, and the fixed-salt canonical start flow.
- Produces: one fixed Boring fixture that advances two independently started sessions through at least three actual travel days and proves matching generated plan details, day outcomes, resource settlement, and reached day numbers.

- [x] **Step 1: Build a fixed Boring journey fixture and reach three days.** Extend the local Domain `CreatePressureSession` fixture only with optional route distance and entropy inputs, preserving existing callers. Use a low-risk route long enough for at least three mounted travel days, a healthy horse/saddle, sufficient food/feed/water, Standard difficulty, Boring entropy and fixed salt. Start two sessions from identical canonical inputs. Advance each exactly one real day per iteration; if a day creates a pending encounter, resolve it in both sessions with the existing internal `ResolveJourneyEncounter("run", null, null, forcedRoll: 0UL)` path until the day's plan is complete. Assert each day was reached once, both results agree on success/status/outcome, and neither session enters a terminal state before day three.

- [x] **Step 2: Compare actual generated and settled state after each reached day.** For the corresponding `TravelDayAdvanced` events, compare day number, day outcome, health delta, narration/entries, and all actual `JourneyTrailEventState` fields through the existing record equality. Add a snapshot comparer for every scalar `TravelJourneySnapshot` field, ordered warnings, pending-encounter details through `ResolutionsAreEquivalent`, and complete `CurrentDayPlan` through `PlansAreEquivalent`; compare current player health, wallet cash, food, horse feed, canteen state, ammunition and horse state after every advance and encounter resolution. Assert the loop reaches `DaysTravelled == 3` for both sessions, rather than only comparing command Success/Status.

- [x] **Step 3: Prove the determinism check catches a reroll.** Temporarily include a fresh per-generation value in Boring's composed seed, run the new test and confirm the two sessions diverge in their generated plan or settled outcome. Restore generator source byte-for-byte and rerun the focused Domain test.

- [x] **Step 4: Remove misleading descriptions and the shallow GameContent test.** Remove the one-day `BoringEntropy_ProducesIdenticalTravelDaysAcrossRepeatedAdvances` test from GameContent; the stronger Domain aggregate test owns this behavior. In `TravelTestSeedCatalog`, change both Boring entry comments from “encounters suppressed” to “fixed-salt, repeatable travel outcomes,” without changing fixture data or transitional loadout scope.

- [x] **Step 5: Verify the focused suites and commit the aggregate proof.** Run the focused Domain generator selection and focused GameContent entropy selection. Inspect the diff to confirm the complete multi-day behavior is owned by Domain and the remaining GameContent checks still cover real salt modes, then commit this task.

### Task 4: Reconcile the finding and validate the slice

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Review: `docs/decisions/ADR-0023-difficulty-and-entropy-vocabulary-and-fairness-contract.md`
- Review: `docs/decisions/ADR-0024-source-taxonomy-implications-for-difficulty-and-entropy.md`
- Review: `docs/decisions/ADR-0040-difficulty-and-randomness-vocabulary.md`
- Review: `docs/features.md`

**Interfaces:**
- Consumes: passing Domain and GameContent behavior tests, the test-quality disposition, current difficulty/randomness decisions and current feature promises.
- Produces: a dated N-16 test disposition limited to entropy/difficulty behavior and reached Boring travel; remaining N-16 roster, generator, graph, layout and map findings stay explicitly open.

- [x] **Step 1: Record only the implemented N-16 disposition.** Describe the fixed-corpus generated Foe comparisons, the multi-day aggregate outcome/resource proof, removal of enum-only/one-day assertions, and corrected Boring fixture description. Do not mark N-16 or row 17 complete and do not convert this into a product feature change.

- [x] **Step 2: Run focused validation and commit the disposition.** Run the focused Domain selection and focused GameContent selection. Commit the investigation disposition through the normal check-only hook; do not bypass the hook.

- [x] **Step 3: Verify the committed tree with the canonical gate.** Run `py -3 tools/run.py ci --check` against the final committed implementation and require it to pass; report any existing skip truthfully. Do not run diagnostics unless this fail-fast gate fails.

- [ ] **Step 4: Review and publish.** Compare the final diff with ADR-0023/0024/0040 and `docs/features.md`, stating why their current durable truth is unchanged. Run the PR author decision-record and feature-matrix checks and obtain a fresh whole-branch review. Create one PR to `develop`, verify its exact head and passing hosted canonical gate, and merge only after that gate passes.

- [ ] **Step 5: Close out after the develop gate.** Fast-forward the primary checkout to the merge and verify the develop-push gate passes. Confirm the worktree is clean; remove it first, delete only its exact local and upstream branch refs, and verify the worktree and both refs are absent before creating the next plan worktree.
