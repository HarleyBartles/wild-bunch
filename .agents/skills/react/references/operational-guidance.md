# React operational guidance

This reference distils React guidance from the React documentation (`react.dev`) for day-to-day
component design and performance.

## Functional components and JSX

Build UI from functions that return JSX. Keep components focused on one responsibility. Use
destructuring for props and type props with TypeScript when the codebase uses it. Avoid defining
components inside other components so their identity stays stable. Use `key` props when rendering
lists and stable, predictable identifiers for reordering.

## Hooks rules

Call hooks only at the top level of a React function, before any early returns. Do not call hooks
inside loops, conditions, or nested functions. Prefix custom hooks with `use` so the React runtime
and lint rules recognize them. Keep hook calls in the same order on every render.

## Common hooks

- `useState` — keep state values simple and granular; prefer multiple `useState` calls over a single
  object when the values change independently.
- `useEffect` — use it to synchronize a component with an external system (network, subscription,
  DOM, storage). Do not use it to compute derived state or respond to every prop change without a
  clear dependency list.
- `useContext` — use for dependency-injection style data that many components need without prop
  drilling; keep contexts narrow and close to where they are consumed.
- Custom hooks — extract reusable stateful logic, not lifecycle logic that belongs in a component.
  Keep them free of JSX and side effects unless clearly documented.

## State lifting and composition

Lift state to the nearest common ancestor when multiple components need to read or write the same
value. Pass data down through props and callbacks; avoid deep prop drilling by composing children or
using context for truly shared concerns. Prefer explicit `children` props and component composition
over inheritance or render props.

## Conditional rendering

Return `null`, conditionals, or short-circuit expressions to render branches. Keep conditional paths
simple and avoid nested ternaries; extract helper components when a branch becomes complex. Ensure
hooks still run in the same order on every render.

## Performance

- `React.memo` — wrap pure components that receive the same props often but cause expensive parent
  renders.
- `useMemo` — memoize expensive calculations during render; do not memoize cheap work.
- `useCallback` — memoize callbacks passed to optimized children; combine with `React.memo` to
  prevent child re-renders.
- Measure before optimizing. Avoid wrapping every component in `memo`; start with state placement
  and component boundaries.

## Server components and framework-specific rendering

Server components and Next.js specifics are outside this skill. Use the appropriate
framework-specific skill when routing, data fetching, or server rendering are the primary concerns.
For client-side React, keep effects and browser APIs inside `useEffect` or event handlers.
