## Summary

- BUNCH-95: Consolidate frontend styling stack around `styled-components` (component-owned) and SASS/SCSS (global/non-component).
- Successfully migrated all components from plain CSS to styled components.
- Deleted `src/styles.css` (1025 lines removed).
- Introduced `src/components/ui/sharedStyled.tsx` for genuine cross-surface primitives.
- Installed durable styling doctrine in `src/WildBunch.Web/AGENTS.md` and `docs/frontend-styling.md`.
- Added automated enforcement test in `src/tests/stylingEnforcement.test.ts` covering file existence, CSS imports, and class usage.

## Changes

### 1. Shared Styled Primitives
Consolidated genuinely cross-surface primitives into `sharedStyled.tsx`: `Panel`, `StatusCard`, `Button`, `Grid`, `ItemCard`, `FlowSurface`. Feature-specific families like `DestinationCard` and `ActionRow` stay local to their respective components.

### 2. SASS Refactor
Moved global element defaults to `_base.scss`. Cleaned up `index.scss` and removed stale legacy comments.

### 3. Component Migrations
All 22 files referencing `styles.css` have been migrated to `styled-components` using the new primitives and design tokens.

### 4. Enforcement & Doctrine
- **Automated Guard**: `stylingEnforcement.test.ts` verifies no `styles.css` exists, no `.css` imports remain, and no legacy class names are used in TSX.
- **Documentation**: Added `docs/frontend-styling.md` and linked it in `docs/INDEX.md`. Updated project `AGENTS.md` with binding styling rules.

### Visual Verification
Screenshots captured during validation:
- Core Play Surface: `town-hub.png`, `saloon.png`, `sheriff.png`, `trail.png`.
- Shell/HUD & Overlays: `case-file.png`, `journal.png`, `pre-session.png`.
- Dev Overlay: `dev-overlay-closed.png`, `dev-overlay-open.png`.

#### Validation

- [x] `npm run typecheck` passes
- [x] `npm test` passes (including enforcement test)
- [x] `npm run build` passes
- [x] `dotnet build` passes
- [x] `.\scripts\postgres-dev.ps1 validate` passes (backend guard checks)

Generated with [Devin](https://devin.ai)

Co-Authored-By: Devin <158243242+devin-ai-integration[bot]@users.noreply.github.com>
