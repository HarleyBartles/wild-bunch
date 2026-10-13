# Row 17 N-16 Roster Behavior Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task.

**Goal:** Replace outcome-searched roster and suspect-feature tests with fixed, reproducible inputs that prove deterministic selection, meaningful variation, compatible feature assignment, and the retained content constraints.

**Architecture:** This is a test-quality slice owned by GameContent. `CaseCharacterRoster` and `CaseSuspectFeaturePool` remain unchanged; direct tests use fixed inputs through `GameSetupDeterministicSource` and preserve the existing separation between seeded generated facts and public seed-code compatibility.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.GameContent.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), N-16; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), `CaseCharacterRosterTests`.

**Execution Strategy:** `executing-plans`, inline and sequential. The work has one test owner and one shared seed fixture; sequential execution keeps the seed proof, disposition, and mutation evidence in one review context.

## Review Focus

- Same explicit seed must yield the same gang roster and feature assignments across independent deterministic-source instances.
- Explicit local fixture values `CreateSeedCode(7)` and `CreateSeedCode(8)` must yield different gang roster and feature assignment signatures; no test may search until a favorable outcome appears.
- `CreateSeedCode(7)` must exercise at least one compatible additional feature while preserving pairwise compatibility and the existing per-suspect classic-nod budget.
- Removing a pool nonempty smoke assertion must not erase the retained source-backed content, role-concealment, or incompatibility behavior checks.
- Internal fixture seeds must not be described as a stable public UUID codec contract.

## Global Constraints

- Start from `develop` at `e6fdd6bd227167c233ca102bca94aa4e8ad4da65` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.134`; it remains the sole authored application version.
- Preserve production roster, feature assignment, setup, event, persistence, and player-facing behavior; do not edit production code.
- Remove outcome-driven seed searches and redundant nonempty pool minimums only; retain independently supported source/content, concealment, incompatibility, and classic-nod budget assertions.
- Do not establish a stable public seed-code byte contract or change ADR-0021, ADR-0039, or `docs/features.md`.
- Prove the fixed-seed behavior tests fail under temporary regressions to seed sensitivity and compatible-feature assignment, then restore production source byte-for-byte.
- Require focused roster tests, the full GameContent test project, the canonical check-only commit gate, a fresh whole-branch review, a passing exact-head hosted gate, and a passing develop-push gate before cleanup.
- After integration, fast-forward the primary checkout; remove this clean worktree and its exact local/upstream branch refs as one paired cleanup; verify local refs directly and query the live upstream rather than trusting remote-tracking refs alone; verify the worktree and both refs are absent and preserve unrelated worktrees and branches.

### Task 1: Record PR #318 and install this JIT successor plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n16-layout-behavior.md`
- Add: `.agents/plans/2026-10-13-row17-n16-roster-behavior.md`

**Interfaces:**
- Consumes: PR #318 source `2c4651552cfbf2837d451703dedd0bdd1a222439`, merge `e6fdd6bd227167c233ca102bca94aa4e8ad4da65`, exact-head gate `38351492010`, develop-push gate `38351890224`, approved whole-branch review, primary fast-forward, and verified paired worktree/ref cleanup.
- Produces: current row-17 pointer to this roster plan, development version `0.1.0-dev.134`, retirement of the completed `.133` plan, removal of the stale duplicate row-17 copy in the dependency table, and a dated disposition closing only the roster seed-search finding after Task 2.

- [x] **Step 1: Record PR #318 and install the successor plan.** Add the merged PR to row 17 and record its source/merge SHAs, both passing hosted gates, review result, primary fast-forward, and paired cleanup. State that only layout test findings closed; roster, seed-world generator, graph, and map findings remain open. Replace the active-plan pointer with this plan, advance the version once to `.134`, delete the completed `.133` plan, and remove the accidentally copied full delivery row from the separate prerequisites-and-outputs table, preserving that table's concise row-17 entry. Commit the successor artifact before test changes.

### Task 2: Replace roster outcome searches with explicit behavior fixtures

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Review unchanged: `src/WildBunch.GameContent/NewGame/CaseCharacterRoster.cs`
- Review unchanged: `src/WildBunch.GameContent/NewGame/CaseSuspectFeaturePool.cs`
- Review unchanged: `src/WildBunch.GameContent/NewGame/GameSetupDeterministicSource.cs`

**Interfaces:**
- Consumes: `CaseCharacterRoster.SelectGangRoster`, `CaseSuspectFeaturePool.SelectAssignedFeatures`, their existing feature-compatibility rule, and `GameSetupDeterministicSource`.
- Produces: deterministic assertions over fixed same/different inputs, one fixed feature-rich assignment case, and a dated N-16 disposition that closes only this roster subfinding.

- [x] **Step 1: Replace the two seed-search helpers with explicit inputs.** Remove `FindVaryingSeed` and `FindFeatureRichSeed`. Use `CreateSeedCode(7)` and `CreateSeedCode(8)` for the fixed variation pair, and `CreateSeedCode(7)` for the feature-rich scenario. Require same-seed signatures to match across independent source instances and both signatures to differ across the fixed pair. Do not assert fixed public codec bytes or freeze unrelated roster contents.
- [x] **Step 2: Remove redundant pool minimums while retaining actual content obligations.** Remove the three redundant `Assert.NotEmpty` checks whose failure is already detected by independently meaningful required-content assertions. Keep source-backed candidate/alias checks, the role-concealment negative, the explicit incompatible feature pairs, pairwise compatibility of assigned additions, and the per-suspect two-classic-nod maximum.
- [x] **Step 3: Prove the tests fail on broken selection behavior.** Temporarily make all deterministic scores seed-insensitive and confirm the fixed-seed roster variation assertion fails; make only feature-label scores seed-insensitive and confirm the feature-assignment variation assertion fails; then suppress compatible additional-feature assignment and confirm the fixed feature-rich scenario fails. Restore all production source byte-for-byte after each mutation. Run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter FullyQualifiedName~CaseCharacterRosterTests`, then the full `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj` project.
- [x] **Step 4: Record the dated N-16 disposition and commit.** State that fixed inputs now prove determinism, variation and compatible feature assignment without outcome-driven search; identify retained content/concealment/compatibility budgets and removed redundant nonempty assertions. Leave seed-world generator, graph and map findings open. Confirm the production files are unchanged, inspect the final test/disposition diff, and commit through the normal check-only hook.

### Task 3: Review, publish and close out the roster proof slice

**Files:**
- Review: `docs/decisions/ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md`
- Review: `docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md`
- Review: `docs/features.md`
- Review: the committed plan, investigation dispositions, and test diff

**Interfaces:**
- Consumes: committed fixed-input behavior proof, dated N-16 dispositions, version `.134`, and current `develop` merge base.
- Produces: one fresh whole-branch-reviewed PR merged to `develop` only after its exact-head hosted gate passes, followed by fast-forward, passing develop-push gate, and paired worktree/ref cleanup.

- [ ] **Step 1: Complete author checks and fresh whole-branch review.** Compare the actual diff with ADR-0021 and ADR-0039; verify the internal fixtures do not promise stable public seed bytes or change setup semantics. Confirm `docs/features.md` remains truthful because product behavior does not change. Obtain a fresh whole-branch review against `develop` and resolve any actionable finding with focused proof.
- [ ] **Step 2: Publish one PR and require exact-head hosted CI.** Push this task branch, open a PR targeting `develop`, read back its exact head and body, and mark it ready only after review is clear. Merge only after the canonical hosted gate passes for the exact local HEAD.
- [ ] **Step 3: Fast-forward and verify the develop-push gate.** Fast-forward the clean primary checkout to the merge commit, verify it is clean, and require the push-triggered canonical gate for that merge SHA to pass.
- [ ] **Step 4: Remove the paired execution unit.** Verify this worktree is clean and at the published PR head, remove it, delete its exact local and upstream branch refs, then verify the worktree and local ref are absent and query the live upstream to verify its ref is absent. Preserve unrelated worktrees and branches; never infer remote absence from a missing remote-tracking ref.
