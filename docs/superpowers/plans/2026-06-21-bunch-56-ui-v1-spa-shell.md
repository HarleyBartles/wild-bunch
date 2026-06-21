# BUNCH-56 UI v1 SPA Shell, HUD, Routes, and Player/Debug Separation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the Wild Bunch web client from a single cockpit page into a v1 SPA shell with a persistent compact HUD, player-facing navigation, and routed play surfaces, while preserving the dense developer cockpit as a clearly separated route.

**Architecture:** The frontend stays an adapter over server-authoritative state. One `GameSessionProvider` context wraps the existing `useCurrentGameSession` hook (plus store-offers/buy wiring) and feeds the HUD and every route. Routing is a dependency-free hash router (`useHashRoute`) with routes declared as data, per ADR-0019's "stay manual until justified" posture. The case file is promoted from a cockpit-hosted modal to a canonical player route (ADR-0011 review trigger). The cockpit is preserved verbatim as a `Dev tools` route so BUNCH-20 journal/modal context is not bulldozed. DOM owns text-heavy HUD/menus/surfaces; there is no canvas/Phaser playfield yet, so the routed content area slots under the HUD without changing the state boundary.

**Tech Stack:** React 18, TypeScript, Vite, TanStack React Query, styled-components, Vitest, Testing Library (per ADR-0016/ADR-0017). No new runtime dependency is added.

**Issue:** BUNCH-56 (UI v1 campaign). This is the first bounded playable slice: the shell/route/surface shape plus one promoted production-shaped player surface (Case file route).

**Source of truth:** Live `HarleyBartles/wild-bunch` `main` at SHA `5f1b5649e2f381cfb6e801089715e9482e0c68e6`. PR #91 (prior attempt) was closed without merge and without stated reason; this plan re-establishes the same shape cleanly on a fresh branch.

---

## File Structure

### Create
- `src/WildBunch.Web/src/state/GameSessionProvider.tsx` — React context wrapping `useCurrentGameSession` + `useTownStoreOffers` + `handleBuyOffer`; single authoritative session source for the shell.
- `src/WildBunch.Web/src/state/useGameSession.ts` — typed `useContext` accessor hook for the provider.
- `src/WildBunch.Web/src/shell/useHashRoute.ts` — dependency-free hash router hook (reads `window.location.hash`, listens to `hashchange`, normalizes to a route key).
- `src/WildBunch.Web/src/shell/AppShell.tsx` — persistent compact HUD + player nav + `<RouteOutlet>`; consumes `GameSessionProvider` and `useHashRoute`.
- `src/WildBunch.Web/src/shell/Hud.tsx` — sticky compact status bar (player, day/turn, town, health, cash, heat/status) sourced from provider.
- `src/WildBunch.Web/src/shell/AppShell.test.tsx` — shell/routing tests (HUD renders, default route is Camp, nav reaches Case/Wanted/Dev tools).
- `src/WildBunch.Web/src/components/AvailableActionsPanel.tsx` — extracted from App's inline actions block; reused by Hunt route and cockpit.
- `src/WildBunch.Web/src/routes/CampRoute.tsx` — `/` start/continue hunt (StartGamePanel + notice/error).
- `src/WildBunch.Web/src/routes/HuntRoute.tsx` — `/hunt` FieldReportPanel + AvailableActionsPanel.
- `src/WildBunch.Web/src/routes/CaseFileRoute.tsx` — `/case` CaseFileSurface, promoted from modal (read-only).
- `src/WildBunch.Web/src/routes/WantedRoute.tsx` — `/wanted` WantedPosterSurface.
- `src/WildBunch.Web/src/routes/TrailRoute.tsx` — `/trail` TravelRoutesPanel (+ TravelPanel when a journey is active).
- `src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx` — `/debug` the former `App.tsx` body, sourcing state from context; case-file modal preserved (BUNCH-20 context).
- `docs/adr/ADR-0027-ui-v1-spa-shell-routing-and-player-debug-separation.md` — documents the v1 shell/route/surface shape, hash-router choice, and player/debug separation.

### Modify
- `src/WildBunch.Web/src/App.tsx` — becomes `GameSessionProvider` + `AppShell` only.
- `src/WildBunch.Web/src/App.test.tsx` — retarget the 27 cockpit behaviours to render `DebugCockpitRoute` inside `GameSessionProvider`; assertions unchanged.
- `src/WildBunch.Web/src/styles.css` — add shell/HUD/nav/route CSS using existing CSS variables; keep existing cockpit styles intact for the Dev tools route.
- `docs/adr/README.md` — add ADR-0027 to the index.

### Non-goals for this slice
- No canvas/Phaser playfield (slots in later under the routed content area).
- No inventory/wallet/travel dedicated routes beyond reusing existing panels.
- No reduced-motion/keyboard route polish (follow-up).
- No backend/API changes; no `dotnet` validation required.

---

### Task 1: Establish the baseline

**Files:**
- Read: `src/WildBunch.Web/src/App.tsx`, `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`, `src/WildBunch.Web/src/App.test.tsx`

- [ ] **Step 1: Install web deps and confirm baseline**

Run (PowerShell, from `src/WildBunch.Web`):
`npm install`
then
`npm test`
Expected: existing suite passes (baseline). Record the passing count.

- [ ] **Step 2: Confirm typecheck and build baseline**

Run:
`npm run typecheck`
then
`npm run build`
Expected: both clean.

### Task 2: GameSessionProvider context

**Files:**
- Create: `src/WildBunch.Web/src/state/GameSessionProvider.tsx`
- Create: `src/WildBunch.Web/src/state/useGameSession.ts`

- [ ] **Step 1: Write the provider**

`GameSessionProvider.tsx` creates a React context that calls `useCurrentGameSession()`, calls `useTownStoreOffers(gameId, currentTown?.id)`, and defines `handleBuyOffer` (moved verbatim from `App.tsx` lines 64-94). It exposes the full `useCurrentGameSession` return plus `storeOffers`, `storeOffersLoading`, `refreshStoreOffers`, `handleBuyOffer`, and `selectedWantedPoster` (computed as in App lines 61-62). The provider value is memoized with `useMemo` to avoid unnecessary rerenders.

- [ ] **Step 2: Write the accessor hook**

`useGameSession.ts` exports `useGameSession()` that calls `useContext(GameSessionContext)` and throws a descriptive error if used outside the provider.

- [ ] **Step 3: Typecheck**

Run: `npm run typecheck`
Expected: clean (provider not yet consumed).

### Task 3: useHashRoute hook

**Files:**
- Create: `src/WildBunch.Web/src/shell/useHashRoute.ts`

- [ ] **Step 1: Write the hook**

`useHashRoute` reads `window.location.hash`, strips the leading `#`, defaults to `"/"` when empty, listens to `hashchange`, and returns the current route string plus a `navigate(path: string)` helper that sets `window.location.hash`. It uses `useState` + `useEffect` with cleanup. No dependency.

- [ ] **Step 2: Typecheck**

Run: `npm run typecheck`
Expected: clean.

### Task 4: Extract AvailableActionsPanel

**Files:**
- Create: `src/WildBunch.Web/src/components/AvailableActionsPanel.tsx`

- [ ] **Step 1: Extract the inline actions block**

Move the entire "Available actions" `<section>` (App.tsx lines 178-290), including the saloon person-of-interest declaration row, into `AvailableActionsPanel`. It consumes `useGameSession()` for `actions`, `session`, `wantedPosters`, `declaredWantedIdentityHandle`, `setDeclaredWantedIdentityHandle`, `loading`, `busyMode`, `gameId`, and the `can*`/`handle*` flags. Props: none (it reads from context). Keep markup and class names identical so the Dev tools route and Hunt route share one implementation.

- [ ] **Step 2: Typecheck**

Run: `npm run typecheck`
Expected: clean.

### Task 5: Route components

**Files:**
- Create: `src/WildBunch.Web/src/routes/CampRoute.tsx`
- Create: `src/WildBunch.Web/src/routes/HuntRoute.tsx`
- Create: `src/WildBunch.Web/src/routes/CaseFileRoute.tsx`
- Create: `src/WildBunch.Web/src/routes/WantedRoute.tsx`
- Create: `src/WildBunch.Web/src/routes/TrailRoute.tsx`
- Create: `src/WildBunch.Web/src/routes/DebugCockpitRoute.tsx`

- [ ] **Step 1: CampRoute**

Renders the "Start a new hunt" panel section: `StartGamePanel` (from context: `session`, `busy=loading`, `gameId`, `resetToken`, `onStartGame=startNewGame`, `onRefresh=reloadCurrentGame`), plus `notice`/`error` banners. No session-required guard — Camp is the entry surface.

- [ ] **Step 2: HuntRoute**

Renders `FieldReportPanel` (when `session` exists) + `AvailableActionsPanel`. Sources from context. If no session, render a muted "Start a hunt from Camp" prompt.

- [ ] **Step 3: CaseFileRoute**

Renders `CaseFileSurface` (read-only) with `journal`, `loading`, `error` from context. This is the promotion from cockpit modal to canonical route (ADR-0011 trigger). No modal wrapper.

- [ ] **Step 4: WantedRoute**

Renders `WantedPosterSurface`. Sources `wantedPosters`, `declaredWantedIdentityHandle`, `setDeclaredWantedIdentityHandle`, `loading` from context.

- [ ] **Step 5: TrailRoute**

Renders `TravelRoutesPanel` (always) and `TravelPanel` when `session?.journey` is active. Sources from context: `gameId`, `session`, `busy=loading`, `onTravel=handleTravel`.

- [ ] **Step 6: DebugCockpitRoute**

Relocate the entire former `App.tsx` body (hero, layout, StartGamePanel, FieldReportPanel, AvailableActions inline block, TravelRoutesPanel, LogPanel, case-file modal) into `DebugCockpitRoute`, sourcing state from `useGameSession()` instead of the raw hook. The case-file modal stays intact here (BUNCH-20 context preserved). The `handleBuyOffer` and store-offers wiring come from context. This is the verbatim cockpit, now behind a separated route.

- [ ] **Step 7: Typecheck**

Run: `npm run typecheck`
Expected: clean.

### Task 6: Hud + AppShell

**Files:**
- Create: `src/WildBunch.Web/src/shell/Hud.tsx`
- Create: `src/WildBunch.Web/src/shell/AppShell.tsx`

- [ ] **Step 1: Hud**

Sticky compact status bar. Reads `session`, `currentTown`, `cockpitMode` from context. Shows: player name, `Day N, Turn N`, current town name, health, cash (from `session.player`/wallet if present on DTO — use existing field paths only; if wallet isn't on the session DTO, show status label instead), and a status label via `formatGameStatus`. Compact: one row, wraps on narrow viewports. Uses existing CSS variables. Does not cover the lower-middle playfield.

- [ ] **Step 2: AppShell**

Renders `<Hud />`, then `<nav className="shell-nav">` with player routes (Camp `/`, Hunt `/hunt`, Case file `/case`, Wanted `/wanted`, Trail `/trail`) and a separated `Dev tools` `/debug` link. Then a `<RouteOutlet>` that switches on the hash route to render the matching route component, defaulting to `CampRoute`. Routes are declared as a data array (`{ key, label, path, element }`) with `Dev tools` flagged `dev: true` for separation styling. Uses `useHashRoute`.

- [ ] **Step 3: Typecheck**

Run: `npm run typecheck`
Expected: clean.

### Task 7: Rewrite App.tsx

**Files:**
- Modify: `src/WildBunch.Web/src/App.tsx`

- [ ] **Step 1: Replace App body**

`App.tsx` becomes:
```tsx
import { GameSessionProvider } from "./state/GameSessionProvider";
import { AppShell } from "./shell/AppShell";

export default function App() {
  return (
    <GameSessionProvider>
      <AppShell />
    </GameSessionProvider>
  );
}
```

- [ ] **Step 2: Typecheck**

Run: `npm run typecheck`
Expected: clean.

### Task 8: Styles

**Files:**
- Modify: `src/WildBunch.Web/src/styles.css`

- [ ] **Step 1: Add shell/HUD/nav/route CSS**

Append CSS for `.v1-shell`, `.hud`, `.hud-metric`, `.shell-nav`, `.shell-nav__link`, `.shell-nav__link--dev`, `.route-outlet`, and a `.route` wrapper. Use existing `:root` CSS variables. HUD is `position: sticky; top: 0;` with a compact single row and `flex-wrap` for narrow viewports. Nav is a horizontal row with `Dev tools` visually separated (muted/dashed border). Keep all existing cockpit styles so `DebugCockpitRoute` renders identically.

- [ ] **Step 2: Build**

Run: `npm run build`
Expected: succeeds.

### Task 9: Tests — retarget cockpit + add shell tests

**Files:**
- Modify: `src/WildBunch.Web/src/App.test.tsx`
- Create: `src/WildBunch.Web/src/shell/AppShell.test.tsx`

- [ ] **Step 1: Retarget App.test.tsx**

The 27 cockpit behaviours now render `<DebugCockpitRoute />` inside `<GameSessionProvider>` instead of `<App />`. Mocks stay the same (the provider calls the same `useCurrentGameSession` which calls the same api module). Assertions unchanged — this proves the cockpit behaves identically after relocation. Replace `import App from "./App"` with `import { DebugCockpitRoute } from "./routes/DebugCockpitRoute"` and `import { GameSessionProvider } from "./state/GameSessionProvider"`, and wrap renders in `<GameSessionProvider>`. The `buyStoreItem`/`getTownStoreOffers` mocks already exist.

- [ ] **Step 2: Write AppShell.test.tsx**

Test: (a) HUD renders player name and clock when a session is loaded; (b) default route is Camp (StartGamePanel visible); (c) clicking "Case file" nav sets hash to `#/case` and renders CaseFileSurface; (d) clicking "Wanted" renders WantedPosterSurface; (e) "Dev tools" nav is present and separated. Mock the api module so `useCurrentGameSession` hydrates a session.

- [ ] **Step 3: Run tests**

Run: `npm test`
Expected: all pass (27 retargeted + new shell tests).

### Task 10: ADR-0027 + index

**Files:**
- Create: `docs/adr/ADR-0027-ui-v1-spa-shell-routing-and-player-debug-separation.md`
- Modify: `docs/adr/README.md`

- [ ] **Step 1: Write ADR-0027**

Follow `docs/adr/TEMPLATE.md`. Status `live`. Decision types `ui, architecture`. Related ADRs: `depends on` ADR-0016, ADR-0019; `related to` ADR-0011, ADR-0022. Document: the v1 shell/route/surface shape; the dependency-free hash router choice (ADR-0019 "stay manual until justified"); the single `GameSessionProvider` over `useCurrentGameSession` keeping the frontend an adapter; the Case file route promotion (ADR-0011 review trigger fired); the player/debug separation via the `Dev tools` route preserving the cockpit and BUNCH-20 modal; the compact HUD protecting the playfield; the DOM-owns-text-heavy boundary; the seam for a future canvas/Phaser playfield slotting into the routed content area.

- [ ] **Step 2: Add to README index**

Add `- [ADR-0027 UI v1 SPA shell, routing, and player/debug separation](ADR-0027-ui-v1-spa-shell-routing-and-player-debug-separation.md)` to the list in `docs/adr/README.md`.

### Task 11: Full validation

- [ ] **Step 1: typecheck**

Run: `npm run typecheck`
Expected: clean.

- [ ] **Step 2: test**

Run: `npm test`
Expected: all pass.

- [ ] **Step 3: build**

Run: `npm run build`
Expected: succeeds.

- [ ] **Step 4: git diff --check**

Run: `git diff --check`
Expected: no whitespace errors.

### Task 12: Browser evidence

- [ ] **Step 1: Start vite preview**

Run (background): `npm run preview` (serves the production build on port 4173). Record the port.

- [ ] **Step 2: Capture screenshots via Playwright**

Navigate to `http://localhost:4173/` and capture: Camp (default), Hunt, Case file, Wanted, Trail, Dev tools, plus a narrow viewport (e.g. 375px) of the shell. Save screenshots under a temp dir.

- [ ] **Step 3: Stop preview**

Kill the preview process. Record PID/port cleanup.

### Task 13: Commit, push, open draft PR

- [ ] **Step 1: Commit**

Stage all changed/new files and commit with a focused message referencing BUNCH-56.

- [ ] **Step 2: Push branch**

`git push -u origin bunch-56/ui-v1-shell`

- [ ] **Step 3: Open draft PR**

`gh pr create --draft --title "BUNCH-56: UI v1 SPA shell, HUD, routes, and player/debug separation" --body "..."` with summary, shape, validation, known gaps/follow-ups.

---

## Self-Review

**Spec coverage:**
- Documented UI v1 shell/route/surface shape → ADR-0027 (Task 10) + AppShell (Task 6).
- At least one production-shaped player-facing surface → Case file route promoted from modal (Task 5 Step 3, ADR-0011 trigger).
- Clear debug/player separation → `Dev tools` route (Task 5 Step 6, Task 6 nav).
- Compact HUD conventions → Hud (Task 6 Step 1).
- Browser evidence → Task 12.
- Validation evidence → Task 11.
- Follow-up issues for unfinished UI work → noted in PR body (inventory/wallet, travel, Phaser playfield, reduced-motion).
- Frontend as adapter (no gameplay truth) → GameSessionProvider wraps existing hook, no logic moved (Task 2).
- Hidden culprit truth untouched → routes render existing read-only surfaces only.

**Placeholder scan:** None — every code step names exact files and content.

**Type consistency:** `useGameSession()` returns the provider value shape used consistently across Hud, AppShell, routes, and AvailableActionsPanel.
