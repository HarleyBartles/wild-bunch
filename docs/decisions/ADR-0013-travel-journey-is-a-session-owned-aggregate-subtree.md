# ADR-0013 Travel Is a Session-Owned Domain Subtree

## Status

`live`

## Dated History

- `2026-06-01` - Chose travel and journey as cohesive state within the `GameSession` command boundary.
- `2026-10-08` - Editorial clarification: internal travel rules do not create an independent command root; ADR-0038 records the current child protocol.

## Decision Type

`architecture`, `gameplay`, `persistence`

## Related ADRs

- `depends on`: ADR-0002
- `related to`: ADR-0028, ADR-0038

## Context

Travel has its own route progress, day advancement, interruption, and arrival rules, but those rules change the same live playthrough as town and case actions.

## Decision

Keep travel and journey state as a cohesive session-owned domain subtree under `GameSession`. It is not an independent command root or repository. The session owns command consistency, while the travel subtree owns its cohesive internal rules.

## Rationale and Alternatives

A separate root would be justified only if travel could be commanded and persisted independently without shared playthrough consistency. Treating journey state as ephemeral UI state would prevent the server from authoritatively resuming travel.

## Consequences

Travel outcomes and progress are part of the playthrough's event-derived state. Internal extraction may clarify travel rules without changing command ownership or persistence authority.
