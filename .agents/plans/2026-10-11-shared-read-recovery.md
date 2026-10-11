# Shared Game Read Recovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Show recoverable failures when shared game action or journal reads fail, without presenting unavailable data as an empty town or a missing playthrough.

**Architecture:** Keep TanStack Query as the owner of action and journal resources. Expose only resource-specific loading, error, and retry behavior through the current game session context; let the town and reference surfaces render that state beside their owned content. Existing session recovery remains responsible for failure to load the playthrough itself.

**Tech Stack:** React, TanStack Query, TypeScript, Vitest, React Testing Library.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, row 14; row 13's shared-resource outcome is reconciled from the current source and prior merged slices in Task 3.

**Current source:** `develop` at `3d74d060048ea8f2fdb32e2cf67cfbd636301eb8`; authored application version `0.1.0-dev.91`.

**Decision records:** ADR-0016 keeps TanStack Query for web server state; ADR-0028 keeps event-backed server state authoritative. This slice changes read failure presentation and retry only, so no durable decision or feature promise changes.

**Execution Strategy:** `executing-plans` because query state, context composition, and the town/reference recovery surfaces form one dependent read lifecycle and need one integrated reviewer context.

## Global Constraints

- The backend remains the authority for game state; the client must not infer an empty town or absent journal from a failed request.
- Keep action, journal, and session failures distinct; session restore continues to use its existing recovery screen.
- Retry the failed query through its TanStack Query observer and preserve any last successful data during a background refetch failure.
- Do not place a read failure in the shared command notice/error state.
- Preserve existing town availability rules and journal/case content when reads succeed.
- Advance the single authored application version from `0.1.0-dev.91` to `0.1.0-dev.92` in this implementation PR.
- Retire `.agents/plans/2026-10-11-travel-route-ownership.md` in this successor PR and update row 13 from current-source evidence; keep the stable baseline spec and roadmap live.

## Review Focus

- An action-read failure must not make a town look like it has no services; successful retry restores only the services the backend reports. Cover in Task 1 with a real `GameSessionProvider` and mounted `TownHubSurface`.
- A journal-read failure must remain reachable from the Journal and Case file affordances, identify the failed read, and recover when the same query succeeds on retry. Cover in Task 2 through the real shell and query client.
- A background journal refetch failure must leave the last successful journal visible while making the failure and retry available. Cover in Task 2 with a prior successful response followed by a rejected refetch.

---

### Task 1: Make town action-read failures visible and retryable

**Files:**
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionQueries.ts`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/flow/TownHubSurface.tsx`
- Test: `src/WildBunch.Web/src/tests/AppShell.test.tsx`

**Interfaces:**
- The session context exposes `actionsLoading: boolean`, `actionsError: string | null`, and `retryActions: () => Promise<void>` from the action query observer.
- `TownHubSurface` shows an initial loading status while action data is unavailable, shows a retry action on query failure, and continues rendering backend-reported services when stale data exists.

- [x] **Step 1: Write the failing player-path test.** In `AppShell.test.tsx`, mount the real shell with a saved GameStarted session; make the first `getAvailableActions` request reject and the retry return the store, sheriff, saloon, and trailhead actions. Assert that the town displays an action-read error and a retry button instead of silently appearing to have no services, then click retry and assert that the service navigation returns.

```tsx
it("recovers town services after the available-actions read fails", async () => {
  primeMocks();
  mockedGetAvailableActions
    .mockRejectedValueOnce(new Error("Connection unavailable"))
    .mockResolvedValueOnce([AvailableActionKind.BuySupplies]);
  window.localStorage.setItem("wild-bunch.current-game-id", "game-1");

  renderShell();

  expect(await screen.findByRole("alert")).toHaveTextContent(/could not read town services/i);
  await userEvent.setup().click(screen.getByRole("button", { name: /try again/i }));
  expect(await screen.findByRole("button", { name: /^store$/i })).toBeInTheDocument();
});
```

- [x] **Step 2: Witness the intended failure.** Run `npm --prefix src/WildBunch.Web test -- src/tests/AppShell.test.tsx -t "recovers town services"` and confirm the new assertion fails because no read-error recovery is rendered.
- [x] **Step 3: Expose action query state and render its recovery.** Return action loading/error/refetch from `useGameSessionQueries`, surface a resource-specific message and `retryActions` through `useCurrentGameSession`, and render an accessible loading or retryable error state in `TownHubSurface` without changing successful action mapping.
- [x] **Step 4: Verify the town behavior.** Run `npm --prefix src/WildBunch.Web test -- src/tests/AppShell.test.tsx -t "recovers town services"` and verify the error, retry, and backend-reported Store route assertions pass.

### Task 2: Make journal and case-file reads recoverable

**Files:**
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/flow/GlobalOverlays.tsx`
- Modify: `src/WildBunch.Web/src/components/JournalSurface.tsx`
- Modify: `src/WildBunch.Web/src/components/CaseFileSurface.tsx`
- Test: `src/WildBunch.Web/src/tests/AppShell.test.tsx`
- Test: `src/WildBunch.Web/src/tests/JournalSurface.test.tsx`

**Interfaces:**
- The session context exposes `journalLoading: boolean`, `journalRefreshing: boolean`, `journalError: string | null`, and `retryJournal: () => Promise<void>` from the journal query observer.
- The Journal and Case file surfaces accept query-specific error and retry inputs; a missing journal caused by a failed request remains openable from its reference control.
- Existing command feedback retains its existing ownership and is not overwritten by query errors.

- [x] **Step 1: Write the failing shell recovery tests.** Add real-shell tests where `getJournal` keeps rejecting until the player retries, the Journal overlay shows the read error and retry action, retry renders the returned journal, and the Case file affordance remains enabled with the same recovery action.
- [x] **Step 2: Witness the intended failure.** Run `npm --prefix src/WildBunch.Web test -- src/tests/AppShell.test.tsx -t "recovers the journal|keeps case-file recovery"` and confirm the current shell hides the failed journal behind a disabled Case file button or generic state and has no retry action.
- [x] **Step 3: Write the failing stale-data test.** In `JournalSurface.test.tsx`, render a known journal together with a journal-read error and a retry callback; assert that the known entry remains visible, the error is announced, and retry invokes the callback.
- [x] **Step 4: Witness the stale-data failure.** Run `npm --prefix src/WildBunch.Web test -- src/tests/JournalSurface.test.tsx -t "keeps the last journal"` and confirm the surface currently cannot display the query failure alongside its existing journal.
- [x] **Step 5: Implement the query-owned recovery state.** Expose journal initial-loading, background-fetching, error, and refetch behavior; let both reference surfaces show a retryable read error without clearing successful journal data; keep normal journal and case-file rendering unchanged when the query succeeds.
- [x] **Step 6: Verify the reference behavior.** Run `npm --prefix src/WildBunch.Web test -- src/tests/AppShell.test.tsx src/tests/JournalSurface.test.tsx src/tests/CaseFileSurface.test.tsx` and confirm initial failure, retry, stale-data retention, and normal rendering all pass.

### Task 3: Reconcile row 13, advance version, and complete the delivery

**Files:**
- Modify: `Directory.Build.props`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-travel-route-ownership.md`
- Keep: `.agents/plans/2026-10-11-shared-read-recovery.md` through this PR.

- [x] **Step 1: Reconcile row 13 against current source.** Confirm route-owned Store, Saloon, Sheriff, and travel commands; shared game-keyed session/actions/journal resources; derived wanted posters; the shared world-map key; all-resource refresh from gameplay and dev commands; and cross-game isolation tests. Record row 13 complete because these exits are now met; keep the action and journal read-recovery gap in row 14, whose current scope owns truthful transport/read feedback.
- [x] **Step 2: Retire the predecessor plan and record the verified row-13 boundary.** Remove the completed travel-route plan and record PR #276's reviewed source `8dbae1ddd582ea4588f2e5552e2bcd42ccca40b7`, merge `3d74d060048ea8f2fdb32e2cf67cfbd636301eb8`, identical tree `ac8dda387a6c8f7fd6a6999450b96a13ee610eaa`, and hosted exact-head/develop-push gates `38167396777` and `38167705458`; distinguish implemented row-13 behavior from the remaining row-14 work.
- [x] **Step 3: Advance the sole authored application version.** Set `Directory.Build.props` to `0.1.0-dev.92`; generated web identity remains build-derived.
- [x] **Step 4: Recheck ADR and feature truth.** Confirm ADR-0016 and ADR-0028 remain accurate and `docs/features.md` promises/dependencies do not change; no ADR or feature-matrix edit is needed for read recovery.
- [ ] **Step 5: Commit the staged candidate through the canonical fail-fast gate.** Stage only the intended source, test, version, roadmap and successor-plan retirement changes, inspect the staged diff, then commit with the check-only pre-commit hook enabled. The hook runs `ci --check` in cheapest-first order and stops at the first failure; resolve any formatter or linter failure before downstream build and test work.
- [ ] **Step 6: Independently review, publish, and verify.** Follow the PR and code-review runbooks; require a fresh whole-branch review, exact-head hosted canonical CI success, merge to `develop`, and a passing develop-push gate.
- [ ] **Step 7: Clean up paired Git refs and worktree.** After verifying the merged source/head and develop integration, remove this clean worktree and its matching local and upstream branch refs together, then verify all three are gone and clean this branch's scratch directory.
