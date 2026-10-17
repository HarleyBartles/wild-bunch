# ADR-0016 Use React, Vite, TanStack Query, and styled-components

## Status

`partially superseded`

## Dated History

- `2026-06-01` - Chose React, Vite, TanStack React Query, and styled-components for the browser client.
- `2026-10-08` - Editorial clarification: the stack decision survives, while the original cockpit and modal routing assumptions were replaced by later shell and flow decisions.

## Decision Type

`ui`, `architecture`

## Related ADRs

- `depends on`: ADR-0015
- `partially superseded by`: ADR-0027, ADR-0030, ADR-0039
- `related to`: ADR-0019, ADR-0035

## Context

The browser client needs a component framework, build tool, server-state coordination, and styling approach while remaining a client of server-authoritative game state.

## Decision

Use React and Vite for the browser client, TanStack React Query for server state and mutation coordination, and styled-components for component-owned styling. Keep gameplay state authoritative on the server; use React state and context for client-owned presentation state.

## Rationale and Alternatives

Scattered fetch logic makes loading, errors, and refresh behavior inconsistent. A global client store is not a substitute for server state. A styling framework or global selector model would not express the chosen component-scoped styling approach.

## Consequences

Queries and mutations must keep client views aligned with server results. Component styling remains close to its display contract. The original cockpit and routing direction is replaced in part by ADR-0027 and the later setup-flow decision in ADR-0039; the stack decision survives.

## Successors and Surviving Scope

ADR-0027, ADR-0030, and ADR-0039 supersede the original route and cockpit assumptions. React, Vite, TanStack React Query, and styled-components remain the selected client stack.
