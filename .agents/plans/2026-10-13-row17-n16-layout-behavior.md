# Row 17 N-16 Town Layout Geometry and Placement Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task.

**Goal:** Replace weak town-layout generator assertions with behavioral proof of required service uniqueness, actual spur geometry and placement, coherent generated layout geometry, and complete same-seed determinism.

**Architecture:** `TownLayoutGenerator` in GameContent generates the persistent town layout consumed by the Domain `TownLayout` and browser renderer. This plan changes generator tests and dated test-finding dispositions only; it preserves the production layout algorithm, persisted layout shape, salts, town-service rules, and player-facing feature scope.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.GameContent.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), N-16; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `TownLayoutGeneratorTests`.

**Execution Strategy:** `executing-plans`, inline and sequential. The test scenarios share one direct generator fixture and one output contract; one implementation task avoids repeated PR/version/review overhead for the same layout behavior.

## Review Focus

- Required service identity must be asserted per kind; a total building count cannot prove a required Store, Sheriff, Saloon, or Telegraph exists exactly once.
- Spur behavior must prove both generated road cells and an actual building at each palette's spur placement; an unchanged total building count proves no spur behavior.
- The fixed road, zones, spawn, trailheads, service footprints, and logical positions must agree as one layout geometry consumed by the renderer.
- Same seed and town inputs must reproduce every `TownLayout` field, including paths, tile grid, resolver version and salts, not only building count and positions.
- One existing empty-path characterization remains because path generation is currently disabled; the duplicate assertion is removed without introducing paths as a 0.1.0 feature.

## Global Constraints

- Start this fresh worktree from current `develop` at `8fe1c426b9f28428765456fe1280e6036f880966` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.133`; it remains the sole authored application version.
- Preserve town layout generation, persisted output, normal salt derivation, renderer contracts and existing service rules; do not add an editor or developer salt override.
- Change no production code, event or persistence contract, feature promise, ADR decision, or map interaction.
- Prove each strengthened behavior test can fail by temporarily breaking the corresponding generator behavior, then restore production source exactly before commit.
- Compare the final test disposition with ADR-0021 and ADR-0039 and `docs/features.md`; state why the durable decisions and current product truth remain unchanged.
- Require the focused GameContent tests, canonical check-only commit gate, fresh whole-branch review, passing exact-head hosted gate, and passing develop-push gate before cleanup.
- After integration, fast-forward the primary checkout, remove this clean worktree, delete only its exact local/upstream branch refs, and verify all three are absent before the next plan worktree.

### Task 1: Record PR #317 and install this JIT successor plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n16-entropy-behavior.md`
- Add: `.agents/plans/2026-10-13-row17-n16-layout-behavior.md`

**Interfaces:**
- Consumes: PR #317 source `f8023c3c612ef4dee48b1865fdfed08cbded212b`, squash merge `8fe1c426b9f28428765456fe1280e6036f880966`, exact-head gate `38347273432`, develop-push gate `38347763686`, fresh whole-branch review and verified paired worktree/ref cleanup.
- Produces: current row-17 pointer to the layout proof plan, development version `0.1.0-dev.133`, and retirement of the completed `.132` plan.

- [x] **Step 1: Record PR #317 and install this successor plan.** Add the merged PR to row 17, record its source/merge SHAs and passing hosted runs, review result, primary checkout fast-forward and paired worktree/branch cleanup; state that only travel entropy/difficulty findings are closed and the remaining N-16 findings stay open. Replace the active-plan pointer with this layout plan, advance the version once to `.133`, delete the completed `.132` plan, and commit the successor artifact before implementation.

### Task 2: Replace weak TownLayout generator checks with generated behavior proof

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Review unchanged: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorLayoutSaltsTests.cs`
- Review unchanged: `tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorLayoutSaltsTests.cs`

**Interfaces:**
- Consumes: current `TownLayoutGenerator.GenerateLayout`, `BuildingLayoutCatalog` palette geometry, the persisted `TownLayout` fields, and the existing map-level required-service/determinism scenarios.
- Produces: direct layout tests that fail when required kinds, spur geometry/placement, renderer geometry or deterministic output is broken, plus a dated disposition closing only the delivered N-16 layout finding.

- [x] **Step 1: Replace required-building smoke with per-kind generated coverage.** Replace the supported-palette set-membership assertion and the all-prosperities total-count assertion with one scenario over every supported palette and every `TownProsperity`, requiring exactly one Store, Sheriff, Saloon and Telegraph, exactly two Trailheads, and no unexplained extra building. Update the fixture helper so the `LayoutDeterministicSource` town identity matches the `townId` passed to the generator.
- [x] **Step 2: Prove actual spur geometry and placement.** Replace the equal-building-count spur test and left-side heuristic with fixed independent cases for west, east, two-spur left/right and two-spur right/left layouts. For each supported spur strategy, assert the expected junction and extension cells and require a generated service building at every corresponding spur building center.
- [x] **Step 3: Consolidate renderer geometry and complete determinism.** Fold the 10-by-10 grid, central two-column road, side building zones, centered spawn, paired trailheads, building footprints, tile-center positions and vertical distribution assertions into one coherent no-spur layout scenario. Strengthen same-seed comparison to include ordered buildings and paths, spawn, prosperity, every tile-grid row, resolver version and layout salts. Retain the default resolver-version assertion and exactly one honest empty-path characterization; remove its duplicate.
- [x] **Step 4: Prove the new checks fail on broken behavior and record the disposition.** Run temporary mutations that remove/duplicate a required service, suppress spur placement, shift a road column, and force identical inputs to produce a different building view on the second generator invocation; require the owning behavior test to fail for each mutation and restore source byte-for-byte. After restoration, run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter FullyQualifiedName~TownLayoutGeneratorTests` and then the full `WildBunch.GameContent.Tests` project. Add a dated N-16 disposition describing the repaired service/spur/geometry/determinism oracles and removed duplicate/weak tests; update the file-level disposition without recording test output as a development receipt.
- [x] **Step 5: Inspect and commit the test disposition.** Confirm the layout-salt and map-level tests remain unchanged and still own their distinct behaviors, inspect the final diff to confirm production code is unchanged, and commit through the normal check-only hook.

### Task 3: Review, publish and close out the layout proof slice

**Files:**
- Review: `docs/decisions/ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md`
- Review: `docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md`
- Modify: `docs/features.md`
- Review: the committed plan, investigation dispositions and test diff

**Interfaces:**
- Consumes: the committed behavior proof, N-16 dated dispositions, version `.133`, and the current `develop` merge base.
- Produces: one reviewed PR merged to `develop` only after exact-head hosted CI succeeds, followed by paired worktree/ref cleanup after the develop-push gate succeeds.

- [x] **Step 1: Complete author checks and fresh review.** Compare the actual diff with ADR-0021, ADR-0039 and `docs/features.md`; add direct TownLayoutGenerator behavior-test evidence to PG-003 while confirming its product promise, dependency, release disposition, overall assessment and seed boundaries do not change. Obtain a fresh whole-branch review against `develop` and resolve actionable findings with focused proof.
- [ ] **Step 2: Publish one PR and require exact-head hosted CI.** Push this task branch, open a PR targeting `develop`, read back its exact head and body, and mark it ready only after review is clear. Merge only after the canonical hosted gate passes for the exact local HEAD.
- [ ] **Step 3: Fast-forward and verify the develop-push gate.** Fast-forward the clean primary checkout to the merge commit, verify it is clean, and require the push-triggered canonical gate for that merge SHA to pass.
- [ ] **Step 4: Remove the paired execution unit.** Verify this worktree is clean and at the published PR head, remove it, delete its exact local and upstream branch refs, then verify the worktree and both refs are absent. Preserve the separate AWS worktree and every unrelated branch.
