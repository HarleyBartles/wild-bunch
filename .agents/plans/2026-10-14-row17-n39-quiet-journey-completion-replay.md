# N-39 Consolidate Quiet Journey Completion and Replay Proof

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Replace redundant and unbounded quiet-journey completion and replay tests with one bounded behavioral proof from journey start through arrival acknowledgment and event-history reconstruction.

**Architecture:** Keep production behavior unchanged. Use `TravelTestFactory.CreateFourDayQuietJourney()` and the evented quiet-travel override to exercise the real aggregate command path, then replay one copy of that command session's ordered `AllEvents` stream and compare the completed-arrival state.

**Tech Stack:** C# 14, .NET 10, xUnit, Wild Bunch Domain event sourcing.

**Spec:** The [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), row 17 of the [roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), N-18 in the [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md), the [test dispositions](../investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md), the [event-sourcing integrity doctrine](../doctrine/event-sourcing-integrity.md), and ADR-0028.

**Execution Strategy:** `executing-plans` (Native inline) because the test consolidation, mutation proof, and disposition update share one scenario and must be reviewed together; a fresh whole-branch reviewer supplies the independent context without repeating per-task implementer handoffs.

## Global Constraints

- N-38 merged to `develop` at `0.1.0-dev.156`; this PR owns the next unique development version, `0.1.0-dev.157`, in `Directory.Build.props`.
- Preserve strict event sourcing: facts from the command path reconstruct the same state through one replay of the exact ordered event history; replay makes no new random choices.
- Do not change production behavior, event contracts, feature promises, or durable architectural decisions.
- Keep one meaningful owner for the quiet-journey completion/replay behavior; do not add shape detectors, helper-absence tests, or assertions that merely repeat setup defaults.
- In the first substantive commit, retire only the fully delivered N-38 plan, record its PR, review, CI, merge, and cleanup facts, and select N-39 in the roadmap.
- Deliver from this fresh worktree through an independently reviewed PR to `develop`, require passing exact-head hosted CI and develop-push CI, then fast-forward the primary checkout and retire this worktree and its paired refs.

## Review Focus

- The fixed quiet route must reach `JourneyCompleted` on the fourth successful day, keep the player at the origin before arrival, and move the player to the declared destination at completion.
- Arrival acknowledgment must succeed, clear only the active journey, and preserve completed-journey history; replay of the same event stream must reconstruct those facts and the same aggregate version.
- Removing the duplicate command-only scenario must not remove an independent invariant; the retained test owns the full command, completion, acknowledgment, and replay lifecycle.
- Apply the quiet override through the normal evented command path for all four days; do not mutate completion state directly or use an unbounded loop.
- Keep roadmap and investigation updates limited to this test consolidation, preserving still-open N-18 travel-effect and generator findings.

---

## Task 1: Record N-38 delivery and commit the N-39 plan

**Files:**
- Create: `.agents/plans/2026-10-14-row17-n39-quiet-journey-completion-replay.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-14-row17-n38-wanted-poi-replay.md`

**Interfaces:**
- Consumes: merged N-38 source `c34a8cfafcb4a7223ade0d06e0184fb8ec55b9b2`, PR #341, merge `064c6d9416482334d1278f1b660932a0f9ea7c3d`, exact-head gate run `38519362402`, develop-push gate run `38519909961`, and the current develop branch.
- Produces: an active N-39 plan, an N-38 delivery record and retirement in the roadmap, and the single authored application version `0.1.0-dev.157`.

- [x] Verify PR #341 merged the exact N-38 branch head to `develop`, both hosted canonical gates passed, the primary checkout is at merge `064c6d9416482334d1278f1b660932a0f9ea7c3d`, and the local and live upstream N-38 branch refs are absent. Record that the Git worktree registration was removed, while the cleanup helper refused to delete the now-unregistered on-disk directory because Windows reported it locked; do not retry deletion.
- [x] Classify N-38 as delivered: its only scope was the forced wanted-saloon-POI replay assertion and truthful evidence updates, all merged in PR #341. Record review and validation evidence, the locked-directory cleanup limit, select N-39 in row 17, and set `Directory.Build.props` to `0.1.0-dev.157`.
- [x] Commit this plan, N-38 retirement, roadmap delivery update, and version transition before editing tests.

## Task 2: Consolidate the quiet-journey lifecycle and replay test

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/EventSourcing/TravelReplayEqualityTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`

**Interfaces:**
- Consumes: `TravelTestFactory.CreateFourDayQuietJourney()`, `DevTravelOverride.ForCategory(TravelDayEncounterCategory.Quiet)`, `GameSession.AllEvents`, and `GameSession.RehydrateFromEvents(...)`.
- Produces: one bounded test requiring the reached four-day completion, successful arrival acknowledgment, archived journey state, and replay equivalence; the duplicate standalone completion test is removed.

- [ ] Rename and strengthen `Replay_FullJourneyCycle_MatchesCommandPath_ExactState` as `Replay_FullQuietJourneyCycle_ReconstructsCompletedArrival`, a bounded four-day scenario. Require `StartJourney` to succeed; for days one through four apply the evented Quiet override, require each `AdvanceJourneyDay` to succeed, assert `Active` for days one through three and `Completed` on day four, require the player to stay at the origin until day four, and require the day-four `JourneyCompleted` event and player location to name the preview destination.
- [ ] Before acknowledgment, require one completed active journey with four days travelled and zero remaining ride-day distance. Require `AcknowledgeJourneyArrival()` to succeed, the active `Journey` to become null, and one completed-history record to retain the destination, foot mode, and four-day count.
- [ ] Capture `commandSession.AllEvents.ToArray()` once after acknowledgment and pass that array once to `GameSession.RehydrateFromEvents`. Require replay to have no active journey, the same player destination, clock and version, and the same completed-history record; retain the existing player health, wallet, food, and pursuit comparisons.
- [ ] Remove `TravelResolverTests.AdvanceJourneyDayCompletesTheRouteAfterTheFinalDay`, whose completion-through-replay assertions are now covered by the single full-lifecycle scenario. Do not add another fixture, a helper-removal test, or a synthetic event stream.
- [ ] Prove the replay assertions are behavior-sensitive by temporarily suppressing `JourneyArrivalAcknowledged` application in `GameSessionEventReplay`, running the focused test and observing the active journey or archived-history assertion fail, then restoring production byte-for-byte and rerunning the focused test successfully.
- [ ] Update the TravelResolver and replay-equality dispositions and add a dated N-39 disposition that closes only this completion/replay consolidation; keep unrelated N-18 travel-effect and generator findings open. Compare the test-only change with ADR-0028, the stable specification, and `docs/features.md`; state why they remain unchanged.
- [ ] Run the focused test with `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~Replay_FullQuietJourneyCycle_ReconstructsCompletedArrival` and the full Domain project with `py -3 tools/run.py dotnet-test --check -- tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`; then commit through the normal check-only hook and require its `py -3 tools/run.py ci --check` staged-candidate gate to pass.

## Task 3: Review and deliver N-39

- [ ] Build the whole-branch review package against the recorded `develop` base and have a fresh independent reviewer inspect the exact committed diff, row-17 scope, ADR-0028, N-18, test dispositions, and feature matrix. The user has given standing authorization for an independent reviewer on each plan in this epic; do not ask again.
- [ ] Resolve any Critical or Important finding with a scoped failing/passing test and re-review the exact fix; retain documented disposition for non-blocking observations.
- [ ] Open a Draft PR to `develop`, verify its body and exact head, mark it ready after the review is clean, and require the exact-head canonical hosted gate to pass.
- [ ] Squash-merge the reviewed PR, verify the merge commit and passing develop-push canonical gate, then fast-forward the primary checkout to `origin/develop`.
- [ ] Remove the clean N-39 worktree before deleting its exact local/upstream refs, and verify the worktree registration and both refs are absent. If the helper reports a locked directory, stop retrying it and record the bounded host-cleanup residue without touching unrelated worktrees.
