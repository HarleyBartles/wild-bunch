# .NET Architecture Notes

Use these notes when a Wild Bunch task touches persistence shape, command/query boundaries, database pressure, or domain layering.

- Let the domain own rules and invariants.
- Route live gameplay mutations through `GameSession` unless current repo source proves a different live-play Aggregate Root.
- Use the application layer for command/query orchestration.
- Keep infrastructure responsible for persistence details, JSON snapshot storage, projections, and read models.
- Favor strongly typed aggregate snapshots that serialize cleanly to JSON for live session state.
- Do not prematurely fan runtime session state out into many tables.
- Add tables later when static content, projections, admin/editor needs, or cross-session data justify them.
- Use CQRS when it helps separate reads from writes; do not make it a blanket requirement.
- Treat event-sourcing concepts as guidance for replay and audit, not as a default persistence mandate.
- Apply onion or clean architecture only insofar as it keeps framework leakage out of the domain.

## Falsification checks

Before approving an architecture plan or worker return, check whether it accidentally:

- moves business rules from domain objects into handlers, controllers, persistence services, or UI code;
- makes database normalization the source of truth for volatile live session state;
- treats CQRS or event sourcing as mandatory ceremony rather than a scoped tool;
- stores framework-specific types in the domain model;
- changes clue, wanted-poster, wallet, inventory, horse, or travel state handling without the issue explicitly asking for it.
