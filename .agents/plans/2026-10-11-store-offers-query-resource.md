# Store Offers Query Resource Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make town store offers a canonical TanStack Query resource scoped to the selected game and town, and ensure a completed purchase refreshes only the offers resource it changed.

**Architecture:** Replace `useTownStoreOffers`' custom effect and local response cache with a query keyed by `(gameId, townId)`. A successful purchase invalidates the exact resource captured by that purchase, so a late response from a previous game or town cannot refresh the newly selected store. Preserve current API authority, loading/error presentation, purchase result handling, and the existing session reload.

**Tech Stack:** React, TypeScript, TanStack Query, Vitest, React Testing Library.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially the row-13 canonical-resource and stale-identity contract and the web-resource exit in the cleanup sequence.

**Decision records:** Preserve the existing React/server ownership and Query-resource decisions. This is a routine implementation of those decisions; no ADR changes are expected unless inspection reveals a durable decision divergence.

**Execution Strategy:** `executing-plans` with Native inline execution. The query hook, purchase invalidation, and the identity-race behavior form one tightly coupled web slice.

## Global Constraints

- Keep store offers server-owned; the client may cache reads but must not synthesize stock, price, or purchase truth.
- Scope cache identity by both game ID and town ID. Missing identity disables the read and must not show another identity's offers.
- A purchase from an earlier selection may update that earlier query only; it must not trigger a refresh of the current game's store.
- Preserve current player-visible loading, unavailable, and purchase feedback behavior. Do not broaden this into general provider narrowing or invalidation redesign.
- The first substantive commit retires `.agents/plans/2026-10-11-world-map-accessibility-scope.md`, records PR #266 delivery in the roadmap, and advances the sole product version from `0.1.0-dev.81` to `0.1.0-dev.82`. Keep this plan and the governing spec through its completing PR.
- Work only from this fresh worktree based on `origin/develop`; publish a PR to `develop`, require exact-head hosted CI to pass before merge, and verify the develop-push gate afterward.

## Review Focus

- Store offers must be keyed by both game and town, with disabled identity producing no unrelated offer data.
- A delayed successful purchase captured for game/town A must not refresh the currently selected game/town B resource.
- Existing StorePlace purchase feedback and failure/loading presentation remain truthful.
- No unrelated map, provider, or application changes enter the diff.

---

### Task 1: Retire the completed predecessor and establish this slice

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-11-world-map-accessibility-scope.md`
- Keep: `.agents/plans/2026-10-11-store-offers-query-resource.md`

Update row 13 to record PR #266's reviewed source `95744da818feed86c9c501ae98b8d6195c8f14cc`, merge `84b625da5e77cea3029748e619be27ee930e632f`, exact-head gate `38121259884`, and develop-push gate `38121717561`. Link this plan as the live row-13 work item and advance the product version to `0.1.0-dev.82`. Retire the completed map-scope plan because its correction and decision history shipped in PR #266. Do not change the roadmap's remaining row-13 scope or the baseline spec.

**Acceptance:** the roadmap accurately distinguishes delivered map/session work from remaining resource work, the predecessor plan and stale link are gone, and `Directory.Build.props` is the only authored product-version value changed.

### Task 2: Prove stale purchase work is isolated from the selected store

**Files:**
- Modify: `src/WildBunch.Web/src/tests/GameSessionProvider.test.tsx`
- Modify only if required by the test: focused test helpers.

Use a real `QueryClient` and the production provider/query hook while mocking only API boundaries. Begin a purchase for game/town A, change the provider's selected session to game/town B before A's purchase resolves, then complete A's successful response. Prove B's already loaded offers are not fetched again by A's completion, and that returning to A observes a refresh for A. Verify this test fails against the current hook, where a delayed `refreshStoreOffers` callback refreshes whichever identity the mounted hook currently represents.

**RED:** run the focused test and confirm the extra B fetch is the observed failure.

### Task 3: Use identity-keyed Query state and exact purchase invalidation

**Files:**
- Create or modify: `src/WildBunch.Web/src/state/townStoreOffersQuery.ts`
- Modify: `src/WildBunch.Web/src/hooks/useTownStoreOffers.ts`
- Modify: `src/WildBunch.Web/src/state/GameSessionProvider.tsx`
- Modify: `src/WildBunch.Web/src/tests/GameSessionProvider.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/StorePlaceFeedback.test.tsx` only if behavior coverage needs its current surface assertion updated.

Create one query key/options owner containing both identity values and a query function enabled only when both IDs exist. Have `useTownStoreOffers` expose query data, loading, and a refresh operation only if a real consumer still needs it. On successful purchase, invalidate the query key using the game/town IDs captured by that purchase; do not invalidate offers on an unsuccessful purchase. Preserve the existing authoritative session update, session reload, and player feedback.

**GREEN:** the Task 2 identity-race behavior passes; run the focused store/provider tests and the web test/typecheck targets.

### Task 4: Validate, review, and publish

Run focused tests for `GameSessionProvider.test.tsx` and `StorePlaceFeedback.test.tsx`, the web TypeScript typecheck/build target, then `py -3 tools/run.py ci --check`. Repair the first failure before running more expensive gates. Review the complete diff against the baseline spec, this plan, relevant ADRs, and the web play-surface profile. Publish the branch, open a Draft PR targeting `develop`, verify its head and hosted exact-head canonical gate, then merge only after that hosted gate passes. Verify the develop-push gate and primary checkout advancement; clean this merged worktree and branch only after merge and tree-equivalence proof.

## Out of Scope

- Generalized query invalidation for every gameplay mutation.
- Provider narrowing, query-resource migration beyond store offers, or a React folder-layout rewrite.
- Changes to store economics, game rules, API contracts, or UI copy.
- Keyboard world-map interaction; ADR-0044 governs that separate input scope.
