# Row 17 N-13 Hidden-Truth Read Proof Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline under the authorized roadmap goal.

**Goal:** Make the retained developer-versus-player hidden-truth boundary test prove real data is available on both routes while preventing a vacuous empty-response pass.

**Architecture:** Keep hidden truth in the guarded developer DTO and player knowledge in player read models. Use the same started session to derive its actual hidden culprit ID from the developer context, then prove the successful player journal contains useful public state without serializing that ID or internal truth markers. Preserve the independent wanted-poster behavior test that already proves a successful populated player response.

**Tech Stack:** .NET 10, xUnit, ASP.NET Core integration tests, PostgreSQL, Wild Bunch command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially test design and query/projection safety; [test-quality investigation N-13](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md#n-13-secrecy-and-post-commit-tests-can-pass-without-observing-useful-data).

**Execution Strategy:** `executing-plans` - the source change is confined to one integration-test owner and its dated audit disposition; the same committed tree then updates the roadmap, version and predecessor-plan custody.

## Global Constraints

- Start from `origin/develop` at `b9d2c62e4f9d11a55419db87c84fdac7c889847d` in this fresh canonical worktree and deliver one PR to `develop`.
- The plan-only commit remains at `0.1.0-dev.127`; advance the sole authored application version in `Directory.Build.props` to `0.1.0-dev.128` in the first substantive implementation commit.
- Retire the N-12 plan in the first substantive implementation commit; retain this plan through delivery for its next substantive successor to assess.
- Preserve the positive developer hidden-truth boundary and the successful, populated wanted-poster player test; do not hard-code a suspect's name or internal ID as a secrecy oracle.
- Do not change production behavior, player feature scope, ADRs, or feature-matrix promises. The prologue test already asserts a successful setup-phase lifecycle response and does not claim post-commit projection proof.
- Use the normal staged-snapshot hook and canonical command bus; never bypass the hook or let checks repair/stage files.
- Require whole-branch review, exact-head hosted CI and develop-push CI before merge; remove the clean worktree and its exact local/upstream refs as one verified closeout before the next plan.

## Review Focus

- A missing or failing player journal route must fail the secrecy test before an empty body can pass absence checks; the same session must provide actual hidden truth through the guarded developer context.
- Public wanted-poster success is already behaviorally covered by `GameApiWantedPostersTests`; avoid duplicating that scenario or treating a public poster name as a hidden-truth leak.
- The existing prologue response test must not be reported as proving event-derived post-commit projection behavior; `CompleteGameStart_ReturnedDto_HudProjectionIncludesGameStartedState` owns useful emitted-event output assertions.

---

### Task 1: Make the hidden-truth boundary test non-vacuous and data-backed

**Files:**
- Modify: `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`

**Interfaces:**
- Consumes: `SaloonDevContextDto.HiddenTruth`, player `JournalDto`, the current `BoringScenarioBuilder.StartingTownReady` path, and the existing positive poster response assertions in `GameApiWantedPostersTests`.
- Produces: one integration scenario that proves a real guarded developer DTO identifies its own true culprit, a successful player journal returns the created session's useful public state, and that response omits the actual hidden culprit ID and internal truth markers.

- [x] **Step 1: Consolidate overlapping hidden-truth reads.** Replace `PublicApiResponsesDoNotLeakHiddenCulpritMarkers` and `DevSaloonContextDeliberatelyExposesHiddenTruth_AndPlayerApiDoesNot` with one clearly named scenario using one started session. Keep `DevTravelContextDoesNotLeakHiddenCulpritMarkers` unchanged. Remove the hard-coded suspect-name absence checks and the duplicated wanted-poster request; the existing poster behavior test already asserts a successful result, discovered suspect, poster content, and hidden-marker absence.

- [x] **Step 2: Assert the actual developer truth and successful player result.** Require HTTP success from the dev saloon-context and player journal reads, deserialize `SaloonDevContextDto` and `JournalDto`, assert the developer truth has a nonempty culprit ID/name matching exactly one `IsTrueCulprit` suspect, and assert the journal's ID, current town, and opening lead are populated from the created session. Assert the actual `TrueCulpritId` is absent from the journal JSON, along with `trueCulpritId`, `isTrueCulprit`, `linkedSuspectIds` and `killerReleaseState`. Do not assert that the culprit's public name is absent.

- [x] **Step 3: Prove the secrecy test can fail for a real leak.** The journal endpoint reads through `GetJournalHandler` and `GameSessionReadStoreLoader.LoadJournalSnapshotAsync`; it does not use `JournalResolver`. Temporarily append `state.CaseFile.TrueCulpritId.Value` to the read-store loader's player-facing `OpeningLead`, run the focused integration test, and confirm it fails on the actual hidden-ID assertion. Restore the production file byte-for-byte and verify it has no diff. Do not alter the assertion itself or manufacture an empty error response as the mutation.

- [x] **Step 4: Run the focused integration owner.** Ensure PostgreSQL with `./tools/postgres-dev.ps1 ensure`, then run `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter FullyQualifiedName~GameApiHiddenTruthTests`. Confirm all tests in the class pass and no test is skipped (2 passed, 0 skipped). The real-ID mutation failed at the intended `DoesNotContain` assertion. An initial mutation of `JournalResolver` did not reach this endpoint; tracing the query path corrected the mutation target to `GameSessionReadStoreLoader` before the successful falsification.

### Task 2: Reconcile N-13 evidence and deliver at the next development version

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n12-fixture-readiness.md`
- Retain: `.agents/plans/2026-10-13-row17-n13-hidden-truth-read-proof.md`

**Interfaces:**
- Consumes: Task 1's strengthened public journal boundary test, the existing successful wanted-poster test, current source for the prologue and complete-start projection tests, and PR #312 delivery evidence.
- Produces: a dated N-13 disposition distinguishing the repaired secrecy test from the already-correct prologue lifecycle test; roadmap evidence for N-12 and active N-13 at `.128`; one version advance to `.128`; and successor retirement of the completed N-12 plan.

- [x] **Step 1: Correct the N-13 test disposition.** Record that the hidden-truth test now derives the secret ID from the same session's guarded developer DTO and requires useful successful player data before checking absence. Record that wanted-poster success and marker absence already belong to `GameApiWantedPostersTests`. Correct the prologue finding: the current method is `ViewPrologue_ReturnedDtoOmitsHudUntilGameStarts`, which asserts a successful lifecycle response and setup-phase omissions; the complete-start HUD test already asserts player name, health, wallet and town. Do not claim either test proves more than those observations.

- [x] **Step 2: Carry N-12 closeout evidence and activate N-13.** Update row 17 with PR #312 reviewed source `c33293634b79799c37270e830c11092094921d03`, merge `b9d2c62e4f9d11a55419db87c84fdac7c889847d`, identical tree `fff09e522d1a126ff6e85239a0c6371fac557d53`, exact-head gate `38325293558`, develop-push gate `38325755766`, and paired cleanup verification. Record that the code review was an author self-review because this harness exposed no independent review dispatch under the active no-delegation constraint. Add PR #312 to row 17's delivery list, make N-13 the current plan at `.128`, and keep row 17 incomplete.

- [x] **Step 3: Advance version and retire N-12 in the first substantive commit.** Set `Directory.Build.props` to `0.1.0-dev.128`, delete the N-12 plan, and retain N-13. Stage the intended test, investigation, roadmap, version and plan changes; commit through the normal check-only hook.

- [ ] **Step 4: Validate and review.** Run the focused `GameApiHiddenTruthTests` command from Task 1 and `py -3 tools/run.py ci --check` on the committed tree. Review the full branch against this plan, N-13, ADR-0007, ADR-0017, ADR-0030, applicable unslop profiles, and the feature matrix. State why no ADR or feature-matrix change is warranted. Require passing exact-head hosted CI and develop-push CI.

- [ ] **Step 5: Publish, merge and close out.** Open one PR targeting `develop`, verify the PR head equals the reviewed source, squash-merge only after the required review and hosted gate pass, verify source/merge tree equality and develop-push gate, fast-forward the primary checkout, then remove this clean worktree before deleting its exact local/upstream branch refs. Verify the worktree and both refs are absent before starting another plan.
