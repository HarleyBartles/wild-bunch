## Summary

- BUNCH-95: Consolidate frontend styling stack around `styled-components` (component-owned) and SASS/SCSS (global/non-component).
- Successfully migrated all components from plain CSS to styled components.
- Deleted `src/styles.css` (1025 lines removed).
- Introduced `src/components/ui/sharedStyled.tsx` for cross-surface primitives.
- Installed durable styling doctrine in `src/WildBunch.Web/AGENTS.md`.
- Added automated enforcement test in `src/tests/stylingEnforcement.test.ts`.

## Changes

### 1. Shared Styled Primitives
Extracted 20+ genuinely cross-surface primitives into `sharedStyled.tsx`, including `Panel`, `StatusCard`, `Button`, `Grid`, `ItemCard`, `ActionRow`, and `FlowSurface`.

### 2. SASS Refactor
Moved global element defaults (like `h1` styling) to `_base.scss`. Finalized `index.scss` to use only project-standard SASS partials.

### 3. Component Migrations
- **Routes (5 files)**: Camp, CaseFile, Hunt, Trail, Wanted.
- **Feature Panels (5 files)**: AvailableActions, FieldReport, Inventory, StoreOffers, TravelRoutes.
- **Modal Surfaces (4 files)**: CockpitOverlayFrame, CaseFileSurface, WantedPosterSurface, JournalSurface.
- **Flow Surfaces (8 files)**: PreSession, Arrival, TownHub, TravelPrep, TrailFlow, SaloonPlace, SheriffPlace, StorePlace.

### 4. Enforcement & Doctrine
- Added `stylingEnforcement.test.ts` which greps for legacy class names in the codebase.
- Updated `AGENTS.md` with strict styling rules to prevent stack drift.

### Visual Verification
Screenshots captured during validation are available in `.agents/superpowers/output/screenshots/`:
- `pre-session.png`, `town-hub.png`, `saloon.png`, `sheriff.png`, `case-file.png`, `journal.png`, `travel-prep.png`, `trail.png`.

#### Validation

- [x] `npm test` passes (all 44 tests passing, including enforcement)
- [x] Automated grep: No `styles.css` imports remain; no legacy class names used in TSX files.
- [x] Manual check: Core gameplay flows verified via local dev server.

Generated with [Devin](https://devin.ai)

Co-Authored-By: Devin <158243242+devin-ai-integration[bot]@users.noreply.github.com>
