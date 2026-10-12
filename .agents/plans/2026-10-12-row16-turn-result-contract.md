# Game Turn Result Journal Contract Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline.

**Goal:** Make the updated `GameSessionDto` the single owner of journal data in game command responses and remove the duplicate top-level travel-diary field.

**Architecture:** The Application command result carries the current session and command-specific travel facts. The session DTO already contains the event-derived travel journal used by the browser; no command response needs a second copy. Collapse the overloaded factory into one method so all calls construct the same response shape while preserving journey status, journey and trail-event facts.

**Tech Stack:** C#/.NET, ASP.NET JSON DTOs, TypeScript, xUnit integration and handler tests.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, player journal decision.

**Execution Strategy:** `executing-plans` (the result factory, DTOs and real response tests change as one sequential behavior slice).

## Global Constraints

- `GameSessionDto.TravelDiary` remains the single response owner of the shared full-playthrough and journey journal.
- Keep `JourneyStatus`, `Journey`, and `TrailEvent` as command-specific turn-result facts.
- Do not change event history, journal projection, gameplay behavior, command legality or endpoint routes.
- This pre-release has no client compatibility obligation for the duplicate ignored/unused response field.
- Use one `Directory.Build.props` application version, advancing exactly once to `0.1.0-dev.103` for this PR.
- Preserve PR delivery to `develop`, exact-head hosted gate, review, successor-artifact retirement, and paired worktree/local/upstream branch cleanup.

## Review Focus

- A travel command must still return actual diary entries and day facts through `currentSession.travelDiary`; the changed HTTP acceptance assertion must fail if that nested projection is absent.
- A three-argument factory call must retain a stable valid result shape after the shorter overload is removed, including when preview fails before journey creation.

---

### Task 1: Remove duplicate turn-result journal ownership

**Files:**
- Modify: `src/WildBunch.Application/Games/Models/GameDtos.cs`
- Modify: `src/WildBunch.Application/Games/Mapping/GameTurnResultFactory.cs`
- Modify: `src/WildBunch.Web/src/api/types.ts`
- Modify: `tests/WildBunch.Application.Tests/Handlers/AdvanceTravelDayHandlerTests.cs`
- Modify: `tests/WildBunch.Application.Tests/Handlers/ResolveJourneyEncounterHandlerTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/GameApiTests.cs`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-saloon-poi-alias.md`
- Keep through this PR: `.agents/plans/2026-10-12-row16-turn-result-contract.md`

**Interfaces:**
- Consumes: `GameSessionDto.TravelDiary` and command-specific journey status/snapshot/trail-event fields.
- Produces: one `GameTurnResultDto` shape whose journal is only `CurrentSession.TravelDiary`.

- [ ] Confirm all production and test consumers of `GameTurnResultDto.TravelDiary`; verify browser behavior reads the cached `currentSession.travelDiary` and no client reads the top-level result diary.
- [ ] Update the existing handler/API behavior assertions from `result.TravelDiary` to `result.CurrentSession.TravelDiary`, preserving concrete expected entry/day/effect assertions. Do not add a test that merely checks a property was deleted.
- [ ] Run the focused API travel acceptance test with the nested journal assertion. Temporarily make `GameSessionMapper` omit the session travel diary, verify the acceptance test fails at that assertion, restore the mapper exactly and rerun green.
- [ ] Remove `TravelDiary` from `GameTurnResultDto` and the matching optional TypeScript field. Keep `GameSessionDto.TravelDiary` and all session-level browser usage.
- [ ] Remove the exact three-argument overload from `GameTurnResultFactory`; retain one `Create` method with optional journey-specific parameters and no duplicated diary projection. Ensure simple purchase and failed-preview calls still return valid current-session DTOs.
- [ ] Add dated AP-18 dispositions to the source and test follow-up records. State that the earlier overloads emitted the same journal nested and top-level only for the extended call path; browser consumers use the session cache; API/handler tests now prove nested response facts. Mark this response-shape item resolved without claiming journal projector consolidation beyond this slice.
- [ ] In the first substantive commit, record PR #287's exact head, merge and hosted gate result in the roadmap; retire the completed saloon-alias plan, add this successor, and advance the application version from `.102` to `.103` once.
- [ ] Compare the changed response contract against applicable ADRs and `docs/features.md`; record why no ADR or feature matrix update is required.
- [ ] Ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, run the focused API travel acceptance test, focused Application handler tests, web validation and `git diff --check` through the command bus.
- [ ] Let the staged-snapshot pre-commit hook run the canonical full gate on the implementation commit; do not rerun it immediately before or after a successful hooked commit.
- [ ] Obtain a fresh whole-branch review, publish the PR to `develop`, verify the exact head and hosted gate before merge, fast-forward the primary checkout, then remove the clean worktree before deleting and verifying its matching local/upstream branch refs.

**Validation:** Real API travel response still exposes a truthful event-derived journey journal through `currentSession`; mutation proof establishes the nested assertion is sensitive; handler and API tests confirm behavior; Application and web type checks; full tracked commit gate; whole-branch review; exact-head hosted gate and post-merge develop state.
