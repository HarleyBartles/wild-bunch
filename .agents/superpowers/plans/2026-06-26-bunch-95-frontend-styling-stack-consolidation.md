# BUNCH-95: Consolidate Frontend Styling Stack Around styled-components and SASS

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mop up the remaining plain CSS debt in `src/WildBunch.Web` so the canonical frontend styling stack is `styled-components` for component-owned styling and SASS/SCSS for global/non-component concerns, with durable repo guidance and lightweight enforcement.

**Architecture:** A single plain CSS file (`src/styles.css`, ~1025 lines) is imported through `src/styles/index.scss` and feeds ~31 component files via global class names. The SASS scaffolding (`_variables.scss`, `_reset.scss`, `index.scss`) and `styled-components` are already installed and used by 15 component files (shell, HUD, dev overlay, start-game flow, travel diary). This plan extracts shared styled primitives, migrates each remaining component from plain CSS classes to `styled-components`, moves the few truly-global element defaults into a new SASS `_base.scss` partial, deletes `styles.css`, and installs durable guidance + enforcement.

**Tech Stack:** React 18, TypeScript, Vite 6, Vitest 4, styled-components 6, sass 1.101 (all already in `package.json`).

## Global Constraints

- Canonical rule: `styled-components` for component-owned/component-local styling; SASS/SCSS for globals, resets, tokens, base layout, and unavoidable global selectors. Plain CSS is not the default going forward.
- No gameplay behavior changes. No feature redesign. Visual output must be preserved (same tokens, same spacing, same responsive breakpoints).
- Backend/domain/application code is untouched.
- Global design tokens (`--bg`, `--text`, `--accent`, `--radius`, etc.) stay in `src/styles/_variables.scss` as the single source of truth; styled-components reference them via `var(--token)`.
- Responsive breakpoints (`1366px`, `960px`, `640px`) must be preserved inside the styled-components that own each surface, not in a global media-query block.
- Tests use role/text queries, not class names — class-name migration must not break existing tests.
- All commands run from `src/WildBunch.Web` unless noted. The worker environment is PowerShell; do not use `&&` for chaining.
- Repo-local superpowers plan records live under `.agents/superpowers/plans/` (not `docs/superpowers/`).
- BUNCH-88 (dev overlay foundation) is already landed (PR #104, commit `5852664`); no rebase risk. Shell/HUD/dev overlay styling is already styled-components and needs no migration.

---

## Source Seams Inspected (Preflight Answers)

1. **Styling systems installed:** `styled-components@^6.4.2` (dependency), `sass@^1.101.0` (devDependency). Vite config (`vite.config.ts`) has no explicit sass config — Vite handles `.scss` imports natively. No CSS-in-JS beyond styled-components.
2. **Files importing styles:** Only `src/main.tsx` imports `./styles/index.scss`. `index.scss` does `@use "variables"; @use "reset"; @use "../styles.css";`. No other `.css` or `.scss` imports exist in `src/`.
3. **Component-owned styles (should be styled-components):** All 31 files listed in the inventory below that currently use plain CSS classes from `styles.css`.
4. **Global/non-component styles (should stay SASS):** `:root` tokens (`_variables.scss`), box reset + base element defaults (`_reset.scss`), plus a few base element rules currently in `styles.css` (`h1`/`h2`/`h3` font defaults, `body`-level rules) that belong in a `_base.scss` partial.
5. **Plain CSS that should remain after this issue:** None. `styles.css` is deleted. The only retained global CSS is the SASS partials (`_variables.scss`, `_reset.scss`, new `_base.scss`).
6. **Existing repo guidance:** `src/WildBunch.Web/AGENTS.md` covers play-surface doctrine and dev overlay work but says nothing about the styling stack. No `docs/frontend-styling.md` exists. No ADR covers frontend styling.
7. **Where durable guidance lives:** Both `src/WildBunch.Web/AGENTS.md` (agent law) and a new `docs/frontend-styling.md` (human-facing doc, indexed from `docs/INDEX.md`). The web AGENTS.md gets a short "Frontend styling stack" section pointing to the doc.
8. **BUNCH-88 shell/HUD/dev overlay handling:** Already styled-components on current `main`. No work needed; this plan does not touch them.
9. **Feature CSS surfaces:** Migrated as-is into styled-components with identical declarations (same values, same breakpoints). No redesign.
10. **Enforcement:** A Vitest test (`src/tests/styling-stack.test.ts`) that asserts no `.tsx` file under `src/` (excluding `src/styles/` and `src/tests/`) imports a `.css` file, and that `src/styles.css` does not exist.
11. **Validation/screenshot coverage:** `npm run typecheck`, `npm test`, `npm run build`, plus browser screenshots of pre-session surface, town hub, a place surface, trail surface, case file modal, wanted posters, journal modal, and dev overlay closed/open.
12. **Split condition:** One PR is sufficient. Shared primitives are extracted first; each subsequent component migration is independent and mechanical. The PR would only need splitting if a shared primitive's API turned out to be unstable across surfaces — unlikely given the existing `travelShared.tsx` precedent.

## Styling Inventory (Plain CSS → Migration Target)

### Already migrated (no work)
- `shell/AppShell.tsx`, `shell/Hud.tsx`, `flow/GlobalOverlays.tsx`
- `dev/DevOverlay.tsx`, `dev/panels/SaloonDevPanel.tsx`, `dev/panels/SessionAuditDevPanel.tsx`, `dev/panels/TravelDevPanel.tsx`
- `components/StartGamePanel.tsx`, `components/StartGameOptionsForm.tsx`, `components/SeedCodeEditor.tsx`, `components/SetupSeedSummary.tsx`
- `components/TravelPanel.tsx`, `components/travel/*` (JourneyDecision, TravelActions, TravelDiaryDayCard, TravelDiaryNotebook, TravelSummary, travelShared)

### Needs migration (plain CSS classes → styled-components)
- **Routes (5):** `routes/CampRoute.tsx`, `routes/CaseFileRoute.tsx`, `routes/HuntRoute.tsx`, `routes/TrailRoute.tsx`, `routes/WantedRoute.tsx`
- **Feature panels (5):** `components/AvailableActionsPanel.tsx`, `components/FieldReportPanel.tsx`, `components/InventoryPanel.tsx`, `components/StoreOffersPanel.tsx`, `components/TravelRoutesPanel.tsx`
- **Modal surfaces (4):** `components/CaseFileSurface.tsx`, `components/CockpitOverlayFrame.tsx`, `components/JournalSurface.tsx`, `components/WantedPosterSurface.tsx`
- **Flow surfaces (9):** `flow/PreSessionSurface.tsx`, `flow/TownHubSurface.tsx`, `flow/TrailFlowSurface.tsx`, `flow/TravelPrepSurface.tsx`, `flow/ArrivalSurface.tsx`, `flow/places/StorePlace.tsx`, `flow/places/SheriffPlace.tsx`, `flow/places/SaloonPlace.tsx`

### Global → SASS (new `_base.scss` partial)
- Base `h1`/`h2`/`h3` font-family/line-height defaults currently in `styles.css` lines 32-37 and similar element-level rules that are not component-owned.

---

## File Structure

### New files
- `src/components/ui/sharedStyled.tsx` — Shared styled-components primitives used across multiple feature surfaces (Panel, PanelHead, PanelSubtitle, Button + variants, Field, StatusCard, StatList, Stack, CompactItem, Tag, TagRow, Muted, Eyebrow, Notice, Error, ActionRow, DestinationCard, BackButton, PlaceHeader, FlowSurface, FlowHero, FlowNotice, FlowError, TrailLockBanner, ArrivalCard). Mirrors the existing `components/travel/travelShared.tsx` pattern.
- `src/styles/_base.scss` — Base element defaults (h1/h2/h3 font-family, line-height) moved out of `styles.css`. Global, non-component.
- `docs/frontend-styling.md` — Human-facing durable guidance for the canonical styling stack.
- `src/tests/styling-stack.test.ts` — Enforcement test asserting no plain CSS imports and no `src/styles.css`.

### Modified files
- `src/styles/index.scss` — Remove `@use "../styles.css";`, add `@use "base";`.
- `src/WildBunch.Web/AGENTS.md` — Add "Frontend styling stack" section.
- `docs/INDEX.md` — Add pointer to `docs/frontend-styling.md`.
- `.agents/superpowers/plans/INDEX.md` — Add this plan entry (and self-heal the missing session-audit plan entry).
- All 23 component files listed under "Needs migration" above.

### Deleted files
- `src/styles.css` — Deleted in Task 8 after all classes are migrated.

---

## Task 1: Extract Shared Styled Primitives

**Files:**
- Create: `src/components/ui/sharedStyled.tsx`

**Interfaces:**
- Produces: Named styled-component exports consumed by Tasks 3-7. Each export is a styled HTML element with the exact CSS declarations from `styles.css` for the corresponding class, including responsive media queries.

- [ ] **Step 1: Create the shared styled primitives file**

Create `src/components/ui/sharedStyled.tsx` exporting styled-components for every class in `styles.css` that is used by more than one component, plus single-use primitives that are clearly reusable. For each, copy the exact CSS declarations from `src/styles.css` (same property values, same breakpoints). Name each export after the class it replaces (PascalCase): `Panel`, `PanelHead`, `PanelSubtitle`, `PanelActions`, `Button` (with `$variant` prop accepting `"primary" | "secondary" | "ghost"`), `Field`, `FieldLabel`, `FieldInput`, `FieldSelect`, `Notice`, `Error`, `StatusCard`, `StatList`, `StatTerm`, `StatValue`, `Stack`, `CompactItem`, `ActionRow`, `DestinationCard`, `DestinationCardBody`, `DestinationRoute`, `DestinationMeta`, `TagRow`, `Tag`, `Muted`, `Eyebrow`, `SessionGrid`, `CaseGrid`, `Layout`, `Hero`, `HeroCopy`, `HeroMetrics`, `Metric`, `BackButton`, `PlaceHeader`, `PlaceBody`, `FlowSurface` (with `$variant` prop for `pre-session | town-hub | place | travel-prep | trail | arrival`), `FlowHero`, `FlowHeroLead`, `FlowNotice`, `FlowError`, `TrailLockBanner`, `ArrivalCard`, `ArrivalLead`, `TownHubHeader`, `TownHubLead`, `TownHubGrid`, `PlaceCard` (with `$trailhead` boolean prop), `PlaceCardIcon`, `PlaceCardBody`, `TravelPrepBody`, `TravelPrepRide`, `TravelPrepActions`.

Reference the existing `src/components/travel/travelShared.tsx` for the export pattern. Use `var(--token)` for all design tokens. Include the responsive `@media` blocks from `styles.css` lines 623-687, 973-1025 inside each affected styled component.

- [ ] **Step 2: Run typecheck to verify the new file compiles**

Run: `npm run typecheck`
Expected: PASS (file is unused so far but must compile standalone)

- [ ] **Step 3: Commit**

```powershell
git add src/components/ui/sharedStyled.tsx
git commit -m "BUNCH-95: extract shared styled-components primitives"
```

---

## Task 2: Move Base Element Defaults to SASS `_base.scss`

**Files:**
- Create: `src/styles/_base.scss`
- Modify: `src/styles/index.scss`

**Interfaces:**
- Produces: A `_base.scss` partial holding global element defaults (h1/h2/h3 font-family, line-height) extracted from `styles.css`. `index.scss` loads it.

- [ ] **Step 1: Create `_base.scss` with global element defaults**

Create `src/styles/_base.scss` containing only truly-global element defaults currently in `styles.css` that are not component-owned: the `h1` font-family/line-height rule (lines 32-37) and any other bare-element rules that apply globally regardless of which component renders them. Do NOT move class-scoped rules here.

- [ ] **Step 2: Update `index.scss` to load `_base.scss`**

Edit `src/styles/index.scss` to add `@use "base";` after `@use "reset";`. Keep the `@use "../styles.css";` line for now — it is removed in Task 8.

- [ ] **Step 3: Run build to verify SASS compiles**

Run: `npm run build`
Expected: PASS (build succeeds; styles.css still imported so no visual change yet)

- [ ] **Step 4: Commit**

```powershell
git add src/styles/_base.scss src/styles/index.scss
git commit -m "BUNCH-95: move global element defaults to SASS _base.scss partial"
```

---

## Task 3: Migrate Route Components

**Files:**
- Modify: `routes/CampRoute.tsx`, `routes/CaseFileRoute.tsx`, `routes/HuntRoute.tsx`, `routes/TrailRoute.tsx`, `routes/WantedRoute.tsx`

**Interfaces:**
- Consumes: `Panel`, `PanelHead`, `PanelSubtitle`, `PanelActions`, `Notice`, `Error`, `Muted` from `src/components/ui/sharedStyled.tsx`
- Produces: Route components that use styled-components instead of `className="panel"` etc.

- [ ] **Step 1: Migrate each route component**

For each of the 5 route files, replace every `className="..."` referencing a `styles.css` class with the corresponding styled-component from `sharedStyled.tsx`. For one-off class combinations (e.g. `panel--wide`), use a local styled component extending the shared one or a `$wide` prop. Preserve the exact JSX structure and text content — only the styling mechanism changes.

Example for `CampRoute.tsx`:
- `<section className="panel panel--wide">` → `<Panel $wide>` (add `$wide?: boolean` prop to `Panel` in sharedStyled if not already present)
- `<header className="panel-head">` → `<PanelHead>`
- `<div className="notice">` → `<Notice>`
- `<div className="error">` → `<Error>`

- [ ] **Step 2: Run typecheck**

Run: `npm run typecheck`
Expected: PASS

- [ ] **Step 3: Run tests**

Run: `npm test`
Expected: PASS (tests use role/text queries, not class names)

- [ ] **Step 4: Commit**

```powershell
git add src/routes/CampRoute.tsx src/routes/CaseFileRoute.tsx src/routes/HuntRoute.tsx src/routes/TrailRoute.tsx src/routes/WantedRoute.tsx src/components/ui/sharedStyled.tsx
git commit -m "BUNCH-95: migrate route components to styled-components"
```

---

## Task 4: Migrate Feature Panels

**Files:**
- Modify: `components/AvailableActionsPanel.tsx`, `components/FieldReportPanel.tsx`, `components/InventoryPanel.tsx`, `components/StoreOffersPanel.tsx`, `components/TravelRoutesPanel.tsx`

**Interfaces:**
- Consumes: `Panel`, `PanelHead`, `PanelSubtitle`, `Stack`, `ActionRow`, `CompactItem`, `StatusCard`, `StatList`, `TagRow`, `Tag`, `Field`, `Button`, `DestinationCard`, `DestinationCardBody`, `DestinationRoute`, `DestinationMeta`, `Muted` from `sharedStyled.tsx`

- [ ] **Step 1: Migrate each feature panel**

Replace plain CSS class usage with styled-components from `sharedStyled.tsx`. For panel-specific styling not in the shared file, add a local styled component at the bottom of the file (following the pattern in `shell/Hud.tsx`). Preserve all text, structure, and behavior.

- [ ] **Step 2: Run typecheck and tests**

Run: `npm run typecheck`; `npm test`
Expected: PASS

- [ ] **Step 3: Commit**

```powershell
git add src/components/AvailableActionsPanel.tsx src/components/FieldReportPanel.tsx src/components/InventoryPanel.tsx src/components/StoreOffersPanel.tsx src/components/TravelRoutesPanel.tsx
git commit -m "BUNCH-95: migrate feature panels to styled-components"
```

---

## Task 5: Migrate Modal Surfaces

**Files:**
- Modify: `components/CockpitOverlayFrame.tsx`, `components/CaseFileSurface.tsx`, `components/JournalSurface.tsx`, `components/WantedPosterSurface.tsx`

**Interfaces:**
- Consumes: `Eyebrow`, `Muted`, `Button`, `PanelSubtitle` from `sharedStyled.tsx` plus local styled components for the `case-modal__*` and `wanted-poster-card__*` and `journal-*` class families.

- [ ] **Step 1: Migrate CockpitOverlayFrame (the modal frame)**

`CockpitOverlayFrame.tsx` uses `case-modal__backdrop`, `case-modal`, `case-modal__header`, `case-modal__body`. Create local styled components (`ModalBackdrop`, `Modal`, `ModalHeader`, `ModalBody`) with the exact CSS from `styles.css` lines 258-301. Export them from this file so `CaseFileSurface` and `JournalSurface` can reuse them, OR move them into `sharedStyled.tsx` if both consume them. Prefer exporting from `CockpitOverlayFrame.tsx` since the modal frame owns that shape.

- [ ] **Step 2: Migrate CaseFileSurface**

`CaseFileSurface.tsx` is the largest migration (~645 lines, many `case-modal__*` classes). Replace each class with a local styled component or shared primitive. The `Section` and `Card` helper sub-components inside this file become styled-components. Preserve all the formatting/anchor-row/deduction logic unchanged — only the JSX styling attributes change.

- [ ] **Step 3: Migrate JournalSurface**

Replace `journal-surface`, `journal-surface__head`, `journal-surface__clock`, `journal-timeline`, `journal-day`, `journal-day__header`, `journal-day__entries`, `journal-entry`, `journal-entry__message` with local styled components. Reuse `ModalBackdrop`/`Modal`/`ModalHeader`/`ModalBody` from CockpitOverlayFrame for the `case-modal__*` classes it also uses.

- [ ] **Step 4: Migrate WantedPosterSurface**

Replace all `wanted-poster-card__*` and `wanted-poster__*` classes with local styled components. This is the second-largest migration; preserve the exact card frame grid, portrait styling, and feature list layout.

- [ ] **Step 5: Run typecheck and tests**

Run: `npm run typecheck`; `npm test`
Expected: PASS

- [ ] **Step 6: Commit**

```powershell
git add src/components/CockpitOverlayFrame.tsx src/components/CaseFileSurface.tsx src/components/JournalSurface.tsx src/components/WantedPosterSurface.tsx
git commit -m "BUNCH-95: migrate modal surfaces to styled-components"
```

---

## Task 6: Migrate Flow Surfaces

**Files:**
- Modify: `flow/PreSessionSurface.tsx`, `flow/TownHubSurface.tsx`, `flow/TrailFlowSurface.tsx`, `flow/TravelPrepSurface.tsx`, `flow/ArrivalSurface.tsx`, `flow/places/StorePlace.tsx`, `flow/places/SheriffPlace.tsx`, `flow/places/SaloonPlace.tsx`

**Interfaces:**
- Consumes: `FlowSurface`, `FlowHero`, `FlowHeroLead`, `FlowNotice`, `FlowError`, `TownHubHeader`, `TownHubLead`, `TownHubGrid`, `PlaceCard`, `PlaceCardIcon`, `PlaceCardBody`, `PlaceHeader`, `BackButton`, `PlaceBody`, `TravelPrepBody`, `TravelPrepRide`, `TravelPrepActions`, `TrailLockBanner`, `ArrivalCard`, `ArrivalLead`, `Panel`, `PanelHead`, `Stack`, `Button`, `Field`, `DestinationCard`, `DestinationCardBody`, `DestinationRoute`, `DestinationMeta`, `Muted` from `sharedStyled.tsx`

- [ ] **Step 1: Migrate PreSessionSurface**

Replace `flow-surface`, `flow-surface--pre-session`, `flow-hero`, `flow-hero__lead`, `flow-notice`, `flow-error` with `FlowSurface $variant="pre-session"`, `FlowHero`, `FlowHeroLead`, `FlowNotice`, `FlowError`.

- [ ] **Step 2: Migrate TownHubSurface**

Replace `flow-surface--town-hub`, `town-hub-header`, `town-hub-lead`, `town-hub-grid`, `place-card`, `place-card--trailhead`, `place-card__icon`, `place-card__body` with the shared primitives. Use `<PlaceCard $trailhead>` for the trailhead variant.

- [ ] **Step 3: Migrate TrailFlowSurface and ArrivalSurface**

`TrailFlowSurface`: replace `flow-surface--trail`, `trail-lock-banner`. `ArrivalSurface`: replace `flow-surface--arrival`, `arrival-card`, `arrival-lead`, `button--primary`.

- [ ] **Step 4: Migrate TravelPrepSurface**

This is the most complex flow surface (uses `place-header`, `back-button`, `travel-prep-body`, `panel`, `panel-head`, `stack`, `travel-prep-ride`, `travel-prep-actions`, `button` variants, `destination-card` family, `flow-notice`, `flow-error`). Replace each with shared primitives.

- [ ] **Step 5: Migrate place surfaces (Store, Sheriff, Saloon)**

Each uses `flow-surface--place`, `place-header`, `back-button`, `place-body`, plus `panel`/`panel-head`/`stack`/`button`/`field`/`muted`/`flow-notice`/`flow-error`. Replace with shared primitives.

- [ ] **Step 6: Run typecheck and tests**

Run: `npm run typecheck`; `npm test`
Expected: PASS

- [ ] **Step 7: Commit**

```powershell
git add src/flow/PreSessionSurface.tsx src/flow/TownHubSurface.tsx src/flow/TrailFlowSurface.tsx src/flow/TravelPrepSurface.tsx src/flow/ArrivalSurface.tsx src/flow/places/StorePlace.tsx src/flow/places/SheriffPlace.tsx src/flow/places/SaloonPlace.tsx
git commit -m "BUNCH-95: migrate flow surfaces to styled-components"
```

---

## Task 7: Verify No Plain CSS Class References Remain

**Files:**
- Read-only verification across `src/`

- [ ] **Step 1: Grep for remaining className references to styles.css classes**

Run from `src/WildBunch.Web`:
```
rg --no-heading "className=\"[^\"]*\"" src/ --glob "!src/tests/**" --glob "!src/styles/**"
```
Scan the output for any `className` value that references a class defined in `styles.css` (e.g. `panel`, `button`, `case-modal__*`, `flow-surface`, etc.). Expected: zero matches. If any remain, return to the relevant task and migrate them.

- [ ] **Step 2: Grep for any remaining `.css` imports**

Run:
```
rg "import\s+['\"].*\.css['\"]|from\s+['\"].*\.css['\"]" src/
```
Expected: zero matches (the only CSS entry is `index.scss` which imports `.scss` partials, not `.css`).

- [ ] **Step 3: Do NOT commit yet — this is a verification gate before deletion**

If clean, proceed to Task 8. If not clean, fix the stragglers and commit them with message `BUNCH-95: migrate remaining plain CSS class stragglers`.

---

## Task 8: Delete `styles.css` and Update `index.scss`

**Files:**
- Delete: `src/styles.css`
- Modify: `src/styles/index.scss`

- [ ] **Step 1: Remove the `@use "../styles.css";` line from `index.scss`**

Edit `src/styles/index.scss` to delete the `@use "../styles.css";` line. The file should now contain only `@use "variables";`, `@use "reset";`, `@use "base";` and the header comment (update the comment to remove the "Feature styles that have not yet been migrated" line since styles.css is gone).

- [ ] **Step 2: Delete `src/styles.css`**

Delete the file `src/styles.css`.

- [ ] **Step 3: Run build to verify SASS still compiles without styles.css**

Run: `npm run build`
Expected: PASS

- [ ] **Step 4: Run typecheck and tests**

Run: `npm run typecheck`; `npm test`
Expected: PASS

- [ ] **Step 5: Commit**

```powershell
git add src/styles/index.scss
git rm src/styles.css
git commit -m "BUNCH-95: delete styles.css, finalize SASS-only global entry"
```

---

## Task 9: Add Enforcement Test

**Files:**
- Create: `src/tests/styling-stack.test.ts`

**Interfaces:**
- Produces: A Vitest test that fails if any `.tsx` file under `src/` (excluding `src/styles/` and `src/tests/`) imports a `.css` file, or if `src/styles.css` exists.

- [ ] **Step 1: Write the enforcement test**

Create `src/tests/styling-stack.test.ts` that:
1. Reads all `.tsx` files under `src/` (excluding `src/tests/` and `src/styles/`) using `fs` + `path` (Node APIs available in Vitest/jsdom).
2. Asserts none contain an `import` of a `.css` file (regex: `/import\s+['"][^'"]+\.css['"]/` or `from\s+['"][^'"]+\.css['"]`).
3. Asserts that `src/styles.css` does not exist (`fs.existsSync`).
4. Asserts that `src/styles/index.scss` exists and does not reference `styles.css`.

Use `describe`/`it` from vitest. Use `import { describe, it, expect } from "vitest"` and `import fs from "node:fs"` and `import path from "node:path"`.

- [ ] **Step 2: Run the test to verify it passes**

Run: `npm test -- --run src/tests/styling-stack.test.ts`
Expected: PASS

- [ ] **Step 3: Run full test suite**

Run: `npm test`
Expected: PASS

- [ ] **Step 4: Commit**

```powershell
git add src/tests/styling-stack.test.ts
git commit -m "BUNCH-95: add enforcement test for canonical styling stack"
```

---

## Task 10: Install Durable Repo Guidance

**Files:**
- Create: `docs/frontend-styling.md`
- Modify: `src/WildBunch.Web/AGENTS.md`
- Modify: `docs/INDEX.md`
- Modify: `.agents/superpowers/plans/INDEX.md` (self-heal: add this plan + missing session-audit plan entry)

- [ ] **Step 1: Create `docs/frontend-styling.md`**

Write a concise human-facing doc stating:
- The canonical frontend styling stack: `styled-components` for component-owned styling; SASS/SCSS (`src/styles/`) for globals, resets, tokens, base element defaults.
- Where each lives: shared styled primitives in `src/components/ui/sharedStyled.tsx`; travel-specific shared primitives in `src/components/travel/travelShared.tsx`; SASS partials in `src/styles/_variables.scss`, `_reset.scss`, `_base.scss`; entry in `src/styles/index.scss`.
- Design tokens are CSS custom properties in `_variables.scss`, referenced via `var(--token)` inside styled-components.
- Plain CSS is not the default. New components must use styled-components. New global concerns go in SASS partials.
- The enforcement test in `src/tests/styling-stack.test.ts` prevents new `.css` imports.

- [ ] **Step 2: Add "Frontend styling stack" section to `src/WildBunch.Web/AGENTS.md`**

Append a short section after the existing content stating the canonical stack and pointing to `docs/frontend-styling.md` for details. Keep it agent-facing (law, not tutorial).

- [ ] **Step 3: Add pointer in `docs/INDEX.md`**

Add a "Key files" entry: `- [Frontend Styling Stack](frontend-styling.md) - Defines the canonical frontend styling stack (styled-components + SASS/SCSS) and where each concern lives.`

- [ ] **Step 4: Update `.agents/superpowers/plans/INDEX.md`**

Add entry for this plan: `- [2026-06-26-bunch-95-frontend-styling-stack-consolidation.md](2026-06-26-bunch-95-frontend-styling-stack-consolidation.md) - Plan for BUNCH-95 frontend styling stack consolidation.`
Also self-heal the missing entry for `2026-06-26-session-audit-dev-panel-content-and-summaries.md`.

- [ ] **Step 5: Commit**

```powershell
git add docs/frontend-styling.md src/WildBunch.Web/AGENTS.md docs/INDEX.md .agents/superpowers/plans/INDEX.md
git commit -m "BUNCH-95: install durable frontend styling stack guidance"
```

---

## Task 11: Full Validation and Screenshot Proof

**Files:**
- Read-only validation + screenshots under `.agents/superpowers/output/screenshots/` (git-ignored)

- [ ] **Step 1: Run full frontend validation**

Run from `src/WildBunch.Web`:
```powershell
npm run typecheck
npm test
npm run build
```
Expected: all PASS. Report any warnings separately from failures.

- [ ] **Step 2: Run backend validation (unchanged, but confirms no accidental breakage)**

Run from repo root:
```powershell
dotnet build
dotnet test
```
Expected: PASS (backend is untouched; this is a guard against accidental cross-impact).

- [ ] **Step 3: Capture browser screenshots**

Start the API server and Vite dev server (record ports/PIDs for cleanup). Use the browser-qa or game-playtest skill to capture screenshots of:
1. Pre-session surface (no active game)
2. Town hub (active game, town view)
3. A place surface (store or saloon)
4. Trail surface
5. Case file modal (open)
6. Wanted posters surface
7. Journal modal (open)
8. Dev overlay closed (normal play surface)
9. Dev overlay open

Save screenshots under `.agents/superpowers/output/screenshots/bunch-95/`. Do NOT commit them (git-ignored).

- [ ] **Step 4: Stop all worker-owned helpers and verify cleanup**

Stop the API server, Vite dev server, and any browser sessions started for screenshots. Record stopped PIDs/ports. Verify no worker-owned processes remain.

- [ ] **Step 5: Final grep closeout evidence**

Run from `src/WildBunch.Web`:
```
rg --no-heading "className=\"[^\"]*\"" src/ --glob "!src/tests/**" --glob "!src/styles/**"
rg "import\s+['\"].*\.css['\"]" src/
rg "styles\.css" src/styles/index.scss
```
Expected: first command shows only styled-components-generated class names (if any) or empty; second and third commands return zero matches.

- [ ] **Step 6: Do NOT commit (screenshots are git-ignored; validation is evidence only)**

---

## Task 12: Publish Draft PR

- [ ] **Step 1: Push the branch**

```powershell
git push -u origin harleydbartles/bunch-95-consolidate-frontend-styling-stack-around-styled-components
```

- [ ] **Step 2: Create the draft PR**

```powershell
gh pr create --draft --base main --title "BUNCH-95: consolidate frontend styling stack around styled-components and SASS" --body "Plan and implementation for BUNCH-95."
```

Wait for plan approval before continuing implementation. The plan PR becomes the implementation PR; do not open a second PR.

---

## DOD Clause Mapping

| DOD clause | Task |
|------------|------|
| Current `main` inspected, frontend styling seams reported | Preflight (this plan's "Source Seams Inspected") |
| Plain CSS files/imports/classes inventoried and classified | This plan's "Styling Inventory" |
| Component-local styles use `styled-components` | Tasks 1, 3-6 |
| Global/non-component styling uses SASS/SCSS | Task 2 |
| Any remaining plain CSS explicitly justified or removed | Task 7-8 (removed; none retained) |
| Durable repo guidance states canonical stack | Task 10 |
| BUNCH-88 shell/HUD/dev overlay styling aligned | Already aligned on `main` (no work needed) |
| Frontend build/typecheck/tests pass | Tasks 3-9, 11 |
| Visual proof covers core play surface, shell/HUD, dev overlay states | Task 11 |
| Closeout grep/inventory evidence for `.css`, imports, legacy classes | Task 11 Step 5 |
| Return evidence: branch, PR URL, SHAs, changed files, validation, screenshots, DOD mapping | Task 12 + worker return |

## Non-Goal Protections

- No gameplay behavior changes — only styling mechanism.
- No feature surface redesign — same layout, spacing, colors, breakpoints.
- No backend/domain/application code changes.
- No CSS-in-JS for global reset/token concerns — those stay in SASS.
- No general visual polish pass.
- No Linear native delegation or `!` labels.
