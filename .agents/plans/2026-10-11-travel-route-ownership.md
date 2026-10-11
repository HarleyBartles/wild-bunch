# Travel Route Ownership Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the travel preparation and trail routes ownership of their travel commands, mutation state, and player feedback while keeping server resources shared through TanStack Query.

**Architecture:** Travel departure belongs to `TravelPrepSurface`; trail-day progression and encounter resolution belong to the `TravelPanel` travel slice; arrival acknowledgement belongs to `TrailFlowSurface`. Each submitted command captures its game identity, writes only its returned game's session, refreshes that game's resources, and keeps its message or request error with the interaction that produced it. The session provider continues to own shared reads and application lifecycle operations.

**Tech Stack:** React, TanStack React Query, TypeScript, Vitest, React Testing Library.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, rows 12 and 13, with retained travel authority and route-owned vertical slices.

**Current source:** `develop` at `2388edbb85a6a90401e6ce9c5df2c2351251ee46` after PR #275; the authored version is `0.1.0-dev.90`.

**Decision records:** ADR-0016 keeps TanStack Query for server state and mutation coordination with component-owned presentation state; ADR-0028 keeps event history authoritative and caches rebuildable. This slice applies those decisions without changing durable gameplay rules or public feature promises, so no ADR or feature-matrix change is expected.

**Execution Strategy:** `executing-plans` because departure, trail progression, encounter resolution, and arrival are one retained travel lifecycle whose cache reconciliation and route-local feedback must remain coherent across both travel routes.

## Global Constraints

- The backend remains authoritative; do not add optimistic game state or client-side travel legality.
- Capture `gameId` and command-specific destination or encounter choice in each mutation's submitted variables.
- Apply returned session data and refresh only resources for the game that submitted the command.
- Distinguish unsuccessful gameplay results from rejected HTTP requests and show both at the owning travel surface.
- Starting a later trail command clears prior command feedback so an older outcome cannot mask the newer result.
- Keep departure gated by the fresh preview matching the selected origin and destination.
- Keep one world map and the existing pointer-operated selection contract; do not add another map or keyboard map-selection feature.
- Preserve setup, prologue, first-town arrival, archive, session restore, and gameplay rules outside this travel-route ownership slice.
- Advance the authored application version once from `0.1.0-dev.90` to `0.1.0-dev.91` in the implementation PR.
- Before implementation, retire the completed Store and town-service predecessor plans in this successor PR and record PR #275 in the roadmap with its source, merge, tree, and hosted gate evidence.

## Review Focus

- A travel command that resolves after the rendered playthrough changes updates only the submitted game's cache and cannot surface its feedback on a different route or game.
- A valid domain result with `success: false` remains visible as a travel outcome, while a rejected request is presented as a request error.
- The public arrival acknowledgement is reachable through the completed-journey route and is not duplicated in the active-travel panel branch that production routing never renders.

---

### Task 1: Keep travel departure feedback and reconciliation in travel preparation

**Files:**
- Modify: `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Modify: `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx`

**Interfaces:**
- `TravelPrepSurface` owns a departure mutation with variables `{ gameId: string; destinationTownId: string }` and renders its pending, returned gameplay message, and transport error locally.
- Departure success updates the session query from `result.currentSession` and refreshes that returned game's resource family; it does not write global notice/error state.
- The shared session hook no longer exposes `handleTravel` and its aggregate loading state no longer includes a travel-departure mutation.

- [x] **Step 1: Prove route-local departure feedback across navigation.** In the existing real-router test harness, start a deferred departure, leave the route before it resolves, then resolve a domain-unsuccessful result and wait for the session refresh to finish. Assert that the unrelated route does not display travel feedback.
- [x] **Step 2: Prove departure request errors remain local.** A rejected departure request displays its request error on the travel surface, leaves the shared session cache unchanged, and submits the captured game ID and destination.
- [x] **Step 3: Implement route-owned departure.** `TravelPrepSurface` owns the departure mutation with captured game and destination variables, preserves preview gating, updates the returned game's session query and refreshes its resources, and renders the mutation result, pending state, or rejection locally.
- [x] **Step 4: Remove shared departure plumbing.** The provider no longer owns the travel departure mutation or callback, and its global loading state excludes departure while retaining unrelated application lifecycle behavior.
- [x] **Step 5: Verify the preparation route.** The route's behavior tests cover local outcomes, request errors, captured identity, and cache reconciliation; TypeScript, formatting, and lint checks cover the changed frontend files.

### Task 2: Make the trail surface own progression, encounter, and arrival outcomes

**Files:**
- Modify: `src/WildBunch.Web/src/hooks/useTravelPanelState.ts`
- Modify: `src/WildBunch.Web/src/components/TravelPanel.tsx`
- Modify: `src/WildBunch.Web/src/flow/TrailFlowSurface.tsx`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Modify: `src/WildBunch.Web/src/tests/TravelPanel.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/TrailFlowSurfaceCompleted.test.tsx`

**Interfaces:**
- `useTravelPanelState` owns advance-day and encounter-resolution mutations, their route-local result/error display, and cache refresh for the captured game; it no longer accepts a global `onTurnResult` callback.
- `TrailFlowSurface` owns the completed-journey arrival acknowledgement mutation and its pending/error handling; successful acknowledgement updates the returned game's session query and refreshes its resources.
- Remove the provider-owned `handleTravelTurnResult` and `handleAcknowledgeArrival` callbacks and mutations.
- Remove the duplicate arrival mutation and action from `useTravelPanelState`/`TravelPanel`; production `TrailFlowSurface` already renders the completed journey as a separate arrival surface before it can render `TravelPanel`.

- [x] **Step 1: Prove trail-turn results are visible where they occur.** Advance-day and encounter behavior tests assert returned feedback in the trail UI and authoritative session reconciliation; the advance-day case uses a legal unsuccessful gameplay result.
- [x] **Step 2: Prove transport rejection and captured identity behavior.** A rejected trail command renders its request error and leaves the session unchanged. A deferred command for one game resolves after another game is rendered; only the submitted game's session cache changes, and stale feedback stays hidden from the current game.
- [x] **Step 3: Implement local trail-turn reconciliation.** `useTravelPanelState` owns result handling, captures game IDs in mutation variables, updates only the submitted game's session query and resources, and presents returned messages or request errors in `TravelPanel`.
- [x] **Step 4: Prove the real arrival interaction.** The completed-journey route tests cover pending duplicate prevention, rejected acknowledgement with retry, and successful session/cache reconciliation; one reachable arrival button remains on `TrailFlowSurface`.
- [x] **Step 5: Implement route-owned arrival and remove the unreachable duplicate.** The completed-travel surface owns the acknowledgement mutation and retryable transport error; `TravelPanel` no longer contains the unreachable completed-state acknowledgement branch.
- [x] **Step 6: Remove remaining shared trail command plumbing.** Deleted provider-owned travel result and arrival callbacks/mutations and inspected `useGameSession` consumers; trail command lifecycle and feedback no longer depend on global notice/error state.
- [x] **Step 7: Verify retained travel interactions.** Travel behavior tests cover route ownership, domain outcomes, rejected requests, captured game identity, pending state, and authoritative session reconciliation; frontend type, format, and lint checks cover the changed files.

### Task 3: Record predecessor delivery, version, and row-13 progress

**Files:**
- Modify: `Directory.Build.props`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-town-service-action-ownership.md`
- Keep: `.agents/plans/2026-10-11-travel-route-ownership.md`

- [x] **Step 1: Reconcile predecessor scope against merged source and delivery proof.** The Store-owned purchase plan was retired in PR #275 and its scope is recorded as shipped; the Sheriff/Saloon action plan was fully delivered by PR #275, its outcomes remain in the roadmap, and the superseded town-service plan was removed in this successor's first committed plan update.
- [x] **Step 2: Record PR #275 accurately.** The roadmap records PR #275's reviewed source `12f2ae46af5f52ccb2c388db05d73a729866a0b7`, merge `2388edbb85a6a90401e6ce9c5df2c2351251ee46`, identical tree `bf0d83d4da7dfb8d8e167feddd368d2cc9662a64`, exact-head gate `38160005996`, develop-push gate `38160463124`, and makes this travel plan the active row-13 plan.
- [x] **Step 3: Advance the one authored version.** `Directory.Build.props` is the single authored version at `0.1.0-dev.91`; the web build derives its generated version identity from it.
- [x] **Step 4: Reassess row 13 from live source.** Travel departure, progression, encounter resolution and arrival are route-owned. Setup and travel use the same `world-map` resource key; shell, travel, reference and dev readers use shared game-keyed session state. Travel mutations reconcile against their submitted game, and a deferred cross-game test proves the current game's data and action state remain isolated. Row 13 stays executing because broader composition and resource ownership exit criteria remain outside this slice.
- [x] **Step 5: Recheck decisions and feature promises.** ADR-0016 governs the React stack and server-owned gameplay state; ADR-0028 governs immutable event history, rebuildable caches, strict CQRS and replay without rerolls. The feature promises remain unchanged.
- [x] **Step 6: Run the canonical gate and commit the staged candidate.** Use the check-only hook as the canonical local gate and commit only the staged candidate it validates.
- [ ] **Step 7: Review and publish.** Request one fresh whole-branch review, resolve Important/Critical findings with witnessed tests, require the exact-head hosted gate to pass, merge into `develop`, verify the develop-push gate and fast-forward the primary checkout.
- [ ] **Step 8: Clean up paired Git refs and worktree.** After verifying the merged source/head and develop integration, remove this clean worktree and its matching local and upstream branch refs together; verify all three are gone and clean this branch's scratch directory.
