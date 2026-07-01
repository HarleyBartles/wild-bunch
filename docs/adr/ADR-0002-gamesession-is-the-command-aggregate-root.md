# ADR-0002 GameSession Is the Command Aggregate Root

## Status

live

## Dated Status History

- 2026-06-01 - live: `GameSession` remains the mutable live-play aggregate root and command handlers persist it through the repository boundary.
- 2026-06-01 - clarified: `GameSession` can contain real session-owned aggregate/subaggregate boundaries, not just flat helper components.

## Decision Type

architecture

## Related ADRs

- `depends on`: ADR-0001
- `informs`: ADR-0003, ADR-0005, ADR-0006, ADR-0007, ADR-0008, ADR-0012, ADR-0013

## Context

The repo's live play state needs a single authoritative mutation boundary. The domain already exposes `GameSession` as a mutable aggregate root, and command handlers load and save it through `IGameSessionRepository`.

## Decision Drivers

- Gameplay mutations need one clear authority.
- Session state includes several cohesive internal components.
- Persistence should follow the aggregate boundary instead of defining it.
- The command model must stay predictable for future refactors.

## Decision Summary

Keep `GameSession` as the single top-level command aggregate root for live play. Real session-owned aggregate/subaggregate boundaries may exist inside the session for coherent state and invariants, but they do not become separate command roots or repositories by default.

## Detailed Decision Breakdown

`GameSession` owns the live play state, the command methods, and the invariant boundary for session mutation. The command side enters through handlers such as new-game creation and session action handlers, then persists the aggregate through the repository abstraction.

This lets the domain keep a single orchestration point for case progress, travel, inventory, logs, and town-visit state while still allowing internally cohesive substructures where they make the model clearer. Those subordinate boundaries are real domain boundaries for internal cohesion and invariants, not DTO-ish helpers and not accidental mirrors of table shape.

Current examples include `CaseFile` and the travel/journey model. Both stay under the session root and both persist as part of the session model.

## Options Considered and Rejected

- Split live play into multiple command aggregate roots by feature.
- Move gameplay mutation into services that sit outside the aggregate.
- Treat the repository as the primary authority and let the aggregate be thin.
- Flatten all internal models into DTO-like helper objects with no real sub-boundaries.

## When a Rejected Option Would Have Been Better

Separate command roots would only be better if the gameplay domains were truly independent and could be mutated without shared consistency concerns. That is not the current shape of Wild Bunch's live session model.

Flattening would only be better for a tiny data-transfer shape with no internal invariants. That is not how the current session model works.

## Benefits

- One clear boundary for command legality and state changes.
- Less duplication of orchestration logic.
- Easier reasoning about persistence and rehydration.

## Accepted Tradeoffs

- `GameSession` remains a substantial class because it owns the live-play orchestration.
- Internally cohesive components must still be kept tidy so the aggregate does not become a catch-all.
- Subordinate boundaries have to stay explicit enough that the session root does not hide distinct invariants behind vague helper names.

## Risks

- The root could grow too large if new decisions are pushed into it without extracting pure helpers.
- A later domain split would need careful migration because other surfaces now rely on the single-root assumption.

## Consequences for Future Work

New gameplay slices should assume the session root owns command mutation unless there is a concrete source-backed reason to introduce a separate root.

## Implementation Status or Plan

Live. The domain and command flow already route through `GameSession`.

- 2026-06-01 - live: `GameSession` remains the mutable live-play aggregate root and command handlers persist it through the repository boundary.
- 2026-06-01 - clarified: `GameSession` can contain real session-owned aggregate/subaggregate boundaries, not just flat helper components.
- 2026-07-01 - live: the child-component extraction pattern (`BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`, `ActionContextTracker`) is now concrete. See `.agents/docs/game-session-decomposition-audit.md` for the current inventory and trajectory. The BUNCH-67/68/72-era "future sub-aggregate splits" language is superseded by this concrete pattern.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Game/GameSession.cs`
- `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`
- `src/WildBunch.Domain/Game/BountyLoop.cs`
- `src/WildBunch.Domain/Game/JourneyLoop.cs`
- `src/WildBunch.Domain/Game/InvestigationLoop.cs`
- `src/WildBunch.Domain/Game/StoreLoop.cs`
- `src/WildBunch.Domain/Game/ActionContextTracker.cs`
- `.agents/docs/game-session-decomposition-audit.md`
- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `src/WildBunch.Domain/Travel/TravelJourney.cs`
- `src/WildBunch.Domain/Travel/TravelModels.cs`
- `src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs`
- `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- `tests/WildBunch.Domain.Tests/CaseFileTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionJourneyHistoryTests.cs`
- `tests/WildBunch.Domain.Tests/TravelResolverTests.cs`
- `tests/WildBunch.Application.Tests/AdvanceTravelDayHandlerTests.cs`
- `tests/WildBunch.Application.Tests/TravelToTownHandlerTests.cs`
- `tests/WildBunch.Integration.Tests/`

## Proof of Implementation or Explicit Non-Implementation

`GameSession` is a sealed aggregate root in the domain, command handlers persist it through `IGameSessionRepository`, and the repository rehydrates the session back through the same boundary. Five internal child domain components (`BountyLoop`, `JourneyLoop`, `InvestigationLoop`, `StoreLoop`, `ActionContextTracker`) own cohesive state and decision logic under the session root; see `.agents/docs/game-session-decomposition-audit.md` for the audit.

## Review Triggers

- When a second command root becomes concrete and unavoidable.
- When `GameSession` starts accumulating unrelated responsibilities that no longer belong to a single live-play aggregate.
- When a new child component is added or an existing one is removed — update `.agents/docs/game-session-decomposition-audit.md` in the same PR.

## Historical Notes

BUNCH-67 (refactor GameSession responsibilities into domain aggregates), BUNCH-68 (map GameSession responsibility slices and aggregate candidates), and BUNCH-72 (introduce bounty loop aggregate candidate inside GameSession) are closed as historical/superseded. The concrete child-component extraction pattern established by BUNCH-112 (BountyLoop), BUNCH-119 (JourneyLoop), and BUNCH-120 (InvestigationLoop + ActionContextTracker + StoreLoop) supersedes the earlier "future sub-aggregate splits" language. Do not reopen those old tracks.
