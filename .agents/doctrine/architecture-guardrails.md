# Architecture guardrails

Wild Bunch uses DDD, CQRS, and event sourcing as established architecture, not
optional aspirations.

## Layer ownership

- `GameSession` is the live-play aggregate root, external gameplay-command
  boundary, event-production owner, apply-dispatch owner, and persistence seam.
- Application handlers coordinate commands and queries; they do not own
  gameplay invariants.
- Infrastructure owns EF Core entities, event envelopes, serializers,
  snapshots, migrations, and projection storage.
- Read models and projections derive query state without becoming a second
  write model.
- Domain models remain independent of EF and storage table shape.

The complete replay, snapshot, projection, upcaster, and version invariants
live in [event-sourcing integrity](event-sourcing-integrity.md).

## Aggregate and child boundaries

- `GameSession` may coordinate cross-component behavior and session-level
  concerns; cohesive state plus rules belong to the owning internal child.
- A child receives narrow context rather than the parent aggregate, returns
  outcomes or events-to-produce, and does not mutate sibling owners, produce
  events directly, or own infrastructure.
- External code never directly mutates the aggregate's child entities.
- Invariant failures use the established result-object pattern rather than
  exceptions.
- Unknown setup values remain nullable until known; placeholder values that
  are silently replaced are forbidden.

## Setup phase

- Before `GameStarted`, current-town state is null.
- `GameSessionCommandHandler.ExecuteWithRetryAsync` centrally rejects gameplay
  commands during setup. Lifecycle/setup handlers explicitly opt out through
  `RequiresGameStarted`; query handlers enforce the setup boundary themselves.
- `ArchivePlaythrough` remains a lifecycle operation available after the event
  stream begins, including during setup.
- Read models, projections, DTOs, and dev mappers preserve nullable town state
  during setup.

## Persistence posture

- Runtime session persistence is JSON snapshot-oriented, while the event stream
  remains the source of history for event-backed sessions.
- A zero-event `StartPrepped` session still requires its current snapshot load
  path.
- Repo-local database artifacts live under repo-root `.local/`, never `src/`.
- Current mainline correctness wins over obsolete internal/save compatibility
  unless compatibility is explicitly required.
- Do not introduce a broker, separate event-store interface, EventStoreDB, or
  normalized live-session table split without explicit scope.

Seed, difficulty, entropy, and starting-town ownership lives only in
[game-content seed pipeline](game-content-seed-pipeline.md). Browser authority
lives in [frontend standards](frontend-standards.md).
