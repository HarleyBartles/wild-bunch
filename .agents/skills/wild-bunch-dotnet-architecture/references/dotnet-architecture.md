# .NET Architecture Notes

Use these notes when a Wild Bunch task touches persistence shape, command/query boundaries, database pressure, or domain layering.

- Let the domain own rules and invariants.
- Route live gameplay mutations through `GameSession` unless current repo source proves
  a different live-play Aggregate Root.
- Current migrated live-play flows are event-sourced at the `GameSession` boundary:
  commands produce typed domain events, `GameSession` applies them, and the repository
  appends those typed events while keeping JSON component snapshots as cache.
- Use the application layer for command/query orchestration.
- Keep infrastructure responsible for persistence envelopes, JSON component snapshot
  cache, projections, and read models.
- Favor event streams plus component snapshot cache for live session state; do not
  treat the snapshots as the conceptual source of history for migrated flows.
- Do not introduce a separate event-store interface, broker, EventStoreDB, or normalized
  live-session table split unless the issue explicitly scopes it.
- Do not prematurely fan runtime session state out into many tables.
- Add tables later when static content, projections, admin/editor needs, or cross-session data justify them.
- Use CQRS when it helps separate reads from writes; do not make it a blanket requirement.
- Apply onion or clean architecture only insofar as it keeps framework leakage out of the domain.
- `GameSession` owns `BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`,
  and `ActionContextTracker`; each child receives narrow context and returns outcomes
  or events-to-produce.

## Falsification checks

Before approving an architecture plan or worker return, check whether it accidentally:

- moves business rules from domain objects into handlers, controllers, persistence services, or UI code;
- makes database normalization the source of truth for volatile live session state;
- treats CQRS or event sourcing as mandatory ceremony rather than a scoped tool;
- stores framework-specific types in the domain model;
- changes clue, wanted-poster, wallet, inventory, horse, or travel state handling without the issue explicitly asking for it.
