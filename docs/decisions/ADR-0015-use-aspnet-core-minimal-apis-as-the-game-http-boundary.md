# ADR-0015 Use ASP.NET Core Minimal APIs as the Game HTTP Boundary

## Status

`live`

## Dated History

- `2026-06-01` - Chose ASP.NET Core Minimal APIs as the HTTP boundary for game capabilities.
- `2026-10-08` - Editorial clarification: endpoint handlers adapt HTTP to application commands and queries; game legality remains in the domain.

## Decision Type

`architecture`

## Related ADRs

- `depends on`: ADR-0014
- `related to`: ADR-0016, ADR-0017, ADR-0019

## Context

The game needs an HTTP adapter that maps requests and responses without making transport code a second gameplay model.

## Decision

Use ASP.NET Core Minimal APIs as the game HTTP boundary. Endpoint code owns HTTP routing, validation at the transport boundary, and response mapping, and delegates application use cases to handlers. Gameplay invariants remain in the domain and persistence remains behind application ports.

## Rationale and Alternatives

Minimal APIs provide explicit capability-oriented route composition without requiring controller ceremony. A GraphQL or controller-centric boundary would require a separate source-backed reason to replace this choice.

## Consequences

HTTP endpoints remain adapters rather than owners of gameplay rules. Route inventories and OpenAPI configuration are operational/source details, not durable parts of this decision.
