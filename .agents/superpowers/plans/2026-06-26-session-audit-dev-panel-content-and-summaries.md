# Session Audit Dev Panel Content and Summaries Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the dev-only session audit surface readable enough to inspect a play session without changing gameplay behavior or leaking hidden truth through player APIs.

**Sequencing Gate:** Implementation must not start until BUNCH-90 has landed on `main`. Before implementation, rebase on current `main`, inspect the merged BUNCH-90 saloon POI dev-control changes, and update the weak-summary inventory and tests to include any new saloon dev events, DTOs, endpoint outputs, and panel-visible behavior.

**Architecture:** Keep summary generation in `WildBunch.Application.Projections` so the dev query handler stays thin and the `/api/dev/` boundary remains centralized. Expand the audit projector, or extract a small formatter beside it, to translate the event stream into readable summaries by event family: session setup, town action context, investigations, saloon activity, travel/journey events, bounty/sheriff events, and dev travel overrides. Keep the React panel focused on presentation only: sequence, event type, and a readable summary, with no gameplay logic, no new routes, and no shell redesign.

**Tech Stack:** C#/.NET 10 / `net10.0`, ASP.NET Core minimal APIs, xUnit, React 18, TypeScript, React Query, styled-components, Vite/Vitest.

## Global Constraints

- Dev audit output stays under `/api/dev/`.
- Normal player APIs and read models must not newly expose hidden truth.
- Do not change gameplay rules or event semantics.
- Do not add new dev commands.
- Do not redesign the dev overlay shell.
- Do not add durable event timestamps unless the store already provides them; the current audit projector synthesizes `OccurredAtUtc` at projection time, so the panel should not promise a true event timestamp.
- Keep the work narrow to the session audit slice.

---

## Preflight Findings

- `src/WildBunch.Application/Projections/FullAuditProjector.cs` currently gives useful text only for `GameStarted` and `StoreItemPurchased`; every other event falls back to `e.GetType().Name`.
- `src/WildBunch.Application/Dev/Queries/GetSessionAuditHandler.cs` is intentionally thin: it loads the event stream, projects audit entries, and maps them to `SessionAuditDto`.
- `src/WildBunch.Api/Dev/DevEndpoints.cs` exposes `GET /api/dev/sessions/{id}/audit` and gates access through `DevRoleGuard`.
- BUNCH-90 is now on `main` and adds the saloon dev-control surface under `/api/dev/sessions/{id}/saloon-context`, `/api/dev/sessions/{id}/saloon/force-override`, and `/api/dev/sessions/{id}/saloon/clear-override`, with `SaloonDevContextDto` and `SaloonDevPanel` as the dev-facing outputs.
- `src/WildBunch.Web/src/dev/panels/SessionAuditDevPanel.tsx` renders a simple list with sequence, event type, and summary; it does not show a real timestamp field.
- `GetEventStreamAsync` returns typed events only. The repository does not surface a durable timestamp for the audit panel, so presentation work should stay focused on sequence, type, and summary.
- The current weak-summary inventory is the set of event types that still render as raw CLR names as of this refreshed plan head: `TownActionContextEntered`, `InvestigationPerformed`, `SaloonPersonOfInterestSpotted`, `SaloonPersonOfInterestConfronted`, `JourneyStarted`, `TravelDayAdvanced`, `TrailEventApplied`, `JourneyEncounterResolved`, `JourneyCompleted`, `JourneyArrivalAcknowledged`, `DevTravelOverrideForced`, `DevTravelOverrideCleared`, `DevTravelOverrideConsumed`, `WantedSuspectConfronted`, and `SheriffTurnInSettled`.

## Source Seams Inspected

- `src/WildBunch.Application/Projections/FullAuditProjector.cs`
- `src/WildBunch.Application/Projections/FullAuditProjection.cs`
- `src/WildBunch.Application/Dev/Queries/GetSessionAuditHandler.cs`
- `src/WildBunch.Application/Dev/Models/SessionAuditDto.cs`
- `src/WildBunch.Api/Dev/DevEndpoints.cs`
- `src/WildBunch.Api/Dev/DevRoleGuard.cs`
- `src/WildBunch.Api/DependencyInjection.cs`
- `src/WildBunch.Application/Dev/Models/SaloonDevContextDto.cs`
- `src/WildBunch.Application/Dev/Mapping/SaloonDevContextMapper.cs`
- `src/WildBunch.Application/Dev/Queries/GetSaloonDevContextHandler.cs`
- `src/WildBunch.Domain/Events/DevSaloonOverrideForced.cs`
- `src/WildBunch.Domain/Events/DevSaloonOverrideCleared.cs`
- `src/WildBunch.Domain/Events/DevSaloonOverrideConsumed.cs`
- `src/WildBunch.Web/src/dev/panels/SessionAuditDevPanel.tsx`
- `src/WildBunch.Web/src/dev/panels/SaloonDevPanel.tsx`
- `src/WildBunch.Web/src/dev/types.ts`
- `src/WildBunch.Web/src/tests/DevOverlay.test.tsx`
- `src/WildBunch.Web/src/tests/TravelDevPanel.test.tsx`
- `tests/WildBunch.Application.Tests/Dev/GetSessionAuditHandlerTests.cs`
- `tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs`
- `tests/WildBunch.Application.Tests/Projections/ProjectionTests.cs`
- `tests/WildBunch.Application.Tests/Projections/GameLogEntryLegacyProjectionTests.cs`
- `tests/WildBunch.Integration.Tests/Dev/DevEndpointTests.cs`
- `tests/WildBunch.Integration.Tests/Dev/DevSaloonEndpointTests.cs`
- `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`
- `docs/adr/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md`
- `docs/adr/ADR-0031-event-sourced-dev-travel-controls.md`

## Target Summary Behavior

- `GameStarted` should stay readable and include player name, starting town, and difficulty.
- `StoreItemPurchased` should stay readable and include quantity, display name, total price, and wallet after the purchase.
- `TownActionContextEntered` should summarize the town action context, town, day/turn, time of day, and pursuit heat instead of the raw type name.
- `InvestigationPerformed` should summarize the source kind, town, and message, and it may note whether the event references a clue or warrant.
- `SaloonPersonOfInterestSpotted` and `SaloonPersonOfInterestConfronted` should read like inspection notes, not raw event names.
- `JourneyStarted`, `TravelDayAdvanced`, `TrailEventApplied`, `JourneyEncounterResolved`, `JourneyCompleted`, and `JourneyArrivalAcknowledged` should surface the travel narration already present on the event payloads.
- `DevTravelOverrideForced`, `DevTravelOverrideCleared`, and `DevTravelOverrideConsumed` should read as dev diagnostics so the audit log shows when a dev override was introduced, cleared, or consumed.
- `DevSaloonOverrideForced`, `DevSaloonOverrideCleared`, and `DevSaloonOverrideConsumed` should also read as dev diagnostics because BUNCH-90 now introduces audit-relevant saloon dev-control events.
- `WantedSuspectConfronted` and `SheriffTurnInSettled` should summarize the confrontation or payout without leaking anything beyond what the dev-only audit surface is allowed to show.
- The fallback for any unhandled event should remain explicit and boring so new event types do not silently become unreadable.

## Before/After Examples

- Before: `TownActionContextEntered`
- After: `Entered the saloon in Pinecross on Day 1, Turn 1 (Morning, heat 0).`

- Before: `JourneyEncounterResolved`
- After: `Resolved the trail encounter by Bribe; continued on foot: no.`

- Before: `DevTravelOverrideForced`
- After: `Forced the next travel override to Foe with a custom encounter message.`

## Task 0: Refresh the audit inventory after BUNCH-90 lands

**Files:**
- Modify: `.agents/superpowers/plans/2026-06-26-session-audit-dev-panel-content-and-summaries.md`
- Inspect: the merged BUNCH-90 saloon POI dev-control changes on current `main`

**Interfaces:**
- Consumes: the post-merge saloon POI dev-control implementation, its event stream effects, and any panel-visible output.
- Produces: an updated weak-summary inventory and test target list that reflects the current `main` after BUNCH-90.

- [ ] **Step 1: Rebase on current main and inspect BUNCH-90**

Rebase this branch onto the latest `main`, then inspect the merged BUNCH-90 saloon POI dev-control changes. Report exactly which saloon POI dev events, DTOs, endpoint outputs, and panel-visible entries exist. For the current mainline merge, the new audit-relevant saloon dev events are `DevSaloonOverrideForced`, `DevSaloonOverrideCleared`, and `DevSaloonOverrideConsumed`, and the saloon dev context surface is `SaloonDevContextDto` plus `SaloonDevPanel`.

- [ ] **Step 2: Refresh the weak-summary inventory**

Update the inventory above to include the saloon dev-control event types added by BUNCH-90 and confirm that no other new audit-relevant saloon events were introduced.

- [ ] **Step 3: Refresh the test targets**

Adjust the backend and frontend test plan entries so they explicitly cover the saloon dev-control summaries and any panel-visible saloon examples that are now part of the current `main` truth.

- [ ] **Step 4: Commit the refresh note**

Commit this plan refresh before implementation begins so the launch criteria are frozen against the BUNCH-90 state of `main`.

## Task 1: Expand audit summaries in the application projector

**Files:**
- Modify: `src/WildBunch.Application/Projections/FullAuditProjector.cs`
- Create: `tests/WildBunch.Application.Tests/Projections/FullAuditProjectorTests.cs`

**Interfaces:**
- Consumes: typed `IDomainEvent` instances from `WildBunch.Domain.Events`.
- Produces: readable `AuditEntry.Summary` values for representative current event families.

- [ ] **Step 1: Write failing projector tests**

Add focused coverage for representative event families and the fallback path. The test file should prove:
- `GameStarted` still produces a useful summary.
- `TownActionContextEntered` includes the town/context/day/turn/time/heat details.
- `InvestigationPerformed` uses the event message instead of the raw CLR type name.
- `SaloonPersonOfInterestSpotted` and `SaloonPersonOfInterestConfronted` summarize saloon activity in plain language.
- `JourneyStarted`, `TravelDayAdvanced`, `TrailEventApplied`, `JourneyEncounterResolved`, `JourneyCompleted`, and `JourneyArrivalAcknowledged` no longer collapse to raw type names.
- `DevTravelOverrideForced`, `DevTravelOverrideCleared`, and `DevTravelOverrideConsumed` each have an explicit dev-only summary.
- `DevSaloonOverrideForced`, `DevSaloonOverrideCleared`, and `DevSaloonOverrideConsumed` each have an explicit dev-only summary because the merged saloon dev-control surface now emits them.
- `WantedSuspectConfronted` and `SheriffTurnInSettled` read as readable inspection notes.
- Unknown event types still fall back to the event type name.

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~FullAuditProjector"`

Expected: the new test file fails until the summary formatter is implemented.

- [ ] **Step 2: Implement the summary formatter**

Expand the projector, or extract a small formatter beside it, so the summary switch stays readable and domain-family specific. Keep the formatter pure and deterministic. Do not add any gameplay mutation, and do not move summary logic into the API layer.

- [ ] **Step 3: Re-run the projector tests**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~FullAuditProjector"`

Expected: the projector tests pass and the fallback path still behaves boringly.

- [ ] **Step 4: Commit the backend summary slice**

Commit only the application projector and its tests so the backend summary behavior can be reviewed in isolation.

## Task 2: Keep the dev audit endpoint thin and prove the payload stays dev-only

**Files:**
- Modify: `tests/WildBunch.Application.Tests/Dev/GetSessionAuditHandlerTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevEndpointTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevSaloonEndpointTests.cs`
- Leave unchanged unless a test proves otherwise: `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`

**Interfaces:**
- Consumes: `GetSessionAuditHandler`, `SessionAuditDto`, and the `/api/dev/sessions/{id}/audit` route.
- Produces: evidence that the handler still maps projected entries cleanly and that the endpoint remains dev-only.

- [ ] **Step 1: Tighten the handler test around readable summaries**

Update the application test so it proves `GetSessionAuditHandler` returns the readable summaries produced by the projector rather than only checking that entries exist.

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj --filter "FullyQualifiedName~GetSessionAuditHandlerTests"`

Expected: the updated assertions fail until the new summary strings are wired through.

- [ ] **Step 2: Tighten the dev endpoint integration test**

Update the dev endpoint test to assert that the payload contains at least one readable summary string from a representative session, not just the raw `"GameStarted"` token. Keep the existing 200/403/404 checks intact.
If the saloon dev-control audit entries remain raw after BUNCH-90, add explicit coverage for `DevSaloonOverrideForced`, `DevSaloonOverrideCleared`, or `DevSaloonOverrideConsumed` so the audit endpoint proves the merged saloon event stream is readable.

If BUNCH-90 added saloon dev force/clear/consume events, extend the integration coverage to include one representative audit payload from those flows so the dev-only endpoint proves the new saloon entries are readable.

Run: `.\scripts\postgres-dev.ps1 ensure`

Run: `.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj --filter "FullyQualifiedName~DevEndpointTests|FullyQualifiedName~GameApiHiddenTruthTests|FullyQualifiedName~DevSaloonEndpointTests"`

Expected: the dev endpoint stays under `/api/dev/`, non-dev access still returns 403, missing sessions still returns 404, and player-facing audit routes remain 404.

- [ ] **Step 3: Commit the endpoint regression coverage**

Commit the application and integration test updates together so the endpoint behavior and the boundary proof move as one slice.

## Task 3: Improve the dev panel presentation for scannability

**Files:**
- Modify: `src/WildBunch.Web/src/dev/panels/SessionAuditDevPanel.tsx`
- Create: `src/WildBunch.Web/src/tests/SessionAuditDevPanel.test.tsx`

**Interfaces:**
- Consumes: `SessionAuditDto` from `src/WildBunch.Web/src/dev/types.ts` and the `getSessionAudit()` client.
- Produces: a more scannable list item layout that highlights sequence, event type, and summary without redesigning the overlay shell.

- [ ] **Step 1: Write a focused panel rendering test**

Add a test that renders a representative audit payload and asserts that:
- the sequence number is visible,
- the event type remains visible,
- the summary reads as the primary content,
- an empty state still renders cleanly,
- loading and error states are preserved.

Run: `cd src/WildBunch.Web; npm test`

Expected: the new test fails until the panel layout is updated.

- [ ] **Step 2: Update the panel markup and styling**

Rework the entry card so it reads as a compact audit row or two-line card instead of a flat metadata strip. Keep the overlay shell unchanged. Do not introduce a timestamp column unless a real timestamp source appears in the implementation.

- [ ] **Step 3: Re-run the web test suite**

Run: `cd src/WildBunch.Web; npm test`

Run: `cd src/WildBunch.Web; npm run build`

Expected: the panel test passes and the web build stays green.

- [ ] **Step 4: Commit the UI slice**

Commit the panel markup and its test together so the UI review can focus on presentation only.

## Task 4: Final validation and evidence collection

**Files:**
- No new source files unless a review finding requires a tiny follow-up.

**Interfaces:**
- Consumes: the backend projector tests, dev endpoint tests, and panel test slices.
- Produces: a coherent evidence bundle for the draft PR and the eventual worker return.

- [ ] **Step 1: Run the full backend build**

Run: `dotnet build WildBunch.sln`

Expected: the solution builds without introducing any persistence, API, or frontend regressions.

- [ ] **Step 2: Run the targeted backend and integration tests**

Run: `dotnet test tests/WildBunch.Application.Tests/WildBunch.Application.Tests.csproj`

Run: `.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests/WildBunch.Integration.Tests.csproj`

Expected: the application tests and PostgreSQL-backed integration tests pass together.

- [ ] **Step 3: Run the frontend validation**

Run: `cd src/WildBunch.Web; npm test`

Run: `cd src/WildBunch.Web; npm run build`

Expected: the audit panel tests and TypeScript build pass.

- [ ] **Step 4: Capture before/after examples for the return**

Record two or three exact audit entries before and after the change, with emphasis on the events that used to render as raw type names. Include one town-context example, one travel example, one dev-travel-override example, and one saloon-dev-control example.

- [ ] **Step 5: Commit the final slice**

Commit the final source state only after the validation commands above pass and the worktree is clean.

## Self-Review

- The plan covers the source seams named in the preflight brief.
- The plan keeps the dev audit surface under `/api/dev/`.
- The plan preserves the player-facing hidden-truth boundary.
- The plan avoids gameplay changes, new dev commands, and overlay-shell redesign.
- The plan includes backend, integration, and frontend validation.
- The plan includes before/after examples for the worker return.
- The plan stays narrow enough to fit in one PR unless a review uncovers a separate missing seam.
