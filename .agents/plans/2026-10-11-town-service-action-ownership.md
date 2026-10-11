# Town-Service Action Ownership Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Give the Sheriff and Saloon routes ownership of their command interactions and feedback, leaving the shared game-session context responsible for shared server state and session lifecycle.

**Architecture:** Keep the active session, available-action query, journal query, and event-derived server state in the existing shared provider and TanStack Query cache. Move wanted-poster reading and Saloon investigation/turn-in mutations into the route that presents each action, capture the game and declared identity in mutation variables, and render that route's pending/result/error state locally. Retire only the unreferenced duplicate `AvailableActionsPanel`; do not change domain commands, gameplay rules, or player promises.

**Tech Stack:** React, TanStack React Query, TypeScript, Vitest, React Testing Library.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, row 13 vertical React ownership, shared server-state authority, and truthful event-derived projections.

**Current source:** `develop` at `b6d73dc8eca5a9f25e3c49e8687342de4114adb2`, after merged PR #274. `Directory.Build.props` is `0.1.0-dev.89`; this PR advances it once to `0.1.0-dev.90`.

**Decision records:** ADR-0016 keeps React Query as server-state and mutation coordination, with component-owned presentation state. ADR-0028 keeps event history authoritative and read caches rebuildable. This slice changes route ownership inside those existing decisions; no ADR or feature-matrix change is expected.

**Execution Strategy:** `executing-plans` because the two route interactions share the same session/query boundary and the same central mutation hook, while their migration and behavioral proof need one coherent result; inline execution preserves the captured-session, refresh, and route-feedback contract across both owners. Splitting these coupled changes into separate PRs would leave the shared hook and busy state partially owned between slices.

## Global Constraints

- Session and journal data remain server-derived; mutation responses update only their returned game’s cache and refresh that game’s resource family.
- Every command mutation carries the submitted `gameId`; confrontation also carries the selected wanted-poster identity. Completion must not use a later active-game or selection value.
- Keep action eligibility derived from the current server-provided available-action list and current session state.
- Keep pending, successful result, unsuccessful domain result, and transport error feedback on the Sheriff or Saloon route that owns the command.
- Do not introduce optimistic inventory, journal, poster, or session state.
- Preserve setup, prologue, start-town arrival, travel, archive, restoration, and shared query behavior.
- Advance the only authored application version from `0.1.0-dev.89` to `0.1.0-dev.90` once in this PR.
- In the successor PR, record PR #274 source `65b1262290ac615dde10b8173655752015db1388`, merge `b6d73dc8eca5a9f25e3c49e8687342de4114adb2`, identical tree `23780aecc4de442db054051030a6afab09778bc8`, PR gate `38145616056`, and develop gate `38155976178`; retire `.agents/plans/2026-10-11-store-owned-purchase.md` and keep this plan through its own completing PR.

## Review Focus

- A command completing after its route changes refreshes only the submitted game's cache and cannot show its feedback on the newly active route.
- A server-declared unavailable action cannot issue its command; a pending command disables duplicate or competing actions on that route.
- A server result with `success: false` remains a domain outcome with its message, while a rejected request renders its transport error.

---

### Task 1: Move Saloon command ownership into the Saloon route

**Files:**
- Modify: `src/WildBunch.Web/src/flow/places/SaloonPlace.tsx`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionState.ts`
- Modify: `src/WildBunch.Web/src/state/GameSessionProvider.tsx`
- Modify: `src/WildBunch.Web/src/tests/SaloonPlace.test.tsx`
- Delete: `src/WildBunch.Web/src/tests/GameSessionProvider.test.tsx`

**Interfaces:**
- `SaloonPlace` owns one mutation for look-around, gossip, and named-person confrontation plus its local pending/result/error presentation. Its variables are `{ kind: "look-around" | "gather-gossip", gameId: string } | { kind: "confront", gameId: string, declaredWantedIdentityHandle: string }`.
- The selected wanted identity is captured at confrontation submission, not read later from shared state.
- A successful API response writes its returned `currentSession` or `currentJournal` only under that response's game ID, then awaits `refreshGameResources` for that ID.
- Wanted posters and current session remain shared query data. The selected identity is Saloon-only presentation state and is matched against current posters before enabling confrontation.
- `useCurrentGameSession` and its mutation helper retain shared lifecycle and travel commands but expose no Saloon command callbacks, eligibility flags, selected identity, or service-specific status strings.

- [ ] **Step 1: Prove Saloon identity and result behavior through the route.** Extend the provider-backed Saloon test to load a permitted confrontation action and wanted poster, choose that poster, click the named take-in action, and assert the API receives that game ID and poster ID. Return an unsuccessful domain result with its server message and wallet values; assert the Saloon displays that outcome and wallet delta without treating it as a transport error.
- [ ] **Step 2: Prove route-local feedback with a delayed result.** Begin a deferred look-around request, switch the rendered route to the Sheriff Office, resolve the Saloon request, and assert its message does not appear in the Sheriff route while the captured game's journal/cache is refreshed. This must fail against the current shared-notice callback because it leaks Saloon feedback into the active Sheriff route.
- [ ] **Step 3: Run the focused stale-feedback test against current code.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/SaloonPlace.test.tsx -t "does not leak a pending Saloon result"`. Confirm the failure is the old Saloon message appearing after the route switch, not setup or import failure.
- [ ] **Step 4: Prove pending, unavailable, and rejected-request behavior.** Use deferred API promises to assert a pending Saloon command disables all Saloon command buttons and a second click does not submit again. With the server action absent, assert its button stays disabled and the API is not called. Reject a permitted request and assert the route displays the request error.
- [ ] **Step 5: Implement the Saloon command mutation.** Define one route-owned TanStack mutation over the discriminated `kind` variables in this task's interface. Dispatch each kind to its existing API function, update returned `currentSession` or `currentJournal` only under the response's game ID, await `refreshGameResources` for that ID, and render the mutation's pending/error/result state locally. Preserve wallet-delta narration for confrontation.
- [ ] **Step 6: Bind Saloon presentation state to its route and game.** Move declared wanted identity out of `useGameSessionState` and `GameSessionProvider`; derive the selected poster in Saloon from current poster data, clear a stale selection when that poster disappears, and key the action owner by game ID so local feedback cannot carry to another playthrough. Remove the now-unused selected-poster context derivation and delete the provider test that only asserted this derived shape; the route behavior test owns the real identity contract.
- [ ] **Step 7: Run focused Saloon tests.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/SaloonPlace.test.tsx`; all remaining Saloon identity, action eligibility, pending, result, stale-route, and error behavior must pass.

### Task 2: Move wanted-poster reading into the Sheriff route and retire duplicate actions

**Files:**
- Modify: `src/WildBunch.Web/src/flow/places/SheriffPlace.tsx`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Delete: `src/WildBunch.Web/src/components/AvailableActionsPanel.tsx`
- Modify: `src/WildBunch.Web/src/tests/SheriffPlace.test.tsx`

**Interfaces:**
- `SheriffPlace` owns its wanted-poster read mutation, pending label, server result, and request error.
- The read command submits its captured `gameId`; its returned journal is written under its own game ID before that game's resources refresh.
- `GameSessionProvider` retains shared posters and queries but no town-service command state; global loading remains for shared query and lifecycle work.
- `AvailableActionsPanel` has no imports or route mounts in the live app. Its deletion removes an unreachable duplicate control surface that otherwise retains provider command consumers.

- [ ] **Step 1: Prove Sheriff feedback stays with the route that initiated the read.** Start a deferred `readWantedPosters` request, navigate through the test router from `/town/sheriff` to `/town/trailhead`, resolve it with an unsuccessful domain result, and assert the Sheriff message does not appear on `TravelPrepSurface` while the submitted game's journal is refreshed. The existing provider callback currently writes a global notice that TravelPrepSurface renders, so this is the red behavior proof.
- [ ] **Step 2: Run the focused stale-feedback test against current code.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/SheriffPlace.test.tsx -t "does not leak a pending poster result"`. Confirm it fails because the Sheriff result appears on TravelPrepSurface, not because of setup or import failure.
- [ ] **Step 3: Prove the normal Sheriff route states.** While a deferred read is pending, assert the read button is disabled and shows its pending label. Resolve a permitted read and assert its server message appears on Sheriff; with the action absent, assert the command is not sent; reject a permitted request and assert its request error appears.
- [ ] **Step 4: Implement Sheriff-owned read behavior.** Define the read mutation in `SheriffPlace`, use its pending/result/error state for the route controls, write the returned journal under the response game ID, and await that game's `refreshGameResources`. Keep the list of posters displayed from the current session query so poster truth remains server-derived.
- [ ] **Step 5: Remove town-service plumbing from the shared session hook.** Remove the four service mutations and their handlers, derived action flags, unused `hasReadWantedPosters`, selected identity, and the now-unused `BusyMode` type/property from `useGameSessionMutations`, `useCurrentGameSession`, and `useGameSessionState`. Compute shared `loading` from active shared queries and startup/travel mutations. Remove the unreachable `AvailableActionsPanel` after confirming `rg` finds no imports.
- [ ] **Step 6: Verify town-service routes and type consumers.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/SaloonPlace.test.tsx src/tests/SheriffPlace.test.tsx` and `npm --prefix src/WildBunch.Web run typecheck`. Inspect all remaining `useGameSession` consumers and confirm no unsupported service command fields remain.

### Task 3: Retire the completed predecessor and deliver the slice

**Files:**
- Modify: `Directory.Build.props`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-store-owned-purchase.md`
- Keep: `.agents/plans/2026-10-11-town-service-action-ownership.md`

- [x] **Step 1: Update row 13 for merged PR #274 and this active plan.** Record the Store-owned purchase outcome, source/merge/tree and completed exact-head plus develop gates listed above; remove stale links to the retired predecessor and preserve this plan through its own PR.
- [ ] **Step 2: Advance the authored application version once to `0.1.0-dev.90`.** Verify the web production build derives the same version from MSBuild evaluation without introducing duplicate package versions or committing generated output.
- [ ] **Step 3: Reconcile durable decisions and product promises against the actual diff.** Confirm ADR-0016 and ADR-0028 remain true and `docs/features.md` needs no semantic change; if live source contradicts that assessment, update the matching record in this PR.
- [ ] **Step 4: Run focused formatting, lint, tests, and browser verification.** Prove Sheriff poster read and Saloon clue/turn-in actions through their actual route UI, including one pending state and one failure/domain-outcome state. Use the shared PostgreSQL service only if the changed path requires it.
- [ ] **Step 5: Commit the intended tree through the normal check-only hook.** The hook runs `py -3 tools/run.py ci --check`; do not run the full gate immediately before or after a successful hooked commit. Inspect the staged diff and include the generated version change deliberately.
- [ ] **Step 6: Publish and complete the plan PR to `develop`.** Push from this fresh worktree; request and resolve independent review, require the exact-head hosted gate to pass, then merge under the active goal authorization. Verify the develop push gate, fast-forward the primary checkout, remove the clean worktree, delete its corresponding local and upstream branch refs, and verify the refs/worktree are gone as one closeout.
