# Row 15 Shared Display Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove caller-owned child styling from shared web wrappers while keeping each field, heading, and travel-header element's visible contract with the component that renders it.

**Architecture:** `sharedStyled.tsx` supplies named heading components and neutral layout primitives. Saloon's one shared-field use becomes a saloon-owned field with explicit label and select styling. Travel summary and diary headers compose named title and metadata styled components; the status pill owns its own colors and size. React still owns presentation state and the server remains authoritative for game state.

**Tech Stack:** React, TypeScript, styled-components, Vitest/Testing Library, the existing web command-bus lane.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, section E and player/read/display contracts; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, row 15.

**Execution Strategy:** `executing-plans`. These edits are one coupled styling-contract migration: shared primitive APIs and their current consumers must change together, while product behavior stays unchanged.

## Global Constraints

- Keep the existing React, Vite, TanStack Query, styled-components and global token/reset stack.
- Keep the existing content, semantic heading levels, accessible labels, form behavior and game-state ownership.
- Do not create CSS class contracts, global styling rules, generic component frameworks, feature redesigns or map-accessibility work.
- Keep deliberate styling of a component's own descendants, such as `StatList`'s `dt`/`dd`, where the markup is owned by that component.
- Do not add tests that assert source spelling, generated class names, exact style strings or the presence/absence of selectors. Existing behavior tests and browser inspection cover the unchanged player-visible contracts.
- This refactor does not change an ADR decision or a product promise; record the author's ADR and feature-matrix comparisons in the PR description.

## Review Focus

- Every style declaration for a caller-owned child moves onto a named child component or into the feature component that owns the child.
- Replacing a styled wrapper does not change accessible names, keyboard operation, field values, or display hierarchy.
- The travel status pill keeps its deliberate accent appearance without `!important`.
- No map selector, feature behavior or new reusable abstraction is introduced.

---

## Tasks

### Task 1: Replace shared descendant styling with explicit child contracts

**Files:**
- Modify: `src/WildBunch.Web/src/components/ui/sharedStyled.tsx`
- Modify: `src/WildBunch.Web/src/components/travel/travelShared.tsx`
- Modify: `src/WildBunch.Web/src/components/FieldReportPanel.tsx`
- Modify: `src/WildBunch.Web/src/components/InventoryPanel.tsx`
- Modify: `src/WildBunch.Web/src/components/TravelRoutesPanel.tsx`
- Modify: `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx`
- Modify: `src/WildBunch.Web/src/flow/places/SaloonPlace.tsx`
- Modify: `src/WildBunch.Web/src/flow/places/SheriffPlace.tsx`
- Modify: `src/WildBunch.Web/src/components/travel/TravelSummary.tsx`
- Modify: `src/WildBunch.Web/src/components/travel/TravelDiaryNotebook.tsx`

- [x] Remove `PanelHead`'s `h2` rule and `StatusCard`'s `h3` rule. Export explicit `PanelHeading` and `StatusCardHeading` styled components with the same current heading styles, then update all current consumers without changing their semantic elements or text.
- [x] Remove the generic `Field` child selectors from `sharedStyled.tsx`. Move the only shared-field use in `SaloonPlace` to local `DeclaredIdentityField`, `DeclaredIdentityLabel` and `DeclaredIdentitySelect` styled components that preserve the current layout and label-to-select behavior.
- [x] Replace `SectionHeader`'s `strong` and `span` rules with explicit travel title and metadata styled components. Give the travel summary status pill its complete appearance without `!important` and update the diary header to use the metadata component.
- [x] Keep `StatList`'s `dt`/`dd` styling and other selectors over markup owned by their styled component.
- [x] Run the focused existing Saloon, TravelRoutes, TravelPrep and TravelPanel tests. Add no test unless execution exposes an actual behavior gap that cannot be covered by those existing tests.

### Task 2: Verify the rendered contracts and retire the predecessor plan

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-row15-branch-worktree-closeout.md`
- Modify: `Directory.Build.props`
- Keep through this PR: `.agents/plans/2026-10-12-row15-shared-display-contracts.md`

- [x] Run `.\tools\postgres-dev.ps1 ensure`, then `.\scripts\dev-servers.ps1 ensure`; verify the browser uses this worktree's API and web URLs.
- [x] Play a read-only browser smoke through the in-town Sheriff and Saloon surfaces. Confirm semantic heading hierarchy, visible controls and narrow-width layout; inspect console and network state. The shared local database currently has an active playtest, so do not archive it or create a second playthrough. The active playtest has no journey; travel summary/diary appearance and behavior remain covered by focused existing component tests. Existing Saloon tests prove the wanted-identity control label and selection behavior. Capture representative desktop and narrow-width views and record results in the off-repo execution ledger only.
- [x] Update row 15 with PR #280's merged source, merge identity, exact-head/develop gate results, verified worktree/ref cleanup and retirement of the `.95` closeout plan. Point the live row at this plan and preserve the remaining row-15 work.
- [x] Repair the malformed row-15 entry in the roadmap's `Prerequisites and outputs` table with the actual row dependency and output contract.
- [x] Retire the completed `.95` predecessor plan and advance `Directory.Build.props` from `0.1.0-dev.95` to `0.1.0-dev.96`.
- [x] Run `py -3 tools/run.py ci --check`, inspect the full diff, verify roadmap links and confirm the generated web version is `0.1.0-dev.96`.
- [ ] Commit the completed implementation for PR publication to `develop`; the normal hook must validate the staged snapshot without changing or staging files.

**Validation:** `npm --prefix src/WildBunch.Web test -- src/tests/SaloonPlace.test.tsx src/tests/TravelRoutesPanel.test.tsx src/tests/TravelPrepSurface.test.tsx src/tests/TravelPanel.test.tsx`; manual browser evidence under `.agents/playbooks/ui-browser-check.md`; `py -3 tools/run.py ci --check`; `git diff --check`; generated web identity check.
