# Repair travel distance and failure-phase legality

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ensure a journey cannot arrive until its route distance and remaining travel days are both exhausted, and a dead player is offered only safe read actions.

**Architecture:** `TravelJourney` owns remaining-distance pacing and completion; `JourneyLoop` emits completion only from that legal state. `ActionAvailabilityResolver` derives legal commands from the recorded journey phase, while map and journal reads remain available for a failed playthrough.

**Tech Stack:** C#/.NET Domain aggregate subtree, typed event snapshots and replay, xUnit behavior tests, feature matrix and roadmap.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, the `Travel, PG-007` binding and route/delay invariants.

**Decision records:** ADR-0013, ADR-0028 and ADR-0038 govern the session-owned journey and event replay; this slice preserves those decisions and requires no ADR change.

**Execution Strategy:** `executing-plans` - distance, days, event completion and read-action legality share the same travel phase transition and are most reliable when implemented and verified in one inline context; per-task handoffs would repeat that state reconstruction without adding an independent review boundary.

## Global Constraints

- A mode change preserves remaining route distance and recalculates the days required at the new daily progress rate.
- Existing delay days remain part of the remaining journey when the travel mode changes.
- Journey completion and arrival require both zero remaining distance and zero remaining days; no snapshot may claim completion while route distance remains.
- Failed playthroughs remain readable but expose no further gameplay action; map and journal remain available.
- Gameplay legality remains in Domain, mutation remains event-backed through `GameSession`, and event replay reconstructs recorded snapshots without recalculation or RNG.
- The application version advances once for this PR to `0.1.0-dev.77`; `Directory.Build.props` is authoritative and generated web identity is build output.
- No migration, event schema change, browser behavior change, new gameplay mechanic or unrelated travel balancing is in scope.

## Review Focus

- If mounted travel becomes foot travel before the first day progresses, the remaining day estimate must expand to cover the same distance at foot speed and the player must not arrive on the stale mounted estimate.
- If legitimate delay days are still pending when the mode changes, recalculation must preserve those delays rather than erase or double them.
- If a day counter reaches zero while route distance remains, the journey must stay active and must not emit `JourneyCompleted` or change the player's town.
- If death marks an in-progress journey failed, available actions must retain map/journal reads and omit `AdvanceTravelDay` and all town/gameplay commands.
- Replaying a completed foot journey after horse loss must reconstruct its actual daily snapshots, final town, completed history and acknowledgement state exactly.

---

## Task 1: Recalculate travel pacing from remaining distance

**Files:** Modify `src/WildBunch.Domain/Travel/TravelJourney.cs`; modify `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`; modify `tests/WildBunch.Domain.Tests/EventSourcing/TravelReplayEqualityTests.cs`.

**Interface:** `TravelJourney.RecalculatePacing(TravelMode)` preserves the current remaining distance, replaces progress days using `RouteProfile.CalculateRemainingDays(remainingDistance, newMode)`, and carries forward the non-progress days still present in `RemainingDays`. `AdvanceOneDay` reports completion only when both remaining days and distance are zero. Existing `JourneyLoop` completion branches consume that result, so no `JourneyCompleted` event or destination transition can occur before both conditions hold.

**Behavior proof:** Extend `CreateProgressionSession` to accept an explicit ride-day distance, then use a real `GameSession` with a three ride-day-unit route and a horse that dies during first-day upkeep. Force quiet travel days through the existing developer override, then assert the first step is on foot with 2.5 units and five days remaining, the player stays in the origin town, completion occurs only after six total trail days at zero distance, and arrival acknowledgement records the destination once. Add a focused `TravelJourney` scenario with an outstanding delay before changing mode; it must retain the same remaining non-progress delay after pace recalculation. Replay the complete command stream and assert the same per-day ending distance/day state and the acknowledged destination history. Replace the current horse-loss assertions that expect one remaining day and accept default fallbacks.

**Steps:**

- [ ] Replace the lame-horse and dead-horse stale-day assertions with the three-unit real command scenario and a non-null result assertion.
- [ ] Add the pending-delay pacing assertion using a `TravelJourney` built from a valid preview, an existing delay, one progressed day and the mounted-to-foot transition.
- [ ] Run `dotnet test WildBunch.sln --filter "FullyQualifiedName~TravelResolverTests|FullyQualifiedName~TravelReplayEqualityTests"` and verify the route scenario reaches the old mounted day limit with distance still outstanding and the delay assertion retains the wrong day count.
- [ ] Update `RecalculatePacing` to preserve pending non-progress days while recalculating required progress days from the remaining distance and new pace.
- [ ] Make `JourneyProgress.Completed` require both zero remaining days and zero remaining distance; leave `MarkCompleted` and event snapshots reachable only through that legal result.
- [ ] Run the command-path scenario through actual completion and arrival acknowledgement; assert no earlier `JourneyCompleted`, no early town change and no dropped distance.
- [ ] Rehydrate the captured setup, override, daily travel, completion and acknowledgement events; assert exact completion history and terminal journey state.

## Task 2: Expose only legal actions after terminal travel failure

**Files:** Modify `src/WildBunch.Domain/Actions/ActionAvailabilityResolver.cs`; modify `tests/WildBunch.Domain.Tests/TravelStarvationTests.cs`.

**Interface:** A failed journey uses the existing terminal journey phase branch and returns only `ViewMap` and `ViewJournal`; active travel, encounter, town-service and new-departure actions are absent. Completed-arrival action behavior remains unchanged.

**Behavior proof:** Extend the real starvation-death command test after `PlayerDied` has made the current journey failed. Build `ActionAvailabilityContext` from that actual session's phase, world, current town and journey snapshot, then assert the exact safe read actions remain and no gameplay action is offered. The existing death command-rejection proof remains the independent mutation guard.

**Steps:**

- [ ] Add the failed-journey action query to the actual terminal-death scenario and verify it currently offers `AdvanceTravelDay` by running `dotnet test WildBunch.sln --filter "FullyQualifiedName~TravelStarvationTests"`.
- [ ] Resolve `JourneyStatus.Failed` through the same no-gameplay-action branch as completed arrival while preserving map and journal reads.
- [ ] Run `dotnet test WildBunch.sln --filter "FullyQualifiedName~TravelResolverTests|FullyQualifiedName~TravelReplayEqualityTests|FullyQualifiedName~TravelStarvationTests|FullyQualifiedName~ActionAvailabilityResolverTests"`; confirm completed arrival and active/pending travel retain their existing action behavior.

## Delivery

- [ ] Update `docs/features.md` PG-007 assessment with the route-distance, delay-preservation, legal completion, failed-action and replay evidence; keep any genuinely unresolved PG-007 exits explicit.
- [ ] Run `py -3 tools/run.py ci --check`, review the complete diff and commit through the normal check-only hook.
- [ ] Request a fresh whole-branch review, resolve all actionable findings, and publish one ready PR to `develop` with exact-head hosted canonical-gate proof.
- [ ] Merge after the exact-head gate passes; verify the develop-push gate and fast-forward the main checkout.
- [ ] Retire this plan in the first substantive successor PR and update the roadmap with verified source, merge, and hosted-gate evidence.
