# ADR-0013 Travel Journey Is a Session-Owned Aggregate Subtree

## Status

live

## Dated Status History

- 2026-06-01 - live: travel and journey state already live inside `GameSession` as a cohesive session-owned boundary.

## Decision Type

architecture, gameplay, persistence

## Related ADRs

- `depends on`: ADR-0002, ADR-0003, ADR-0004
- `informs`: ADR-0007, ADR-0008, ADR-0011

## Context

The current travel model is not a separate command root. `GameSession` owns the command entry, while `TravelJourney` and related travel state form a coherent session-owned subtree for route progress, day advancement, interruptions, and arrival handling.

## Decision Drivers

- Travel must advance through the live session, not through a parallel root.
- The route and journey model has its own internal invariants and state.
- Persistence already serializes journey and travel diary state with the session.
- The model should stay precise without claiming any future systems that do not exist yet.

## Decision Summary

Keep travel/journey state as a session-owned aggregate subtree under `GameSession`. It is cohesive and internally bounded, but it is not a separate command aggregate root or repository.

## Detailed Decision Breakdown

`GameSession` owns the active journey, travel progression methods, arrival acknowledgement, encounter resolution, and the travel diary history that belongs to the current session. `TravelJourney` carries route preview, remaining days, remaining distance, day-plan state, pending encounters, and history needed to advance the trail one day at a time.

That makes travel a real internal boundary with its own invariants, while still keeping command authority and persistence at the session level. The travel subtree is part of the live game aggregate, not a separate system.

## Options Considered and Rejected

- Promote travel to its own top-level command aggregate root.
- Treat journey state as ephemeral UI-only state.
- Flatten journey, diary, encounter, and route progress into helper DTOs with no domain boundary.

## When a Rejected Option Would Have Been Better

A separate root would only be better if travel had independent command lifecycles and separate persistence ownership. UI-only state would only be better for a throwaway prototype, not for the current live trail system.

## Benefits

- Travel progress stays coherent inside the live session.
- Route, day-plan, interruption, and arrival rules remain close together.
- Persistence can round-trip the travel subtree as part of the session snapshot.

## Accepted Tradeoffs

- `GameSession` remains the command owner for travel actions.
- The travel subtree has enough behavior that it must stay carefully named and not be reduced to a vague helper bucket.

## Risks

- The travel model could drift into a hidden root if future work starts routing commands around `GameSession`.
- Travel wording could become misleading if it starts implying future systems like richer lawman or route-simulation mechanics.

## Consequences for Future Work

Future travel features should continue to live under the session root unless a new ADR proves a separate command boundary is necessary.

## Implementation Status or Plan

Live. `TravelJourney` and related travel state are already owned by `GameSession` and serialized through the session snapshot layer.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Game/GameSession.cs`
- `src/WildBunch.Domain/Travel/TravelJourney.cs`
- `src/WildBunch.Domain/Travel/TravelModels.cs`
- `src/WildBunch.Domain/Travel/TravelRouteModels.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Travel.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- `tests/WildBunch.Domain.Tests/GameSessionJourneyHistoryTests.cs`
- `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- `tests/WildBunch.Application.Tests/AdvanceTravelDayHandlerTests.cs`
- `tests/WildBunch.Application.Tests/TravelToTownHandlerTests.cs`

## Proof of Implementation or Explicit Non-Implementation

`GameSession` owns `Journey`, starts and advances it through session methods, persists it via the serializer, and the travel tests exercise the route and journey boundary as a live part of the session.

## Review Triggers

- When travel starts being commanded outside `GameSession`.
- When route progress stops being a coherent internal subtree.
- When a future travel system would need its own command root or repository.
