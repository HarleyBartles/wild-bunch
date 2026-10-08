# ADR-0034 Playthrough Archival Is Event-Backed and Terminal

## Status

`partially superseded`

## Dated History

- `2026-06-27` - Chose an event-backed archive state that preserves an abandoned playthrough instead of deleting it; the initial single-player flow also aimed to leave one active playthrough after starting another.
- `2026-10-08` - Factual correction and partial supersession: the current setup path archives sessions it finds active, but the database does not enforce a global uniqueness constraint and per-stream concurrency does not prove a global one-active guarantee. ADR-0042 establishes the planned per-user invariant and immediate start-over archival.

## Decision Type

`architecture`, `persistence`, `gameplay`

## Related ADRs

- `depends on`: ADR-0002, ADR-0028
- `partially superseded by`: ADR-0042
- `related to`: ADR-0003

## Context

Starting over should retire the current playthrough without erasing the record of what happened. The original local flow treated the application as a single-player process and used an at-most-one-active-playthrough rule to select the current game.

## Decision

Archival is a durable lifecycle change recorded by a `PlaythroughArchived` event. An archived playthrough remains in history and cannot become active again under this decision. Archival is not deletion.

The original single-player rule intended at most one active playthrough in the application store. The current setup path stages archival of sessions it finds active alongside the new session, but this is application-level behavior and the database does not enforce global uniqueness across competing starts. ADR-0042 replaces that global description with the accepted, planned per-user invariant and requires confirmed start-over to archive immediately, independently of whether a replacement is created.

## Rationale and Alternatives

Deleting an abandoned playthrough would destroy its event history and prevent later inspection or exact reconstruction. Treating an archive request as a no-op until replacement creation succeeds would leave the confirmed start-over without its own durable effect.

## Consequences

An archive event remains part of the session's history and replay. The current source path does not establish race-safe global uniqueness; do not treat its query-and-archive sequence as a database guarantee. Ownership and active-session selection for hosted play follow the planned user-scoped decision in ADR-0042.

## Successors and Surviving Scope

ADR-0042 partially supersedes the global one-active-playthrough description and clarifies that confirmed start-over archives before replacement creation. Event-backed archival, retention of archived history, and the terminal archived state remain authoritative.
