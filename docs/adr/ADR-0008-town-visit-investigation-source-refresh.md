# ADR-0008 Town-Visit Investigation Source Refresh

## Status

live

## Dated Status History

- 2026-06-01 - live: investigation sources are tracked by session-owned,
  town-local visit state with explicit first-check, revisit, and refresh
  markers when the player arrives in a town.
- 2026-06-01 - live: the session-owned `TownAggregate` boundary now owns the
  town definition, source affordances, visit refresh, and wanted-poster
  bookkeeping while `GameSession` remains the live-play command route.

## Decision Type

gameplay, domain

## Related ADRs

- `depends on`: ADR-0005, ADR-0006
- `informs`: ADR-0007, ADR-0009, ADR-0020

## Context

Investigation sources such as telegraph leads, local gossip, notice boards,
local records, and wanted posters are town-visit scoped interactions. The game should remember
which sources were used in the current town while allowing the same action to be
fresh again after travel.

## Decision Drivers

- Repeat clicks in the same town should not keep generating new knowledge.
- Leaving town should refresh the source opportunities.
- The model needs to preserve a “nothing new here” response for repeat actions.
- The rule should be easy to test.

## Decision Summary

Treat investigation sources as town-visit scoped session state. Same-visit
repeats are spent; arriving in a town refreshes the current town's source
markers and wanted-poster state while preserving per-town history in the
session-owned town visit book.

## Detailed Decision Breakdown

`TownAggregate` keeps the current town definition together with the session's
town visit state. `TownVisitState` tracks per-town visit records, per-source
refresh markers, and the wanted-poster flag for the current town. `GameSession`
delegates town-local behavior to the aggregate when the player changes towns or
checks a source, which makes investigation sources reusable after travel
without making them endlessly repeatable inside one visit.

The action methods use that state to return source-specific “nothing new” feedback
when the player revisits the same source in the same town.

## Options Considered and Rejected

- Make each investigation source globally one-time for the whole case.
- Allow repeated sources to keep revealing new knowledge forever.
- Move source refresh logic into the UI instead of the domain.

## When a Rejected Option Would Have Been Better

Global one-time sources would only be better if the product wanted a single
evergreen source-of-truth flag for the whole case. Unlimited repeats would only
be better if the gameplay loop were intentionally grindy, which it is not.

## Benefits

- Town travel matters mechanically.
- Investigation remains bounded and legible.
- The same source can matter again in a new place without being exploitable in
  one stop.

## Accepted Tradeoffs

- The state has to remember per-town source usage.
- Repeat feedback needs to be explicit so the player understands why nothing
  happened.

## Risks

- Reset logic could break if town travel is refactored without preserving the
  town-visit boundary.
- New investigation sources could forget to participate in the same spent-state
  pattern.

## Consequences for Future Work

Any new town-scoped source should follow the same refresh pattern and use the
same visit state rather than inventing a separate repeatability system.

## Implementation Status or Plan

Live. The town-visit state and investigation actions already use this pattern.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Game/TownSourceVisitState.cs`
- `src/WildBunch.Domain/Game/TownVisitState.cs`
- `src/WildBunch.Domain/Game/GameSession.cs`
- `tests/WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs`
- `tests/WildBunch.Domain.Tests/TownVisitStateTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs`

## Proof of Implementation or Explicit Non-Implementation

`TownAggregate` stores the current town definition and delegates into
`TownVisitState` for the per-town visit records and source refresh markers.
`GameSession` refreshes the aggregate when the player arrives in a new town.

## Review Triggers

- When a new investigation source type needs different repeat rules.
- When town travel no longer cleanly resets the visit state.
