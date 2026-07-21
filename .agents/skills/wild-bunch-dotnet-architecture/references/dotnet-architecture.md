# .NET Architecture

## Layer boundaries

| Layer | Owns |
| --- | --- |
| Domain | `GameSession`, child components, invariants, typed domain events, `Apply` behavior |
| Application | Command/query orchestration, retries, setup guards, DTO mapping, projections |
| Persistence | EF Core entities, stored-event envelopes, JSON component snapshots, serializers, migrations |
| API and web | Transport and presentation adapters over application contracts |

## Persistence contract

- The typed event stream is the replayable history for `GameSession`.
- Component snapshots cache current state. They are not required for correctness.
- A stale or incomplete snapshot must fall back to full event replay.
- Command-path and replay-path state must converge.
- Stream and snapshot versions remain explicit; optimistic concurrency checks the committed stream version.
- Projection-backed reads derive from events or dedicated read storage without mutating the aggregate.

## Falsification checks

Reject a change that:

- moves gameplay rules into handlers, controllers, serializers, repositories, or UI code;
- mutates aggregate state without producing and applying a typed event;
- treats a JSON snapshot as the only recoverable source;
- couples domain types to EF Core or transport frameworks;
- normalizes volatile live-session state without explicit scope;
- introduces infrastructure ceremony that the task does not need.

Inspect `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, relevant serializers, events, and replay tests before retaining current-state claims.
