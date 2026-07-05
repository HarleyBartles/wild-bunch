# Full Branch Review — BUNCH-124: URL Routing + Vite Bundle Splitting

**PR:** #143
**Base:** ecbb3a60edb3c1f0292185ebd5eb1759b687f7d2
**Head:** e22f0e923495b8835480ace38fe34c11a484c97b
**Review date:** 2026-07-05
**Reviewer:** Senior Code Reviewer (automated)

---

## Spec Compliance Verdict

**Substantially compliant with one Important gap.** The implementation delivers the core BUNCH-124 goals: URL routing via TanStack Router, lazy-loaded route components, Phaser isolation via explicit lazy boundaries, and Vite vendor chunk splitting. The route tree structure, sync hooks, dev surface re-homing, dead code removal, and agent guidance documentation all match the design spec and implementation plan.

The one notable gap: the `?arrived=1` arrival notice param is never set by any code path. The design spec explicitly states `usePhaseRouteSync` should navigate to `/town?arrived=1` when transitioning from trail to town after journey completion, but the implementation navigates to `/town` without the search param. The arrival notice UI exists in `TownHubSurface` but is unreachable in production. See Important Issue #1.

The 3 scope changes are justified:
1. **`chunkSizeWarningLimit` 1100→1500:** Actual Phaser size is ~1.48 MB, not ~1 MB as estimated. 1500 gives ~15 kB headroom. Justified.
2. **`manualChunks` object→function form:** Object-form bare string IDs don't match `react/jsx-runtime` and `react-dom/client` imports from `@vitejs/plugin-react`. Function-form with path matching is the correct approach. Justified.
3. **Agent guidance docs added:** Discovered during implementation, fixes are small and in-scope per repo improvement check. PR body transparently documents this as a scope change. Justified.

---

## Strengths

### Architecture & Design
- **Clean separation of concerns.** Routing (`router.tsx`), phase-URL reconciliation (`usePhaseRouteSync.ts`), dev surface tracking (`useDevSurfaceSync.ts`), and surface components are cleanly separated. Each file has one clear responsibility. (`src/shell/router.tsx`, `src/shell/usePhaseRouteSync.ts`, `src/shell/useDevSurfaceSync.ts`)
- **`createAppRouter()` factory solves test isolation.** The shared `router` singleton retained internal state between tests (TanStack Router doesn't react to `window.history.replaceState`), causing test ordering flakes. The factory pattern creates a fresh router per test. All 5 `RouterProvider` test files were updated. (`src/shell/router.tsx:93-98`, `src/tests/AppShell.test.tsx:79`)
- **Flat town place routes.** Town place routes (`/town/store`, `/town/sheriff`, `/town/saloon`, `/town/trailhead`) are flat siblings under `rootRoute`, not children of `townRoute`. This is correct because `TownHubSurface` renders the hub directly without `<Outlet />`, so child routes wouldn't render. The code includes an inline comment explaining why. (`src/shell/router.tsx:49-75`)
- **`sessionLoading` guard prevents stale-URL redirect.** `usePhaseRouteSync` skips sync while the session query is loading, preventing redirect of a deep-linked `/town/store` URL before we know whether a session exists. The `sessionLoading` field was correctly exposed from `useCurrentGameSession` as `sessionQuery.isLoading` (distinct from the mutation/refresh `loading` field). (`src/shell/usePhaseRouteSync.ts:25-27`, `src/hooks/useCurrentGameSession.ts:291`)
- **Phaser isolation via explicit lazy boundary.** `StartingTownStep` is lazy-loaded inside `PreSessionSurface` (only when `effectiveStep === "town"`), ensuring Phaser is not downloaded during name/prologue steps. `TravelPrepSurface` is lazy-loaded as a route component, isolating Phaser to the trailhead chunk. (`src/flow/PreSessionSurface.tsx:12-14,107-115`)
- **`validateSearch` returns `{}` not `{ arrived: undefined }`.** The implementation correctly returns `{}` when the `arrived` param is absent, avoiding the TanStack Router typing trap that would make the param required. This is enforced by `routingConventions.test.ts`. (`src/shell/router.tsx:40-45`, `src/tests/routingConventions.test.ts:60-68`)

### Testing
- **`routingConventions.test.ts` is effective enforcement.** It reads the router source and asserts: all route components are lazy-loaded, town place routes are flat siblings, `validateSearch` returns `{}`, and `createAppRouter` factory exists. This will catch violations when routes are added or changed. (`src/tests/routingConventions.test.ts`)
- **`usePhaseRouteSync` tests cover key edge cases.** Redirect on mismatch, no-op on match, stale deep-link with no session, and session-loading guard. Tests use fresh routers and real `GameSessionProvider` with mocked API. (`src/tests/usePhaseRouteSync.test.tsx`)
- **`TrailFlowSurfaceCompleted.test.tsx` tests real behavior.** Verifies arrival heading renders, "Step into town" button renders, and `acknowledgeTravelArrival` is called on click. Uses `userEvent` for realistic interaction. (`src/tests/TrailFlowSurfaceCompleted.test.tsx`)
- **224/224 tests pass.** Full suite passes with no skips. CI is green (Backend, Frontend, Index mesh all pass). `npx tsc --noEmit` is clean.

### Agent Guidance Documentation
- **Comprehensive and well-structured.** Code review guide, implementing guide, planning guide, frontend standards, test quality standards, write-tool doctrine, mesh policy update — all are accurate, useful, and follow the AGENTS.md-as-routing-file pattern. The lane guides are discoverable from the root `AGENTS.md` tree.
- **Mesh policy correctly encodes AGENTS.md-as-routing-file rule.** Section 1 now explicitly states AGENTS.md files must be routing files with "must read when" pointers, not doctrine containers. (`.agents/docs/mesh-policy.md:9-15`)
- **`src/WildBunch.Web/AGENTS.md` correctly converted to routing file.** It now has a "Must Read When" section with pointers to standards docs, instead of inline doctrine. (`src/WildBunch.Web/AGENTS.md`)
- **INDEX.md files regenerated.** Index mesh check passes: "OK index mesh: 104 indexes current."

---

## Issues

### Critical (must fix)
None.

### Important (should fix)

**I1. Arrival notice `?arrived=1` param is never set — feature is dead code.**

The design spec explicitly states:
> "On acknowledge (`handleAcknowledgeArrival`) -> session updates -> journey clears -> `useGamePhase` derives `in-town` -> sync navigates `/trail` -> `/town?arrived=1`."
> "The `?arrived=1` param is set by `usePhaseRouteSync` when navigating from `/trail` to `/town` after the journey completes."

But `usePhaseRouteSync` navigates with `void navigate({ to: expectedPrefix })` (line 39), which navigates to `/town` without any search params. No code in the codebase ever navigates to `/town?arrived=1`. The arrival notice UI in `TownHubSurface` (lines 137-148), the `validateSearch` parsing (router.tsx:40-45), and the dismiss logic (TownHubSurface.tsx:143) are all unreachable in production.

A grep for `arrived.*1|search.*arrived` across the entire `src/` tree confirms: `validateSearch` parses it, `TownHubSurface` reads it, but nobody sets it.

**Fix:** `usePhaseRouteSync` should detect when the phase transitioned from `on-trail` to `in-town` and set `?arrived=1` in the navigation. Alternatively, `TrailFlowSurface`'s acknowledge handler could navigate to `/town?arrived=1` directly after calling `handleAcknowledgeArrival`. The former is more aligned with the design spec's "sync hook handles the route change" approach.

**Files:** `src/shell/usePhaseRouteSync.ts:39`, `src/flow/TownHubSurface.tsx:137-148`, `src/shell/router.tsx:40-45`

**I2. Arrival→town regression coverage incomplete.**

The design spec says the `TrailFlowSurfaceCompleted.test.tsx` should:
> "verify arrival content shows when `journey.status === Completed`, and that after clicking 'Step into town' (calling `handleAcknowledgeArrival`), the town hub renders. This **replaces the regression coverage** from the deleted `GameFlowRouter.test.tsx` 'shows town hub after acknowledging arrival' test."

The actual test verifies:
1. Arrival heading renders when journey is Completed ✓
2. "Step into town" button renders when journey is Completed ✓
3. `acknowledgeTravelArrival` is called when button is clicked ✓

But it does NOT verify that the town hub renders after clicking "Step into town". The original `GameFlowRouter.test.tsx` test (lines 230-273) verified the end-to-end arrival→town transition. The replacement only tests half of what the original tested. The end-to-end transition (acknowledge → session updates → phase changes → sync navigates → town hub renders) is untested.

This is related to I1 — since `?arrived=1` is never set, an end-to-end test would also expose that the arrival notice doesn't appear.

**Files:** `src/tests/TrailFlowSurfaceCompleted.test.tsx:152-176`

### Minor (nice to have)

**M1. `useDevSurfaceSync` test missing on-trail case.**

The design spec says to cover "on-trail + `/trail` → `"trail"` (including `Completed` journey)" but the test only covers `in-town + /town → "town"`, `in-town + /town/store → "store"`, and `pre-session → "pre-session"`. The on-trail mapping is untested. This is a simple string mapping unlikely to break, but the design spec explicitly called it out.

**Files:** `src/tests/useDevSurfaceSync.test.tsx`

**M2. `className="trailhead"` plain CSS class (fix-while-here).**

The frontend standards say "Do NOT use plain CSS classes in `className`. All component styling must be handled via styled components." `TownHubSurface.tsx:178` uses `className="trailhead"` with a `&.trailhead` selector in the `PlaceCard` styled component. This is pre-existing (not introduced by this PR), but the PR modifies this exact line (changed the `onClick` handler). A fix-while-here opportunity: replace `className="trailhead"` with a `$isTrailhead` transient prop and use `&[data-trailhead]` or `${props => props.$isTrailhead && ...}` in the styled component.

**Files:** `src/flow/TownHubSurface.tsx:178`

**M3. `useSearch` type assertion bypasses type safety.**

`TownHubSurface.tsx:116` uses `useSearch({ strict: false }) as { arrived?: string }`. The `validateSearch` returns `{ arrived?: "1" }` but the cast widens to `{ arrived?: string }`. With `strict: false`, TanStack Router returns a generic type, so the cast is needed — but it should be narrower (`{ arrived?: "1" }`) to match the `validateSearch` return type.

**Files:** `src/flow/TownHubSurface.tsx:116`

**M4. `phase: string` instead of `GamePhase` in helper functions.**

Both `phaseToUrlPrefix` (`usePhaseRouteSync.ts:43`) and `deriveDevSurface` (`useDevSurfaceSync.ts:22`) accept `phase: string` instead of the `GamePhase` type. The switch/if statements handle all `GamePhase` values but the parameter type doesn't enforce it, losing compile-time safety against typos or unhandled phases.

**Files:** `src/shell/usePhaseRouteSync.ts:43`, `src/shell/useDevSurfaceSync.ts:22`

**M5. `hasSession` unused in effect body.**

`usePhaseRouteSync.ts:16` destructures `hasSession` from `useGamePhase()` and includes it in the dependency array (line 40), but never references it in the effect body. It's redundant with `phase` since `hasSession` is derived from `phase` (if phase is `"pre-session"`, `hasSession` is false; otherwise true). Removing it from both the destructure and the deps array would be cleaner.

**Files:** `src/shell/usePhaseRouteSync.ts:16,40`

---

## Per-Lens Findings

### Principal Architect

This PR is frontend-only — no domain, persistence, or command/query handler changes. Architecture review focuses on frontend architecture: routing, component boundaries, and state management.

**Findings:**
- **Routing architecture is sound.** The route tree correctly reflects game state via URL paths. The phase-sync pattern (backend-derived phase drives top-level route, player navigates freely within a phase) is a clean separation of backend authority and player agency.
- **Sync hooks are well-placed.** `usePhaseRouteSync` and `useDevSurfaceSync` run in `ShellChrome` (the root route component), which is the correct location — they need to be inside the router context and above the route outlet.
- **`createAppRouter` factory is the right pattern.** It solves the shared-singleton test isolation problem without adding complexity to production code.
- **Dev surface re-homing is correct.** `useDevSurfaceSync` correctly replaces the `GameFlowRouter`'s `useSetDevSurface()` mapping. The `"arrival"` removal from `DevSurface` is consistent with the arrival flow rework.
- **No ADR update needed.** This PR doesn't change an architectural decision — it implements one (URL routing) that was already planned. No ADR freshness issue.

### Senior QA Engineer

**Findings:**
- **Test quality is high.** Tests verify real behavior (rendered output, API calls, URL changes) rather than mock interactions. `usePhaseRouteSync` tests assert on `window.location.pathname`, not on mock navigate calls.
- **Edge cases covered for sync hook.** Stale URL redirect, no-session redirect, session-loading guard, and phase-match no-op are all tested.
- **Gap: arrival→town transition untested (I2).** The regression test from `GameFlowRouter.test.tsx` was supposed to be preserved, but the replacement only tests the button click, not the end-to-end transition.
- **Gap: `useDevSurfaceSync` on-trail case untested (M1).** The design spec explicitly called out this case.
- **No flaky tests.** The `createAppRouter` factory and extended timeouts on `findBy*` queries address the common flake causes. Full suite passes consistently.
- **Test output has pre-existing noise.** "Query data cannot be undefined" warnings from `SessionDevPanel.test.tsx` and "Not implemented: Window's scrollTo()" are pre-existing, not introduced by this PR.

### Senior Software Engineer

**Findings:**
- **Naming is accurate.** `usePhaseRouteSync`, `useDevSurfaceSync`, `RouteLoading`, `createAppRouter` all describe what they do.
- **DRY without premature abstraction.** The two sync hooks share the same inputs (phase + location) but have different responsibilities. They're separate hooks, not a combined abstraction — correct for now.
- **Error handling at the right boundary.** Surfaces return `null` when no session (`TownHubSurface:118`, `TrailFlowSurface:42`, `StorePlace:27`). The sync hook handles stale URLs. No defensive error handling in the wrong places.
- **`phaseToUrlPrefix` and `deriveDevSurface` could share a phase-to-surface mapping.** Both functions map `GamePhase` to something. A shared mapping table would be DRY-er, but the current duplication is small (3 cases each) and the mappings are different (URL prefix vs DevSurface). Not worth abstracting yet.
- **Minor code quality items (M3-M5)** are noted above.

### Product Owner (invoked because this touches game flow)

Invoking product owner lens because this PR changes the arrival flow and navigation model.

**Findings:**
- **URL routing delivers real player value.** Deep-linking, back-button support, and URL reflection of game state are meaningful improvements. A player can bookmark `/town/store` or use the back button to return from a place to the town hub.
- **Arrival flow rework is the right design.** Moving arrival content into `TrailFlowSurface` (showing the last day's resolution + acknowledge button) is better than a separate `ArrivalSurface` that skipped the last day's content.
- **Gap: arrival notice never shows (I1).** The `?arrived=1` arrival notice in `TownHubSurface` is a player-facing feature that doesn't work. A player who acknowledges arrival will transition to town without seeing "You've arrived in [town]. Take a moment to look around." This is a product gap, not just a code gap.
- **Scope change (agent docs) is transparent.** The PR body clearly documents the documentation scope change and offers to split it into a separate PR. Good product ownership.

### Player (invoked because this touches player-facing UI and game flow)

Invoking player lens because this PR changes the arrival flow and navigation model.

**Findings:**
- **Back button works.** Place surfaces use `useNavigate({ to: "/town" })` for back buttons. The browser back button also works because these are real URL routes.
- **Place cards feel game-native.** "Store", "Sheriff Office", "Saloon", "Hit the trail" with icons — no dashboard drift or product chrome.
- **Arrival card is in-world.** "You've arrived in Dust Fork. You put 3 days of trail behind you." with a "Step into town" button — reads like a game moment, not a product notification.
- **Trail lock banner is clear.** "You're on the trail. No turning back until you reach your destination." — orients the player without being verbose.
- **Loading state is minimal.** `RouteLoading` is a muted centered div — same visual register as existing `Muted` loading states. Doesn't break immersion.
- **Gap: arrival notice missing (I1).** The player won't see the "You've arrived in [town]" notice because `?arrived=1` is never set. The transition from trail to town will feel abrupt without the acknowledgment notice.

---

## Repo Improvement Check

### 1. Fix-while-here opportunities
- **`className="trailhead"` (M2):** The PR modifies this exact line in `TownHubSurface.tsx` (changed the `onClick` handler). Replacing `className="trailhead"` with a `$isTrailhead` transient prop would be a small change within the file. This is a fix-while-here opportunity, but it's Minor because the pattern is pre-existing and the PR didn't introduce it.

### 2. Cheap fix deferrals
- **`?arrived=1` gap (I1):** This is not a deferred fix — it's a missing implementation. The design spec specified it, the UI code exists for it, but the trigger was never wired. The fix is small (add search param to the navigate call in `usePhaseRouteSync` when transitioning from trail to town, or navigate directly from `TrailFlowSurface`'s acknowledge handler).
- **No other cheap fixes were silently deferred.** The PR body transparently documents the scope changes and the documentation work.

### 3. Perpetuating legacy patterns
- **`className="trailhead"` (M2):** The PR perpetuates a plain CSS class pattern that the frontend standards say to avoid. However, this is pre-existing and the PR only changed the `onClick` handler on the same line, not the `className` itself. New code in the PR uses styled-components correctly throughout.
- **No other legacy patterns perpetuated.** New code follows the repo's better patterns (styled-components, `useNavigate`, `createAppRouter` factory, `validateSearch`).

---

## DoD Compliance Checklist

| Item | Status | Notes |
|------|--------|-------|
| All tests pass | ✅ | 224/224 passing (`npx vitest run`) |
| CI passes | ✅ | Backend, Frontend, Index mesh all green |
| Build succeeds | ✅ | `npm run build` succeeds (per PR body) |
| `npx tsc --noEmit` clean | ✅ | Verified — no errors |
| `dotnet ef migrations list` | N/A | Frontend-only PR |
| New code covered by tests | ⚠️ | Mostly covered; arrival→town transition untested (I2) |
| No flaky tests | ✅ | `createAppRouter` factory + extended timeouts address flake causes |
| INDEX.md regenerated | ✅ | "OK index mesh: 104 indexes current" |
| ADR log fresh | ✅ | No architectural decision changed |
| Linear issue updated | ✅ | PR references BUNCH-124; scope changes documented in PR body |
| PR description accurate | ✅ | Transparent about scope changes, bundle sizes, verification |
| No secrets committed | ✅ | No credentials in diff |
| No junk/phantom files | ✅ | Workspace clean, no phantom files in parent directories |
| Matches Linear issue goal | ✅ | URL routing + bundle splitting delivered as requested |

---

## Assessment and Verdict

**Needs fixes (one Important issue).**

The PR is high-quality work with clean architecture, effective testing, and comprehensive agent guidance documentation. The core BUNCH-124 goals are fully delivered: URL routing, lazy loading, Phaser isolation, and vendor chunk splitting all work correctly. CI is green, all 224 tests pass, tsc is clean, and the workspace is pristine.

However, the `?arrived=1` arrival notice feature is incomplete (I1). The design spec explicitly specified that `usePhaseRouteSync` should set `?arrived=1` when navigating from trail to town, but the implementation navigates to `/town` without the param. The arrival notice UI, `validateSearch` parsing, and dismiss logic are all dead code. This is a player-facing feature gap that should be fixed before merge. The fix is small — add search param to the navigate call when transitioning from `on-trail` to `in-town`.

The incomplete regression coverage (I2) is related — an end-to-end test would expose the `?arrived=1` gap. This should also be addressed.

The Minor issues (M1-M5) are polish items that can be addressed in follow-up or in the same PR if convenient.

**Recommendation:** Fix I1 (wire `?arrived=1` in `usePhaseRouteSync` or `TrailFlowSurface`) and I2 (add end-to-end arrival→town test) before merge. The Minor issues can be addressed at the author's discretion.
