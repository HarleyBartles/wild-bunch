# Reconcile travel resource losses with their recorded effects

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make travel theft and trail-event resource losses remove only available resources and record the actual outcome consistently in events, snapshots, inventory and the player's journal.

**Architecture:** `TravelJourney` owns journey resource quantities; the event stream records actual outcomes and absolute snapshots reconstruct them. Apply bounded deltas to journey state before producing outcome events, then derive event quantities and player-facing narration from those applied deltas. Keep existing event shapes and replay semantics; do not add migrations or new travel mechanics.

**Ruling:** Food and HorseFeed theft is reflected in `JourneySnapshot`; `StolenItem*` remains descriptive for those kinds, while only RevolverAmmo theft is applied additively because ammunition is not part of that snapshot. This avoids replay applying the same food/feed loss twice when a later `TrailEventApplied` snapshot already includes it. If this boundary is wrong, replay can diverge from the live journey or charge the player twice.

**Tech Stack:** C#/.NET domain aggregate, immutable domain events and replay tests.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, `Travel and resource outcomes` and `PG-007`.

**Execution Strategy:** `executing-plans` - theft and trail-event losses share the invariant that every recorded resource loss equals the actual resource removed; verify both through normal command flow and event replay.

## Global Constraints

- Every resource quantity and charge remains at or above zero.
- Event deltas and snapshots record actual applied resource effects, not nominal losses that exceed remaining resources.
- Player-facing journal text must not claim a larger resource loss than the event applied.
- Replaying the emitted facts reconstructs the same remaining resource state as the command path.
- The application version advances once for this PR to `0.1.0-dev.75`; `Directory.Build.props` is authoritative and generated web identity is build output.
- Keep hostile travel encounters; do not include route-distance, starvation, terminal-death or arrival-phase repairs.

## Review Focus

- A successful retaliation theft of Food or HorseFeed reduces that journey resource and leaves event `StolenItem*`, snapshot and player inventory equal.
- A trail loss greater than remaining Food or canteen charges records only the available amount and narrates the actual loss.
- Sequential losses in one travel day consume the remaining balance once and never underflow.
- Replaying the emitted theft and trail-event facts reconstructs the same state without a second charge.

---

## Task 1: Record retaliation theft against journey resources

**Files:** `src/WildBunch.Domain/Game/JourneyLoop.cs`, `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Domain/Events/JourneyEncounterResolved.cs`, `src/WildBunch.Domain/Travel/TravelJourney.cs`, `tests/WildBunch.Domain.Tests/TravelResourceSettlementTests.cs`.

**Interfaces:** Consume the selected theft kind and quantity from `JourneyEncounterResolutionPlan`; produce a journey snapshot and `JourneyEncounterResolved` whose stolen-item fields and absolute resource snapshot describe the same loss.

**Tests:** Drive a real bribe-retaliation command with Food as the only stealable resource and assert its event, live player inventory and journey snapshot all lose the same quantity. Repeat with HorseFeed. Replay the emitted facts and assert the same quantities, rather than relying on whole-state equality alone. Each case must fail if its resource mutation is removed or if the event snapshot remains unchanged.

**Steps:**

- [x] Add focused failing behavior tests for Food theft and HorseFeed theft through `GameSession.ResolveJourneyEncounter`.
- [x] Run only the new tests and confirm each fails because theft is absent from the journey snapshot.
- [x] Apply the theft quantity to the matching `TravelJourney` resource before continuation and event production; preserve the existing event shape.
- [x] Replay the emitted events in the focused tests and prove the exact remaining resource quantity matches the live command result.
- [x] Run the domain test project and inspect the diff for accidental test-only hooks or unrelated travel edits.

## Task 2: Bound trail-event losses and publish actual effects

**Files:** `src/WildBunch.Domain/Game/JourneyLoop.cs`, `tests/WildBunch.Domain.Tests/TravelResourceSettlementTests.cs`, `docs/features.md`.

**Interfaces:** Consume a `JourneyTrailEventState` with nominal Food and canteen losses; produce a `TrailEventApplied` event whose deltas, `JourneySnapshot`, `Message` and diary narration report the bounded applied losses.

**Tests:** Arrange a pending hostile encounter followed by a resource-loss trail event, then resolve through the normal command. Start with fewer resources than the event's nominal loss; assert the actual `FoodDelta` and `CanteenChargeDelta`, resulting inventory and snapshot, and player-facing text all report the amount available. Include sequential same-day losses and replay to show the balance floors at zero and is charged once. Prove the test is sensitive by temporarily restoring the nominal subtraction and observing its failure.

**Steps:**

- [x] Add failing normal-command tests for overlarge Food and canteen losses, including sequential same-day resource events.
- [x] Run the focused tests and confirm the current underflow or false event outcome fails for the intended assertion.
- [x] Apply each trail-event resource delta against the current remaining quantity, preserving non-resource effects and applying canteen capacity rules.
- [x] Build the event message and diary entry from actual deltas; do not claim nominal losses when the player had fewer resources.
- [x] Assert replayed snapshots and inventory match the emitted actual deltas without a second resource change.
- [x] Update PG-007 evidence while keeping its overall assessment partial for the unresolved travel slices.
- [ ] Run focused domain tests, then `py -3 tools/run.py ci --check` and review the final diff.

## Delivery

- [ ] Request a fresh independent review of the complete branch and address findings.
- [ ] Publish one PR to `develop`; verify the exact-head hosted CI gate passes before merge.
- [ ] Merge the PR to `develop`, verify the develop-push hosted gate passes, and update the main checkout.
- [ ] Retire this plan in the first substantive successor PR and record its source, merge and CI evidence in the roadmap.
