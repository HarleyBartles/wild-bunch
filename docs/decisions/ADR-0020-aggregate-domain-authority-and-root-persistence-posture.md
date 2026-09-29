# ADR-0020 Aggregate Domain Authority and Root Persistence Posture

## Status

live

## Dated Status History

- 2026-06-01 - live: aggregates are first-class domain authorities in Wild Bunch, and aggregate roots coordinate persistence and transaction posture without overriding other aggregate boundaries.

## Decision Type

architecture, persistence

## Related ADRs

- `depends on`: ADR-0002, ADR-0005, ADR-0007, ADR-0008, ADR-0013, ADR-0014
- `informs`: ADR-0010

## Context

Wild Bunch already uses multiple real domain boundaries inside the live session model. `GameSession` is the command/persistence route for now, but it is not the only meaningful place where invariants, legality, and lawful mutation live. `CaseFile` owns case and hidden-truth legality. `TownAggregate` now owns town-local service affordances, visit-source legality, and wanted-poster bookkeeping. Future pursuit or lawman boundaries are expected to own their own legality when they exist.

The repo needs an explicit doctrine that keeps aggregate authority separate from persistence posture. A root may coordinate a composed mutation tree, but that does not make every subordinate aggregate a helper or a loosely owned bag of state.

## Decision

Aggregates are first-class domain authorities. Each aggregate owns the legality, invariants, state transitions, and domain events inside its own boundary. The aggregate root owns its own consistency boundary and coordinates persistence or transaction posture for that boundary, but it does not gain authority to mutate or override another aggregate's internal domain rules.

Persistence ownership is not domain ownership. A session/root aggregate may stage, accept, reject, or commit a composed set of legal effects for the command it is handling, but the legal meaning of each effect still belongs to the aggregate that owns the boundary it touches.

## Wild Bunch Application

`GameSession` remains the live-play command route and session persistence boundary for now. That is a coordination choice, not a claim that `GameSession` owns the full legality of every domain surface beneath it.

Concrete examples in the current architecture:

- `TownAggregate` owns town-local services, source affordances, source repeat legality, return-refresh behavior, and wanted-poster bookkeeping.
- `CaseFile` owns case progression, clue legality, and hidden-truth boundaries.
- `GameSession` coordinates the live-play command flow and composes the session snapshot, but it should delegate into those aggregate-owned APIs instead of hand-coding their internal legality.
- Future pursuit or lawman aggregates should own their own pursuit legality when they become concrete.

This means a session/root aggregate can coordinate a command that touches several aggregates, but each aggregate still decides what state changes are lawful within its own boundary.

## Cross-Aggregate Effects

Cross-aggregate effects travel as explicit facts, events, or commands routed through aggregate-owned APIs. One aggregate may emit a fact from lawful mutation, and orchestration may route that fact to another aggregate's public API.

The receiving aggregate then decides, according to its own invariants, whether to accept it, reject it, no-op it, or emit further lawful effects.

No code reaches across an aggregate boundary to mutate state directly. That is true whether the caller is `GameSession`, an application handler, or another aggregate.

## Persistence and Transaction Posture

The coordinating aggregate root or application route may commit all legal effects together, reject the full mutation tree, or discard optional rejected side effects when the command policy says that is lawful.

That persistence posture is coordination, not authority. Committing multiple effects together does not mean the root has semantic control over every aggregate involved.

If a command needs to touch several aggregates, the orchestration layer may group those legal changes into one transaction boundary. Each aggregate still owns the legality of its own state transition and event emission.

## Anti-Patterns

- Direct reach-through mutation across aggregate boundaries.
- `GameSession` hand-coding another aggregate's legality instead of calling that aggregate's API.
- Treating composed aggregates as lesser helpers or disliked second cousins of the root.
- Treating persistence ownership as domain ownership.
- Letting a root patch another aggregate's internal state just because both objects are in memory.

## Consequences

Future refactors should prefer aggregate-owned APIs and explicit events or effects over direct state patching. When a new boundary appears, its invariants should live inside that aggregate instead of being duplicated in `GameSession` or an application service.

The current architecture stays simple: `GameSession` coordinates the session, while the boundary-specific aggregate owns the legality of its own model. That posture should guide future splits and should prevent the repo from re-centralizing domain authority in the session root.

## Validation

- `git diff --check`

## Proof of Implementation or Explicit Non-Implementation

This ADR is doctrine-only. The motivating example is the concrete `TownAggregate` boundary already present in source, but this document does not add new production behavior.
