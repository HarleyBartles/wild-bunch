# Frontend Styling

Wild Bunch uses a consolidated frontend styling stack centered on `styled-components` and SASS.

## Core Principles

1.  **Component-Owned Styling**: All layout and appearance specific to a component must live within the component file using `styled-components`.
2.  **Global Concerns**: SASS (`src/styles/`) is reserved for genuinely global concerns:
    *   `_variables.scss`: Design tokens and theme variables.
    *   `_reset.scss`: CSS reset and normalization.
    *   `_base.scss`: Base element defaults (e.g., `h1`, `p` defaults).
3.  **No Plain CSS Classes**: Direct usage of `className="legacy-class"` is forbidden for component styling. All styling must be handled via styled components.
4.  **Token Discipline**: Reference design tokens via `var(--token-name)` instead of hardcoded hex/rgb values to ensure palette consistency.

## Shared Primitives

Genuninely cross-surface (used in 3+ unrelated surfaces) primitives are extracted to `src/components/ui/sharedStyled.tsx`.

Current shared primitives include:
*   `Panel`: Main layout container.
*   `StatusCard`: Boxed status/info area.
*   `Button`: Standard themed buttons with variants (`$variant="primary"`, etc.).
*   `Grid`: Responsive grid layout helper.
*   `Stack`: Vertical layout helper with consistent gaps.
*   `ItemCard`: Small card for list items.

## Feature-Specific Styling

Styling that is specific to a feature (e.g., `DestinationCard` for travel) should stay local to that feature's components, even if it uses common patterns like `ItemCard` as a base.

## Enforcement

The styling stack is enforced by an automated test: `src/tests/stylingEnforcement.test.ts`. This test asserts:
*   `src/styles.css` does not exist.
*   `src/styles/index.scss` does not reference legacy CSS.
*   No `.css` imports remain in TSX files.
*   No legacy plain CSS classes from the original `styles.css` are used in `className`.
