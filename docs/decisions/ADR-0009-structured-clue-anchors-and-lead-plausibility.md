# ADR-0009 Structured Clue Anchors and Lead Plausibility

## Status

`live`

## Dated History

- `2026-06-01` - Chose structured clue anchors for subjects, locations, times, and directions where they clarify what a lead means.
- `2026-10-08` - Editorial clarification: anchors support fair interpretation without claiming that every lead is backed by a fully simulated movement history.

## Decision Type

`gameplay`

## Related ADRs

- `depends on`: ADR-0005, ADR-0006, ADR-0007
- `related to`: ADR-0010

## Context

Clue prose alone may not preserve enough meaning for a case board or other player read surface to explain who or where a lead concerns.

## Decision

Use structured anchors for clue subjects, locations, times, and directions when those facts are relevant to the clue. Interpret and display leads from their authored meaning rather than implying unimplemented world simulation.

## Rationale and Alternatives

Plain prose alone makes it difficult to preserve a clue's subject and place across authoring and display. Requiring full movement simulation for every lead would promise more world behavior than the game needs to support this decision.

## Consequences

Clue authoring and presentation must preserve the meaning of any supplied anchors. New anchor forms should express a player-understandable lead, not expose hidden truth or imply unsupported simulation.
