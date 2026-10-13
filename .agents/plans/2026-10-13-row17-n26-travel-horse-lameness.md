# Row 17 N-26 Travel Run Horse Lameness Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task.

**Goal:** Replace the travel test that starts with a dead horse and asserts only a nonnull result with a reached command-path scenario proving that a mounted run itself can lame a viable horse, switch the journey to foot travel, recalculate remaining pacing, synchronize inventory and replay the recorded result.

**Architecture:** GameSession owns the journey command boundary under ADR-0013. The forced foe is reached through the event-backed developer travel override and normal day advancement; do not synthesize a pending encounter or patch horse state after journey start. Resolution records the actual result in JourneyEncounterResolved with an absolute journey snapshot. Replay applies those recorded facts and does not roll the encounter again under ADR-0028. ADR-0031 governs the developer override used to reach this path; no architecture or gameplay decision changes are expected.

**Tech Stack:** C#/.NET 10, xUnit, WildBunch.Domain.Tests.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), row 17; [whole-test quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), N-18; [.NET test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), TravelResolverTests.cs.

**Execution Strategy:** executing-plans, inline and sequential. This is one Domain command/event/replay invariant with one test owner. Do not delegate pieces or broaden to unrelated travel findings.

**Governing Decisions:** ADR-0013 keeps travel within the session-owned aggregate boundary. ADR-0028 requires event-derived state, absolute event snapshots and exact replay. ADR-0031 makes the forced travel override event-backed and one-shot. The test exercises these decisions without changing them. The author and reviewer must independently compare the final diff against these ADRs and the feature matrix; no ADR or feature-matrix edit is expected if the behavior remains as currently decided.

**Scope:** Rewrite only ResolveJourneyEncounterRunCanLameMountedHorseAndFallBackToFoot. Start with a healthy mounted horse, use ordinary travel advancement to reach a forced foe encounter, resolve a successful mounted run that crosses the Challenging lameness threshold but not its death threshold, and prove Foot mode, remaining distance, recalculated remaining days, horse/saddle inventory and the recorded event snapshot. Rehydrate the complete event stream and assert the same journey and inventory facts. Update only the N-18 lame-run finding and TravelResolverTests.cs disposition. Leave the remaining N-18 travel and generator findings open. No production changes, new gameplay, feature promise, or ADR changes are expected.

## Review Focus

- Before the run, prove that the encounter was reached by ForceDevTravelOverride(ForCategory(Foe)) plus AdvanceJourneyDay, the horse is alive and capable of mounted travel, and its state is below the Challenging lameness threshold.
- Use an OpenRange route with route water and a known partial remaining distance so ordinary upkeep does not independently lame the horse and changing from mounted to foot requires one additional travel day.
- Use the deterministic run-roll seam to reach the successful mounted-run branch. Assert the encounter result succeeds and causes the expected exhaustion delta; do not infer causality from a pre-lamed horse.
- Assert the command result and JourneyEncounterResolved.JourneySnapshot report Foot mode, unchanged remaining distance, increased remaining days, a live lame horse and ContinuedOnFoot. Assert the player's Horse and Saddle remain present and its horse state matches the journey.
- Rehydrate from the complete actual event history and assert the same travel mode, remaining distance/days, horse state and inventory. Do not compare incidental event counts, ordering, narration, or implementation method names.
- The mutation proof must preserve the TravelMode change but suppress the RemainingDays recalculation in TravelJourney.RecalculatePacing; the new test must fail specifically because remaining days stay at the mounted value. Restore immediately and rerun the test.

---

### Task 1: Retire N-25 and install N-26

**Files:**
- Modify: .agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md
- Modify: Directory.Build.props
- Delete: .agents/plans/2026-10-13-row17-n25-saloon-replay-proof.md
- Add: .agents/plans/2026-10-13-row17-n26-travel-horse-lameness.md

**Interfaces:**
- Consumes: N-25 PR #328, reviewed source 049c7712683488f66edae0dd49ea62359b801464, squash merge 57129bcaca0ca9ac71ef710d95477ca1acf95c89, identical tree 2c330e4e561612975f7ab0840136c17762ac5d84, no-finding independent review, exact-head hosted gate 38413750758, develop-push gate 38415284951, and verified paired checkout/ref cleanup.
- Produces: active N-26 plan at 0.1.0-dev.144, N-25 retirement, and dated N-25 delivery evidence in row 17.

- [ ] **Step 1: Verify successor boundary.** Confirm current develop and primary checkout are at 57129bc; verify N-25 source, merge/tree identities, independent review, exact-head hosted gate, develop-push gate, and that N-25's worktree and paired local/upstream refs are absent. Confirm the proposed N-26 branch name is absent from worktrees and live local/upstream refs before using it.
- [ ] **Step 2: Record N-25 and install N-26.** Add PR #328 to row 17 and add a dated delivery record with the consumed identities and checks above; update the active row plan/version to this N-26 plan and 0.1.0-dev.144; record that N-25 fixed only N-17 saloon override replay and that N-26 addresses only the N-18 run-induced horse-lameness proof; advance Directory.Build.props exactly once to 0.1.0-dev.144.
- [ ] **Step 3: Retire and commit planning artifacts.** Remove the completed N-25 plan in this first substantive commit, retain the N-26 plan, validate links and version ownership, and commit only planning/version changes before editing tests or production code.

### Task 2: Prove mounted-run lameness through command and replay

**Files:**
- Modify: tests/WildBunch.Domain.Tests/TravelResolverTests.cs
- Modify: .agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md
- Modify: .agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md

**Interfaces:**
- Consumes: CreateProgressionSession, TravelResolver.PreviewJourney, GameSession.StartJourney, event-backed ForceDevTravelOverride, AdvanceJourneyDay, ResolveJourneyEncounter, GameSession.AllEvents, and GameSession.RehydrateFromEvents.
- Produces: one reached run-induced lameness scenario proving command state, emitted absolute snapshot, player inventory synchronization and replay equivalence; a dated correction to the exact N-18 finding and its test-file disposition.

- [ ] **Step 1: Reach a viable mounted encounter.** Replace the existing smoke test setup with a CreateProgressionSession route on OpenRange with route water, a healthy horse plus saddle, Challenging rules and a partial distance such as 1.5m. Start the journey, force a Foe override, and advance a day through the normal command. Assert the day is interrupted for a foe, travel remains mounted, and the horse remains alive, healthy and able to provide mounted travel before resolving the encounter. Adjust only deterministic fixture values if live rule calculations show the proposed distance does not expose a pacing change; keep the initial healthy-horse and reached-command-path requirements.
- [ ] **Step 2: Assert the run's observable effects.** Resolve run with the deterministic successful roll. Assert success and session change; exhaustion reaches the Challenging lame threshold while remaining below death; the journey resumes in Foot mode; remaining route distance is unchanged by the encounter; and remaining days reflect foot pacing. Assert the emitted JourneyEncounterResolved records the mounted-run exhaustion delta, ContinuedOnFoot, and matching absolute snapshot. Assert player inventory retains exactly the Horse and Saddle and carries the same live lame horse state as the journey.
- [ ] **Step 3: Assert event replay.** Rehydrate from the complete event stream and same world. Assert the replayed journey is active and in Foot mode with the same remaining distance/days and horse state, and its inventory retains Horse/Saddle with the same state. If replay reveals a real command/event divergence, stop and revise the plan before changing production behavior or durable decisions.
- [ ] **Step 4: Prove test sensitivity.** Run the focused test, then temporarily mutate TravelJourney.RecalculatePacing to change travel mode without recalculating RemainingDays. Confirm the new assertion fails specifically on expected foot remaining days versus the unchanged mounted count. Restore production source and rerun the focused test successfully. Do not commit the mutation or test output.
- [ ] **Step 5: Correct the audit records.** In N-18, replace only the lame-run smoke-test claim with the reached setup and observed command/event/replay evidence; keep every other N-18 travel and generator finding open. Update the TravelResolverTests.cs disposition to say the run-induced lameness oracle is repaired while its other null-event, weak-effect and completion findings remain. Do not add a test-run receipt.
- [ ] **Step 6: Validate and commit.** Run dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~TravelResolverTests, inspect the diff, and run py -3 tools/run.py ci --check. Commit the test and accurate audit/disposition changes. Make no production change unless the behavior test demonstrates a real implementation defect; if it does, stop and update this JIT plan and any affected ADR before proceeding.

### Task 3: Review, publish, merge and close out

**Files:**
- Review: full N-26 diff, N-18 disposition, ADR-0013, ADR-0028, ADR-0031, feature matrix and selected backend/code-review unslop profiles.

**Interfaces:**
- Consumes: committed N-26 planning/version and test/audit slices, fresh review, canonical local gate, and refreshed develop.
- Produces: independent whole-branch review, one PR to develop, passing exact-head hosted gate, merged source/merge/tree identity, passing develop-push gate, fast-forwarded primary checkout, and paired worktree/local/upstream branch cleanup.

- [ ] **Step 1: Obtain fresh independent review.** Review the full diff against this plan and its scoped authorities. Independently verify the ADR and feature-matrix checks against the actual diff. Resolve findings with witnessed behavior tests and rerun affected checks; obtain a fresh review after corrections.
- [ ] **Step 2: Publish and merge.** Push the branch and open one PR to develop. Verify its head equals local HEAD, the author ADR and feature-matrix checks are resolved, and the exact-head hosted CI gate passes before squash merge.
- [ ] **Step 3: Verify and retire paired checkout.** Confirm develop-push CI passes, record verified source/merge/tree identities, fast-forward the primary checkout, and verify the merge is present. Confirm the worktree is clean, remove it first, then delete only this plan's exact local and upstream branch refs and verify all three are absent. Preserve the separate AWS learning worktree.
