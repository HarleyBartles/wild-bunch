# Row 17 N-21 Map Output Behavior Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace misleading map-generator tests with reachable behavioral proofs for outlier uniqueness, variant terrain influence, risk semantics, and full same-input world repeatability.

**Architecture:** Test-only changes at the existing GameContent generator and terrain-assignment owners. Production generation, event history, persistence, feature claims, and player behavior remain unchanged. Reuse current deterministic seams and compare generated outcomes rather than copied algorithms or incomplete shape checks.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.GameContent.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), map-generator findings; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), map-generator and `TerrainAssignerTests` entries.

**Execution Strategy:** `executing-plans`, inline and sequential. Each test correction shares the same deterministic map seams and its finding owner; one inline implementation context avoids repeated fixture reconstruction, with one independent whole-branch review at the end.

## Review Focus

- The outlier uniqueness assertion must reach `MapGenerator.Generate`, require a materialized outlier, and compare generated town IDs; it must not duplicate `SeedWorldFactory` naming logic.
- Variant influence must compare the same intra-cluster edge and deterministic roll under Canonical and Frontier, reaching the variant-controlled terrain branch.
- Same-input repeatability must compare all generated town, layout, and trail facts, with nested collection values compared by contents.

## Global Constraints

- Start from refreshed `origin/develop` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.139`; it remains the sole authored application version.
- Retire N-20 only after its full scope is verified in PR #323; preserve this active N-21 plan through its completing PR.
- Do not change production source, gameplay behavior, ADRs, feature promises, or unrelated test owners.
- Follow the check-only staged-snapshot hook and fail-fast canonical validation; do not use `--no-verify`.
- Before merge, pass fresh whole-branch review and exact-head hosted CI. After merge, verify develop-push CI, fast-forward the primary checkout, then remove this clean worktree and its exact local/upstream branch refs together.
- Preserve the separate AWS learning worktree and branch.

---

### Task 1: Retire N-20 and install N-21

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n20-map-analysis-membership.md`
- Add: `.agents/plans/2026-10-13-row17-n21-map-output-behavior.md`

**Interfaces:**
- Consumes: N-20 PR #323 source `d9e101759db695e1526b288aa0aa564d36fa36de`, squash merge `a2bcbcc2e7edf72e6d26a45c09065cfbfe53f13b`, identical tree `253aa696be0cb82283b3ba988c3f0c5e55103919`, exact-head hosted gate `38383329297`, develop-push gate `38384048089`, approved independent review, and verified paired worktree/local/upstream branch cleanup.
- Produces: active N-21 plan at development version `0.1.0-dev.139`, N-20 retirement, and dated N-20 delivery evidence in the roadmap.

- [x] **Step 1: Record N-20 delivery and select this successor.** Add PR #323 to row 17 and record its reviewed source, merge identity/tree, hosted checks, review, and paired cleanup. Point the active row to N-21, advance the version to `.139`, and state that N-20 closed actual cluster membership and legal sample accounting while this successor addresses the remaining map output behavior findings.
- [x] **Step 2: Retire N-20 and commit the plan before test edits.** Confirm N-20's entire scope is shipped and durable findings remain in the investigation/disposition documents. Delete its completed plan, add this plan, validate version and links, and commit only these planning/version changes before changing tests.

### Task 2: Prove generated outlier uniqueness

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs`

**Interfaces:**
- Consumes: `MapGenerator.Generate(SeedWorld, GameSetupDeterministicSource, GameEntropy, SaltSource?)` and the existing non-Boring outlier scenario theory.
- Produces: an end-to-end generated-world assertion that the configured outlier is present exactly once and all generated town IDs, including the outlier, are unique.

- [x] **Step 1: Strengthen the reachable generated-world theory.** In `Generate_OutlierSlot_AllTownsHaveUniqueIds`, assert exactly one generated town has `IsOutlier`, then assert the complete generated ID set has no duplicates. Keep the existing parameterized generator inputs; remove `Generate_OutlierSlot_NeverProducesDuplicateTownIds`, which only reproduces a name-pool algorithm and never calls the generator.
- [x] **Step 2: Prove RED and GREEN.** Temporarily make the generated outlier reuse an existing town ID and run the focused theory; require generation to fail on the duplicate `TownId`. Restore production code, run the focused theory, and confirm all cases materialize one outlier and pass the full generated-world assertions.
- [x] **Step 3: Commit the focused behavior correction.** Review the test diff to ensure it has no copied name-selection algorithm and commit the test plus the completed task status.

### Task 3: Prove the variant-controlled terrain behavior

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/TerrainAssignerTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/MapGeneratorBruteForceAnalysisTests.cs`

**Interfaces:**
- Consumes: `TerrainAssigner.Assign`, the deterministic source seed and salt label `terrain-0-1-variant-2`, and the actual same-cluster terrain branch in `TerrainAssigner.ClassifyEdge`.
- Produces: a paired same-edge assertion that Canonical yields OpenRange and Frontier yields Hills for a deterministic roll in the variant-controlled base-terrain range; removes the weak all-pairs aggregate test and its now-unused collector data.

- [x] **Step 1: Add the paired behavior test first.** Use one intra-cluster edge `(0, 1)` with deterministic source seed `variant-test-seed` and fixed salt `variant-2`; this produces roll `% 100 == 3`, inside the existing `< 60` branch. Assign the identical edge and inputs once with Canonical and once with Frontier, then assert Canonical is OpenRange and Frontier is Hills.
- [x] **Step 2: Witness intended RED and GREEN.** Temporarily remove the variant choice from the production branch so both variants use Hills; run the focused paired test and require Canonical to fail. Restore production code and confirm the test passes. Do not retain a production edit.
- [x] **Step 3: Remove the weak aggregate detector and unused state.** Delete `BruteForce_VariantInfluence_ProducesDifferentTerrainDistributions`, `TerrainByVariant`, and the collector accumulation used only by that test. This makes the claimed effect precise at the owning terrain assignment boundary rather than accepting any pair among four variants.
- [x] **Step 4: Correct the risk test's name.** Rename `Assign_IntraClusterEdge_GetsEasierOrEqualTerrainThanInterCluster` to state its asserted risk relationship; keep the existing risk assertion and inputs unchanged.
- [x] **Step 5: Run focused tests and commit.** Run the paired terrain test and the renamed risk test, verify the aggregate test has no remaining references, and commit the scoped test corrections.

### Task 4: Compare the complete generated world on repeat input

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`

**Interfaces:**
- Consumes: two `MapGenerator.Generate` results using the same seed, entropy, deterministic source, and salt; `Town.Layout` values include buildings, paths, tile rows, resolver version, and layout salts; trails expose IDs, endpoints, risk, terrain, water, and ride-day distance.
- Produces: a same-input comparison covering town identities, names, prosperity, coordinates, outlier flags, complete layout values, and the full trail graph and classification values, plus dated closure dispositions for the findings addressed by N-21.

- [ ] **Step 1: Expand the existing non-Boring repeatability test.** Keep its identical inputs. Compare towns ordered by ID and assert scalar town facts, every layout scalar, building and path records, tile-grid row counts and values, resolver version, and layout-salt values. Compare trails as ordered value tuples of ID, endpoint IDs, risk, terrain, water feature, and ride-day distance. Do not compare nested collections by reference or change the separate cross-salt Boring test's narrower claim.
- [ ] **Step 2: Prove the richer test detects a changed outcome.** Temporarily perturb one generated trail classification on only the second generation call and run the focused repeatability test; require it to fail on the changed trail fact. Restore production code and confirm the test passes.
- [ ] **Step 3: Record dispositions.** Add a dated N-21 finding disposition stating that the copied outlier test was removed in favor of reached generator behavior, the variant claim is now tested at its controlled branch, the risk test name matches its assertion, and repeatability covers complete generated layout and trail values. Preserve any unrelated residual test findings as open.
- [ ] **Step 4: Run owning validation and commit.** Run the focused map and terrain tests, then the full `WildBunch.GameContent.Tests` suite. Inspect changed files and commit the comparison and finding dispositions.

### Task 5: Review, publish, merge, and close out

**Files:**
- Review: the complete branch diff and staged-candidate validation result.
- Modify: this plan's checkboxes as each step is completed.

- [ ] **Step 1: Obtain a fresh whole-branch review.** Build a review package from the merge base to the final commit and review the code, tests, dispositions, N-20 retirement, and roadmap against the investigation findings and review runbook. Correct actionable findings with witnessed behavior tests and rerun affected validation.
- [ ] **Step 2: Publish and merge.** Push this branch and open one PR to `develop`. Verify the PR head equals local `HEAD`, it targets `develop`, and exact-head hosted CI passes before merging. The next successor will record this PR's delivery and paired cleanup evidence in row 17.
- [ ] **Step 3: Verify and retire the paired checkout.** Confirm develop-push CI passed, fast-forward the primary checkout, verify the merge identity and resulting tree, and remove the clean worktree before deleting this plan's exact local and upstream branch refs. Confirm the worktree and both refs are absent; preserve unrelated worktrees and refs.
