# N-33 Event-Backed Journey Completion Proof Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace the travel-completion test that repairs failed advances by directly changing aggregate state with one fixed quiet route that completes through normal day advancement, records destination arrival, and reconstructs that result from the event stream.

**Architecture:** Test and audit correction only. `GameSession.AdvanceJourneyDay` and `JourneyLoop` remain the command and event owners; the test observes their actual transition and replay. No production behavior or feature promise is expected to change.

**Specification and evidence:** The stable 0.1.0 baseline specification, row 17 roadmap, N-18 domain test follow-up, and `TravelResolverTests` disposition govern scope. The backend architecture unslop profile governs aggregate/event/replay proof. This slice does not change an ADR or product feature truth.

**Governing behavior:** A traveler completes the fixed four-day route through successful `AdvanceJourneyDay` commands. The resulting `JourneyCompleted` fact establishes the destination and completed journey, and replaying the same history reconstructs those facts.

**Execution Strategy:** Native inline execution. One existing test, its behavioral mutation proof, and two audit dispositions are tightly coupled. Inline work keeps the scenario, mutation, and evidence aligned; a single fresh whole-branch reviewer covers the committed slice.

## Global Constraints

- Preserve the approved stable 0.1.0 contract, strict event sourcing, CQRS, and replay boundaries.
- The merged N-32 predecessor is `0.1.0-dev.150`; N-33 advances the single authored application version to `0.1.0-dev.151` exactly once.
- In the first substantive commit, assess and retire the completed N-32 plan, record PR #335 delivery evidence in the roadmap, activate this successor plan in row 17, and advance `Directory.Build.props`.
- Keep the proof in `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`. Rename the stale `CreateSixDayQuietJourney` fixture and all its references to `CreateFourDayQuietJourney`; it uses the Easy foot progress of 0.75 over a 3-unit route and completes in four travel days. Do not change the fixture's behavior, search seeds, or accept failed advances.
- Update old test method names, helper names, and comments that call this same route six-day; do not change unrelated test behavior or assertions.
- The test must not call `ResumeFromEncounter`, `SetCurrentDayPlan`, `MarkCompleted`, or another aggregate state-repair method. Each route day must advance through `GameSession.AdvanceJourneyDay`.
- Assert observable completion, destination, one recorded `JourneyCompleted` event, the fixed six-day route duration, and correct reconstruction from `session.AllEvents`.
- Prove the test fails when the `JourneyCompleted` event is suppressed from the ordinary quiet completion path, then restore `JourneyLoop.cs` byte-for-byte. Do not commit mutation code.
- Update the N-18 travel disposition and the `TravelResolverTests` .NET disposition with N-33's precise correction. Other weak travel oracles and completion-adjacent findings remain open.
- Do not change production code, ADRs, feature promises, generator behavior, travel tuning, or unrelated tests.
- Obtain a fresh independent whole-branch review for this PR. Require exact-head hosted CI and the post-merge develop-push gate before cleanup.
- Remove the clean worktree before deleting its exact local and upstream branch refs; preserve the separate AWS learning worktree.

## Review Focus

Confirm each advance succeeds without direct state repair; the destination is established by exactly one `JourneyCompleted` event; the fixed route takes four days; and current and replayed aggregates both report the expected completed journey and destination. Check that the completion-event mutation makes the destination/event assertion fail and that no mutation remains in production source.

## Task 1: Successor plan, delivery record, and version

Verify PR #335 is merged to `develop` from source `871857e08032d28591891867733e44f22069db10` as `8723cc0de036eef96efb2b69eb774cc75972e19f`, with identical tree `f6987d520d4d9749d97f5d0825faf8bf99291f7`. Confirm the fresh corrected review, exact-head gate `38461300568`, and develop-push gate `38462117613` passed, and confirm the primary checkout is clean on that merge. The previous N-32 plan, worktree, and exact local/upstream refs are eligible for retirement; preserve the AWS learning worktree.

Delete `.agents/plans/2026-10-13-row17-n32-nohorse-exclusion.md`, update row 17 to link this plan and record PR #335's verified delivery facts, and advance `Directory.Build.props` to `0.1.0-dev.151`. Check for stale references to the retired plan, verify the roadmap link and one-source version identity, inspect the complete intended diff, and commit this planning/version slice through the normal staged-snapshot hook before implementation.

## Task 2: Replace the repaired completion test with normal event-backed travel

Rename `CreateSixDayQuietJourney` and `CreateSixDayQuietJourneyWithSetupEvents` to `CreateFourDayQuietJourney` and `CreateFourDayQuietJourneyWithSetupEvents`, and update their existing method-name/comment references. The route is three ride-day units at the Easy foot rate of 0.75, so it completes in four travel days. Preserve all existing test behavior and values during this identifier/comment correction.

In `TravelResolverTests.cs`, replace `AdvanceJourneyDayCompletesTheRouteAfterTheFinalDay`. Start the fixed four-day route from `TravelTestFactory.CreateFourDayQuietJourney()` and advance exactly four days by calling `session.AdvanceJourneyDay()` once per day. Require every result to succeed; require the first three resulting states to remain active and the fourth to be completed. Assert four travelled days, zero remaining days and ride distance, clock day five and turn zero, destination town `d2`, and exactly one `JourneyCompleted` event naming that destination.

Before accepting the new test, temporarily suppress only the `JourneyCompleted` event emitted by `JourneyLoop.HandleCompletedTravelDay`. Run the focused test and confirm it fails because the authoritative destination/completion fact is absent, not because of a setup error. Restore `JourneyLoop.cs` exactly and verify no production diff remains. Run the focused test and all `TravelResolverTests` after restoration.

Rehydrate a fresh `GameSession` with `GameSession.RehydrateFromEvents(session.Id, session.World, session.AllEvents.ToList())`. Assert its current town is `d2`, its journey is completed after four days, and its live completed journey snapshot structurally matches the source session, including nested sequence contents. Keep expected values independent of `TravelJourney` production calculations and do not add arbitrary resource, diary-prose, or implementation-shape assertions.

Append a dated N-33 disposition to `.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md`, stating that this test and the misleading six-day helper naming are corrected while other travel completion consolidation remains open. Update the `TravelResolverTests` and `TravelTestFactory` rows in `.agents/investigations/stable-0.1.0/2026-10-06-dotnet-test-dispositions.md` to state the four-day route/event/replay proof and truthful helper name. Update PG-007's evidence paragraph in `docs/features.md` to describe the new completion/replay proof; its product promise, dependencies and disposition remain unchanged. Review the ADR catalogue against the diff and record in the PR why no durable decision changes.

Stage the intended test and audit changes, inspect the full diff, and commit through the normal check-only hook. Do not run the complete CI immediately before this commit; the hook is the canonical staged-tree gate.

## Task 3: Review and deliver

Build the fresh whole-branch review package from merge base `8723cc0de036eef96efb2b69eb774cc75972e19f` to the final committed head. Dispatch an independent fresh-context reviewer for the exact package, this plan, the stable baseline spec, applicable ADRs, feature matrix, and N-18 dispositions. Resolve actionable findings with witnessed behavioral proof and re-review every corrected head.

Push the branch and open a pull request to `develop`; publish it ready for review after author checks and the independent review pass. Require the hosted canonical gate to pass on the exact PR head before squash merge. Verify source/merge tree identity and the develop-push gate, fast-forward the primary checkout, then remove the clean N-33 worktree before deleting the exact local and upstream refs. Verify all three are absent and preserve the AWS learning worktree.
