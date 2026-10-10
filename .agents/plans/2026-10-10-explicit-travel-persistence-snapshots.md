# Explicit Travel Persistence Snapshots Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Persist travel diary and current-day nested travel facts through explicit persistence-owned snapshots while preserving the existing JSON payload shape and reconstructed gameplay facts.

**Architecture:** `GameSessionJsonSerializer.Travel.cs` already owns outer persistence snapshots and explicitly maps active `JourneyEncounterState`, but directly serializes nested `JourneyTrailEventState`, `TravelDiaryEncounterResolutionState`, and `HorseTravelState` values in journey and diary payloads. Add persistence-owned value snapshots for those records and reuse the existing `JourneyEncounterSnapshot` for nested encounters, mapping both directions at the codec boundary. The persisted property names, enum representation, projection version, domain behavior, event stream, and query write boundary remain unchanged.

**Tech Stack:** .NET 10, C#, EF Core, PostgreSQL, xUnit.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, “Cache-backed state and recovery”; row 07 of `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; PS-14 in `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and its test follow-up.

**Execution Strategy:** `executing-plans`, inline and sequential. The nested snapshot codecs share one persistence file and two real repository behaviors; splitting this into separate implementer contexts would repeat the same data-shape and compatibility analysis without independent deliverables.

## Global Constraints

- Start from merge `ea3bd3c1147e04feaee2cdc2ee2be4c818f13781` on `develop` in this fresh linked worktree and target `develop`.
- Commit this JIT plan, retire the completed `.49` plan, record PR #234 and its exact merge/CI evidence, and advance `Directory.Build.props` once from `0.1.0-dev.49` to `0.1.0-dev.50` before source edits.
- Keep the JSON field names, casing, enum encoding, numeric values and null semantics identical; no diary projection version bump or database migration is expected.
- Map Domain values explicitly in Persistence. Do not add Domain mutation, generic reflection, event facts, replay behavior, query writeback, developer features, or a blanket DTO layer.
- Preserve the existing event-derived diary projector, valid journey/diary behavior, and fail-closed authoritative event history.
- Retain the developer override codecs and mark their separate direct serialization as remaining PS-14 scope for its owning later slice; do not turn this travel payload change into developer tooling work.
- No ADR or feature-matrix change is expected because the persistence seam changes while event authority, cache semantics, and player behavior remain the same.

## Review Focus

- **Nested travel-state loss in active journey or diary persistence:** generated trail events and horse state must retain their exact recorded IDs, kinds, narration, deltas, and resource facts after a fresh repository load. Cover with deterministic PostgreSQL repository round trips.
- **Interrupted and resolved encounter continuity:** the encounter and its eventual resolution facts shown in a travel diary must survive fresh repository loads alongside the active journey state. Cover the real encounter command path and reload.
- **Persisted v1 compatibility:** the explicit snapshots must read the existing camel-case nested value fields from a v1 payload and keep those values available to the Domain. A small literal payload fixture provides the old persisted contract independently of the new serializer's output.

---

### Task 1: Record PR #234 and commit this `.50` successor plan

**Files:** Modify `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-10-batch-stale-component-cache-rebuild.md`; create this plan.

**Interfaces:** Record PR #234 source `c90b64fd6060fc908cbed885ce457ff81893bb38`, merge `ea3bd3c1147e04feaee2cdc2ee2be4c818f13781`, exact-head hosted canonical gate run `38008881201`, develop push gate run `38009298471`, and delivered identity `0.1.0-dev.49`. Row 07 remains executing and points to this `.50` plan.

- [x] Verify PR #234 is merged to `develop` at the stated source and merge SHAs and both hosted gates passed on those exact commits.
- [x] Record PR #234's one-reconstruction-per-load recovery and disclosed self-review fallback in row 07; append PR #234 to the merged-PR list.
- [x] Correct the dated roadmap handoff that still proposes the already-delivered whole-session snapshot retirement; state that the next slice maps persisted travel nested values explicitly and leaves developer-only overrides for their owning later disposition.
- [x] Advance `Directory.Build.props` to `0.1.0-dev.50`, retire the completed `.49` plan and stale row pointer, and commit this plan before source edits.

**Expected:** The roadmap reflects verified PR #234 delivery, the `.49` plan is retired, and this committed plan is row 07's live pointer before implementation begins.

### Task 2: Map nested travel values through persistence-owned snapshots

**Files:** Modify `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Travel.cs` and `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`; append dated PS-14 dispositions to `.agents/investigations/stable-0.1.0/2026-10-07-persistence-layer-investigation.md` and `.agents/investigations/stable-0.1.0/2026-10-07-persistence-test-followup.md`.

**Interfaces:** Add private snapshot value types for `JourneyTrailEventState`, `TravelDiaryEncounterResolutionState`, and `HorseTravelState`, each with explicit `FromDomain` and `ToDomain` mappings. In `JourneySnapshot`, `TravelDayEncounterSnapshot`, and `TravelDiaryDaySnapshot`, map Domain values through Persistence snapshots; reuse the existing `JourneyEncounterSnapshot` for `JourneyEncounterState`. Property names in serialized JSON remain exactly `trailEvent`, `pendingEncounter`, `resolution`, and `encounterResolution` under the existing web JSON options.

- [x] Strengthen the existing lucky-travel PostgreSQL round trip without a forced developer override; capture the event generated by its existing deterministic game seed from the actual diary day, persist, reload, and compare the complete event record. The forced Lucky override produces a choice prompt without a `TrailEvent`.
- [x] Strengthen the structured diary PostgreSQL round trip to compare real `HorseStateBefore` and `HorseStateAfter` values after reload, and assert the active journey's recorded horse state is unchanged.
- [x] Strengthen the interrupted-foe PostgreSQL scenario to compare the actual `JourneyEncounterState` in the current journey day plan, active journey, and diary day after reload; resolve the encounter through `GameSession.ResolveJourneyEncounter`, persist again, then assert the resulting `TravelDiaryEncounterResolutionState` fields survive a second fresh load. The normal resolution clears the completed current-day plan, so its resolution is asserted from the persisted diary.
- [x] Add a focused `DeserializeTravelDiaryDay` test using a literal previously persisted v1 JSON payload with non-null trail event, foe encounter and resolution values; assert their exact identifiers, labels, choices, foe profile, and recorded deltas. Keep the fixture limited to the nested persisted contract rather than asserting every unrelated outer JSON field.
- [x] Add a focused journey-snapshot codec test that starts with a route-derived `TravelJourneySnapshot`, supplies a populated current-day plan value with trail event, pending encounter and resolution, then serializes and deserializes through `GameSessionJsonSerializer`; assert all nested facts are retained. This exercises persistence mapping without inventing or applying game events.
- [x] Run these assertions against the existing direct-serialization implementation first to confirm the real setup reaches each intended populated state; do not fabricate or directly replace authoritative aggregate state.
- [x] Introduce the explicit nested snapshot mappings without changing JSON fields, values, or null semantics. Prove the strengthened tests are sensitive by temporarily corrupting a non-null trail-event, resolution, and horse-state mapping, observe the corresponding assertions fail, then restore each mapping and rerun the tests.
- [x] Run the focused PostgreSQL tests plus `SaveAndLoadTravelDiaryRoundTripsStructuredDiaryState`, `SaveAfterInterruptedTravelRoundTripsPendingEncounterState`, `SaveAfterLuckyTrailEventRoundTripsWalletGain`, serializer round trips, and `ProjectionVersionCompletenessTests`.
- [x] Append dated PS-14 dispositions describing the covered travel payload boundary, preserved persisted shape, and remaining developer-override direct serialization; do not claim PS-14 is wholly closed.
- [x] Recheck ADR-0028 and `docs/features.md`; leave them unchanged because no durable decision or player-facing contract changes.

**Expected:** Persisted journey plans and diary days retain the exact Domain travel event, encounter, resolution, and horse-state values through explicit Persistence mappings. A fresh PostgreSQL aggregate load supports resolving the lawful pending encounter and retains its resolution in the diary.

### Task 3: Validate, review and publish to `develop`

- [ ] Run the focused PostgreSQL and serializer behavior tests against the final candidate.
- [ ] Run the canonical fail-fast `py -3 tools/run.py ci --check` gate on the staged candidate.
- [ ] Inspect the full branch diff, actual JSON shape compatibility, nested mappings, real encounter continuation, PS-14 dispositions, ADR-0028, feature matrix, and applicable persistence/code-review unslop profiles. Use and disclose the self-review fallback if reviewer-agent dispatch is unavailable.
- [ ] Publish a Draft PR to `develop`, reconcile its remote head with local `HEAD`, correct and reread the PR body, mark it ready, and require hosted canonical CI to pass on that exact head before merging.
- [ ] Merge to `develop`, verify the push gate passes on the exact merge SHA, fast-forward the main checkout, and retire this plan in the next substantive successor after verifying delivery evidence.

**Expected:** `.50` is merged to `develop` with exact-head hosted validation; travel payloads retain explicit persistence-owned nested mappings and the existing v1 serialized contract.
