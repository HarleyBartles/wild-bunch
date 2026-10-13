# Row 17 N-17 Travel Seed Fixture Behavior Plan

> **For agentic workers:** Use the repository's inline `executing-plans` lane for this bounded slice.

**Goal:** Replace self-checking travel seed-catalog guardrails with behavior proof at the game-session consumer, and keep the test fixture catalogue limited to real, currently used scenarios.

**Architecture:** Test-only GameContent changes. Seed-code representation remains owned by `SeedWorldResolver` codec tests. `TravelTestSeedCatalog` owns only deterministic travel-session setup fixtures; production seed resolution, game setup, entropy, world generation and event behavior remain unchanged.

**Tech Stack:** C#/.NET 10, xUnit, `WildBunch.GameContent.Tests`.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), N-16; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), travel seed-catalog entries.

**Execution Strategy:** `executing-plans`, inline and sequential. The slice changes test fixture ownership and its consumer proof in one GameContent test project.

## Review Focus

- A real `SeededNewGameFactory` consumer must prove that the Boring and Classic travel fixtures resolve the same seed-owned world facts while their salt-source modes differ as intended.
- Test seed-code round trips must not duplicate the independently owned codec behavior tests or imply a public UUID byte contract.
- Every retained `TravelTestSeedCatalog` entry and helper must have a current behavior-test consumer; comments must describe only behavior the test actually exercises.
- The gang-candidate culprit-eligibility assertion belongs with the roster behavior tests, not in a travel fixture guardrail file.
- No production source, gameplay behavior, event schema, event payload, ADR or feature promise changes.

## Global Constraints

- Start from `develop` at `72c7654aa306ccd64757f5b8bc658e4de649d64f` and deliver one PR to `develop`.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.135`; it remains the sole authored application version.
- Preserve the single production seed-code and setup behavior; do not freeze codec bytes or expand the supported public seed-code contract.
- Remove catalog-only round-trip assertions only after confirming the remaining behavior tests reach the same catalog-to-factory path and existing codec tests own format behavior.
- Prove the consumer test fails when the catalog's seed world is deliberately changed and when the expected entropy salt posture is deliberately inverted; restore all test/source files after each mutation.
- Run focused GameContent tests, the full GameContent test project, and the canonical check-only gate; obtain fresh whole-branch review and passing exact-head and develop-push hosted gates before merging.
- After integration, fast-forward the primary checkout, then retire this clean worktree and its exact local/upstream branch refs as one paired closeout. Verify the live upstream directly and preserve the separate AWS learning worktree and branch.

### Task 1: Record PR #319 and install this JIT successor plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n16-roster-behavior.md`
- Add: `.agents/plans/2026-10-13-row17-n17-travel-seed-fixture-behavior.md`

**Interfaces:**
- Consumes: PR #319 source `d4e155d34ad6b206c58707a7a50c8b12ce42221e`, squash merge `72c7654aa306ccd64757f5b8bc658e4de649d64f`, exact-head gate `38356918069`, develop-push gate `38357541663`, completed roster proof, and verified worktree/local/upstream branch cleanup.
- Produces: current row-17 pointer to this JIT plan, development version `0.1.0-dev.135`, retirement of `.134`, accurate closure status for roster and layout findings, and a dated note that the previously named next-town civic-clue test is absent from current `SeededNewGameFactoryTests` and must not be planned as though it exists.

- [ ] **Step 1: Record the merged roster slice and install this plan.** Add PR #319 and its reviewed source, merge, review, exact-head/develop-push gates and paired cleanup to row 17. Replace the active-plan pointer, advance the version once to `.135`, delete the completed `.134` plan, and correct dated N-16 wording so roster/layout are closed while travel-catalog, seeded-factory, seed-world-factory, graph and map findings remain accurately scoped. Record that the cited next-town civic-clue test is not in the current source inventory. Keep the spec's existing paired worktree/branch cleanup rule as the durable authority; do not duplicate it with a second rule. Commit the successor artifacts before test changes.

### Task 2: Make travel seed fixtures prove consumed behavior

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalog.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelTestSeedCatalogGuardrailTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/TravelEntropyVarianceTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs`
- Review unchanged: `src/WildBunch.GameContent/NewGame/SeedWorldResolver.cs`
- Review unchanged: `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`

**Interfaces:**
- Consumes: `SeedWorldResolver.CreateRepresentativeSeedCode`, `TravelTestSeedCatalog.CreateSession` / `CreateSessionWithRuntimeSalt`, `SeededNewGameFactory`, and the retained roster's culprit-eligibility behavior.
- Produces: test evidence from actual game sessions that seed-owned world facts are stable across entropy modes while salt posture follows entropy; a fixture catalogue whose entries and helpers all have current consumers; and roster eligibility asserted in its owning test suite.

- [ ] **Step 1: Inspect the current catalog consumers and codec owner.** Confirm every `TravelTestSeedCatalog` member reference with repository search, and verify `SeedWorldResolver` tests already exercise seed encoding/decoding. Do not replace those codec tests or rely on repeated `ResolveSeedCode -> Resolve` smoke assertions.
- [ ] **Step 2: Replace round-trip guardrails with consumer behavior.** Strengthen the existing Boring-versus-Classic comparison to assert the actual generated seed-owned world signature is equal while the salt-source modes differ. Keep assertions limited to seed-owned world facts and the entropy posture this test owns; do not snapshot unrelated roster, case, or UI output.
- [ ] **Step 3: Retire unsupported catalog inventory and correct fixture documentation.** Remove unused seed entries, route helpers, and explicit-start overloads only after search proves no consumer. Remove stale BUNCH-94 transitional/future claims and route/loadout descriptions that no current test reaches. Keep the two current entropy-mode entries and the session factories needed by behavior tests. Remove duplicated catalog codec-roundtrip tests; retain independent codec tests at their owner.
- [ ] **Step 4: Put culprit eligibility with roster behavior.** Move the existing all-candidates culprit-eligibility assertion into `CaseCharacterRosterTests`, keeping the claim intact and removing only its misplaced copy from travel catalog guardrails.
- [ ] **Step 5: Falsify the test and record disposition.** Temporarily change the Boring fixture's seed-owned descriptor so the consumer world signature diverges, confirm the behavior test fails for that difference, then restore it. Temporarily invert the expected entropy salt posture, confirm that assertion fails, then restore it. Run `dotnet test tests/WildBunch.GameContent.Tests/WildBunch.GameContent.Tests.csproj --filter FullyQualifiedName~TravelTestSeedCatalogGuardrailTests` and focused tests covering the updated entropy/roster files, then run the full GameContent test project. Update both investigation disposition tables based on the final behavior tests, not merely test names or output counts.

### Task 3: Review, publish and close out the travel fixture slice

**Files:**
- Review: `docs/decisions/ADR-0021-uuid-shaped-setup-seeds-resolve-to-legal-starting-world-descriptors.md`
- Review: `docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md`
- Review: `docs/features.md`
- Review: the committed plan, investigation findings, fixture changes and tests

**Interfaces:**
- Consumes: committed consumer behavior proof, accurate dated N-16 disposition, version `.135`, and current `develop` merge base.
- Produces: one fresh whole-branch-reviewed PR merged to `develop` only after its exact-head hosted gate passes, followed by fast-forward, passing develop-push gate, and paired worktree/ref cleanup.

- [ ] **Step 1: Complete author checks and fresh whole-branch review.** Confirm this slice does not change seed-code compatibility, entropy semantics, product behavior, or durable architecture. Confirm catalog deletion removes only unused test fixtures and that the roster assertion remains behaviorally covered at its owning suite. Resolve actionable review findings with focused proof.
- [ ] **Step 2: Publish one PR and require exact-head hosted CI.** Push this task branch, open a PR targeting `develop`, read back its exact head and body, and mark it ready only after review is clear. Merge only after the canonical hosted gate passes for the exact local HEAD.
- [ ] **Step 3: Fast-forward and verify the develop-push gate.** Fast-forward the clean primary checkout to the merge commit, verify it is clean, and require the push-triggered canonical gate for that merge SHA to pass.
- [ ] **Step 4: Remove the paired execution unit.** Verify this worktree is clean and at the published PR head, remove it, delete its exact local and upstream branch refs, then verify the worktree and local ref are absent and query the live upstream to verify its ref is absent. Preserve unrelated worktrees and branches; never infer remote absence from a missing remote-tracking ref.
