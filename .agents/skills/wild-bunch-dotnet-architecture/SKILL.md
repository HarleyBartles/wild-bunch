---
name: wild-bunch-dotnet-architecture
description: Use when Wild Bunch C# work changes GameSession command flows, application boundaries, typed event persistence, component snapshots, projections, or framework dependencies.
metadata:
  status: active
  scope: Wild Bunch .NET application and persistence architecture.
  use_when:
    - Use when a task crosses domain, application, persistence, API, or read-model boundaries.
  do_not_use_when:
    - Do not use for isolated gameplay-rule changes that stay inside the domain model.
---

# Wild Bunch .NET Architecture

## Owned decision

Keep domain rules in the `GameSession` aggregate, application code focused on command/query orchestration, and infrastructure responsible for persistence and framework concerns.

## Current architecture

- `GameSession` is the live-play aggregate root and event-production boundary.
- Command flows produce typed domain events, apply them through `GameSession`, and record uncommitted events.
- Persistence appends the typed event stream. Event-backed sessions can fully replay when JSON component snapshots are stale or incomplete.
- `StartPrepped` sessions may have zero events. Snapshot components are required to load that current zero-event state because full replay returns `null` when no stored events exist.
- `GameSession` owns `BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`, and `ActionContextTracker` as internal child components.
- Application handlers coordinate commands and queries but do not own gameplay invariants.
- Infrastructure owns EF Core entities, event envelopes, serializers, snapshot components, migrations, and projection storage.
- Read models and projections derive query state without becoming a second write model.

## Decision pattern

1. Inspect the live domain, handler, repository, event, and test paths.
2. Put invariants and state transitions in the aggregate or owning child component.
3. Put use-case sequencing in application code.
4. Put database and serialization details in persistence.
5. Prove replay and snapshot paths converge for event-backed sessions, and preserve the zero-event `StartPrepped` load path.

Use `ddd`, `cqrs`, `event-sourcing`, `event-driven-architecture`, `clean-architecture`, and `dotnet` only for the generic architecture question they own.

## Reference

Read [.NET architecture](references/dotnet-architecture.md) for persistence and falsification checks.

## Stop conditions

- Do not add direct aggregate mutation outside typed event production and `Apply`.
- Do not make component snapshots the conceptual source of history for event-backed sessions, but do not remove the snapshots required by zero-event `StartPrepped` sessions.
- Do not introduce a broker, EventStoreDB, separate event-store interface, or normalized live-session table split unless the task explicitly requires it.
