# Store Offers Route Ownership Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Load store offers only while the Store route is active, while preserving the canonical game-and-town query identity and captured purchase invalidation.

**Architecture:** Move the existing `useTownStoreOffers` consumer from the app-wide `GameSessionProvider` into the lazy `StorePlace` route, which is the only mounted product consumer. Keep the Query key/options owner and current purchase command/invalidation contract unchanged; remove store read data from the shared context and preserve the existing player-visible loading, unavailable, success, and error states.

**Tech Stack:** React, TypeScript, TanStack Query, TanStack Router, Vitest, React Testing Library.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, section E and row-13 canonical-resource/state-ownership contract.

**Decision records:** Preserve ADR-0016 and ADR-0027. This moves an existing server read to its only live consumer and does not change gameplay authority, server truth, or the product promise.

**Execution Strategy:** `executing-plans` (Native inline execution) because the provider value, route consumer and the purchase/query identity tests form one coupled change; repeated implementer handoffs would reconstruct the same QueryClient and route lifetime, while one whole-branch review can assess the composed behavior.

## Global Constraints

- Store offers remain server-owned; the client caches reads and must not synthesize stock, price, or purchase truth.
- Keep the canonical query key scoped by game ID and town ID, and keep the query disabled when either identity is absent.
- A successful purchase invalidates only the game-and-town resource captured when the purchase began.
- Only the active Store route observes store offers; visiting the town hub or another route must not initiate an offers request.
- Preserve current purchase feedback and unavailable presentation; do not add pending UX, quantity semantics, retry policy, or general provider redesign.
- The current epic owns the per-plan worktree, successor-plan retirement, version increment, Draft PR to `develop`, exact-head hosted gate, merge, develop-push gate, and cleanup sequence.
- Commit this plan before executing it; keep the previous plan until the delivery commit includes its retirement and the version advancement.

## Review Focus

- Store offers must not load from the app-wide provider on unrelated routes; the routed test proves no request on Town Hub followed by the expected request and display after entering Store.
- Captured purchase invalidation must still refresh only its original game-and-town key after selection changes.
- Failed refetches must continue to hide cached offers and show the existing unavailable state.
- Removing offer fields from context must not break the mounted Store route or unrelated shared session consumers.

---

### Task 1: Move the offers query to the Store route

**Files:**
- Modify: `src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/GameSessionProvider.test.tsx`
- Modify: `src/WildBunch.Web/src/state/GameSessionProvider.tsx`
- Modify: `src/WildBunch.Web/src/flow/places/StorePlace.tsx`
- Keep: `src/WildBunch.Web/src/state/townStoreOffersQuery.ts`
- Keep: `src/WildBunch.Web/src/hooks/useTownStoreOffers.ts`

- [ ] Add a routed behavior test in `StorePlaceFeedback.test.tsx`: start at `/town`, wait until the `Tumbleweed` hub is rendered, assert `getTownStoreOffers` has not been called, activate the accessible `Store` navigation button, then assert the offers request uses `game-1` and `t-town` and the returned offer is shown.
- [ ] Run that focused test against the current implementation and confirm it fails because the provider starts the offers request before Store is entered.
- [ ] Remove `storeOffers`, `storeOffersLoading`, and the `useTownStoreOffers` call from `GameSessionProvider`; read `gameId` and `currentTown` from `useGameSession` in `StorePlace` and use `useTownStoreOffers(gameId, currentTown?.id)` to supply the panel's data/loading.
- [ ] Adapt the test-only provider reader to invoke `useTownStoreOffers` with the currently selected game/town identity, preserving the purchase command tests.
- [ ] Keep the existing delayed-purchase test that independently changes town and game IDs and the failed-refetch test proving cached offers become unavailable.
- [ ] Run `npm test -- src/tests/StorePlaceFeedback.test.tsx src/tests/GameSessionProvider.test.tsx` and `npm run typecheck` from `src/WildBunch.Web`.

The StorePlace consumer should have this ownership shape:

```tsx
const { gameId, currentTown, session, loading, handleBuyOffer, notice, error } = useGameSession();
const { storeOffers, loading: storeOffersLoading } = useTownStoreOffers(gameId, currentTown?.id);
```

Do not expose these query values through `GameSessionContext` or move the query key/options into the route; the shared query owner remains `state/townStoreOffersQuery.ts`.

Adapt `GameSessionProvider.test.tsx` so its test-only context reader calls `useTownStoreOffers` with the selected session identity while continuing to exercise the provider purchase command. Keep the existing delayed-purchase test that independently changes town and game IDs, and keep the failed-refetch test proving cached offers become unavailable. These tests cover command invalidation and query error mapping; the routed Store test covers production ownership and mount lifetime.

Run from `src/WildBunch.Web`: `npm test -- src/tests/StorePlaceFeedback.test.tsx src/tests/GameSessionProvider.test.tsx` and `npm run typecheck`. Confirm the new routed test fails before the provider/query move for the expected request-on-town-hub reason and passes after the move.

### Task 2: Record delivery and retire the predecessor plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-11-store-offers-query-resource.md`
- Keep: `.agents/plans/2026-10-11-store-offers-route-ownership.md`

- [ ] Record PR #267's reviewed source `7227a681767e53499a3d55989b49b057baf24676`, merge `a122ed2f6bc7e8b0238361e2bea868d904583d60`, exact-head gate `38124223316`, and develop-push gate `38124540425` in row 13.
- [ ] Describe the store offers query and captured invalidation as delivered, link this plan as the live row-13 work item, and preserve the remaining provider narrowing, identity-lifetime, and capability-composition scope.
- [ ] Advance the sole authored product version from `0.1.0-dev.82` to `0.1.0-dev.83`; do not hand-edit generated version mirrors.
- [ ] Retire `.agents/plans/2026-10-11-store-offers-query-resource.md` only in this successor delivery and retain the current plan through its completing PR.
- [ ] Leave `docs/features.md` and ADRs unchanged because no product capability or durable decision changed.

Before the first implementation commit, apply the repository formatter to the touched source files and confirm the roadmap's existing Markdown wrapping policy. Stage the code, test, version, roadmap and predecessor retirement together so this PR carries its successor-artifact and development-version obligations.

**Acceptance:** the Store route is the only production observer of offers; the game/town query key, failed-refetch state and captured purchase invalidation remain covered; row 13 records PR #267 and the remaining scope accurately; `Directory.Build.props` is the sole authored product-version source at `0.1.0-dev.83`.

### Task 3: Validate, review, publish and merge

- [ ] Run `py -3 tools/run.py ci --check` on the complete implementation tree after focused tests and formatting; the staged-candidate hook validates the commit before it is created.
- [ ] Review the complete diff against the specification, this plan, ADR-0016/0027, the decision-record and feature-matrix playbooks, frontend standards, testing doctrine and scoped web unslop profile.
- [ ] Open a Draft PR targeting `develop`, verify its body and exact head, mark it ready to trigger the hosted canonical gate, and merge only after that exact-head gate passes.
- [ ] Record the resulting merge and both hosted runs in the next JIT successor plan/roadmap update.
- [ ] After merge, verify the develop-push gate, source/merge tree equality, and primary-checkout fast-forward; remove only the verified clean worktree and branch.

## Out of Scope

- Moving the purchase mutation, changing purchase pending/result UX, or changing quantity validation; row 14 owns those feedback contracts.
- Broadly decomposing `GameSessionProvider`, relocating every command, or redesigning the shared session state API.
- Query-resource migrations beyond store offers, generalized invalidation, or query-cache garbage-collection policy.
- Product feature, gameplay, API, event, ADR, or feature-matrix changes.
- Resurrecting the unused `FieldReportPanel`; its retirement or repair belongs with residual source ownership after live consumers are established.
