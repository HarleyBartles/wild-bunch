# Refresh game-scoped query resources after commands

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Keep every active state-dependent player and developer query for a playthrough aligned with backend state after a command returns an authoritative result.

**Architecture:** Preserve current TanStack Query key values while defining their game-scoped ownership in one resource catalogue and one awaited refresh operation. Successful player and developer commands, including unsuccessful gameplay outcomes that still return updated state, refresh the active mutable resource families for the playthrough captured by that command; immutable setup prologue and world-map resources remain cached, and server state and event history remain authoritative.

**Tech Stack:** React, TanStack React Query, TypeScript, Vitest, React Testing Library.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, web slice and canonical server-resource ownership.

**Investigation:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, WB-03 and WB-19. Inspect current code because earlier resource findings have already been partly corrected.

**Decision records:** Preserve ADR-0016's TanStack Query server-state decision, ADR-0028's event-derived state and projections, and ADR-0030 through ADR-0032's developer boundary and event-backed controls. No durable decision changes are expected.

**Execution Strategy:** `executing-plans` because the behavior tests, query ownership helper, and all command call sites form one dependency-ordered cache-consistency change; splitting or parallelizing them would leave a slice with stale readers or duplicate policies.

## Global Constraints

- Backend query results and the event stream remain the only authority for game facts; no optimistic player or developer state is introduced.
- Existing query-key values and endpoint contracts remain stable so cached data is not forked during this cleanup.
- A command refreshes only resources keyed to the playthrough it actually changed, even if the user switches to another playthrough while the command is pending.
- Every returned gameplay result that may carry changed facts is reconciled regardless of its `success` value; preserve current player-facing outcome text and error semantics.
- Query invalidation waits for active refetches but does not claim that independent endpoints form one atomic read snapshot.
- Advance the single authored product version from `0.1.0-dev.87` to `0.1.0-dev.88` exactly once in this PR.
- In the successor PR, record PR #272's merge and hosted-gate evidence, retire its completed identity-scope plan, and keep this plan until its own implementation PR is merged.

## Review Focus

- An already-open developer reader shows a player command's newly recorded state without requiring the developer to close or refocus the panel.
- A developer command refreshes its contextual view and the other active readers of the same playthrough from backend state.
- A returned unsuccessful gameplay result can still change authoritative state and must not leave active queries stale.
- A slow completion for playthrough A does not invalidate or replace cached readers for playthrough B.
- Cache refresh remains awaited before a command flow reports its settled result, without treating concurrent endpoint reads as a database transaction.

---

### Task 1: Refresh active readers after player command results

**Files:**
- Modify: `src/WildBunch.Web/src/tests/GameSessionProvider.test.tsx`
- Create: `src/WildBunch.Web/src/state/gameResourceQueries.ts`
- Create: `src/WildBunch.Web/src/tests/gameResourceQueries.test.tsx`
- Modify: `src/WildBunch.Web/src/state/gameSessionQuery.ts`
- Modify: `src/WildBunch.Web/src/state/townStoreOffersQuery.ts`
- Modify: `src/WildBunch.Web/src/state/travelPreviewQuery.ts`
- Modify: `src/WildBunch.Web/src/dev/panels/SessionAuditDevPanel.tsx`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionQueries.ts`
- Modify: `src/WildBunch.Web/src/state/GameSessionProvider.tsx`

**Interfaces:**
- `gameResourceQueries.ts` owns stable key builders for the existing session, actions, journal, store-offer, travel-preview, session-dev, saloon-dev, travel-dev, and session-audit query families; existing query helpers delegate to these builders without changing key arrays.
- `refreshGameResources(queryClient, gameId)` invalidates the listed resource prefixes for that `gameId` and resolves after all active refetches settle.
- The player command mutation hook and store-purchase provider use the shared operation after any returned command result; archival keeps its terminal cache-removal behavior.

- [ ] **Step 1: Add a failing provider behavior test.** Mount an active `dev-session-context` query beside the existing `GameSessionProvider` store-offer reader, complete a purchase for game A, and assert that the active developer reader refetches and displays its new response. Preserve the existing game/town-switch setup and assert that game B's query function is not called by game A's completion.
- [ ] **Step 2: Run the focused test against current code.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/GameSessionProvider.test.tsx`; expect the stale developer response to remain visible because purchase completion currently refreshes only the current-game reader and store offers.
- [ ] **Step 3: Implement the key catalogue and awaited invalidation operation.** Reuse the current key shapes from `gameSessionQuery.ts`, `townStoreOffersQuery.ts`, `travelPreviewQuery.ts`, and the developer panel query declarations; use per-game prefixes for resources with additional town or preview dimensions. Do not invalidate the immutable `prologue` or `world-map` query families.
- [ ] **Step 4: Make existing query-key helpers and the session-audit reader consume the catalogue.** Keep every serialized query key equal to its prior value.
- [ ] **Step 5: Route player command refresh and store-purchase completion through `refreshGameResources`.** Preserve direct `setQueryData` from authoritative command responses, use the returned session ID where available and the command's captured game ID otherwise, and await active refetches before reporting the settled result. Keep archival as terminal cache removal.
- [ ] **Step 6: Add direct refresh-boundary tests.** In `gameResourceQueries.test.tsx`, mount active query observers for every declared mutable family for games A and B, invoke `refreshGameResources` for A, and assert that A's observed values refetch while B's query functions do not run.
- [ ] **Step 7: Verify non-success purchase reconciliation.** Extend the purchase behavior test with a `success: false` response carrying an updated session; assert the active game-scoped readers refetch while the existing error presentation remains unchanged.
- [ ] **Step 8: Run focused session and query tests.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/GameSessionProvider.test.tsx src/tests/gameResourceQueries.test.tsx` and confirm returned results update only their own playthrough's readers.

### Task 2: Route developer command results through the same boundary

**Files:**
- Modify: `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`
- Modify: `src/WildBunch.Web/src/dev/panels/SaloonDevPanel.tsx`
- Modify: `src/WildBunch.Web/src/dev/panels/TravelDevPanel.tsx`
- Test: `src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx`
- Test: `src/WildBunch.Web/src/tests/SaloonDevPanel.test.tsx`
- Test: `src/WildBunch.Web/src/tests/TravelDevPanel.test.tsx`

**Interfaces:**
- Consumes: the key catalogue and refresh operation from Task 1.
- Produces: successful difficulty, entropy, saloon-override, and travel-override commands await refresh of the current playthrough's player and developer readers.

- [ ] **Step 1: Add behavior assertions for active cross-panel readers.** For each developer panel's existing successful command tests, mount an active query owned by a different developer panel and one player reader, give their query functions changed second responses, and assert both render the refreshed backend values after the command settles.
- [ ] **Step 2: Run the focused Saloon, Travel, and Session panel tests before changing their handlers.** Confirm the cross-panel assertions fail because current handlers invalidate only their own context key.
- [ ] **Step 3: Replace panel-local invalidation calls with `refreshGameResources(queryClient, gameId)`.** Preserve the panel's local form, pending, error, and success behavior; do not make a panel's state authoritative or introduce a universal command form.
- [ ] **Step 4: Confirm failed developer commands do not display a fabricated refreshed state.** Keep the command error visible and do not overwrite drafts with optimistic query data; only a returned command result triggers the shared refresh.
- [ ] **Step 5: Run the focused panel and resource tests.** Run the four named web test files and confirm each command refreshes the same game's active readers.

### Task 3: Advance, retire and publish

**Files:**
- Modify: `Directory.Build.props`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-dev-form-identity-scope.md`

- [ ] **Step 1: Advance the product version to `0.1.0-dev.88`.** Keep `Directory.Build.props` as the only authored application version and use the production web build to verify generated identity.
- [ ] **Step 2: Update roadmap row 13 with PR #272's merged source SHA, merge SHA, matching tree identity, exact-head hosted gate `38136303353`, develop-push gate `38136649765`, and the new active plan.** Record that PR #272 preserves developer draft and async feedback state within the captured playthrough identity.
- [ ] **Step 3: Retire `.agents/plans/2026-10-11-dev-form-identity-scope.md` in this successor PR.** Keep the current query-freshness plan linked and active until its successor PR.
- [ ] **Step 4: Run the focused web tests, formatter check, and linter check before staging.** Stage only the intended tree and make the normal hooked commit; the hook runs the full fail-fast gate against that exact staged candidate. Do not run the full gate immediately before or after a successful hooked commit.
- [ ] **Step 5: Review the actual diff against ADR-0016, ADR-0028, ADR-0030, ADR-0031, and ADR-0032.** State why no durable decision changed, then obtain an independent review against the same diff.
- [ ] **Step 6: Commit the plan before implementation, then commit the implementation, push the branch, and open a PR to `develop`.** Confirm exact-head hosted CI passes before merge; merge, confirm the develop-push gate passes, fast-forward the primary checkout, and remove only this verified merged worktree and local branch.
