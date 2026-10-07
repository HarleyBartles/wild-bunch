# Cache-backed state in strict CQRS and event sourcing

## Spike question

The user supplied a diagram in which immutable events continuously maintain a cache, consumers obtain current state from that cache, and invalidation causes replay to rebuild the cache. Is this appropriate for Wild Bunch's existing DDD, strict CQRS and event-sourcing architecture? This is research and a recommendation for the ongoing design, not implementation approval or a new framework adoption.

## Findings

Yes. The event stream is authoritative history; a materialized representation of that history is the normal efficient state source. Microsoft's CQRS guidance describes materialized views as durable read-only caches, while its event-sourcing guidance describes snapshots as optimizations regenerable from events. Fowler likewise distinguishes stored/current application state from the event history that can rebuild it. Neither requires full replay on every request.

Strict CQRS still separates command-side aggregate loading from query-side presentation models. A snapshot hydrates GameSession so it can enforce command invariants; journal, casebook, HUD and other read models answer queries without executing gameplay. They can share one PostgreSQL database and derive from the same stream. The diagram's single cache box is a useful logical simplification, not a requirement to expose the command aggregate through every query API or maintain an independent truth store for every audience.

Continuously maintained caches do not require asynchronous consumers or eventual consistency. Marten's documented inline projections update derived documents with newly appended events in the same database transaction. This is relevant evidence for the pattern in .NET/PostgreSQL, not a recommendation to replace EF with Marten. Our low-volume game can keep event append and current snapshots/projections in one transactional unit of work, using expected stream position to reject concurrent conflicting writes. A successful result then corresponds to committed events and matching derived state.

Cache validity includes recorded stream position, projection/schema version, completeness and supported state invariants. Matching version integers or parseable JSON alone are insufficient. Different component reads must represent one coherent stream position. An invalid cache must not generate new salts, invoke decision logic or replace established facts with defaults.

A bounded full rebuild from authoritative supported events is the recommended invalid-cache path for 0.1.0. Rebuild once for the selected coherent stream position, derive required snapshot/read representations and return through their ordinary ports. Publish repaired caches only when the relevant version is still current, so an older rebuild cannot overwrite a newer committed state. Rebuilding is infrastructure maintenance, not a gameplay command; no game events, clocks, payments, random rolls or external effects are repeated. A rebuilt representation can be used in memory without a mandatory database write-and-reread round trip. Exact transactional repair mechanics remain JIT implementation choices under the freshness and CQRS contract.

Updating a current cache with newly committed events is normal projection maintenance. Rebuilding an invalid cache from its complete stream is recovery. These are distinct from loading an older snapshot and applying a tail of events as an additional supported recovery strategy. The latter is valid generally, but is not needed to justify dormant competing machinery in this baseline. Invalid/unsupported authoritative history fails explicitly; invalid derived state is rebuilt. Event upcasters remain necessary for supported event versions; changed projection shapes are discarded/rebuilt rather than given a second upcasting architecture.

## Repository implications

Existing event-sourcing doctrine largely matches the diagram and already says caches are optional, projections rebuild and writeback converges. Its partial-replay flow diagram and zero-event developer-prep exception need reconciliation with the cleanup's selected route retirement and current loader findings. PS-03 through PS-12 and related Application read findings identify gaps in validity, partial diary/component loss, read coherence and repeated rebuilds. The user's stale-browser-tab allowance does not authorize incoherent or stale server command state. No cache, event, schema or product code was changed during this spike.

## Primary sources

- [Microsoft Event Sourcing pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing): immutable history, snapshots, concurrency and duplicate-event handling.
- [Microsoft CQRS pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs): separate models can share one database; event-derived materialized read views are caches.
- [Marten inline projections](https://martendb.io/events/projections/inline): event append and projection updates in one database transaction.
- [Marten command handler workflow](https://martendb.io/scenarios/command_handler_workflow.html): snapshot-backed aggregate loading with optimistic concurrency.
- [EventSourcingDB common issues](https://docs.eventsourcingdb.io/best-practices/common-issues/): distinguish aggregate snapshot acceleration from full read-model rebuilding.
- [Fowler Event Sourcing](https://martinfowler.com/eaaDev/EventSourcing.html): current application state is maintained and can be rebuilt from event history.
