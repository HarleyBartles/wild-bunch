# Event Sourcing Operational Guidance

## Event store as source of truth
Store each state change as an immutable event. The store is append-only and authoritative; any current state is a derived fold over events.

## Events, event streams, projections, snapshots
Events belong to streams. Projections build read models. Snapshots cache folded state to avoid replaying long streams from the beginning.

## Versioning and schema evolution
Add new event types and versioned fields without changing historical events. Support reading old event shapes and migrating projections.

## Concurrency models and idempotency
Use optimistic concurrency checks on stream appends. Make handlers idempotent by detecting duplicate event IDs or operation keys.
