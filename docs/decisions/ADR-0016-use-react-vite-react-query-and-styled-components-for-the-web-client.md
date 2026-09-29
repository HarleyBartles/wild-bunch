# ADR-0016 Use React, Vite, TanStack React Query, and styled-components for the web client

## Status

live

## Dated Status History

- 2026-06-01 - live: the web client uses React 18, Vite, TanStack React Query, and styled-components as its current stack.

## Decision Type

ui, architecture, tooling

## Related ADRs

- `depends on`: ADR-0015
- `informs`: ADR-0011, ADR-0017, ADR-0019

## Context

The web client is a TypeScript React application built with Vite. Source shows React Query coordinating server state and mutation invalidation, styled-components handling component styling, and a hand-authored API module providing typed access to the backend.

## Decision Drivers

- The client needs a clear, current stack for rendering and server-state coordination.
- Server truth should not be duplicated in local component state when React Query can own fetch, mutate, and invalidate behavior.
- Styling should remain component-scoped and composable.
- The current cockpit/modal evolution should not be prematurely locked into a final routing stance.

## Decision Summary

Use React 18 with Vite, TanStack React Query, and styled-components for the web client. Treat React Query as the primary server-state and mutation-coordination layer, and keep styling component-scoped instead of defaulting to a CSS framework or global state pattern.

## Detailed Decision Breakdown

The current web app bootstraps React through Vite, wraps the app in a `QueryClientProvider`, and centralizes API calls in a typed client module. Hooks such as the travel panel state hook use React Query to fetch fresh server state, run mutations, and invalidate stale queries after successful actions.

styled-components is the established styling approach for UI surfaces and shared travel components. That keeps component styling close to the component tree while allowing the cockpit and modal surfaces to evolve without forcing a different app-wide layout strategy too early.

This ADR deliberately stays aligned with the current source shape and does not claim a final routing architecture beyond the cockpit/modal direction already captured in ADR-0011.

## Options Considered and Rejected

- Use Redux or another global client-state library as the default server-state tool.
- Scatter ad hoc fetch, loading, and error handling through individual components.
- Move to a server-rendered or full-stack framework as the default client posture.
- Make a CSS framework the primary styling strategy.

## When a Rejected Option Would Have Been Better

A global client-state store would only be better if the app's primary problem were local-only state orchestration rather than server truth. A full-stack framework would only be better if the product had already committed to server-driven routing and rendering as the default model.

## Benefits

- Server state and mutation coordination live in one clear place.
- The UI stack stays familiar and productive for current feature work.
- Component styling stays expressive without a framework lock-in.

## Accepted Tradeoffs

- The app depends on a client build step and a Vite toolchain.
- Query invalidation discipline has to stay consistent so server truth remains authoritative.
- Styled-components introduces a runtime styling dependency that must remain intentional.

## Risks

- API shape drift could spread if the typed client is not kept centralized.
- Component-level styling could become inconsistent if shared tokens or primitives are not reused.

## Consequences for Future Work

New UI work should assume React Query for server state, the existing typed API module for transport, and styled-components for styling unless a later ADR changes the stack.

## Implementation Status or Plan

Live. The current source tree already uses the described client stack.

## Related Stable Source Surfaces

- `src/WildBunch.Web/package.json`
- `src/WildBunch.Web/src/main.tsx`
- `src/WildBunch.Web/src/api/wildBunchApi.ts`
- `src/WildBunch.Web/src/api/types.ts`
- `src/WildBunch.Web/src/hooks/useTravelPanelState.ts`
- `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- `src/WildBunch.Web/src/hooks/useTownStoreOffers.ts`
- `src/WildBunch.Web/src/components/`
- `src/WildBunch.Web/src/components/travel/travelShared.tsx`

## Proof of Implementation or Explicit Non-Implementation

`package.json` declares the React, Vite, React Query, and styled-components stack, `main.tsx` wires the query client provider, and the hooks/components already use the centralized typed API module and component-scoped styling.

## Review Triggers

- When the client moves to a different framework or rendering model.
- When client state starts duplicating server truth instead of using React Query.
- When styling shifts away from styled-components as the main approach.
