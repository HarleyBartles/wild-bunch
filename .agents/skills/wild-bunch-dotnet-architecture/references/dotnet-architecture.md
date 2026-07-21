# .NET Architecture

## Layer boundaries

| Layer | Owns |
| --- | --- |
| Domain | `GameSession`, child components, invariants, typed domain events, `Apply` behavior |
| Application | Command/query orchestration, retries, setup guards, DTO mapping, projections |
| Persistence | EF Core entities, stored-event envelopes, JSON component snapshots, serializers, migrations |
| API and web | Transport and presentation adapters over application contracts |

## Persistence contract

- The typed event stream is the replayable history for event-backed `GameSession` sessions.
- Event-backed sessions can rebuild from full replay when component snapshots are stale or incomplete.
- `StartPrepped` can persist with zero stored events. Full replay returns `null` for an empty stream, so snapshot components are required to load that current prepped state.
- Command-path and replay-path state must converge for event-backed sessions. The zero-event prepped path must preserve and load its snapshot components until setup itself is event-backed.
- Stream and snapshot versions remain explicit; optimistic concurrency checks the committed stream version.
- Projection-backed reads derive from events or dedicated read storage without mutating the aggregate.

## Falsification checks

Reject a change that:

- moves gameplay rules into handlers, controllers, serializers, repositories, or UI code;
- mutates aggregate state without producing and applying a typed event;
- treats a JSON snapshot as the only recoverable source;
- deletes snapshot components without preserving the zero-event `StartPrepped` load path;
- couples domain types to EF Core or transport frameworks;
- normalizes volatile live-session state without explicit scope;
- introduces infrastructure ceremony that the task does not need.

Inspect `src/WildBunch.Domain/Game/GameSession.cs`, `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`, relevant serializers, events, and replay tests before retaining current-state claims.
