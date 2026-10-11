# Reject missing recorded hostile-encounter facts

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** A retained hostile trail encounter can be resolved only from its recorded foe profile. If that required fact is missing, reject the command without inventing a replacement profile or changing the journey.

**Architecture:** `GameSession` and `JourneyLoop` remain the command and event owners. Generated foe profiles are settled with encounter generation and stored in the encounter snapshot. Resolution consumes that fact; it does not regenerate it from current salt, health, time, or encounter text.

**Tech Stack:** C#/.NET Domain aggregate subtree, typed event snapshots and replay, xUnit behavioral tests, feature matrix and roadmap.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, the `Travel, PG-007` binding and the event/history boundary. Pre-alpha playthroughs are invalidated by the established forward migration; this plan adds no compatibility reader or migration.

**Decision records:** ADR-0013, ADR-0028 and ADR-0038 remain in force. This removes an implementation fallback and does not change their durable decisions.

**Execution Strategy:** `executing-plans` - all three hostile encounter choices pass through the same journey command and missing-fact guard; one focused behavioral scenario covers the shared boundary and exact mutation result.

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

## Task 1: Prove missing foe facts are rejected without mutation

**Files:** Modify `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`.

**Interface:** Add one parameterized `GameSession` command scenario for `run`, `fight` and `bribe`. Start an ordinary journey, clear the current day plan, mark it interrupted with a `JourneyEncounterState.CreateChoiceEncounter("foe", ..., foeProfile: null)`, capture the complete journey snapshot, resource values and uncommitted-event count, then submit each choice.

**Behavior proof:** Before changing production code, run the focused test and confirm it currently succeeds in resolving at least one choice by synthesizing a profile. The final test must require an unsuccessful result explaining that recorded foe details are unavailable, `SessionChanged == false`, interrupted status, a still-null profile, an exactly equal journey snapshot, unchanged health/cash/ammo, and no added uncommitted events. Assertions must compare independently captured state, not reconstruct expected values through production helpers.

**Steps:**

- [ ] Add the parameterized missing-profile scenario using the normal journey fixture and a directly malformed current encounter.
- [ ] Run `dotnet test WildBunch.sln --filter "FullyQualifiedName~TravelResolverTests"` and verify that the current recovery resolves the malformed encounter instead of rejecting it.
- [ ] Add no production fallback before observing that intended RED result.

## Task 2: Remove unrecorded encounter-profile synthesis

**Files:** Modify `src/WildBunch.Domain/Game/JourneyLoop.cs`; modify `src/WildBunch.Domain/Travel/JourneyEncounterResolutionEngine.cs`; modify `tests/WildBunch.Domain.Tests/TravelResolverTests.cs` only if the focused assertions need a second independent direct boundary.

**Interface:** In `ResolveJourneyEncounter`, after selecting the encounter from the actual pending/current-day snapshot, return a failed result for a `foe` with no `FoeProfile` before any call that updates the journey or rebuilds resolution state. Remove `RecoverFoeProfile` and its salt-derived fallback seed. Make the resolution engine reject a missing profile rather than returning an invented default profile, so direct callers cannot silently regain the old behavior.

**Behavior proof:** Rerun the parameterized command test and confirm all three choices fail with no state mutation or event. Rerun existing run/fight/bribe behavior tests and event replay tests to prove valid generated foe profiles still control outcomes and reconstruct unchanged.

**Steps:**

- [ ] Add the early missing-profile failure before all state synchronization and encounter-choice resolution.
- [ ] Delete the deterministic-but-unrecorded `RecoverFoeProfile` implementation and all now-unused imports/helpers.
- [ ] Remove the engine's invented default `JourneyFoeProfile` fallback; preserve explicit argument validation and valid profile behavior.
- [ ] Run `dotnet test WildBunch.sln --filter "FullyQualifiedName~TravelResolverTests|FullyQualifiedName~TravelReplayEqualityTests"` and compare every outcome with the plan.

## Task 3: Reconcile the successor artifacts and delivery

**Files:** Update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; update `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`; delete `.agents/plans/2026-10-11-travel-distance-legality.md`; update `Directory.Build.props` once to `0.1.0-dev.78`.

**Interface:** The roadmap records PR #262's verified source, merge and exact-head/develop gates and identifies the current missing-profile slice. The dated investigation record preserves its 2026-10-10 noticeboard proposal, then clarifies that the user superseded it on 2026-10-11 before implementation: the noticeboard and fake sheriff-records surfaces are removed, wanted posters remain in the Sheriff Office, and no pre-alpha compatibility is retained. The roadmap classifies the `.77` plan as complete and retires it. The existing baseline spec and feature matrix already express this decision and need no duplicate edits.

**Behavior proof:** Run `py -3 tools/run.py ci --check`, validate generated version identity, inspect the full diff, and commit through the normal check-only hook. A fresh independent whole-branch review and exact-head hosted canonical gate must pass before merge to `develop`; verify the develop-push gate and fast-forward the main checkout afterward.

**Steps:**

- [x] Record the verified PR #262 delivery in row 12 and point it to this plan at `0.1.0-dev.78`.
- [x] Retire the merged `.77` plan in this first substantive successor and preserve its merged history in the roadmap.
- [x] Append the dated Domain investigation correction without erasing the superseded proposal or changing its original date.
- [ ] Advance the sole authored version to `0.1.0-dev.78`; rebuild the generated web identity.
- [ ] Run the canonical gate, review the diff, and commit the successor implementation.
- [ ] Request a fresh independent whole-branch review, publish a PR to `develop`, and verify exact-head hosted CI before merge.
- [ ] Merge after green hosted CI, verify the develop-push gate, fast-forward the main checkout, and verify its clean state.
