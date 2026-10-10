# Make the journey view use the shared player journal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the journey-focused view a filter/detail view of the one event-derived player journal, with no second owner composing travel narration.

**Architecture:** `JournalLogProjector` remains the sole player-history curation owner. It tags travel entries with the `JourneySequence` already recorded in each travel event snapshot. The Application mapper exposes all entries through the full journal and the latest journey's entries through `TravelDiaryDto`; the sequence is filtering metadata and is never exposed to the player. `TravelDiaryDayProjector` and its persisted, rebuildable detail remain for structured travel facts. The journey notebook displays the same journal-entry component as the full journal plus structured day details.

**Tech Stack:** C#/.NET 10, event projections, Application DTO mapping, React/TypeScript, xUnit, Vitest, PostgreSQL integration tests.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially the player journal decision and PG-008. Full-playthrough and current/completed journey views share curation, entries, effects and game-day/turn ordering. Journey selection uses recorded identity, never text, location or inferred effects.

**Execution Strategy:** Use `executing-plans` inline and sequential. This is one cross-layer behavior slice: the projector, filtered response, shared renderer and persisted two-journey proof must agree on the same contract.

**Roadmap:** Row 08, `Restore strict read ownership and honest projections`; this is `.62` after PR #246.

**Scope:** Carry recorded journey identity on projected travel journal entries; expose the latest journey subset in the journey response; render shared entries in both views; retain structured travel-day detail; retire independently composed travel prose and its tests; prove filtering and persistence through two real journeys; update PG-008 and the dated investigation/test dispositions.

**Out of scope:** New travel or encounter mechanics, new event facts, event/schema version changes, deletion of the structured travel-day projection, full journal redesign, new browser automation, or claiming all PG-008 work complete.

**Review Focus:**

- Journal entries in the full view remain in recorded order and keep their actual game day and turn.
- A second journey appears in the journey view while the first remains in full history; prove filtering by recorded sequence, not equal message text, current location or day number.
- The same retained event message and order are visible in both views after a persisted reload; do not assert that two whole collections are equal.
- Neither DTO mapper nor React creates a second travel story from choice IDs, end-state resources, trail labels or current location.
- Structured day outcomes/resources remain available beside shared narration, and journal messages do not disappear when a journey is acknowledged.

## Global Constraints

- Develop from the current `origin/develop`, target this PR to `develop`, and leave `main` as the release line.
- Keep the sole authored version in `Directory.Build.props`; advance `.61` to `.62` in the successor handoff commit.
- Preserve immutable events as the only authority; this plan adds projection metadata only and does not change event payloads, replay, or persistence schema.
- Do not expose `JourneySequence` in public DTOs or player UI.
- Keep `TravelDiaryDayProjector` as a rebuildable structured detail projection. Remove only its separate player-narration composition/DTO/display path when all consumers are verified.
- Preserve occurrence order. Do not sort entries by message, location, or journey-relative day; use the existing day/turn and projection order.
- Start PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure` before PostgreSQL-backed validation and run `py -3 tools/run.py ci --check` as the complete local gate.
- Read ADR-0028; amend the ADR log only if the implementation changes its durable event/projection decision.
- Update `docs/features.md` and the Application/Web investigation and test follow-ups with dated current-state dispositions, not development receipts.
- For each new behavior test, witness the intended RED, restore the implementation and prove GREEN. Do not add source-shape, endpoint-absence, or count-only tests.
- Keep this plan through its completing PR. The next successor plan retires it while preserving Git history.

## Task 1: Record the `.61` handoff and commit this successor plan

**Files:** `.agents/plans/2026-10-10-casebook-knowledge-without-inference.md`, this plan, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, and `Directory.Build.props`.

- [x] Verify PR #246 merged exact source `5c8ec5eff34eaaf252e0a0e5e407dbee9ac136fd` as `c68dc49b579ea29da4e346e4c4527a1e86a8b8ee`; exact-head gate `38052106105` and develop-push gate `38052474044` passed.
- [x] Retire the completed `.61` plan in this successor, preserving it in Git history.
- [x] Update row 08 with PR #246 source/merge identities and gate IDs, point its active-plan link to this plan, and describe the `.62` shared-journal journey slice while leaving later read-boundary work active.
- [x] Set the sole authored application version to `0.1.0-dev.62`.
- [x] Commit this plan handoff before changing implementation source; rely on Git history for the commit, not a committed development receipt.

## Task 2: Associate projected travel entries with recorded journeys

**Files:** `src/WildBunch.Domain/Game/GameLogEntry.cs`, `src/WildBunch.Application/Projections/JournalLogProjector.cs`, and `tests/WildBunch.Application.Tests/Projections/JournalLogProjectorTests.cs`.

- [x] Add a focused projection behavior test with travel events from two distinct `JourneySequence` values; require each projected travel entry to retain the sequence from its event snapshot and non-travel entries to remain unassociated.
- [x] Run the focused test and verify RED because projected entries currently discard journey identity.
- [x] Add optional internal projection metadata to `GameLogEntry` and tag every travel entry from `JourneyStarted`, `TravelDayAdvanced`, `TrailEventApplied`, `JourneyEncounterResolved`, `JourneyCompleted`, and `JourneyArrivalAcknowledged` using that event's snapshot/sequence.
- [x] Preserve existing entry wording, game-day/turn assignment, ordering and all non-travel projection behavior.
- [x] Run the focused `JournalLogProjectorTests` lane and compare all expected output.

## Task 3: Supply one shared history and a recorded-identity journey subset

**Files:** `src/WildBunch.Application/Games/Models/GameDtos.cs`, `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`, `src/WildBunch.Application/Games/Mapping/JournalMapper.cs`, `src/WildBunch.Web/src/api/types.ts`, and the relevant Application mapper tests.

- [x] Add a mapper behavior test whose full history contains two journeys with overlapping day/message values; require the journey response to include all and only the latest sequence while full history retains both.
- [x] Run the test and witness RED because `TravelDiaryDto` currently has no shared journal-entry subset.
- [x] Add a journey-entry collection to `TravelDiaryDto`, populated from the already projected full history by the maximum recorded travel sequence; return no journey entry collection when no travel entries exist.
- [x] Remove narrative-only `TravelDiaryDayDto` fields that are replaced by shared journal entries (`OpeningNarration`, `JourneyBeat`, `ResourceBeat`, and `Entries`); retain structured travel state, outcome, warnings, and beat-slot detail that the player surface uses.
- [x] Ensure `JourneySequence` remains internal and is absent from JSON serialization; use the public DTO contract to expose only kind/message/day/turn.
- [x] Update TypeScript API types and focused mapper/API serialization tests to reflect the player response contract.

## Task 4: Render the same journal entries in both player views

**Files:** `src/WildBunch.Web/src/components/JournalSurface.tsx`, a focused shared journal-entry component under `src/WildBunch.Web/src/components/journal/`, `src/WildBunch.Web/src/components/travel/TravelDiaryNotebook.tsx`, `src/WildBunch.Web/src/components/travel/TravelDiaryDayCard.tsx`, `src/WildBunch.Application/Games/Mapping/TravelDiaryTextRenderer.cs`, `tests/WildBunch.Application.Tests/Renderers/TravelDiaryTextRendererTests.cs`, and `src/WildBunch.Web/src/tests/TravelPanel.test.tsx` plus relevant journal tests.

- [x] Replace independent travel narration expectations with a user-visible scenario in which the event-projected message says the rider retaliated after a multi-round fight; require that exact message and prohibit the current generic one-round/success claim.
- [x] Make the test fail against the existing `TravelDiaryDayCard` outcome summary before changing the component.
- [x] Extract the existing journal-entry grouping/formatting into one component used by `JournalSurface` and `TravelDiaryNotebook`; preserve full journal loading/error behavior and day/turn ordering.
- [x] Render latest-journey entries through that shared component. Keep the day cards for structured travel details and remove their independent prose, including generated encounter success summaries.
- [x] Retire the now-unused `TravelDiaryTextRenderer` and its renderer-specific tests after confirming no remaining production caller; keep meaningful fact/outcome tests on event-backed projector and consumer behavior.
- [x] Update the focused React tests to prove the journey view displays event-authored messages and does not invent contradictory outcome text.
- [x] Run `py -3 tools/run.py web` and verify lint, formatting, typecheck, Vitest, and production build.

## Task 5: Prove journey filtering through a persisted API flow and reconcile records

**Files:** `tests/WildBunch.Integration.Tests/GameApiTests.cs`, `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`, `docs/features.md`, `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`, `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, and ADR-0028 only if needed.

- [x] Extend `HighRiskTravelCanPauseResolveAndResumeWithoutSkippingTheTrail` to inspect the first journey in full history, start the second journey, reload, and assert that the same actual second-journey event message appears in full history and journey view while first-journey narration remains only in full history.
- [x] Assert independently observed messages and recorded day/turn/order; do not require the two view collections to be identical.
- [x] Prove the test fails if latest-journey filtering includes the first journey or loses the recorded message, then restore the projector/mapper and verify it passes.
- [x] Reconcile PG-008's current assessment: PR #246 preserves learned identity/facts through capture; this slice provides shared full/journey narration; remaining read ownership and end-to-end behavior stays partial.
- [x] Add dated dispositions for AP-19/WB-21 that distinguish retired independent narration from any still-retained structured facts; update behavioral test follow-ups with the actual replacement proof.
- [x] Re-read ADR-0028 against the diff and confirm in the PR description that event authority and rebuildable projections remain unchanged, unless evidence requires a dated ADR note.

## Task 6: Validate, review and publish

**Files:** this plan and the row 08 roadmap entry after verified merge.

- [x] Run `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, `py -3 tools/run.py ci --check`, `dotnet tool restore`, and `dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`; no event/schema migration is expected.
- [x] Build a fresh whole-branch review package and obtain an independent review against this plan, the spec, applicable backend/UI unslop profiles, ADR-0028 and PG-008; fix Critical/Important findings with RED/GREEN evidence, rerun the gate and get fresh review of changed behavior.
- [ ] Publish a Draft PR to `develop`, verify its exact source SHA, mark it ready only after review and the complete local gate pass, and wait for the exact-head hosted gate to pass before merge.
- [ ] Merge only after independent review and exact-head CI pass; verify merge SHA and develop-push gate, then update row 08 with source/merge SHAs and both run IDs.
- [ ] Retire the merged worktree/branch only after merge, gate, ancestry and worktree ownership are verified.
