# ADR-0002 GameSession Is the Command Aggregate Root

## Status

`live`

## Dated History

- `2026-06-01` - Chose `GameSession` as the single top-level command root for live play, while allowing coherent session-owned substructures.
- `2026-07-01` - Extracted focused child components under the root. This changed the internal organization without creating separate command roots.
- `2026-10-08` - Editorial clarification: the root consistency and event-production protocol is recorded in ADR-0038; this record retains the top-level root decision.

## Decision Type

`architecture`

## Related ADRs

- `depends on`: ADR-0001
- `related to`: ADR-0005, ADR-0013, ADR-0038

## Context

Live play includes rules and state for investigation, travel, inventory, and town visits. Those concerns can be cohesive internally while still sharing one command consistency boundary.

## Decision

`GameSession` is the single top-level command aggregate root for live play. Session-owned components may own cohesive rules and invariants, but they do not become independent command roots or repositories merely because they have internal boundaries. Persistence shape does not define domain ownership.

## Rationale and Alternatives

Separate command roots would be appropriate only if the relevant game rules could be changed independently without shared consistency concerns. Flattening all session concepts would obscure cohesive internal rules. The selected boundary keeps player actions coherent while allowing focused domain components.

## Consequences

New gameplay commands use the session boundary unless a later decision establishes a genuinely independent consistency boundary. The root may delegate cohesive rules internally; this does not make it a catch-all owner of every rule's implementation.
