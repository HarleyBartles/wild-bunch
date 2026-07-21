# Frontend React Profile

Portable anti-slop profile for React implementation defaults.

## Purpose

Catch React implementation defaults that look professional but add unnecessary abstraction, state confusion, or performance theater.

## Where to Use

Before creating or reviewing React components, hooks, state flows, frontend feature slices, component libraries, or React refactors.

## Slop Patterns to Avoid

- Generic component soup: `Card`, `Modal`, `Dashboard`, `Layout`, `Provider`, and `useThing` abstractions without demand
- Premature `useMemo`, `useCallback`, context providers, reducers, or custom hooks
- Future-proof prop APIs with many options before real use cases exist
- State ownership drift between local state, parent state, URL state, cache state, and server state
- UI components that hide domain behavior behind generic styling wrappers
- Accessibility and loading/error states mentioned vaguely or omitted

## Required Avoid Rules

Prohibit abstraction-by-default, memoization-by-default, context-by-default, and future-proof prop design when no actual repeated use case or measured performance issue exists.

Catch React code plans that do not identify state ownership and user-visible behavior.

## Required Prefer-Instead Rules

- Start with the smallest component or hook that satisfies the current behavior
- Name state ownership and data flow explicitly
- Add memoization only when there is a measured or obvious render-cost reason
- Add reusable abstractions only after at least two concrete uses or a clear boundary
- Include loading, empty, error, disabled, and accessibility behavior when relevant

## False Positives / Do Not Overapply

Do not ban React abstractions. Ban default abstractions that precede evidence. Reusable components, context, reducers, and memoization are valid when the profile records the reason.

## Examples

### Before (Avoid)
> Create a reusable component with hooks for state management and memoization for performance.

### After (Prefer)
> Create a `UserProfile` component that displays user name and email. State: local component state for loading/error. No memoization needed - component renders once on mount. Accessibility: include `alt` text for avatar, keyboard navigation for edit button.

### Before (Avoid)
> Use a flexible prop-driven architecture so the component can handle future cases.

### After (Prefer)
> Create a `Button` component with `variant` (primary/secondary) and `size` (small/medium/large) props. These are the only variants needed for the current design system. Future variants can be added when use cases emerge.

## Acceptance Checks

This profile is acceptable only if it would stop a React agent from producing reusable infrastructure before proving the current feature needs it.
