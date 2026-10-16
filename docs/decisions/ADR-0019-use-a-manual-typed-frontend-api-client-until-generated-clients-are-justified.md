# ADR-0019 Keep a Manual Typed Frontend API Client

## Status

`live`

## Dated History

- `2026-06-01` - Chose a small hand-authored typed client for browser-to-API communication until the contract makes generated clients worthwhile.
- `2026-10-08` - Editorial clarification: request hooks and endpoint inventories are implementation details; the generation threshold is the durable decision.

## Decision Type

`architecture`, `ui`

## Related ADRs

- `depends on`: ADR-0015, ADR-0016
- `related to`: ADR-0027

## Context

The web client needs a typed transport boundary without introducing generated-client tooling before the API contract justifies the maintenance cost.

## Decision

Maintain a typed hand-authored frontend API client while the HTTP contract remains small enough to keep it accurate. Adopt generated clients only when contract breadth or drift creates a concrete need that outweighs their generation and maintenance costs.

## Rationale and Alternatives

Direct fetch calls spread transport details across components. Generated clients can reduce duplication at scale, but introduce tooling and generated-artifact ownership that are not justified solely by the existence of an API.

## Consequences

The manual client must remain typed and aligned with the server contract. Its hooks, query keys, and endpoint list may evolve without changing this decision.
