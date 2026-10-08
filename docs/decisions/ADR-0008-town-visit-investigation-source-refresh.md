# ADR-0008 Town-Visit Investigation Source Refresh

## Status

`live`

## Dated History

- `2026-06-01` - Chose town-visit-scoped use of investigation sources, with refresh on arrival while retaining each town's visit history.
- `2026-10-08` - Editorial clarification: this decision governs repeatability across town visits, not which clue sources are currently enabled or their player-facing placement.

## Decision Type

`gameplay`

## Related ADRs

- `depends on`: ADR-0005, ADR-0006
- `related to`: ADR-0007, ADR-0009

## Context

Investigation sources can be revisited as a player travels. Repeating the same source during one visit should not endlessly produce new knowledge, while a later visit may present a fresh opportunity.

## Decision

Track investigation-source use within town-visit state. Repeating a spent source in the same visit does not reveal it again; arriving in a town refreshes the visit-scoped opportunity while preserving the town's history.

## Rationale and Alternatives

Making sources globally one-time would discard the significance of returning to a town. Unlimited repeats would allow repeated actions in one stop to generate unbounded knowledge. Keeping the rule in game state rather than the UI makes it authoritative across clients.

## Consequences

New town-scoped investigation sources must use the same visit boundary unless a later decision gives them different repeat rules. The rule does not imply that every planned source type is implemented or enabled.
