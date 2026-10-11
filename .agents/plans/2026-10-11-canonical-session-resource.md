# Share the Canonical Playthrough Resource Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the shell and active travel surface observe one TanStack Query resource for a playthrough so updates from either reader are immediately shared.

**Architecture:** A focused game-session query module owns the canonical query key and options for the server session resource. The general session hook and travel hook observe the same keyed cache; travel mutations rely on their existing authoritative result callback to update that resource, instead of invalidating a parallel travel cache. The backend remains authoritative and travel selection or other local UI state is unaffected.

**Tech Stack:** React 18, TanStack Query 5, TypeScript, Vitest, Testing Library.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, row 13 React resource ownership and server authority.

**Decision records:** No durable decision changes. This implements the accepted Query ownership boundary and does not change player promises, persistence or event history.

**Execution Strategy:** `executing-plans` with Native inline execution. The single cache transition is shared by hooks and mutation callbacks; TDD and one whole-branch review benefit from keeping that tightly coupled change in one integration context.

## Global Constraints

- Backend/player-known state remains authoritative; React does not invent game truth.
- TanStack Query owns API-derived state; do not keep a second cache for the same session endpoint.
- Do not introduce new dependencies, a generic query framework, or a broad React folder migration.
- This PR advances the sole authored product version from `0.1.0-dev.79` to `0.1.0-dev.80`.
- Each successor plan uses a fresh worktree from refreshed `origin/develop`, retires eligible completed artifacts in its first substantive commit, and merges through a PR to `develop` only after hosted exact-head CI passes.

## Review Focus

- When a mounted travel view receives a changed session resource through the shell's canonical query, the travel display must update rather than retain its previous duplicate value. The TravelPanel behavior test in Task 1 covers this.
- Travel actions must continue to apply the authoritative turn result to the selected playthrough and refresh dependent session/action/journal reads through the existing callback. Task 1 preserves and verifies the existing TravelPanel result callback scenarios.

---

### Task 1: Use one session query for shell and travel

**Files:**
- Create: `src/WildBunch.Web/src/state/gameSessionQuery.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionQueries.ts`
- Modify: `src/WildBunch.Web/src/hooks/useTravelPanelState.ts`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Test: `src/WildBunch.Web/src/tests/TravelPanel.test.tsx`
- Modify: `Directory.Build.props`

**Interfaces:**
- `gameSessionQueryKey(gameId: string | null)` returns the canonical `['session', gameId]` TanStack Query key.
- `gameSessionQueryOptions(gameId: string | null)` returns the canonical key, `getGame` query function, enabled state and current no-retry policy for that resource.
- The existing `onTurnResult` callback remains the travel-command result boundary and continues to receive the backend result.

- [x] **Step 1: Add the behavioral regression test.** Extend the existing `TravelPanel.test.tsx` harness to wait for an active travel view, write a session with a visibly changed route warning to the shell's canonical query key, and assert the mounted travel panel displays the new warning and no longer displays the old one. Use the existing `renderTravelPanel` returned `queryClient`; do not assert query counts, cache inventory or hook implementation.

- [x] **Step 2: Witness the intended failure.** Run `npm test -- --run src/tests/TravelPanel.test.tsx` from `src/WildBunch.Web`. Expected: the new test fails because the travel panel continues observing `['travel-session', gameId]`, while the shell update is written to `['session', gameId]`. Confirm the failure is the missing changed warning, not an async setup or rendering error.

- [x] **Step 3: Introduce the canonical session resource and migrate both observers.** Add the key/options owner in `state/gameSessionQuery.ts`. Use it in `useGameSessionQueries` and `useTravelPanelState`; retain travel's journey-based enabled condition and supplied-session initial data. Replace direct `['session', id]` set/remove/invalidate keys in `useCurrentGameSession` and `useGameSessionMutations` with `gameSessionQueryKey` so reads and command updates cannot drift to another key.

- [x] **Step 4: Remove travel's duplicate-cache invalidations.** The existing `onTurnResult` path writes the returned current session and awaits the shell's dependent query refresh. Remove the `['travel-session', gameId]` invalidations and query-client dependency from travel mutation setup; do not add speculative invalidation graphs or alter command outcomes. Keep all three existing travel command paths and callback payload behavior.

- [x] **Step 5: Verify the focused behavior and regression boundary.** Run `npm test -- --run src/tests/TravelPanel.test.tsx src/tests/usePhaseRouteSync.test.tsx` and confirm both the new shared-resource behavior and existing session query error behavior pass. Inspect the changed hooks to confirm there is no `travel-session` key and all session cache writes use the canonical key.

- [x] **Step 6: Advance development identity, review the diff and commit.** Set `Directory.Build.props` to `0.1.0-dev.80`; do not hand-edit generated web identity files. Run the focused web formatter, linter, typecheck and build; inspect the generated identity and full diff; then commit through the normal check-only hook, which runs the canonical gate against the staged tree. Do not run the expensive canonical gate immediately before the commit because the hook verifies that same staged candidate. No generated build output is committed.

- [ ] **Step 7: Complete readiness review and publish.** Run plan completion-readiness, prepare and request a fresh independent whole-branch review against this plan, the spec, frontend doctrine, testing doctrine and web play-surface unslop profile. Resolve Critical or Important findings with witnessed RED/GREEN proof and rerun the canonical gate. Open or update a PR targeting `develop`, verify the PR head matches the reviewed commit and hosted exact-head canonical CI passes before merge. After merge, verify the develop-push gate and fast-forward the main checkout to current `develop`; leave the active plan and roadmap for the next substantive successor to assess.

## Exit Criteria

- Shell and travel observe the same session resource and a shell-side authoritative update changes the mounted travel display.
- Travel advance, encounter resolution and arrival acknowledgement continue to pass their existing result and display tests.
- Session reset, archive, mutation and refresh paths use the same canonical session key.
- No duplicate travel-session cache remains; no broad provider, map, API transport or local UI-state redesign is included.
- PR advances `Directory.Build.props` once to `.80`; local and hosted canonical gates pass on the reviewed head before merge.
- Row 12 is recorded complete with PR #264 merge and gate evidence, and its completed plan is retired in this successor's first substantive commit.
