# BUNCH-20: Journal Modal Playtest Slice Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the existing cockpit activity-log surface into a production-shaped Journal modal overlay that reads live `JournalDto` data, helps the player read what happened, and does not leak hidden case truth.

**Architecture:** Reuse the current React Query-backed session hook and the existing `CockpitOverlayFrame` modal shell. The backend journal API already exposes the minimum player-safe data for a v1 journal timeline (`currentTown`, `caseFile`, and `logEntries`), so the first slice should stay frontend-only unless implementation proves the read model is missing a required field. The cockpit remains the trigger hub; Journal becomes a modal surface, not an inline panel or a new route.

**Tech Stack:** React 18, Vite, TanStack React Query, TanStack React Router, TypeScript, CSS, Vitest, Testing Library.

## Global Constraints

- Work from current `main` inspected at `6cdd23e427418ab2417367aeb132dd300c209cdc`.
- Journal stays read-only.
- Do not add canonical routing.
- Reuse `CockpitOverlayFrame`; do not introduce a second modal shell.
- Do not change `JournalDto`, `GameLogEntryDto`, or journal API endpoints unless a stop condition proves the data shape is insufficient.
- Keep hidden-truth markers out of the UI: no `trueCulpritId`, `isTrueCulprit`, `linkedSuspectIds`, `killerReleaseState`, backend-only ids, or raw debug payloads.
- Keep new frontend tests under `src/WildBunch.Web/src/tests/`.
- Use the established React Query + typed client conventions already present in `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts` and `src/WildBunch.Web/src/api/wildBunchApi.ts`.

## Preflight Findings

1. Current journal source model: `src/WildBunch.Domain/Game/GameLogEntry.cs`, `src/WildBunch.Domain/Game/GameLogEntryKind.cs`, `src/WildBunch.Domain/Journal/JournalSnapshot.cs`, `src/WildBunch.Domain/Journal/JournalResolver.cs`, and `src/WildBunch.Application/Games/Mapping/JournalMapper.cs`.
2. Safe player-facing fields: `kind`, `message`, `day`, `turn`, plus public journal/case-file fields already projected by `JournalDto`; hidden markers are already guarded by `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`, `tests/WildBunch.Application.Tests/GetJournalHandlerTests.cs`, and `tests/WildBunch.Domain.Tests/JournalResolverTests.cs`.
3. API/query shape: `src/WildBunch.Api/Games/JournalEndpoints.cs` -> `src/WildBunch.Application/Games/Queries/GetJournalHandler.cs` -> `IGameJournalReadRepository`; frontend transport lives in `src/WildBunch.Web/src/api/wildBunchApi.ts`.
4. Read model refinement: current `JournalDto` plus `logEntries` looks sufficient for v1 timeline grouping and readable chrome, so start frontend-only. If the journal needs a new historical field or paging cue that cannot be derived from existing data, stop and split before backend edits.
5. Existing trigger/modal pattern: `src/WildBunch.Web/src/components/CockpitOverlayFrame.tsx`, `src/WildBunch.Web/src/flow/GlobalOverlays.tsx`, and `src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx`.
6. Frontend conventions: `useCurrentGameSession` already uses React Query, the typed API client is centralized, and UI styling lives in `src/WildBunch.Web/src/styles.css`.
7. Player-visible timeline content: Opening, Travel, Case update, and Purchase entries are already modeled; a live journal should help Harley read the session chronology without surfacing ids or culprit truth.
8. Leak risks: avoid rendering `session.player.currentTownId`, suspect ids, warrant internals, `trueCulpritId`, `killerReleaseState`, or raw JSON dumps in the journal modal.
9. Reusable modal-shell work: none beyond `CockpitOverlayFrame`; keep the shell generic and feed it Journal content.
10. Validation and evidence: front-end unit/integration tests, local browser smoke, and screenshots of the open journal modal on desktop and mobile.

## Task 1: Build the Journal surface component

**Files:**
- Create: `src/WildBunch.Web/src/components/JournalSurface.tsx`
- Modify: `src/WildBunch.Web/src/components/LogPanel.tsx`
- Modify: `src/WildBunch.Web/src/styles.css`
- Test: `src/WildBunch.Web/src/tests/JournalSurface.test.tsx`

**Interfaces:**
- Consumes: `JournalDto`, `GameLogEntryDto`, `formatLogKind`
- Produces: `JournalSurface({ journal, loading, error })` and small helpers such as `groupEntriesByDay(entries)` and `formatJournalClock(journal)`

- [ ] **Step 1: Lift the current log feed into a Journal surface**

Render a journal summary block that shows `journal.currentTown.name`, `journal.clock.day`, `journal.clock.timeOfDay`, `journal.caseFile.caseSummary`, and a live entry count.

- [ ] **Step 2: Make the chronology readable**

Replace the flat `log-list` with a grouped chronological timeline, using day separators and kind badges instead of an undifferentiated feed. Keep the entry order stable and preserve the public message text as-is.

- [ ] **Step 3: Keep hidden truth out of view**

Do not render backend ids, culprit markers, raw DTO dumps, or any field that only exists for internal aggregation. The journal should read like a player log, not a debug snapshot.

- [ ] **Step 4: Give the modal the right look**

Add stylesheet classes for the journal summary, day grouping, entry cards, badge treatment, and responsive modal spacing. Reuse the existing modal palette and panel language instead of introducing a new design system.

- [ ] **Step 5: Prove the surface in isolation**

Add a Vitest/Testing Library test that renders the new surface with a sample journal and asserts chronology, grouping, modal-readable copy, and absence of hidden ids.

## Task 2: Wire the cockpit triggers to the modal Journal

**Files:**
- Modify: `src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx`
- Modify: `src/WildBunch.Web/src/flow/GlobalOverlays.tsx`
- Modify: `src/WildBunch.Web/src/tests/App.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/AppShell.test.tsx`

**Interfaces:**
- Consumes: `JournalSurface`, `CockpitOverlayFrame`, existing `useGameSession`
- Produces: a cockpit button that opens Journal in a modal overlay and a flow-shell overlay entry that uses the same surface

- [ ] **Step 1: Remove the inline journal panel from the cockpit route**

Replace the inline `LogPanel` in `DebugCockpitRoute` with an `Open journal` button beside `Open case file`, then open `JournalSurface` inside `CockpitOverlayFrame` from that cockpit state.

- [ ] **Step 2: Reuse the same Journal surface in the shell overlay bar**

Point `GlobalOverlays` at the same `JournalSurface` and relabel the overlay from `Activity log` to `Journal` so both host shells use one shared surface instead of duplicate log chrome.

- [ ] **Step 3: Keep modal behavior inherited**

Preserve `Escape`, backdrop click, and focus return behavior from `CockpitOverlayFrame`; do not add journal-specific modal handling unless a later bug proves it is needed.

- [ ] **Step 4: Update the route tests to match the new host behavior**

Update the cockpit tests to assert the journal opens as a dialog, not as an inline section, and that the cockpit body no longer renders the old log heading. Keep the flow-shell overlay test aligned with the new label and confirm the modal still hides hidden-truth markers.

## Task 3: Validation and browser proof

**Files:**
- Modify: `src/WildBunch.Web/src/tests/App.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/AppShell.test.tsx`

**Interfaces:**
- Consumes: the updated Journal modal and cockpit trigger
- Produces: front-end proof that the Journal is modal, readable, and safe

- [ ] **Step 1: Run the web checks**

Run `npm run typecheck`, `npm test`, and `npm run build` from `src/WildBunch.Web`.

- [ ] **Step 2: Only add backend checks if the plan changes backend files**

If implementation review ends up changing a backend file, run `dotnet build WildBunch.sln` and `dotnet test WildBunch.sln`. If the slice stays frontend-only, do not add backend churn just to satisfy the plan.

- [ ] **Step 3: Capture manual browser evidence**

Start the local API and web app, open `/debug`, click `Open journal`, and capture desktop and mobile screenshots that show the modal overlay, its chronology, and focusable close behavior.

- [ ] **Step 4: Verify hidden-truth safety in the browser**

Confirm the modal contents do not display hidden ids, culprit markers, raw JSON, or debug-only fields before marking the slice ready.

## Split / Stop Conditions

- If the journal surface needs a new backend field, stop and split into a backend read-model slice before editing API code.
- If the modal needs a new shell primitive beyond `CockpitOverlayFrame`, stop and split; do not turn this into a design-system task.
- If screenshots show the overlay still reads like a debug log instead of a journal, refine the UI before any API change.
