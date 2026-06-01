# ADR-0019 Use a manual typed frontend API client until generated clients are justified

## Status

live

## Dated Status History

- 2026-06-01 - live: the web client uses a hand-authored typed API module as the current transport convention.

## Decision Type

architecture, tooling, ui

## Related ADRs

- `depends on`: ADR-0015, ADR-0016

## Context

The web client already has a centralized `wildBunchApi.ts` module and matching `types.ts` contracts. The module encodes request paths, request bodies, and shared error extraction in one place, and the current tests verify that the request shape is intentional.

## Decision Drivers

- A small, explicit typed client is sufficient for the current API surface.
- Transport concerns should stay centralized instead of leaking into components.
- Generated clients should only be introduced when the API surface size or duplication justifies them.
- Ad hoc fetch calls should not proliferate through the component tree.

## Decision Summary

Use a manually maintained typed frontend API client for now. Keep request/response shapes centralized in `wildBunchApi.ts` and `types.ts`, and defer generated client tooling until the repo has a source-backed reason to adopt it.

## Detailed Decision Breakdown

The current API module owns the fetch wrapper, base URL selection, JSON parsing, and error extraction logic. It then exports typed functions for game, travel, journal, store, and investigation actions.

This is a deliberate middle ground: typed enough to keep the client readable and testable, but not yet tied to generated client infrastructure that would add extra tooling and maintenance cost without a clear current payoff.

## Options Considered and Rejected

- Scatter untyped fetch calls through components and hooks.
- Introduce an OpenAPI-generated client before the duplication or scale justifies it.
- Treat transport code as a one-off detail in each UI surface.

## When a Rejected Option Would Have Been Better

Generated clients would only be better if the API were large enough that hand-maintained transport code had become a duplication problem. Scattered fetch calls would only be better for tiny throwaway demos.

## Benefits

- Transport concerns stay centralized and testable.
- The client remains easy to edit with the API surface.
- Request and response types stay visible in source.

## Accepted Tradeoffs

- The client module has to be maintained by hand.
- API shape changes require a deliberate source update.

## Risks

- The module could become a catch-all if too many unrelated transport concerns are added.
- A future generated-client decision should not be blocked by sentiment if the API outgrows the manual approach.

## Consequences for Future Work

New frontend work should keep using the centralized typed client until a later ADR justifies generated tooling or a different transport layer.

## Implementation Status or Plan

Live. The current web client already uses the manual typed API module and tests it directly.

## Related Stable Source Surfaces

- `src/WildBunch.Web/src/api/wildBunchApi.ts`
- `src/WildBunch.Web/src/api/types.ts`
- `src/WildBunch.Web/src/api/wildBunchApi.test.ts`
- `src/WildBunch.Web/src/hooks/useTravelPanelState.ts`
- `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts`
- `src/WildBunch.Web/src/hooks/useTownStoreOffers.ts`
- `src/WildBunch.Web/src/components/TravelPanel.test.tsx`
- `src/WildBunch.Web/src/components/TravelRoutesPanel.test.tsx`

## Proof of Implementation or Explicit Non-Implementation

`wildBunchApi.ts` centralizes the transport layer, the request-shape tests assert the encoded payloads, and the UI hooks/components consume the shared typed functions rather than making ad hoc fetch calls.

## Review Triggers

- When the API surface becomes large enough that hand-maintained transport code is a burden.
- When a generated client becomes justified by duplication or contract complexity.
- When ad hoc fetch calls begin appearing outside the shared API module.
