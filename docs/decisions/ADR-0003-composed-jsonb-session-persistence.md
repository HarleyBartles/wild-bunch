# ADR-0003 Composed JSONB Session Persistence

## Status

`partially superseded`

## Dated History

- `2026-06-01` - Chose a composed persistence shape for session state rather than a single opaque payload or a fully relational model.
- `2026-06-24` - Removed the dedicated `GameSessionLogEntries` table and its write path; commit `6cdd23e` records the event-projection replacement and removal of the legacy log path.
- `2026-10-08` - Editorial clarification: composed session snapshots remain, while event history and projections, not dedicated log rows, own the durable event-derived history.

## Decision Type

`architecture`, `persistence`

## Related ADRs

- `depends on`: ADR-0002
- `partially superseded by`: ADR-0028
- `related to`: ADR-0004

## Context

Session state is composed of concepts with different persistence needs. A coherent resumable session requires a durable snapshot without coupling the domain model to EF Core or forcing every runtime detail into normalized tables.

## Decision

Persist session state through a session envelope and composed component snapshots. Keep persistence translation in the adapter. Durable event history and event-derived player-facing history are governed by ADR-0028; this record does not establish separate log-entry rows as an authority.

## Rationale and Alternatives

A fully relational runtime model would be appropriate if ad hoc relational queries over all session details were the primary need. One opaque blob would lose useful component boundaries. The composed shape allows the domain and persistence adapter to evolve separately while preserving a coherent session snapshot.

## Consequences

Snapshot serialization, persistence mapping, and replay must preserve the same session meaning. Schema migrations remain necessary when persisted payloads change. The old dedicated log table is historical and is not to be recreated as a parallel source of truth.

## Successors and Surviving Scope

ADR-0028 replaces this record's dedicated log-history authority with immutable event history and rebuildable projections. The composed snapshot decision survives.
