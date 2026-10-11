# Reject missing recorded hostile-encounter facts

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** A retained hostile trail encounter can be resolved only from its recorded foe profile. If that required fact is missing, reject the command without inventing a replacement profile or changing the journey.

**Architecture:** `GameSession` and `JourneyLoop` remain the command and event owners. Generated foe profiles are settled with encounter generation and stored in the encounter snapshot. Resolution consumes that fact; it does not regenerate it from current salt, health, time, or encounter text.

**Tech Stack:** C#/.NET Domain aggregate subtree, typed event snapshots and replay, xUnit behavioral tests, feature matrix and roadmap.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, the `Travel, PG-007` binding and the event/history boundary. Pre-alpha playthroughs are invalidated by the established forward migration; this plan adds no compatibility reader or migration.

**Decision records:** ADR-0013, ADR-0028 and ADR-0038 remain in force. This removes an implementation fallback and does not change their durable decisions.

**Execution Strategy:** `executing-plans` - all three hostile encounter choices pass through the same journey command and missing-fact guard; one focused behavioral scenario covers the shared boundary and exact mutation result.

**Execution ruling, 2026-10-11:** The initial plan split the failing behavior test from its production correction. The observed RED confirms they are one inseparable behavior slice, and the execution gate requires each task to finish green. They are combined into Task 1; the plan was corrected before implementation.

**Scope ruling, 2026-10-11:** Final source review found the row 12 follow-up's weak `BribeInsultinglyLowOfferCanRetaliateAndSteal` test still present. It can pass its theft assertion on the offer cost alone, while `TravelResourceSettlementTests` already selects and proves exact Food/HorseFeed theft through the event, live state and replay. Consolidate that stale test into its current behavior owner rather than leave the roadmap exit unresolved or add another small PR.

**Behavior ruling, 2026-10-11:** A retaliatory low bribe ends the encounter even though the player's attempt failed. `JourneyEncounterResolved.Resolved` records whether the encounter remains pending, not whether the player got the desired outcome. Assert the event's recorded theft, health, wallet and encounter-state facts without equating encounter completion with bribe success.

**Review ruling, 2026-10-11:** Independent review found the strengthened bribe test asserted health and wallet facts only on the event. It now also compares live state and rehydrated replay against those event facts, so the outcome is verified through event application and replay.

**Coverage ruling, 2026-10-11:** The DN-06 audit follow-up still called for low/zero-reserve generated loss coverage. Existing cap/replay coverage manually constructs a trail event; it does not establish that ordinary generator output reaches the application path. Add one bounded real `GameSession.AdvanceJourneyDay` scenario using deterministic runtime salts to reach generated dust loss at zero food, then verify its recorded and replayed applied delta. This closes the specific retained row 12 gap without adding a generator probability or balancing assertion.

## Global Constraints

- Current encounter generation records a complete `JourneyFoeProfile` before the encounter becomes available to the player.
- A `foe` encounter without that profile is invalid current state. Resolution must fail before changing the pending encounter, hidden state, clock, resources, or uncommitted event stream.
- Do not generate a substitute profile from current salt, player state, clock, route or message text. Do not add a compatibility shim for pre-alpha playthroughs discarded by the existing migration.
- Preserve normal run, fight and bribe outcomes when the encounter has its recorded profile; preserve replay of the actual profile from supported events.
- Keep the failure in Domain command behavior. Do not add event application side effects, Persistence fallback, API gameplay rules, or a schema change.
- The application version advances once for this PR to `0.1.0-dev.78`; `Directory.Build.props` is authoritative and generated web identity is build output.
- This successor also corrects the superseded noticeboard proposal in the dated Domain investigation record and retires the completed `.77` plan. The accepted spec and feature matrix already state the current Sheriff Office-only wanted-poster surface; do not reimplement or duplicate that shipped change.

## Review Focus

- A missing profile rejected through each legal hostile resolution choice leaves the pending encounter and journey exactly as they were, with no emitted events or resource changes.
- The test witnesses the existing synthetic fallback producing a resolution before the fix, then fails after profile validation is introduced for the intended missing-fact reason.
- Valid recorded profiles still drive existing run/fight/bribe outcomes and remain identical through supported replay.
- The PR record no longer presents the abandoned noticeboard interaction as the current decision, while preserving that it was a real earlier proposal.

---

## Task 1: Reject missing foe facts without mutation

**Files:** Modify `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`; modify `src/WildBunch.Domain/Game/JourneyLoop.cs`; modify `src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs`.

**Interface:** Add one parameterized `GameSession` command scenario for `run`, `fight` and `bribe`. Start an ordinary journey, clear the current day plan, mark it interrupted with a `JourneyEncounterState.CreateChoiceEncounter("foe", ..., foeProfile: null)`, capture the complete journey snapshot, resource values and uncommitted-event count, then submit each choice.

**Behavior proof:** The focused test has been run before the fix and failed on all three choices because the current recovery resolves the malformed encounter. Keep that witnessed RED. The final test must require an unsuccessful result explaining that recorded foe details are unavailable, `SessionChanged == false`, interrupted status, a still-null profile, an exactly equal journey snapshot, unchanged health/cash/ammo, and no added uncommitted events. Assertions must compare independently captured state, not reconstruct expected values through production helpers.

**Steps:**

- [x] Add the parameterized missing-profile scenario using the normal journey fixture and a directly malformed current encounter.
- [x] Run `dotnet test WildBunch.sln --filter "FullyQualifiedName~TravelResolverTests"`; all three new cases failed at the assertion that the command must reject the missing fact, confirming the old recovery path resolves `run`, `fight` and `bribe`.
- [x] Add the early missing-profile failure before all state synchronization and encounter-choice resolution.
- [x] Delete the deterministic-but-unrecorded `RecoverFoeProfile` implementation and all now-unused imports/helpers.
- [x] Remove the engine's invented default `JourneyFoeProfile` fallback; preserve explicit argument validation and valid profile behavior.
- [x] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TravelResolverTests|FullyQualifiedName~TravelReplayEqualityTests"`; all 49 matching tests passed.

## Task 2: Keep bribe theft proof on its exact behavior owner

**Files:** Modify `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`; modify `tests/WildBunch.Domain.Tests/TravelResourceSettlementTests.cs`; update `.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md`.

**Interface:** Remove `ResolveJourneyEncounterBribeInsultinglyLowOfferCanRetaliateAndSteal` and its only-used `FindBribeOutcomeRoll` helper. In `TravelResourceSettlementTests.BribeRetaliationRecordsTheResourceActuallyStolen`, assert the retaliatory bribe event, captured health and wallet result, exact stolen kind/quantity, live inventory/journey state, and replay. This test's selected branch uses a $1 offer against a $10 minimum and confirms stolen Food or HorseFeed; theft no longer shares an ambiguous assertion with payment.

**Behavior proof:** The existing theft test independently proves one exact item was stolen through the recorded event, current inventory, journey reserve and replay. Strengthen it with the failed bribe and event-recorded health/wallet facts, run it for both resource variants, and remove the redundant test/helper whose only theft assertion accepted the fee decrease.

**Steps:**

- [x] Capture health and cash before the selected low offer; assert the single `JourneyEncounterResolved` records the exact stolen resource and quantity, ends the encounter after retaliation, applies health loss and charges only the $1 offer.
- [x] Preserve exact live inventory, journey reserve and replay assertions for both Food and HorseFeed.
- [x] Remove the weak theft test and its now-unused outcome-roll helper from `TravelResolverTests`.
- [x] Append a dated DN-04 disposition in the test follow-up explaining which proof was retired and which independent behavior test now owns the resource-settlement invariant.
- [x] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TravelResourceSettlementTests|FullyQualifiedName~TravelResolverTests"`; 46 tests passed with no skips.

## Task 3: Prove generated dust loss settles against zero food

**Files:** Modify `tests/WildBunch.Domain.Tests/TravelTestFactory.cs`; modify `tests/WildBunch.Domain.Tests/TravelResourceSettlementTests.cs`; update `.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md`.

**Interface:** Allow the focused high-risk travel fixture to start with zero food, no horse, and a supplied deterministic runtime salt. In `TravelResourceSettlementTests`, try a bounded ordered set of named salts until normal `GameSession.AdvanceJourneyDay` generates and applies `BadLuckFoodLoss`; make no manual `TravelDayPlanState` or `JourneyTrailEventState` for this behavior. For the selected command, require the event's applied food delta and journey snapshot to record zero available food, live inventory and journey reserves to remain zero, and replay to preserve those facts.

**Behavior proof:** The generated `TrailEventApplied` must be found from the command's own event stream. A test that only constructs the event under test does not prove the normal generation-to-application route. The bounded salt list is deterministic and exists only to reach the existing generated event, not assert weights, distributions or balance.

**Steps:**

- [x] Extend the high-risk fixture with optional initial food, horse presence and salt-source arguments while preserving existing defaults.
- [x] Add the bounded real-command scenario and establish its selected generated `BadLuckFoodLoss` through emitted events.
- [x] Assert zero applied food loss, zero current food, no underflow/throw, and exact post-command replay for zero inventory/journey reserves.
- [x] Append a dated DN-06 disposition identifying generated source selection and resource application/replay proof.
- [x] Run `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~TravelResourceSettlementTests"`; generated dust loss and resource-settlement scenarios passed.

## Task 4: Reconcile successor artifacts and delivery

**Files:** Update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; update `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`; update `.agents/investigations/stable-0.1.0/2026-10-07-domain-test-followup.md`; delete `.agents/plans/2026-10-11-travel-distance-legality.md`; update `Directory.Build.props` once to `0.1.0-dev.78`.

**Interface:** The roadmap records PR #262's verified source, merge and exact-head/develop gates and identifies the current missing-profile slice. The dated investigation record preserves its 2026-10-10 noticeboard proposal, then clarifies that the user superseded it on 2026-10-11 before implementation: the noticeboard and fake sheriff-records surfaces are removed, wanted posters remain in the Sheriff Office, and no pre-alpha compatibility is retained. The roadmap classifies the `.77` plan as complete and retires it. The existing baseline spec and feature matrix already express this decision and need no duplicate edits.

**Behavior proof:** Run `py -3 tools/run.py ci --check`, validate generated version identity, inspect the full diff, and commit through the normal check-only hook. A fresh independent whole-branch review and exact-head hosted canonical gate must pass before merge to `develop`; verify the develop-push gate and fast-forward the main checkout afterward.

**Steps:**

- [x] Record the verified PR #262 delivery in row 12 and point it to this plan at `0.1.0-dev.78`.
- [x] Retire the merged `.77` plan in this first substantive successor and preserve its merged history in the roadmap.
- [x] Append the dated Domain investigation correction without erasing the superseded proposal or changing its original date.
- [x] Advance the sole authored version to `0.1.0-dev.78`; the production web build regenerated `dist/version.json` from `Directory.Build.props` and the web target check passed.
- [x] Run `py -3 tools/run.py ci --check`; the fail-fast canonical gate passed on the complete implementation diff. Review the full diff and commit through the normal check-only hook.
- [ ] Request a fresh independent whole-branch review, publish a PR to `develop`, and verify exact-head hosted CI before merge.
- [ ] Merge after green hosted CI, verify the develop-push gate, fast-forward the main checkout, and verify its clean state.
