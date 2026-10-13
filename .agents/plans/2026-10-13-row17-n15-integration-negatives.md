# Row 17 N-15 Integration Negative Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task inline under the authorized roadmap goal.

**Goal:** Close the remaining N-15 integration-test gaps by proving invalid developer travel overrides and rejected store purchases leave fresh persisted state and event-derived history unchanged, while recording already-satisfied and retired portions accurately.

**Architecture:** Strengthen behavior at the existing real PostgreSQL HTTP-test owners. Do not change production behavior: the route and command guards already reject these operations. Preserve the existing transition and error contracts, read state again through a fresh request, and compare event-derived audit history to prove no write occurred.

**Tech Stack:** C#/.NET 10, xUnit, ASP.NET integration tests, PostgreSQL, Wild Bunch command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), test design and acceptance; [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17 and N-15 allocation; [whole-test quality findings](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), N-15; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), integration-test file dispositions.

**Execution Strategy:** executing-plans - the cases share one integration-test project, one PostgreSQL boundary, and one no-mutation invariant; inline sequential execution keeps the event/history oracle consistent across the related API paths.

## Global Constraints

- Base: origin/develop at 137dde7f4b21bb5c2b0e24e92e05d98b3a738905, merged PR #314.
- Deliver one PR to develop; advance Directory.Build.props exactly once to 0.1.0-dev.130.
- Retire .agents/plans/2026-10-13-row17-n14-paired-retirement.md in the first substantive commit after recording PR #314, its successful exact-head and develop-push gates, and verified worktree plus paired-branch removal.
- Use real PostgreSQL-backed API tests for persistence claims. Do not infer persistence from the command response alone.
- Prove each new test can fail by temporarily breaking its guarded behavior, observing that exact test fail, then restoring production code before continuing.
- No unrelated-criminal ledger event replay coverage is added: the feature was retired under the accepted 0.1.0 scope. Preserve remaining row 17 inventory work.
- Do not add duplicate upcaster-transition or invalid-category coverage; Registry_DuplicateTransition_ThrowsAtConstruction and the existing retired-category HTTP case already exercise those rejections.
- Do not add a second no-trail travel negative: the Application handler test already reaches a real existing town with no trail and proves no save or commit.
- No production code, ADR, or feature-matrix change is in scope.
- Require fresh whole-branch review, passing exact-head hosted CI, and passing develop-push CI before removing this plan's worktree and paired branch refs.

## Review Focus

- The blank-category request reaches its distinct HTTP guard; the existing retired-category case continues to cover invalid enum input. Both leave active travel state and event-derived audit history unchanged after fresh reads.
- Each unsuccessful purchase path is distinct and re-read from persistence; returned response state alone must not satisfy the no-purchase-effect claim. If an attempt enters a new Store action context, that context event and its beat are expected; establish Store context before the comparison baseline when proving a rejected purchase itself adds no effects.
- The unknown-town test is named for the actual nonexistent-town input and is not confused with the separately covered existing-town/no-trail case.
- Existing duplicate-upcaster rejection is recognized as complete, while retired unrelated-criminal ledger parity is not resurrected as a 0.1.0 test obligation.

---

### Task 1: Prove invalid developer travel override requests do not persist

**Files:**
- Modify: tests/WildBunch.Integration.Tests/Dev/DevTravelEndpointTests.cs

**Interfaces:**
- Consumes: the real PostgreSqlApiFactory, /api/dev/sessions/{id}/travel/force-override, player session GET, travel-context GET, and audit GET.
- Produces: a blank-category HTTP rejection case and fresh persisted-state proof for both that case and the existing retired-category case.

- [x] **Step 1: Add the blank-category negative and strengthen the existing retired-category case.** Use an active, persisted travel session and valid request envelope; capture player state, travel context and audit before each request. Require BadRequest, then obtain those views again through fresh HTTP requests and compare current town, journey status, pending override, inventory/clock facts and ordered audit entries including sequence, event type, summary and occurrence time. Neither case may observe a DevTravelOverrideForced entry.

- [x] **Step 2: Prove the blank-category test fails for the no-mutation behavior it protects.** Temporarily mutate the blank-category rejection branch to append a valid travel override while still returning its normal BadRequest response. Run the focused theory and confirm the blank case fails on its fresh travel-context or audit-history comparison. Restore the API source immediately and verify the diff contains only the intended test change.

- [x] **Step 3: Run the focused test against restored production code.** Run dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~DevTravelEndpointTests with PostgreSQL available at localhost:5435; require all cases to pass.

### Task 2: Prove rejected purchases leave persisted state and history unchanged

**Files:**
- Modify: tests/WildBunch.Integration.Tests/GameApiPurchaseTests.cs
- Modify: tests/WildBunch.Integration.Tests/GameApiValidationTests.cs

**Interfaces:**
- Consumes: the existing wrong-town, insufficient-cash, and unavailable-catalog purchase scenarios; fresh /api/games/{id} state; journal and audit reads.
- Produces: no-mutation assertions for each rejected purchase path and an accurately named unknown-town integration scenario.

- [x] **Step 1: Strengthen each rejected purchase scenario with fresh persistence reads.** Preserve the three distinct requests and their existing response assertions; after each response, GET the game and compare wallet, inventory, location and clock against the pre-request persisted state, then compare ordered journal entries and complete audit event tuples. This must prove no purchase event, wallet change, inventory change, or additional turn/log advancement was persisted. For insufficient cash, enter Store context with a valid purchase before taking the baseline; first entry to Store legitimately records TownActionContextEntered and consumes a beat even when a later purchase fails.

- [x] **Step 2: Prove the purchase assertions fail when a rejected path mutates state.** Temporarily bypass the corresponding rejection for one scenario at a time, run the matching test, and confirm its fresh persisted state or event-history assertion fails. Restore each production guard before the next scenario and keep production source unchanged in the final diff.

- [x] **Step 3: Rename the nonexistent-town integration test.** Rename TravelToUnconnectedTownReturnsSuccessFalse to describe its actual "nonexistent-town" request and not claim it exercises a real disconnected town. Keep its existing outcome assertions. The Application no-trail scenario remains the owner of that distinct condition.

- [x] **Step 4: Run the focused integration tests.** Run dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~GameApiPurchaseTests|FullyQualifiedName~GameApiValidationTests" with PostgreSQL available; require all cases to pass.

### Task 3: Record N-15 reconciliation and retire the completed successor

**Files:**
- Modify: .agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md
- Modify: Directory.Build.props
- Delete: .agents/plans/2026-10-13-row17-n14-paired-retirement.md
- Retain: .agents/plans/2026-10-13-row17-n15-integration-negatives.md

**Interfaces:**
- Consumes: PR #314 merge 137dde7f4b21bb5c2b0e24e92e05d98b3a738905, exact-head gate 38331305862, develop-push gate 38331780443, and verified N-14 worktree/ref removal.
- Produces: row 17 linked to N-15 at 0.1.0-dev.130, a dated N-14 delivery record, and explicit dispositions for each N-15 subfinding.

- [x] **Step 1: Reconcile N-15 subfindings in the roadmap.** Record that duplicate-transition rejection and the existing-town/no-trail negative are already covered; the nonexistent-town test was renamed; the retired unrelated-criminal ledger does not receive parity tests; and the new travel-override and rejected-purchase persistence negatives close the remaining test gaps. Keep row 17 in progress with later N findings active.

- [x] **Step 2: Verify successor retirement and the version already assigned.** Confirm the first N-15 plan commit `1a5949c5fcd06da35ac293dd25b1bb587176ca36` already records PR #314, its two passed hosted gates, paired worktree/branch removal, the deletion of the N-14 plan and the one-time Directory.Build.props advance to `.130`. Do not repeat the version change or deletion; add this PR's delivery evidence in the roadmap after merge.

- [x] **Step 3: Run the repository gate and inspect the committed tree.** Run py -3 tools/run.py ci --check; require the complete fail-fast gate to pass. Run git diff --check origin/develop...HEAD and verify row 17, version, plan retirement and test dispositions agree. Do not add a test asserting plan/checklist/document shape.

- [ ] **Step 4: Review, publish and close out.** Complete a fresh whole-branch review, create one PR against develop, and require passing exact-head and develop-push hosted gates before merge. Fast-forward the primary checkout, remove the clean worktree first, delete only its exact local and upstream refs, and verify all three are absent before the next plan.
