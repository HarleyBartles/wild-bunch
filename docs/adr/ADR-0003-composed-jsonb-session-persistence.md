# ADR-0003 Composed JSONB Session Persistence

## Status

live

## Dated Status History

- 2026-06-01 - live: session state is persisted as an envelope plus composed
  component payloads and replayable history rows.

## Decision Type

architecture, persistence

## Related ADRs

- `depends on`: ADR-0002
- `informs`: ADR-0004, ADR-0005, ADR-0007, ADR-0008, ADR-0012

## Context

Wild Bunch persists live session state through PostgreSQL-backed EF storage, but
the runtime model is not a simple one-table relational shape. Session-owned
state is composed of an envelope, JSON-backed component payloads, and ordered
history rows.

## Decision Drivers

- The domain should stay plain and rehydratable.
- Session-owned state needs durable snapshots without forcing a full relational
  redesign of runtime state.
- The store must preserve ordered history and component-level shape.
- Persistence should remain adaptable if the store evolves later.

## Decision Summary

Persist runtime session state as a composed store: a session envelope, JSONB
component payloads for the major session-owned pieces, and dedicated ordered
rows for log and diary history.

## Detailed Decision Breakdown

The current persistence layer writes the aggregate envelope, the main session
components, and the ordered history lists separately. The serializer owns the
conversion between domain objects and snapshot payloads, while the repository
coordinates save/load behavior.

This shape keeps the domain model free of EF concerns while preserving the
durable session state needed to resume play exactly where it left off.

## Options Considered and Rejected

- Fully relationalize every runtime detail into many normalized tables.
- Collapse the whole session into one opaque blob with no composed sub-shape.
- Move persistence shape knowledge into the domain model itself.

## When a Rejected Option Would Have Been Better

A fully relational model would only be better if the repo needed rich ad hoc SQL
reporting over runtime state as the primary use case. An opaque blob would only
be better if the app never needed component-level reasoning or partial updates.

## Benefits

- The aggregate can be rehydrated coherently.
- JSON payloads stay focused on coherent session-owned components.
- The persistence adapter can evolve without forcing domain refactors.

## Accepted Tradeoffs

- The store shape is more complex than a single table.
- Save/load logic has to keep several durable pieces in sync.

## Risks

- The composed shape can drift if the serializer and repository are not kept in
  lockstep.
- Multiple persistence surfaces mean more places to verify during schema work.

## Consequences for Future Work

Any future schema change should preserve the aggregate boundary and the
composed session shape unless a source-backed reason exists to replace it.

## Implementation Status or Plan

Live. The current persistence stack already uses the composed session model.

## Related Stable Source Surfaces

- `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs`
- `src/WildBunch.Persistence/GameSessions/`
- `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`
- `tests/WildBunch.Integration.Tests/MigrationTests.cs`

## Proof of Implementation or Explicit Non-Implementation

The repository saves a `GameSessions` envelope plus composed component,
log-entry, and diary-day rows, and the serializer can round-trip the aggregate
back into a `GameSession`.

## Review Triggers

- When the store shape no longer needs the component/history split.
- When a better source-backed store topology emerges.
