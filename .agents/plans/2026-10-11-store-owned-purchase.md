# Store-Owned Purchase Lifecycle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Give the Store slice ownership of purchase submission, pending state, quantity validation, and result feedback instead of routing its command through the global game-session context.

**Architecture:** `StorePlace` submits a mutation with the game and town identity captured at the user's action, writes an authoritative returned session only under that session's key, and awaits `refreshGameResources`. The Store surface renders pending, successful, unsuccessful, and transport-error outcomes locally; the shared provider continues to own shared playthrough resources and identity selection.

**Tech Stack:** React, TanStack React Query, TypeScript, Vitest, React Testing Library.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, web slice and store/inventory behavior.

**Investigation:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, WB-01 and WB-09. Current source was inspected at `53b5b852eb490c16cfe3b5ad023af7cb47b2a043`; purchase still lives in `GameSessionProvider`, and its quantity field parses partial numeric prefixes.

**Decision records:** Preserve ADR-0016's TanStack Query server-state decision and ADR-0028's event-derived state and projections. This slice changes feature ownership within the existing client architecture; no durable decision changes are expected.

**Execution Strategy:** `executing-plans` because mutation identity, pending state, legal quantity validation, authoritative refresh, and store feedback form one tightly coupled Store interaction with one route-level test boundary; inline execution preserves a single test and state model, while per-task delegation would duplicate route setup and split its lifecycle contract.

## Global Constraints

- Server state and event history remain authoritative; do not create optimistic inventory or wallet state.
- Every returned purchase result refreshes the playthrough's mutable resources, including `success: false` results that include an updated session.
- A purchase quantity must be a positive whole number before the client submits it; invalid input displays `Enter a whole number of at least 1.` beside the field, and the backend remains responsible for authoritative legality and affordability.
- A delayed purchase remains scoped to the game and town captured when submitted; it must not write another playthrough's cache or feedback.
- A route-level test switches from game A to game B before the deferred purchase completes, then proves game A's resource family refreshes while game B retains its own inventory and receives no stale feedback.
- Advance the single authored application version from `0.1.0-dev.88` to `0.1.0-dev.89` exactly once in this PR.
- In the successor PR, record PR #273's merge and hosted-gate evidence, retire its completed query-freshness plan, and keep this plan until its implementation PR is merged.

## Review Focus

- A pending purchase disables duplicate submission and stays pending until active resource refreshes settle; the route-level test must observe the disabled control during a deferred request.
- Blank, zero, negative, and fractional quantities never reach the API; the route-level test must prove the request is absent for each invalid value.
- A completed purchase for the captured game refreshes that game's active resources and preserves the server result's success or failure presentation.
- Switching playthrough identity while the mutation is pending unmounts the old Store mutation state without preventing its result from refreshing only the captured game's resources.

---

### Task 1: Move purchase behavior into the Store slice

**Files:**
- Modify: `src/WildBunch.Web/src/flow/places/StorePlace.tsx`
- Modify: `src/WildBunch.Web/src/components/StoreOffersPanel.tsx`
- Modify: `src/WildBunch.Web/src/state/GameSessionProvider.tsx`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/GameSessionProvider.test.tsx`

**Interfaces:**
- The Store surface owns a TanStack mutation whose variables include the captured `gameId`, `townId`, offer and validated quantity; its completion updates `gameSessionQueryKey(result.currentSession.id)` and awaits `refreshGameResources` for that returned session.
- `StoreOffersPanel` retains an explicit offer-display contract and reports validated purchase intent; it exposes a pending state supplied by its Store owner and does not coerce malformed input.
- `GameSessionProvider` keeps shared session and wanted-identity behavior but no longer declares `handleBuyOffer`; `useCurrentGameSession` no longer exposes `setSession` when no remaining consumer needs it.
- Purchase feedback is rendered within the Store surface: successful results use the existing notice treatment, unsuccessful gameplay results use the existing error treatment, and request failures retain their message.

- [x] **Step 1: Add a deferred-purchase behavior test.** In `StorePlaceFeedback.test.tsx`, keep the store route mounted while `buyStoreItem` is unresolved, assert the Buy control is disabled during the request, attempt a second submission, and prove the API was called once.
- [x] **Step 2: Run the focused test against current code.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/StorePlaceFeedback.test.tsx -t "disables duplicate purchase submission"`; the pending-state assertion failed because the current Store receives only global session loading and has no purchase-owned pending state.
- [x] **Step 3: Add strict quantity behavior tests.** Submit blank, zero, negative, and fractional values through the Store control and assert each displays `Enter a whole number of at least 1.` beside the field without calling `buyStoreItem`; submit a valid whole quantity and assert the exact quantity reaches the API.
- [x] **Step 4: Run the quantity tests against current code.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/StorePlaceFeedback.test.tsx -t "fractional quantity"`; the assertion failed because `parseInt` converted `1.5` to `1` and the API received the request.
- [x] **Step 5: Move the command into `StorePlace`.** Use `useMutation` with captured game/town values, cache the returned session only under its own session ID, await game-scoped refresh for all returned outcomes, and expose mutation pending/data/error to the Store surface without optimistic state.
- [x] **Step 6: Make quantity submission explicit and valid.** Use a submitted quantity field with positive-integer validation and accessible field feedback; the handler must revalidate parsed numeric input and submit no fallback quantity for malformed values.
- [x] **Step 7: Remove Store-only command plumbing from the universal context.** Delete `handleBuyOffer` from `GameSessionProvider` and remove the now-unconsumed `setSession` helper from `useCurrentGameSession`; preserve the other session fields and wanted-identity selection.
- [x] **Step 8: Relocate and reconcile Store behavior tests.** Move provider-level purchase ownership, unsuccessful-result refresh, and failed-catalog-refresh coverage to `StorePlaceFeedback.test.tsx`; retain only wanted-identity behavior in `GameSessionProvider.test.tsx`. Keep query-family game scoping covered by `gameResourceQueries.test.tsx`.
- [x] **Step 9: Run focused Store and provider tests.** Run `npm --prefix src/WildBunch.Web test -- --run src/tests/StorePlaceFeedback.test.tsx src/tests/GameSessionProvider.test.tsx src/tests/gameResourceQueries.test.tsx`; all 15 tests pass, including delayed purchase completion after a playthrough switch.
- [x] **Step 10: Verify the Store route in the browser.** In worktree `Z:\_agent-worktrees\wild-bunch\codex\row13-provider-narrowing`, API `http://localhost:5275`, frontend `http://localhost:5173`, create a disposable QA playthrough and buy three Food. The route showed `Purchased 3 Food for $6.00.`, cash changed from $25 to $19, inventory Food changed from four to seven, and the purchase plus game/action/journal/store refreshes returned 200. No browser console or dynamic network errors remained during this Store interaction. The app's ordinary `/api/games/setup` request returned 415 because its idempotency header replaces the JSON content type; row 14 now owns that separate transport defect. Worktree API and frontend were stopped afterward; shared PostgreSQL remains running.
- [x] **Step 11: Preserve delayed-purchase identity across a playthrough switch.** Add a route-level deferred-purchase test that switches from game A/town A to game B/town B before A completes; prove the request retained A's identity, only A's resource probe refetched, B retained its own cash, and A's completion message did not appear in B. Failure proof: temporarily removed the Store's game/town React key and reran this test; it failed because A's success message appeared in B. Restored the key and confirmed the test passes.

### Task 2: Advance and publish the Store slice

**Files:**
- Modify: `Directory.Build.props`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-gameplay-query-freshness.md`

- [x] **Step 1: Advance the product version to `0.1.0-dev.89`.** Keep `Directory.Build.props` as the only authored application version and verify the production web build emits the same version.
- [x] **Step 2: Update roadmap row 13 with PR #273's source SHA `194c8f5fd1190cab400743218c514316280436ba`, merge SHA `53b5b852eb490c16cfe3b5ad023af7cb47b2a043`, matching source/merge tree `138294e73fead2e511a0dabc9c59e1069ac82061`, exact-head hosted gate run `38139560542`, develop-push gate run `38139950383`, and this Store-owned purchase plan as active.** Record that active player and developer resources refresh by game-scoped query families after commands.
- [x] **Step 3: Retire `.agents/plans/2026-10-11-gameplay-query-freshness.md` in this successor PR.** Its query-freshness behavior shipped in merged PR #273; preserve this current Store plan through its own completing PR.
- [x] **Step 4: Review the actual diff against ADR-0016 and ADR-0028.** The TanStack Query server-state and event-derived projection decisions remain true; this slice only moves Store interaction ownership into its route and adds no durable architecture decision.
- [x] **Step 5: Compare the actual diff with `docs/features.md`.** No product promise, dependency or feature disposition changes; store purchase behavior remains the same, with stricter input validation and truthful local feedback.
- [x] **Step 6: Run focused web tests, formatter and linter checks before staging.** Stage only intended files and create the normal hooked implementation commit; do not run the complete gate immediately before or after a successful hooked commit. The hooked staged-candidate `py -3 tools/run.py ci --check` passed, including 517 domain tests (1 skipped), 189 GameContent, 165 Application, 1 API, 333 PostgreSQL integration, 346 web tests and the production build at `0.1.0-dev.89`; focused formatter and lint checks passed.
- [ ] **Step 7: Push the branch and open a PR to `develop`.** Verify the PR body, head SHA and state; after independent review and exact-head hosted CI pass, merge under the active goal authorization, verify the develop push gate, fast-forward the primary checkout, and remove only this verified merged worktree and local branch.
