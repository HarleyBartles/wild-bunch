# ADR-0028 Immutable Event History and Rebuildable State

## Status

`partially superseded`

## Dated History

- `2026-06-22` - Adopted typed domain events, event-derived state, a coordinated write path, and projections for the first migrated gameplay flows.
- `2026-06-23` - Extended the event-sourced flows to investigation, bounty, saloon, and travel actions.
- `2026-06-24` - Replaced the dedicated `GameSessionLogEntries` table and its write/read paths with journal projection from the event stream; commit `6cdd23e` records the removal.
- `2026-07-18` - Full event replay became the production state-reconstruction path, rather than an unimplemented future migration.
- `2026-10-08` - Editorial clarification: immutable event history is the source for reconstruction, current caches serve ordinary state reads and are rebuilt from ordered history when invalid. ADR-0038 partially supersedes the child command/event protocol described by earlier wording.

## Decision Type

`architecture`, `persistence`

## Related ADRs

- `depends on`: ADR-0002, ADR-0014
- `partially supersedes`: ADR-0003
- `partially superseded by`: ADR-0038
- `related to`: ADR-0007, ADR-0013, ADR-0020, ADR-0039

## Context

The game needs exact reconstruction of what happened, safe player-facing projections, and one coherent consistency boundary for each command. Recording events beside unrelated mutable state would create an audit log without making the event history authoritative.

## Decision

The immutable typed event stream records facts that occurred and is the source from which game state can be reconstructed. Commands evaluate legality against current state and produce the facts of their result. Applying committed events rebuilds state; replay does not make new random choices. Random outcomes become facts in the stream, so replay never rerolls them.

The session snapshot and other event-derived caches are rebuildable. Ordinary reads may use a current valid cache. When a cache is missing or invalid, replay ordered event history to rebuild it, then serve state from that rebuilt cache. Neither a cache nor a second journal table is an independent source of truth. Player journal, HUD, casebook, and developer audit views are audience-specific projections of event facts and must preserve the hidden-truth boundary.

CQRS remains strict: commands own mutation decisions and writes; queries return read models and do not mutate game state. Event envelopes and serialization belong to persistence. A command stages its aggregate snapshot and event append through the same repository and Unit of Work so the stream and cache advance coherently.

## Rationale and Alternatives

Mutable snapshots plus an event log would permit the same action to produce contradictory state and history. Reconstructing state from immutable facts keeps replay exact, while a derived cache provides an efficient read path. A separate event-store save path or independently authoritative journal table would split one command across competing transaction and truth boundaries.

## Consequences

State reconstruction must apply the recorded event sequence without consulting a new random roll. Invalid derived state is repaired from intact history or rejected when history cannot establish it; loading a snapshot must not invent missing facts. Read models may be rebuilt without changing the event stream. Persistence migrations must preserve event compatibility and the repository's upcasting strategy.

## Successors and Surviving Scope

ADR-0038 partially supersedes this record's child event-emission and cross-aggregate protocol. The decisions for immutable event history, replay, rebuildable caches, strict command/query responsibilities, coordinated persistence, and event-derived projections remain authoritative. ADR-0003's composed snapshot decision survives; this record partially supersedes its former dedicated log-history authority. The removal of `GameSessionLogEntries` is part of the history and must not be reversed by restoring a parallel journal authority.
