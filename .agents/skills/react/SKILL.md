---
name: react
description: Use when building or reviewing React component architecture, hooks usage,
  and performance patterns. Do not use when the work is framework-agnostic styling,
  routing, or state management owned by another skill.
metadata:
  source-id: react
  source-path: sources/first_party/skills/react/SKILL.md
  provenance-name: React first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: React component architecture, hooks usage, and performance patterns.
  use_when:
  - Use when building or reviewing React component architecture.
  - Use when choosing, ordering, or refactoring hooks.
  - Use when lifting state, composing components, or handling conditional rendering.
  - Use when optimizing renders with memo, useMemo, useCallback, or React.memo.
  - Use when deciding whether server components or a framework-specific skill is more
    appropriate.
  do_not_use_when:
  - Do not use when the work is framework-agnostic styling; use the web-styling skill.
  - Do not use when the work is TypeScript-specific type design; use the typescript
    skill.
  - Do not use when the work is routing or global state management owned by another
    skill.
  - Do not use when the task requires Next.js or framework-specific server components;
    use the appropriate framework skill.
  related_skills:
  - typescript
  - web-styling
license: MIT
---

# React

## Overview

React lets you build UIs from declarative components. This skill covers component architecture,
hooks, and performance patterns grounded in the React documentation.

## When to Use

- Designing or reviewing functional components and JSX.
- Choosing, ordering, or refactoring hooks (`useState`, `useEffect`, `useContext`, custom hooks).
- Lifting state, composing children, or handling conditional rendering.
- Optimizing renders with `memo`, `useMemo`, `useCallback`, or `React.memo`.
- Deciding when server components or a framework-specific skill such as Next.js is more appropriate.

Do not use for framework-agnostic styling; defer to `web-styling`. Do not use for TypeScript-only
type design; defer to `typescript`. Do not use for routing, global state management, or
framework-specific server frameworks.

## Core Pattern

1. Favor small, single-responsibility functional components.
2. Keep hooks at the top level and outside loops, conditions, and nested functions.
3. Lift state to the closest common ancestor and pass data down through props.
4. Use `useEffect` to synchronize with external systems, not to derive state.
5. Memoize only after measuring; avoid premature optimization.

## Common Mistakes

- Calling hooks conditionally or inside loops.
- Using `useEffect` to transform state that can be derived during render.
- Passing new object or array literals to optimized children without memoization.
- Mixing presentational concerns with side effects.
- Choosing server components or Next.js when only client-side composition is needed.
