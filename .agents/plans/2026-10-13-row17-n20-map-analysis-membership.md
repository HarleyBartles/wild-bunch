# Row 17 N-20 Map Analysis Membership Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the map-generator brute-force analysis measure cluster separation using the generator's actual cluster assignments and account for every legal generated world.

**Architecture:** Test-only change in `MapGeneratorBruteForceAnalysisTests`. Use `ClusterPlacementGenerator.Place` with the same seed, entropy, deterministic source, and salt as the generated scenario. Cluster membership comes from its returned assignments; pair distances come from its returned placements. The collector must report and propagate any failed generation instead of silently dropping that sample. Production generation, world shape, events, persistence, and player behavior remain unchanged.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.GameContent.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), map-suite finding; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `MapGeneratorBruteForceAnalysisTests.cs`.

**Execution Strategy:** `executing-plans`, inline and sequential. The collector's cluster classification and generation-failure accounting share one matrix and one test owner. A sensitivity mutation for collapsed cluster assignments and a separate injected generation failure verify the corrected test harness; one fresh whole-branch review checks the resulting branch.

## Review Focus

- Intra-cluster versus inter-cluster classification must use `ClusterPlacementGenerator.Place`'s returned assignment for each generated town slot, not a threshold derived from measured distance.
- The analysis must account for the full legal 4,224-world matrix: 4 variants × 2 densities × 4 cluster counts × (6 base-town counts + 5 legal outlier-town counts) × 4 entropy values × 3 salts.
- An unexpected `MapGenerator.Generate` failure must fail analysis with the scenario identity; it must not be caught and omitted from statistical denominators.
- No production generator or player-facing behavior change is warranted by this test-quality finding.

## Global Constraints

- Start from `develop` at `a7551daaa796b112cf92c2636c9a69cefd4bcb78` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.138`; it remains the sole authored application version.
- Retire N-19 because its graph-test scope shipped in PR #322 and its delivery and paired cleanup are verified in row 17.
- Keep changes in the existing GameContent analysis test owner and its finding dispositions. Do not change production source, the separate exhaustive generator-invariant matrix, feature claims, or ADRs.
- Run the focused cluster-analysis test and complete GameContent suite; let the normal staged-snapshot commit hook run the canonical `py -3 tools/run.py ci --check` gate. Obtain fresh whole-branch review and passing exact-head and develop-push hosted gates before merging.
- After integration, fast-forward the primary checkout and remove this clean worktree together with its exact local and upstream branch refs. Preserve the separate AWS learning worktree and branch.

---

### Task 1: Retire N-19 and install the map-analysis successor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n19-graph-minimum-degree.md`
- Add: `.agents/plans/2026-10-13-row17-n20-map-analysis-membership.md`

**Interfaces:**
- Consumes: PR #322 reviewed source `d44768a9f039e66cb8161f04be63b0147e997b29`, squash merge `a7551daaa796b112cf92c2636c9a69cefd4bcb78`, identical source/merge tree `8eddd6e0d48f347999bfd6b9913f16848c110587`, exact-head hosted gate `38372819166`, develop-push gate `38373587841`, approved whole-branch review, and verified N-19 worktree/ref cleanup.
- Produces: row 17's active N-20 map-analysis plan, development version `0.1.0-dev.138`, N-19 retirement, and dated N-19 delivery evidence.

- [x] **Step 1: Record N-19 and select this live slice.** Add PR #322's source, merge, review, hosted gates, and verified paired cleanup to row 17. Point the row to N-20, advance the version once to `.138`, and state that graph-test ownership is closed while map-analysis and other map-generator findings remain open.
- [x] **Step 2: Retire N-19 and commit the successor artifacts.** Delete the completed N-19 plan, add this plan, validate the row and version, then commit the roadmap/version/plan installation before changing tests.

### Task 2: Make brute-force map analysis report actual memberships and all legal samples

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/MapGeneratorBruteForceAnalysisTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Review unchanged: `src/WildBunch.GameContent/NewGame/ClusterPlacementGenerator.cs`, `src/WildBunch.GameContent/NewGame/MapGenerator.cs`, and `tests/WildBunch.GameContent.Tests/MapGeneratorTests.cs`

**Interfaces:**
- Consumes: `ClusterPlacementGenerator.Place`'s slot-keyed coordinate and cluster-assignment dictionaries; `GameSetupDeterministicSource.Roll`, which is deterministic by seed and label; the existing 4,224 legal sample matrix in `BruteForceDataCollector.CollectAll`.
- Produces: cluster-pair distance statistics keyed by actual assignment, an explicit successful-world cardinality check, and a dated disposition that leaves unrelated map-generator findings open.

- [x] **Step 1: Make the analysis sample obligation explicit.** Add an expected legal-world count derived from the matrix dimensions and the existing max-town outlier exclusion, then assert the collector produces all 4,224 worlds. Temporarily inject a generation exception for the first legal sample while the old catch remains; the focused test fails with a 4,223-of-4,224 count. Restore production source and confirm the unmutated test passes.
- [x] **Step 2: Classify cluster pairs through generated membership.** For each matrix input, obtain placement and assignment dictionaries from `ClusterPlacementGenerator.Place` using the same seed, entropy, stateless source, and salt as `MapGenerator.Generate`. Accumulate every base-town pair as intra-cluster when its slot assignments match and inter-cluster when they differ. Remove the `<100` and `>150` distance classification thresholds; retain the independent 1.5 average-separation expectation, which passes for the real-membership matrix.
- [x] **Step 3: Stop silently omitting failed worlds.** Remove the catch-and-continue path around `MapGenerator.Generate`. Wrap and rethrow failures with variant, density, cluster count, outlier mode, town count, entropy, and salt. Repeating the injected first-sample exception makes analysis fail immediately with that sample identity and inner exception; restore production source and rerun the focused test.
- [x] **Step 4: Prove actual membership is behavior-sensitive.** Temporarily force the production placement generator to assign all towns to one cluster; `BruteForce_ClusterSeparation_IsReal` fails because no inter-cluster pairs were collected. Restore production source and confirm the focused test passes.
- [x] **Step 5: Update finding dispositions.** Record that pair labels now come from actual slot assignments, the legal matrix cardinality is explicit, and unexpected generator exceptions cannot disappear from analysis. Keep the copied outlier-name test, variant influence, terrain-labelled test, and rich-world-repeatability findings open for later slices.
- [x] **Step 6: Commit the analysis correction.** Run the focused cluster-analysis test and complete GameContent suite, inspect the intended diff, then commit without bypassing the check-only hook. The hook ran the canonical `py -3 tools/run.py ci --check` gate on the staged candidate.

### Task 3: Review, publish, and close out

**Files:**
- Review: complete branch diff and staged-tree result.
- Modify: the active plan checkboxes as each step is completed.

- [x] **Step 1: Obtain fresh independent whole-branch review.** Review the committed branch against this plan, the test-quality investigation, and repository review runbook. Correct actionable findings and rerun affected checks through the normal hook; write no development receipt files into the repository. The review of `a7551daaa796b112cf92c2636c9a69cefd4bcb78..6ce0d36edd1930f55d4a4816ceb5fe1313555c5a` approved without actionable findings; the review report is in branch scratch.
- [ ] **Step 2: Publish and merge.** Push this branch, open one PR to `develop`, verify the diff and development version, obtain passing exact-head hosted CI, and merge only after review and gates are clean. The successor plan will record this PR's merge and cleanup evidence in row 17.
- [ ] **Step 3: Complete paired cleanup.** Fast-forward the primary checkout to the merge and verify the develop-push gate passes. Confirm this worktree is clean and merged, remove it, delete its exact local and upstream branch refs, and verify the worktree and both refs are absent. Preserve unrelated worktrees and branches.
