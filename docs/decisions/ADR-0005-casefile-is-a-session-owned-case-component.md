# ADR-0005 CaseFile Is a Session-Owned Case Component

## Status

`live`

## Dated History

- `2026-06-01` - Chose to keep the case file and its case-local invariants within the live session rather than create a separate command root or repository.
- `2026-10-08` - Editorial clarification: the session's command and event consistency protocol is stated in ADR-0038; this record describes the case ownership boundary.

## Decision Type

`architecture`, `gameplay`

## Related ADRs

- `depends on`: ADR-0002
- `related to`: ADR-0006, ADR-0007, ADR-0038

## Context

Case knowledge and hidden case truth belong to one playthrough. Their rules are cohesive, but their lifecycle is the lifecycle of that session.

## Decision

Keep case-local state and invariants in a session-owned case component. The component is not an independent command root or repository. The session remains the authority that coordinates player commands affecting the case.

## Rationale and Alternatives

A separate case root would be appropriate if a case could be owned, commanded, and persisted independently from its playthrough. A transport-only collection would obscure case-local invariants. Neither alternative describes the chosen game model.

## Consequences

Case behavior remains consistent with the session and its event history. The component may own cohesive case rules without creating a second persistence or command boundary.
