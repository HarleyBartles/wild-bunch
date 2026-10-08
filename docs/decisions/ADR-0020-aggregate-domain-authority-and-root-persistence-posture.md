# ADR-0020 Aggregate Domain Authority and Root Persistence Posture

## Status

`partially superseded`

## Dated History

- `2026-06-01` - Chose to keep domain legality within the boundary that owns the rule, and to distinguish that authority from persistence coordination.
- `2026-10-08` - Partially superseded the autonomous child event and cross-aggregate protocol with ADR-0038. Cohesive child rule ownership and the no-reach-through rule remain.

## Decision Type

`architecture`, `persistence`

## Related ADRs

- `depends on`: ADR-0002, ADR-0014
- `partially superseded by`: ADR-0038
- `related to`: ADR-0005, ADR-0008, ADR-0013, ADR-0028

## Context

The game contains cohesive domain boundaries under a single live-play session. The code needs to preserve the rules owned by those boundaries without treating persistence coordination as permission to mutate another boundary's state directly.

## Decision

Each domain boundary owns its own legality and invariants. A coordinating session may compose a command's legal effects, but it may not bypass another boundary's rules or mutate its internals by reach-through. Persistence ownership does not imply domain ownership. External command consistency and event production follow ADR-0038.

## Rationale and Alternatives

Centralizing all rules in the session root would duplicate or obscure boundary-specific legality. Letting a caller patch another component's state would make invariants dependent on call order. The decision preserves internal rule ownership while keeping the command's external consistency boundary coherent.

## Consequences

Components may own cohesive rules without becoming autonomous command roots. Effects cross boundaries through the session-owned protocol and the receiving boundary's rules. Earlier language that gave children independent event-production and cross-aggregate consistency authority is superseded by ADR-0038.

## Successors and Surviving Scope

ADR-0038 partially supersedes this record's child event ownership and cross-aggregate protocol. This record remains authoritative for cohesive domain legality, persistence/domain distinction, and prohibition on reach-through mutation.
