# Game

GameSession aggregate root, player, clock, dev overrides, town aggregates, and related state.

## Key files

- [GameSession.cs](GameSession.cs) - GameSession aggregate root (live-play command authority).
- [GameSession.BountyLoopCoordinator.cs](GameSession.BountyLoopCoordinator.cs) - Bounty loop coordination partial.
- [GameSessionEventReplay.cs](GameSessionEventReplay.cs) - Event replay partial for GameSession.
- [GameSessionId.cs](GameSessionId.cs) - GameSession identifier.
- [GameStatus.cs](GameStatus.cs) - Game status enum.
- [GameClock.cs](GameClock.cs) - In-game clock and turn tracking.
- [TimeOfDay.cs](TimeOfDay.cs) - Time-of-day enum.
- [Player.cs](Player.cs) - Player state.
- [PursuitState.cs](PursuitState.cs) - Pursuit/lawman pressure state.
- [TravelDayOutcome.cs](TravelDayOutcome.cs) - Travel day outcome.
- [TownActionContext.cs](TownActionContext.cs) - Town action context.
- [TownAggregate.cs](TownAggregate.cs) - Town aggregate.
- [TownVisitState.cs](TownVisitState.cs) / [TownSourceVisitState.cs](TownSourceVisitState.cs) - Town visit state.
- [WantedSuspectPresenceLedger.cs](WantedSuspectPresenceLedger.cs) - Wanted suspect presence ledger.
- [GameLogEntry.cs](GameLogEntry.cs) / [GameLogEntryKind.cs](GameLogEntryKind.cs) - Legacy game log entry and kind.
- [DevTravelOverride.cs](DevTravelOverride.cs) - Dev travel override state.
- [DevSaloonOverride.cs](DevSaloonOverride.cs) - Dev saloon override state.

Back to [WildBunch.Domain/](../INDEX.md)
