# Make Integration Scenario Fixtures Tell the Truth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Remove metadata-only and repeated fixture readiness checks while preserving direct behavior assertions for the deterministic integration scenarios that tests actually consume.

**Architecture:** This is test-infrastructure cleanup only. Scenario helpers may construct reproducible game inputs, but only observable `GameSession`, API, route, resource, or service behavior can establish that a scenario is ready; metadata copied from its own descriptor cannot. Keep setup helpers small and retain HTTP/PostgreSQL assertions at the boundary they exercise.

**Tech Stack:** .NET 10, xUnit, integration test infrastructure, Wild Bunch command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially test design and acceptance; [test-quality investigation N-12](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md#n-12-fixture-readiness-labels-and-ignored-requests-masquerade-as-preparation).

**Execution Strategy:** `executing-plans` - the catalog's self-derived metadata checks, misleading fixture names, and repeated preflight calls all obscure the same question: what real behavior does each fixture establish? Update their shared owner and consumer calls together, then verify the retained behavior assertions.

## Global Constraints

- Start from the current `origin/develop` in this fresh canonical worktree and deliver one PR to `develop`.
- Keep this plan-only commit at `0.1.0-dev.126`; advance the sole authored version in `Directory.Build.props` to `0.1.0-dev.127` in the first substantive implementation commit.
- Retire the N-11 plan in the first substantive implementation commit; retain this plan through delivery for its next substantive successor to assess.
- Preserve deterministic scenario inputs and behavioral checks for starting resources, world graph/routes, mounted travel, and starting-town services where a consumer needs them.
- Do not reintroduce RNG control endpoints, skipped town-layout dev tests, or any dev feature already retired from 0.1.0.
- Do not add production behavior, compatibility machinery, or tests that assert helper names, file locations, headings, or current metadata constants.
- Use the normal staged-snapshot hook, which validates without mutating or staging files; do not run `git commit --no-verify`.
- Require fresh whole-branch review, passing exact-head hosted CI, and passing develop-push CI before paired worktree and branch cleanup.

## Review Focus

- A scenario is accepted because its produced game/session/API behavior meets the prerequisite, not because a descriptor and observed string repeat the same literal.
- Removing repeated `AssertReady` calls must not remove the focused tests that prove fixture behavior or the API/PostgreSQL assertions belonging to a consuming feature.
- Fixture names and test titles must describe the current mounted/easy/resource/service behavior, not retired foot-travel or cache semantics.
- Do not spend test runtime repeatedly validating unrelated scenario properties before every API test.

---

### Task 1: Replace self-derived scenario signatures with behavioral fixture proofs

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalog.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedFixture.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/BoringScenarioBuilder.cs`
- Modify: `tests/WildBunch.Integration.Tests/TestInfrastructure/BoringScenarioBuilderTests.cs`
- Delete: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedDescriptor.cs`
- Delete: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedDescriptorTests.cs`
- Delete: `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalogTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiTests.cs`

**Interfaces:**
- Consumes: deterministic seeded `GameSession` creation, `BoringScenario` fixture factories, and the existing semantic assertions in `ScenarioSeedCatalog`.
- Produces: deterministic scenarios with no self-referential codec/role/service signature or cached-fixture gate; focused builder tests that assert actual created-session resources, connected routes, travel mode and preview behavior.

- [x] **Step 1: Remove metadata checks that cannot detect drift.** Delete `ScenarioSeedDescriptor`, its formatting tests, and catalog tests that only compare signature labels or test a fabricated diagnostic callback. Remove `Contract`, `DescribeShapeSignature`, `AssertCachedFixtureContract`, the current-codec equality branch, and all `Describe*Shape` methods. Keep direct `AssertCreatedSession`, `AssertTravelPreview`, `AssertTravelTurn`, starting-town service/API checks, and high-risk route behavior assertions where consumers use them.

- [x] **Step 2: Correct current scenario names and claims.** Rename `NoHorseFootTravelReady` and its fixture/test labels to describe Easy difficulty with mounted travel (the current produced state); update its use in `GameApiTests`. Remove transitional BUNCH comments that describe this shipped test fixture as a no-horse or foot-travel scenario. Rename the builder tests whose titles claim a cached fixture or service surface while asserting only current mounted/resource state. Where a test claims starting-town service readiness, assert actual available actions and store offers through the existing HTTP helper; otherwise narrow its name to the resource facts it really asserts.

- [x] **Step 3: Keep one behavior-owning test for each fixture contract.** In `BoringScenarioBuilderTests`, keep assertions on produced `GameSession` state and travel preview for mounted and Easy scenarios, connected graph/routes for the high-risk scenario, and actual initial resource facts for the starting-town scenario. Remove calls to the descriptor readiness gate. Do not add tests for the builder's returned name, record layout, or signature format.

### Task 2: Remove unrelated repeated fixture preflights from integration consumers

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevTravelEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevSaloonEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiActionsTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiArchiveTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiInvestigationActionsTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiJournalTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiPurchaseTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiStoreOffersTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiValidationTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiWantedPostersTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/OneActivePlaythroughInvariantTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/ProjectionEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/SetupPhaseGuardTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/WorldMapEndpointTests.cs`

**Interfaces:**
- Consumes: focused builder behavior coverage from Task 1 and each integration test's own setup and HTTP/PostgreSQL assertions.
- Produces: endpoint tests whose proof begins at the endpoint behavior under test, without running a full unrelated catalog validation before each case.

- [x] **Step 1: Remove the repeated `scenario.AssertReady()` preflight calls.** The `AssertReady` facade is removed in Task 1. Keep scenario construction, required `EnsureSuccessStatusCode` checks, and each test's own domain/API/HTTP/persistence assertions. Keep `AssertStartingTownReady` only in tests that actually require the starting-town action/service surface.

- [x] **Step 2: Audit surviving fixture assertions by consumer.** Search all integration tests for `AssertReady`, `AssertCachedFixtureContract`, descriptor/signature terms, and old foot/no-horse labels. Resolve every remaining production reference. Confirm actual starting-town actions/store offers, graph connectivity, and travel preview assertions remain at their current behavioral owners; do not replace deleted checks with another global preflight.

### Task 3: Record N-12 disposition and validate the behavior boundary

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row17-n11-diary-version-rebuild.md`
- Modify: `.agents/plans/2026-10-13-row17-n12-fixture-readiness.md`

**Interfaces:**
- Consumes: N-12 audit and the actual test-helper/consumer diff.
- Produces: dated N-12 disposition distinguishing removed self-derived readiness labels from retained behavior checks; roadmap evidence for PR #311 and N-12 active at `.127` until this delivery is merged; one version advance to `.127`.

- [x] **Step 1: Prove the retained assertions can detect a broken scenario.** Run the focused `BoringScenarioBuilderTests` and the integration consumers that own starting-town service, travel, and high-risk route behavior. Temporarily break one actual scenario output at its production owner (for example, make the deterministic scenario produce no mounted-travel capability) and verify the corresponding behavioral assertion fails. Restore production source byte-for-byte and confirm no production diff remains. Do not mutation-test a deleted helper or metadata string.

- [x] **Step 2: Record truthful dated status.** Mark N-12 disposition as fixture behavior tests retained and stale signature/preflight tests removed. State that RNG endpoints and town-layout dev tests were already retired earlier and are not restored. Update row 17 with PR #311 source `6c1e9499e6a495da74b209c46fa0dd2981b91501`, merge `6f0e659dfcfd734749d56c2c16349c6e4f033677`, identical tree `c1ca9b5237c5e1616565725ee385bbc670343a0e`, exact-head gate `38320009221`, develop-push gate `38320567827`, and verified paired worktree/ref cleanup. Mark N-12 active at `.127` and row 17 incomplete.

- [x] **Step 3: Advance version and retire N-11 in the first substantive commit.** Set `Directory.Build.props` to `0.1.0-dev.127`, delete the N-11 plan and retain N-12. Stage and review only the intended implementation, test-disposition, roadmap, version and plan changes, then commit through the normal check-only hook.

- [x] **Step 4: Run focused and canonical validation.** Ran the focused `BoringScenarioBuilderTests` (4/4), ensured PostgreSQL and ran the complete integration suite (331/331, no skips), then ran `py -3 tools/run.py ci --check` on commit `4edbb65` (pass). The check-only hook also passed against the clean committed tree. One commit-time hook invocation reported a changed HEAD/index after all checks passed; subsequent direct canonical gate and clean-tree hook runs passed without changing repository state. Report the existing one skipped Domain test accurately.

- [ ] **Step 5: Review and deliver.** Independently review the committed whole branch against this plan, N-12, the baseline spec, applicable unslop profiles, ADR catalogue and feature matrix. Confirm no ADR or product claim changed; record that result in PR preparation. Require exact-head hosted CI and develop-push CI to pass. Squash merge under the authorized epic flow, fast-forward the primary checkout, verify source/merge tree equality, remove this clean worktree, delete only this exact local/upstream branch pair, and verify all three absences before the next worktree.
