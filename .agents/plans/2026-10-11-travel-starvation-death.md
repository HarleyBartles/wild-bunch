# Apply starvation toll and event-backed death

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make each unfed trail day cost 25 health exactly once and make zero health an event-backed terminal failed playthrough that cannot advance, reward, or arrive afterward.

**Architecture:** `GameSession` remains the command and event boundary; `JourneyLoop` owns daily travel outcomes; typed events record bounded health effects and the terminal death fact; replay restores health, failed status and failed journey state; HUD and journal projections expose only those established facts.

**Ruling:** Add a typed `PlayerDied` fact after the health-changing event in the same committed command. It carries a player-facing diary message but no cause field because the preceding event records the actual cause and adding a cause taxonomy is outside the settled design. The health event records the bounded actual loss; `PlayerDied` is valid only at zero health, sets `GameStatus.Failed` and marks any active journey failed. A lethal upkeep records `TravelDayOutcome.Failed` and stops before reducing route distance or generating encounters. This keeps death explicit in history without inventing a second health authority or reason model.

**Tech Stack:** C#/.NET event-sourced `GameSession`, Application projections, JSON event decoding, React HUD, behavior and replay tests.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, `Travel and resource outcomes`, the resource floor and death decision, and `PG-007`.

**Execution Strategy:** `executing-plans` - prove the normal travel-day upkeep, real lethal encounter outcomes, event replay, command rejection and player-visible terminal status. Add only the explicit `PlayerDied` lifecycle fact and failed travel-day outcome required here; do not add other travel events, death causes, balance rules or unrelated route repairs.

## Global Constraints

- A trail day with at least one food consumes one unit and causes no starvation damage; a day beginning with no food costs 25 health exactly once.
- Actual health loss is bounded by remaining health; health and resource quantities never become negative.
- Repeated reads, preview, pending encounter choice, command retry and replay do not consume another day or charge starvation twice.
- Zero health produces `PlayerDied` in the same committed command as the event recording the final health loss; replay reconstructs `GameStatus.Failed` and a failed journey when travel was underway.
- A lethal upkeep or encounter stops before later encounters, `JourneyCompleted`, town arrival, arrival refill, bounty or other later effects.
- Failed sessions remain queryable and archivable through confirmed Start Over but reject further gameplay mutations.
- Preserve already applied consequences and truthful travel journal facts; do not add saloon combat, revival, hunt victory or a new death-cause taxonomy.
- The application version advances once for this PR to `0.1.0-dev.76`; `Directory.Build.props` is authoritative and generated web identity is build output.
- Keep route-distance and legal-arrival repairs outside this slice.

## Review Focus

- Last food feeds that day; only the next day that starts without food charges 25 health.
- A starvation toll of 25 with 10 health remaining records a 10-point loss, final health zero and one death fact, never negative health.
- Lethal run and fight outcomes record actual health zero and stop the current day before later encounters or journey completion.
- Event rehydration and the HUD projection restore `Failed`; the failed session remains readable and archiveable while gameplay commands cannot mutate it.
- Normal and fatal paths retain one event-derived journal account without inventing an extra occurrence or losing the cause recorded by the preceding event.

---

## Task 1: Apply daily starvation as bounded travel damage

**Files:** `src/WildBunch.Domain/Game/JourneyLoop.cs`, `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Domain/Game/Player.cs`, `src/WildBunch.Domain/Events/TravelDayAdvanced.cs`, `src/WildBunch.Domain/Travel/TravelDayOutcome.cs`, `src/WildBunch.Domain/Travel/TravelJourney.cs`, `tests/WildBunch.Domain.Tests/TravelStarvationTests.cs`.

**Interface:** Consume one available meal at daily upkeep; otherwise produce a `TravelDayAdvanced` health delta equal to the actual bounded starvation loss, advance the clock once, and finish the operation immediately if the player reaches zero.

**Tests:** Use real `AdvanceJourneyDay` command flows for an available last meal, an unfed day with more than 25 health, and an unfed day with less than 25 health. Assert food, event health delta, final health, event order, journey status, remaining distance, no later encounter/completion, and replay. The last-meal case must show no starvation damage that day and a subsequent unfed day must charge exactly 25 once. Prove each new test fails before implementation for the missing toll or false completion.

**Steps:**

- [ ] Add focused failing normal-command tests for last-food upkeep, one unfed day, cumulative unfed days and a lethal toll with less than 25 health.
- [ ] Run the tests and verify failure is caused by missing/bounds-invalid starvation effects, not fixture setup or impossible travel state.
- [ ] Apply the 25-point toll only when upkeep begins without food and record the actual bounded health delta in `TravelDayAdvanced`.
- [ ] Stop event generation at lethal upkeep; the final snapshot must carry a failed journey and no later encounter, completion, destination, refill or reward may be emitted.
- [ ] Rehydrate the recorded events and prove exact health, food, status and journey state match the command result without charging another day.

## Task 2: Make lethal encounter damage terminal

**Files:** `src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs`, `src/WildBunch.Domain/Game/JourneyLoop.cs`, `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Domain/Game/Player.cs`, `src/WildBunch.Domain/Events/PlayerDied.cs`, `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`, `tests/WildBunch.Domain.Tests/TravelDeathTests.cs`, `tests/WildBunch.Domain.Tests/EventSourcing/TravelReplayEqualityTests.cs`.

**Interface:** Resolve running and fighting against current health, clamp the recorded final health at zero, and append exactly one `PlayerDied` fact after the health result. Applying that fact marks any interrupted journey failed. Replay applies the same typed events and rejects invalid ordering or a death fact above zero health.

**Tests:** Drive actual `ResolveJourneyEncounter` run and fight choices with low-health sessions and deterministic outcomes. Assert the resolved encounter event records zero health, the next event records death, the journey snapshot is failed, remaining encounters do not apply, and no arrival/reward event follows. Replay the full event stream and verify health, status, encounter history and journey state; prove duplicate/invalid death cannot reapply or revive the session.

**Steps:**

- [ ] Add failing tests for lethal run, lethal fight, exact-zero and over-lethal damage, including an encounter plan with later same-day effects.
- [ ] Verify the intended red failures show negative health, continued encounters, or missing terminal status on the current command path.
- [ ] Bound actual run/fight damage by remaining health and mark the journey failed before constructing its final outcome snapshot.
- [ ] Emit `PlayerDied` once after the health-changing event; have `Apply` validate zero health and set `GameStatus.Failed` without adding health or inventory side effects.
- [ ] Register and replay the new event through the existing serializer/type dispatch; preserve existing event payloads and require no migration for prior streams.
- [ ] Reject subsequent gameplay mutations at the aggregate boundary while preserving queries and `ArchivePlaythrough`; test real command methods rather than a status-only helper.
- [ ] Prove the complete fatal event stream rebuilds the same terminal status and does not continue into journey completion or rewards.

## Task 3: Project the terminal state truthfully

**Files:** `src/WildBunch.Application/Projections/HudProjector.cs`, `src/WildBunch.Application/Projections/JournalLogProjector.cs`, `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`, `src/WildBunch.Web/src/tests/Hud.test.tsx`, `docs/features.md`.

**Interface:** Project the explicit death fact as `GameStatus.Failed` and one player-safe death journal entry; existing API status mapping and the web HUD must expose `Failed` with zero health, while the failed session can still be opened and archived.

**Tests:** Project a real command event stream, assert the status and one truthful terminal entry, then render the HUD with the returned status and health and assert `Failed` is visible. Protect start-over access through the existing settings flow without adding new account, revival or game-over mechanics.

**Steps:**

- [ ] Add failing projector and rendered HUD behavior tests for the new event-backed failed state and player-visible death.
- [ ] Run each new test and confirm it fails on absent projection behavior or misleading/omitted terminal UI state.
- [ ] Project death from the typed event and preserve event order and player-safe narration; do not expose developer audit or hidden state.
- [ ] Update PG-007 assessment with the newly proven starvation/death behavior while keeping route-distance and legal-arrival work visibly open.
- [ ] Run focused Domain, Application and web behavior suites, then `py -3 tools/run.py ci --check` and review the final diff.

## Delivery

- [ ] Request a fresh independent review of the complete branch and address findings.
- [ ] Publish one PR to `develop`; verify the exact-head hosted CI gate passes before merge.
- [ ] Merge the PR to `develop`, verify the develop-push hosted gate passes, and update the main checkout.
- [ ] Retire this plan in the first substantive successor PR and record its source, merge and CI evidence in the roadmap.
