# BUNCH-95: Consolidate Frontend Styling Stack Around styled-components and SASS

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Mop up the remaining plain CSS debt in `src/WildBunch.Web` so the canonical frontend styling stack is `styled-components` for component-owned styling and SASS/SCSS for global/non-component concerns, with durable repo guidance and lightweight enforcement.

**Architecture:** A single plain CSS file (`src/styles.css`, ~1025 lines) is imported through `src/styles/index.scss` and feeds 22 component files via global class names. The SASS scaffolding (`_variables.scss`, `_reset.scss`, `index.scss`) and `styled-components` are already installed and used by 18 component files (shell, HUD, dev overlay, start-game flow, travel diary). The existing styled-components usage is mostly idiomatic (composition, transient props, colocation) but has token drift in the start-game and travel families — literal colours instead of `var(--token)`. This plan extracts a narrow set of genuinely cross-surface shared primitives, migrates each remaining component from plain CSS classes to local `styled-components` (feature-specific families stay local), moves the few truly-global element defaults into a new SASS `_base.scss` partial, deletes `styles.css`, installs durable guidance + enforcement, and does light token-drift cleanup on the already-migrated families.

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
3. **Component-owned styles (should be styled-components):** The 22 files listed in the inventory below that currently use plain CSS classes from `styles.css`.
4. **Global/non-component styles (should stay SASS):** `:root` tokens (`_variables.scss`), box reset + base element defaults (`_reset.scss`), plus a few base element rules currently in `styles.css` (`h1`/`h2`/`h3` font defaults, `body`-level rules) that belong in a `_base.scss` partial.
5. **Plain CSS that should remain after this issue:** None. `styles.css` is deleted. The only retained global CSS is the SASS partials (`_variables.scss`, `_reset.scss`, new `_base.scss`).
6. **Existing repo guidance:** `src/WildBunch.Web/AGENTS.md` covers play-surface doctrine and dev overlay work but says nothing about the styling stack. No `docs/frontend-styling.md` exists. No ADR covers frontend styling.
7. **Where durable guidance lives:** Both `src/WildBunch.Web/AGENTS.md` (agent law) and a new `docs/frontend-styling.md` (human-facing doc, indexed from `docs/INDEX.md`). The web AGENTS.md gets a short "Frontend styling stack" section pointing to the doc.
8. **BUNCH-88 shell/HUD/dev overlay handling:** Already styled-components on current `main`. No work needed; this plan does not touch them.
9. **Feature CSS surfaces:** Migrated as-is into styled-components with identical declarations (same values, same breakpoints). No redesign.
10. **Enforcement:** A Vitest test (`src/tests/styling-stack.test.ts`) that asserts (a) no `.tsx` file under `src/` (excluding `src/styles/` and `src/tests/`) imports a `.css` file, (b) `src/styles.css` does not exist, and (c) no `.tsx` file under `src/` (excluding `src/styles/` and `src/tests/`) references any retired `styles.css` class token via `className`. The retired-token check uses an explicit allowlist for any `className` that is genuinely not styling-stack debt (e.g. third-party widget class hooks). The closeout grep is retained as a separate manual evidence step.
11. **Validation/screenshot coverage:** `npm run typecheck`, `npm test`, `npm run build`, plus browser screenshots of pre-session surface, town hub, a place surface, trail surface, case file modal, wanted posters, journal modal, and dev overlay closed/open.
12. **Split condition:** One PR is sufficient. Shared primitives are extracted first; each subsequent component migration is independent and mechanical. The PR would only need splitting if a shared primitive's API turned out to be unstable across surfaces — unlikely given the existing `travelShared.tsx` precedent.

## Styling Inventory (Plain CSS → Migration Target)

Inventory rerun on PR base `c6d6ac4` (current `origin/main`). Total non-test, non-styles `.tsx` files: 47. Of those, 22 reference `styles.css` classes (need migration), 18 already use styled-components with zero `styles.css` class references (already migrated), and 7 use neither (routers/providers/entry — no styling work).

### Already migrated (18 files, no migration work)
- **Shell (3):** `shell/AppShell.tsx`, `shell/Hud.tsx`, `flow/GlobalOverlays.tsx`
- **Dev overlay (4):** `dev/DevOverlay.tsx`, `dev/panels/SaloonDevPanel.tsx`, `dev/panels/SessionAuditDevPanel.tsx`, `dev/panels/TravelDevPanel.tsx`
- **Start-game flow (4):** `components/StartGamePanel.tsx`, `components/StartGameOptionsForm.tsx`, `components/SeedCodeEditor.tsx`, `components/SetupSeedSummary.tsx`
- **Travel diary (7):** `components/TravelPanel.tsx`, `components/travel/JourneyDecision.tsx`, `components/travel/TravelActions.tsx`, `components/travel/TravelDiaryDayCard.tsx`, `components/travel/TravelDiaryNotebook.tsx`, `components/travel/TravelSummary.tsx`, `components/travel/travelShared.tsx`

### Needs migration (22 files, plain CSS classes → styled-components)
- **Routes (5):** `routes/CampRoute.tsx`, `routes/CaseFileRoute.tsx`, `routes/HuntRoute.tsx`, `routes/TrailRoute.tsx`, `routes/WantedRoute.tsx`
- **Feature panels (5):** `components/AvailableActionsPanel.tsx`, `components/FieldReportPanel.tsx`, `components/InventoryPanel.tsx`, `components/StoreOffersPanel.tsx`, `components/TravelRoutesPanel.tsx`
- **Modal surfaces (4):** `components/CaseFileSurface.tsx`, `components/CockpitOverlayFrame.tsx`, `components/JournalSurface.tsx`, `components/WantedPosterSurface.tsx`
- **Flow surfaces (8):** `flow/PreSessionSurface.tsx`, `flow/TownHubSurface.tsx`, `flow/TrailFlowSurface.tsx`, `flow/TravelPrepSurface.tsx`, `flow/ArrivalSurface.tsx`, `flow/places/StorePlace.tsx`, `flow/places/SheriffPlace.tsx`, `flow/places/SaloonPlace.tsx`

### Neither (7 files, no styling work)
`App.tsx`, `main.tsx`, `dev/DevPanelRegistry.tsx`, `dev/DevSurfaceContext.tsx`, `flow/GameFlowRouter.tsx`, `shell/router.tsx`, `state/GameSessionProvider.tsx`

### Global → SASS (new `_base.scss` partial)
- Base `h1`/`h2`/`h3` font-family/line-height defaults currently in `styles.css` lines 32-37 and similar element-level rules that are not component-owned.

---

## Existing styled-components Inventory (Idiomatic Double-Check)

All 18 already-migrated files were inspected against five idiomatic-use criteria. The goal is to confirm we are consolidating around a healthy pattern, not wrapping old CSS blobs in styled-components and calling it done.

### Criteria checked

1. **Composition from smaller named primitives** — Are styled components composed from shared bases where that helps readability?
2. **Variants via transient props / extension / small local components** — Not stringly class-style blobs?
3. **Colocation** — Are styles colocated with the owning component, except for genuinely shared primitives?
4. **Design tokens from `var(--token)`** — Not duplicated literal colours/spacings?
5. **Not the bad version** — Not dumping large chunks of plain CSS into styled-components unchanged?

### Findings by family

| Family | Files | Composition | Variants | Colocation | Tokens | Verdict |
|--------|-------|-------------|----------|------------|--------|---------|
| Shell/HUD | `AppShell.tsx`, `Hud.tsx` | Good — local components, no shared base needed | Good — `$active` transient prop on `DevToggleButton` | Good — local at file bottom | Good — `var(--border)`, `var(--accent)`, etc. | Clean, no cleanup |
| Dev overlay | `DevOverlay.tsx`, `SaloonDevPanel.tsx`, `SessionAuditDevPanel.tsx`, `TravelDevPanel.tsx` | Good — `MutedText` extended into `StatusText` | Good — `$active`, `$expanded`, `$top` transient props | Good — local at file bottom | Good — `var(--token)` throughout | Clean, no cleanup |
| Global overlays | `GlobalOverlays.tsx` | Good — small local components | Good — `:disabled` state, no variant prop needed | Good — local at file bottom | Good — `var(--token)` throughout | Clean, no cleanup |
| Start-game | `StartGamePanel.tsx`, `StartGameOptionsForm.tsx`, `SeedCodeEditor.tsx`, `SetupSeedSummary.tsx` | Good — `ButtonBase` → `PrimaryButton`/`GhostButton`; `baseControl` css fragment shared via interpolation | Good — extension for button variants | Good — local at file bottom | **Drift — literal colours** (`#efc37e`, `#f2efe8`, `rgba(242,239,232,0.62)`) instead of `var(--accent-strong)`, `var(--text)`, `var(--muted)` | **Light cleanup needed** (Task 0a) |
| Travel diary | `TravelPanel.tsx`, `travelShared.tsx`, `TravelSummary.tsx`, `JourneyDecision.tsx`, `TravelDiaryDayCard.tsx`, `TravelDiaryNotebook.tsx`, `TravelActions.tsx` | Good — `Card`/`SectionHeader`/`ButtonBase` from `travelShared.tsx`; `SummaryCard extends Card`; `ChoiceButton extends ButtonBase` | Good — `data-state` attribute selectors on `DayBadge`; extension for button variants | Good — shared primitives in `travelShared.tsx`, rest local | **Drift — literal colours** (`#efc37e`, `#f2efe8`, `rgba(242,239,232,0.58)`) instead of `var(--accent-strong)`, `var(--text)`, `var(--muted)` | **Light cleanup needed** (Task 0a) |

### Verdict

The existing styled-components usage is **idiomatic and healthy** in composition, variant strategy, and colocation. It is NOT the bad version (wrapped CSS blobs). The one real drift is **token literals instead of `var(--token)`** in the start-game and travel-diary families. The shell/HUD/dev-overlay/global-overlays families already use `var(--token)` correctly.

**Cleanup scope (Task 0a):** Replace literal colour values with `var(--token)` equivalents in the 11 start-game + travel-diary files where a matching token exists in `_variables.scss`. Where a literal does not map to a token (e.g. a one-off gradient stop), leave it with a code comment. This is mechanical token-substitution, not redesign. Do NOT restructure those components, change their composition, or move their local styled components into shared files.

---

## File Structure

### New files
- `src/components/ui/sharedStyled.tsx` — **Narrow set of genuinely cross-surface primitives only.** Contains only primitives used by 3+ unrelated surfaces: `Panel` (with `$wide` prop), `PanelHead`, `PanelSubtitle`, `Button` (with `$variant` prop: `"primary" | "secondary" | "ghost"`), `Muted`, `Eyebrow`, `Notice`, `Error`, `Stack`, `StatusCard`, `StatList`, `Field`, `FlowSurface` (with `$variant` prop), `FlowNotice`, `FlowError`, `BackButton`. Feature-specific styling families (case-modal, wanted-poster, journal, town-hub, place-card, travel-prep, arrival, trail-lock, destination-card, action-row, compact-item, tag, hero/metric/layout/session-grid/case-grid) stay as **local styled components in the owning component** unless concrete reuse is named during implementation. Mirrors the existing `components/travel/travelShared.tsx` pattern (which only exports `Card`, `SectionHeader`, `ButtonBase`, and utility functions — not a giant bucket).
- `src/styles/_base.scss` — Base element defaults (h1/h2/h3 font-family, line-height) moved out of `styles.css`. Global, non-component.
- `docs/frontend-styling.md` — Human-facing durable guidance for the canonical styling stack.
- `src/tests/styling-stack.test.ts` — Enforcement test asserting no plain CSS imports, no `src/styles.css`, and no retired `styles.css` class tokens in `className` (with narrow explicit allowlist).

### Modified files
- `src/styles/index.scss` — Remove `@use "../styles.css";`, add `@use "base";`.
- `src/WildBunch.Web/AGENTS.md` — Add "Frontend styling stack" section.
- `docs/INDEX.md` — Add pointer to `docs/frontend-styling.md`.
- `.agents/superpowers/plans/INDEX.md` — Add this plan entry (and self-heal the missing session-audit plan entry).
- All 22 component files listed under "Needs migration" above.
- 11 already-migrated files (start-game + travel-diary families) for light token-drift cleanup (Task 0a).

### Deleted files
- `src/styles.css` — Deleted in Task 8 after all classes are migrated.

---

## Task 0a: Token-Drift Cleanup on Already-Migrated Families

**Files:**
- Modify: `components/StartGamePanel.tsx`, `components/StartGameOptionsForm.tsx`, `components/SeedCodeEditor.tsx`, `components/SetupSeedSummary.tsx`, `components/TravelPanel.tsx`, `components/travel/travelShared.tsx`, `components/travel/TravelSummary.tsx`, `components/travel/JourneyDecision.tsx`, `components/travel/TravelDiaryDayCard.tsx`, `components/travel/TravelDiaryNotebook.tsx`, `components/travel/TravelActions.tsx`

**Interfaces:**
- Consumes: Token names from `src/styles/_variables.scss` (`--accent`, `--accent-strong`, `--text`, `--muted`, `--border`, `--border-strong`, `--danger`, `--bg-panel`, `--radius`, `--shadow`)
- Produces: Same components with literal colours replaced by `var(--token)` where a matching token exists.

- [ ] **Step 1: Map literals to tokens**

Read `src/styles/_variables.scss` and build the literal→token mapping. Known mappings from inspection:
- `#efc37e` / `#f0bb73` → `var(--accent-strong)` (or `var(--accent)` for `#df9f4f`)
- `#f2efe8` → `var(--text)`
- `rgba(242, 239, 232, 0.62)` / `rgba(242, 239, 232, 0.58)` / similar → `var(--muted)` (with opacity variant if needed — use `color-mix` or keep the rgba if no token matches the opacity)
- `rgba(255, 255, 255, 0.08)` / `rgba(255, 255, 255, 0.12)` → `var(--border)` / `var(--border-strong)`
- `#f07e6e` → `var(--danger)`

Where a literal does not map to a token (e.g. a one-off gradient stop like `rgba(236, 203, 146, 0.14)`), leave it and add a `/* no token match */` comment.

- [ ] **Step 2: Replace literals in each file**

For each of the 11 files, replace literal colour values with `var(--token)` where the mapping applies. Do NOT restructure components, move styled components, or change composition. This is mechanical substitution only.

- [ ] **Step 3: Run typecheck and tests**

Run: `npm run typecheck`; `npm test`
Expected: PASS (visual output unchanged since token values match the literals)

- [ ] **Step 4: Commit**

```powershell
git add src/components/StartGamePanel.tsx src/components/StartGameOptionsForm.tsx src/components/SeedCodeEditor.tsx src/components/SetupSeedSummary.tsx src/components/TravelPanel.tsx src/components/travel/travelShared.tsx src/components/travel/TravelSummary.tsx src/components/travel/JourneyDecision.tsx src/components/travel/TravelDiaryDayCard.tsx src/components/travel/TravelDiaryNotebook.tsx src/components/travel/TravelActions.tsx
git commit -m "BUNCH-95: replace literal colours with var(--token) in already-migrated families"
```

---

## Task 1: Extract Shared Styled Primitives

**Files:**
- Create: `src/components/ui/sharedStyled.tsx`

**Interfaces:**
- Produces: A **narrow** set of genuinely cross-surface styled-component exports consumed by Tasks 3-6. Only primitives used by 3+ unrelated surfaces belong here. Feature-specific families stay local.

- [ ] **Step 1: Create the shared styled primitives file**

Create `src/components/ui/sharedStyled.tsx` exporting ONLY these genuinely cross-surface primitives (each with exact CSS declarations from `src/styles.css`, using `var(--token)` for all design tokens, including responsive media queries where the primitive owns them):

`Panel` (with `$wide?: boolean` prop), `PanelHead`, `PanelSubtitle`, `Button` (with `$variant?: "primary" | "secondary" | "ghost"` prop), `Muted`, `Eyebrow`, `Notice`, `Error`, `Stack`, `StatusCard`, `StatList`, `Field`, `FlowSurface` (with `$variant?: "pre-session" | "town-hub" | "place" | "travel-prep" | "trail" | "arrival"` prop), `FlowNotice`, `FlowError`, `BackButton`.

Do NOT include feature-specific families in this file: case-modal, wanted-poster, journal, town-hub, place-card, travel-prep, arrival, trail-lock, destination-card, action-row, compact-item, tag, hero/metric/layout/session-grid/case-grid. Those become local styled components in the owning component during Tasks 3-6. If during implementation a primitive is found to be genuinely shared by 3+ surfaces, it may be promoted into `sharedStyled.tsx` with a note in the commit message — but start narrow.

Reference `src/components/travel/travelShared.tsx` for the export pattern (it exports only `Card`, `SectionHeader`, `ButtonBase` — not a giant bucket).

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

`CockpitOverlayFrame.tsx` uses `case-modal__backdrop`, `case-modal`, `case-modal__header`, `case-modal__body`. Create **local** styled components (`ModalBackdrop`, `Modal`, `ModalHeader`, `ModalBody`) with the exact CSS from `styles.css` lines 258-301. Export them from this file so `CaseFileSurface` and `JournalSurface` can import them — the modal frame owns that shape, so it is the right home. Do NOT move them into `sharedStyled.tsx`; they are a feature-specific family consumed by modal surfaces, not a cross-surface primitive.

- [ ] **Step 2: Migrate CaseFileSurface**

`CaseFileSurface.tsx` is the largest migration (~645 lines, many `case-modal__*` classes). Replace each class with a **local** styled component (for `case-modal__section`, `case-modal__card`, `case-modal__state`, `case-modal__grid`, `case-modal__identity-grid`, `case-modal__deductions`, `case-modal__anchor-list`, `case-modal__lead-list`, etc.) or import `ModalBackdrop`/`Modal`/`ModalHeader`/`ModalBody` from `CockpitOverlayFrame` for the frame shell. Use shared `Eyebrow`, `Muted`, `Button`, `PanelSubtitle` from `sharedStyled.tsx` where those exact primitives apply. The `Section` and `Card` helper sub-components inside this file become local styled components. Preserve all the formatting/anchor-row/deduction logic unchanged — only the JSX styling attributes change.

- [ ] **Step 3: Migrate JournalSurface**

Replace `journal-surface`, `journal-surface__head`, `journal-surface__clock`, `journal-timeline`, `journal-day`, `journal-day__header`, `journal-day__entries`, `journal-entry`, `journal-entry__message` with **local** styled components. Import `ModalBackdrop`/`Modal`/`ModalHeader`/`ModalBody` from `CockpitOverlayFrame` for the `case-modal__*` classes it also uses. Use shared `Eyebrow`, `Muted` from `sharedStyled.tsx`.

- [ ] **Step 4: Migrate WantedPosterSurface**

Replace all `wanted-poster-card__*` and `wanted-poster__*` classes with **local** styled components. This is the second-largest migration; preserve the exact card frame grid, portrait styling, and feature list layout. Use shared `Eyebrow`, `Muted`, `Tag` from `sharedStyled.tsx` where those exact primitives apply. Do NOT move the wanted-poster family into `sharedStyled.tsx`.

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
- Consumes: `FlowSurface`, `FlowNotice`, `FlowError`, `BackButton`, `Panel`, `PanelHead`, `Stack`, `Button`, `Field`, `Muted` from `sharedStyled.tsx`. All other styling (`FlowHero`, `FlowHeroLead`, `TownHubHeader`, `TownHubLead`, `TownHubGrid`, `PlaceCard`, `PlaceCardIcon`, `PlaceCardBody`, `PlaceHeader`, `PlaceBody`, `TravelPrepBody`, `TravelPrepRide`, `TravelPrepActions`, `TrailLockBanner`, `ArrivalCard`, `ArrivalLead`, `DestinationCard` family) becomes **local styled components** in the owning component.

- [ ] **Step 1: Migrate PreSessionSurface**

Replace `flow-surface`, `flow-surface--pre-session` with `FlowSurface $variant="pre-session"`. Replace `flow-notice`, `flow-error` with `FlowNotice`, `FlowError`. Replace `flow-hero`, `flow-hero__lead` with **local** styled components (`FlowHero`, `FlowHeroLead`) in this file.

- [ ] **Step 2: Migrate TownHubSurface**

Replace `flow-surface--town-hub` with `FlowSurface $variant="town-hub"`. Replace `town-hub-header`, `town-hub-lead`, `town-hub-grid`, `place-card`, `place-card--trailhead`, `place-card__icon`, `place-card__body` with **local** styled components in this file. Use a `$trailhead` transient prop on the local `PlaceCard` for the trailhead variant.

- [ ] **Step 3: Migrate TrailFlowSurface and ArrivalSurface**

`TrailFlowSurface`: replace `flow-surface--trail` with `FlowSurface $variant="trail"`; replace `trail-lock-banner` with a **local** styled component. `ArrivalSurface`: replace `flow-surface--arrival` with `FlowSurface $variant="arrival"`; replace `arrival-card`, `arrival-lead` with **local** styled components; replace `button--primary` with `<Button $variant="primary">`.

- [ ] **Step 4: Migrate TravelPrepSurface**

This is the most complex flow surface. Replace `flow-surface--travel-prep` with `FlowSurface $variant="travel-prep"`. Replace `place-header`, `travel-prep-body`, `travel-prep-ride`, `travel-prep-actions`, `destination-card` family with **local** styled components. Use shared `BackButton`, `Panel`, `PanelHead`, `Stack`, `Button` (with `$variant`), `FlowNotice`, `FlowError`, `Muted` from `sharedStyled.tsx`.

- [ ] **Step 5: Migrate place surfaces (Store, Sheriff, Saloon)**

Each uses `flow-surface--place` → `FlowSurface $variant="place"`. Replace `place-header`, `place-body` with **local** styled components. Use shared `BackButton`, `Panel`, `PanelHead`, `Stack`, `Button`, `Field`, `Muted`, `FlowNotice`, `FlowError` from `sharedStyled.tsx`.

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
- Produces: A Vitest test with three assertion groups: (a) no `.css` imports, (b) `src/styles.css` does not exist, (c) no retired `styles.css` class tokens appear in `className` attributes in source `.tsx` files (with a narrow explicit allowlist).

- [ ] **Step 1: Write the enforcement test**

Create `src/tests/styling-stack.test.ts` with three `describe` blocks:

**Block 1 — no plain CSS imports:**
1. Recursively read all `.tsx` files under `src/` (excluding `src/tests/` and `src/styles/`) using `fs.readdirSync` + `path`.
2. Assert none contain an `import` of a `.css` file (regex: `/import\s+['"][^'"]+\.css['"]/` or `from\s+['"][^'"]+\.css['"]`).

**Block 2 — styles.css is gone:**
3. Assert that `src/styles.css` does not exist (`fs.existsSync` returns false).
4. Assert that `src/styles/index.scss` exists and does not reference `styles.css`.

**Block 3 — no retired class tokens in className:**
5. Define a `RETIRED_CLASS_TOKENS` array containing the class names from the deleted `styles.css` (the full list from the inventory: `app-shell`, `hero`, `panel`, `panel--wide`, `panel-head`, `panel-actions`, `panel-subtitle`, `eyebrow`, `muted`, `button`, `button--ghost`, `button--primary`, `button--secondary`, `notice`, `error`, `metric`, `hero-copy`, `hero-metrics`, `layout`, `session-grid`, `case-grid`, `status-card`, `stat-list`, `stack`, `action-row`, `destination-card`, `destination-card__body`, `destination-route`, `destination-meta`, `compact-item`, `log-list`, `log-entry`, `log-entry__meta`, `case-summary`, `case-lead`, `case-release`, `case-modal`, `case-modal__backdrop`, `case-modal__header`, `case-modal__body`, `case-modal__state`, `case-modal__grid`, `case-modal__identity-grid`, `case-modal__identity-suspects`, `case-modal__section`, `case-modal__section--wide`, `case-modal__section-head`, `case-modal__card`, `case-modal__stats`, `case-modal__anchor-line`, `case-modal__minor`, `case-modal__anchor-list`, `case-modal__lead-list`, `case-modal__deductions`, `wanted-poster__list`, `wanted-poster-card`, `wanted-poster-card__frame`, `wanted-poster-card__portrait`, `wanted-poster-card__portrait-label`, `wanted-poster-card__content`, `wanted-poster-card__header`, `wanted-poster-card__quick-view`, `wanted-poster-card__meta`, `wanted-poster-card__feature-block`, `wanted-poster-card__section-head`, `wanted-poster-card__text-only-note`, `wanted-poster__feature-list`, `wanted-poster__feature`, `wanted-poster__feature-copy`, `wanted-poster__feature-tags`, `tag-row`, `tag`, `journal-surface`, `journal-surface__head`, `journal-surface__clock`, `journal-timeline`, `journal-day`, `journal-day__header`, `journal-day__entries`, `journal-entry`, `journal-entry__message`, `flow-surface`, `flow-surface--pre-session`, `flow-surface--town-hub`, `flow-surface--place`, `flow-surface--travel-prep`, `flow-surface--trail`, `flow-surface--arrival`, `flow-hero`, `flow-hero__lead`, `flow-notice`, `flow-error`, `town-hub-header`, `town-hub-lead`, `town-hub-grid`, `place-card`, `place-card--trailhead`, `place-card__icon`, `place-card__body`, `place-header`, `back-button`, `place-body`, `field`, `travel-prep-body`, `travel-prep-ride`, `travel-prep-actions`, `trail-lock-banner`, `arrival-card`, `arrival-lead`, `shell-overlay-bar`).
6. Define an `ALLOWLIST` array (initially empty — add entries only if a `className` is genuinely not styling-stack debt, e.g. a third-party widget hook). Each allowlist entry is `{ file: string, token: string, reason: string }`.
7. For each `.tsx` file under `src/` (excluding `src/tests/` and `src/styles/`), extract all `className="..."` and `className={...}` string values. Assert none contain a retired token unless the `(file, token)` pair is in the allowlist.

Use `import { describe, it, expect } from "vitest"` and `import fs from "node:fs"` and `import path from "node:path"`.

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
- Modify: `src/WildBunch.Web/.agents/unslop/play-surface-ui.md`
- Modify: `docs/INDEX.md`
- Modify: `.agents/superpowers/plans/INDEX.md` (self-heal: add this plan + missing session-audit plan entry)

- [ ] **Step 1: Create `docs/frontend-styling.md`**

Write a concise human-facing doc stating:
- The canonical frontend styling stack: `styled-components` for component-owned styling; SASS/SCSS (`src/styles/`) for globals, resets, tokens, base element defaults.
- Where each lives: shared styled primitives in `src/components/ui/sharedStyled.tsx`; travel-specific shared primitives in `src/components/travel/travelShared.tsx`; SASS partials in `src/styles/_variables.scss`, `_reset.scss`, `_base.scss`; entry in `src/styles/index.scss`.
- Design tokens are CSS custom properties in `_variables.scss`, referenced via `var(--token)` inside styled-components. Do not duplicate token values as literals in styled-components.
- Shared primitives are narrow (3+ unrelated surfaces). Feature-specific styling families stay local in the owning component.
- Variants use transient props (`$variant`, `$active`, `$wide`), `styled(Base)` extension, or `data-state` attribute selectors — not stringly class-style blobs.
- Plain CSS is not the default. New components must use styled-components. New global concerns go in SASS partials.
- The enforcement test in `src/tests/styling-stack.test.ts` prevents new `.css` imports and retired class tokens.

- [ ] **Step 2: Add "Frontend styling stack" section to `src/WildBunch.Web/AGENTS.md`**

Append a short section after the existing content stating the canonical stack and pointing to `docs/frontend-styling.md` for details. Keep it agent-facing (law, not tutorial).

- [ ] **Step 3: Update `src/WildBunch.Web/.agents/unslop/play-surface-ui.md` with styling-stack drift-prevention rules**

Add a new "Frontend styling stack" section to the unslop profile with these review rules (agent-facing filter, not tutorial):
- Component-owned styling must use `styled-components`, not plain CSS classes or `.css` imports.
- Global/non-component styling (resets, tokens, base element defaults) must use SASS/SCSS partials under `src/styles/`.
- Design tokens must be referenced via `var(--token)` from `_variables.scss`. Flag literal colour/spacing values in styled-components that duplicate a token — require a `/* no token match */` comment or token substitution.
- Shared styled primitives (`src/components/ui/sharedStyled.tsx`) must be genuinely cross-surface (3+ unrelated surfaces). Flag new additions that are feature-specific — they belong local in the owning component.
- Variants must use transient props, `styled(Base)` extension, or `data-state` attribute selectors. Flag stringly class-style variant blobs.
- The enforcement test (`src/tests/styling-stack.test.ts`) blocks new `.css` imports and retired `styles.css` class tokens. New `className` usage referencing a retired token requires an explicit allowlist entry with reason.
- This profile section is a review filter applied before designing, implementing, or reviewing any `src/WildBunch.Web` UI surface.

- [ ] **Step 4: Add pointer in `docs/INDEX.md`**

Add a "Key files" entry: `- [Frontend Styling Stack](frontend-styling.md) - Defines the canonical frontend styling stack (styled-components + SASS/SCSS) and where each concern lives.`

- [ ] **Step 5: Update `.agents/superpowers/plans/INDEX.md`**

Add entry for this plan: `- [2026-06-26-bunch-95-frontend-styling-stack-consolidation.md](2026-06-26-bunch-95-frontend-styling-stack-consolidation.md) - Plan for BUNCH-95 frontend styling stack consolidation.`
Also self-heal the missing entry for `2026-06-26-session-audit-dev-panel-content-and-summaries.md`.

- [ ] **Step 6: Commit**

```powershell
git add docs/frontend-styling.md src/WildBunch.Web/AGENTS.md src/WildBunch.Web/.agents/unslop/play-surface-ui.md docs/INDEX.md .agents/superpowers/plans/INDEX.md
git commit -m "BUNCH-95: install durable frontend styling stack guidance and unslop drift-prevention"
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

Start the API server (`dotnet run --project src/WildBunch.Api`) and Vite dev server (`npm run dev` from `src/WildBunch.Web`), record ports/PIDs for cleanup. Use the browser-qa or game-playtest skill to capture screenshots. Each screenshot has a named filename and maps to a DOD clause:

| # | Filename | Route/state | Setup path | DOD clause |
|---|----------|-------------|------------|------------|
| 1 | `01-pre-session.png` | Pre-session surface (no active game) | Load `/` with no `wild-bunch.current-game-id` in localStorage | Core play surface |
| 2 | `02-town-hub.png` | Town hub (active game, town view) | Start a new game via the seed form, land in town hub | Core play surface |
| 3 | `03-place-saloon.png` | Saloon place surface | From town hub, click the Saloon place card | Core play surface |
| 4 | `04-trail.png` | Trail surface (active journey) | From town hub, click trailhead, confirm travel to next town | Core play surface |
| 5 | `05-case-file-modal.png` | Case file modal (open) | From the overlay bar, click "Case file" | Feature surface (case modal) |
| 6 | `06-wanted-posters.png` | Wanted posters surface (open) | From the overlay bar, click "Wanted" (requires posters read first via Sheriff) | Feature surface (wanted) |
| 7 | `07-journal-modal.png` | Journal modal (open) | From the HUD, click "Journal" | Feature surface (journal) |
| 8 | `08-dev-overlay-closed.png` | Dev overlay closed (normal play surface) | With a game active, dev toggle off | Dev overlay closed state |
| 9 | `09-dev-overlay-open.png` | Dev overlay open | Click "Dev" toggle in chrome bar | Dev overlay open state |

Save screenshots under `.agents/superpowers/output/screenshots/bunch-95/`. Do NOT commit them (git-ignored). The final return must map each screenshot filename to the DOD clause it satisfies.

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
