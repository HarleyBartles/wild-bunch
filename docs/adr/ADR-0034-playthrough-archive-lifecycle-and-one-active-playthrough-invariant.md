# ADR-0034 Playthrough Archive Lifecycle and One-Active-Playthrough Invariant

## Status

`live`

## Dated Status History

- 2026-06-27 - live: BUNCH-102 introduces player-facing start-over. `GameStatus.Archived` and the `PlaythroughArchived` event are implemented. The one-active-playthrough invariant is enforced in `StartNewGameHandler`, which archives all pre-existing `Active` sessions in the same UoW transaction as the new session create. Archived sessions remain persisted and queryable by id with `Archived` status; they are not deleted.

## Decision Type

architecture, persistence, gameplay

## Related ADRs

- `depends on`: ADR-0002 (GameSession is the command aggregate root — archive is a lifecycle mutation on the session root)
- `depends on`: ADR-0028 (event-sourcing posture — `PlaythroughArchived` is a typed domain event replayed through `Apply`)
- `depends on`: ADR-0003 (composed JSONB session persistence — `Archived` is a new enum value stored as string in the existing Status column; no new tables or migrations)
- `related to`: BUNCH-102 (player-facing start-over)

## Context

BUNCH-102 requires a player-facing start-over flow. When a player starts a new game, any pre-existing in-progress playthrough must be retired so the player re-enters the game at a single clean starting point. The previous design had no `Archived` status: sessions were either `Active`, `Completed`, or `Failed`. A start-over that simply created a new `Active` session would leave multiple `Active` sessions in the store, with no clear "current playthrough" and no durable record of the abandoned one.

Two competing needs shape the decision:

1. **Player-facing simplicity.** The player should have exactly one active playthrough at any time. Starting over must not strand the player between multiple in-progress games.
2. **Audit/history retention.** Abandoned playthroughs are useful history — for the player's own record and for any future analytics/replay. Deleting them on start-over would destroy that history. They should remain persisted and queryable, just not `Active`.

The one-active-playthrough invariant is the contract that reconciles these: at most one `GameStatus.Active` session exists in the persisted store after any successful start-new-game call. Retired sessions transition to `Archived`, not deleted.

## Decision Drivers

- Start-over must leave the player with exactly one current playthrough.
- Abandoned playthroughs must remain durable for audit/history, not be deleted.
- The invariant must hold across concurrent start-new-game attempts (transactional archive + create).
- The change should not force a persistence schema redesign — `Archived` is a new enum value, not a new table.
- Archive must be event-sourced (consistent with ADR-0028): a typed `PlaythroughArchived` event replayed through `Apply`, not a direct status mutation beside event emission.

## Decision Summary

Introduce a playthrough archive lifecycle:

- `GameStatus.Archived` (enum value `3`) marks a session as retired-without-deletion.
- `PlaythroughArchived` is a typed domain event carrying the archive reason, the player's last position (town, day, turn), and the status before archive.
- `GameSession.ArchivePlaythrough(reason)` is the command method: it validates the session is not already archived, constructs the event, and applies it through `ProduceEvent` (the single command-produces-event-then-applies path from ADR-0028).
- `Apply(PlaythroughArchived)` sets `Status = GameStatus.Archived`. The last-position snapshot on the event is decision data, not re-applied to live state (archive is terminal for play).
- The one-active-playthrough invariant is enforced at the application/persistence level in `StartNewGameHandler`: before creating a new session, it loads all `Active` sessions via `GetByStatusAsync(GameStatus.Active)`, archives each with reason `superseded-by-new-playthrough`, stages all archive appends and the new session create on the same `DbContext` under one correlation id, and commits in a single UoW transaction.

## Detailed Decision Breakdown

1. **`GameStatus.Archived` enum value.** Added to `GameStatus` as value `3` alongside `Active` (0), `Completed` (1), `Failed` (2). Stored as a string in the existing Status column of the composed session store (ADR-0003). No new persistence tables or EF migrations are required — the column already stores the enum as a string.

2. **`PlaythroughArchived` typed domain event.** A sealed record implementing `IDomainEvent`, owned by `WildBunch.Domain/Events/`. It carries:
   - `ArchivedAtUtc` — when the archive happened.
   - `ArchiveReason` — caller-supplied reason (e.g. `superseded-by-new-playthrough` for start-over, `start-over` for the explicit archive endpoint).
   - `PlayerName`, `LastTownId`, `LastTownName`, `Day`, `Turn` — a snapshot of the player's last position at archive time.
   - `StatusBeforeArchive` — the `GameStatus` the session held before archiving (usually `Active`, but the event records it honestly for audit).

   The event is registered in the persistence event deserializer (`ResolveEventType`) so it round-trips through the event stream like every other typed domain event.

3. **`GameSession.ArchivePlaythrough` command method.** Throws `InvalidOperationException` if the session is already `Archived` (archive is terminal — a session is not re-activated). Constructs the event from current live state (current town, clock day/turn, status) and calls `ProduceEvent`, which applies the event through `Apply` and records it in the uncommitted-events list. This is the ADR-0028 command-produces-event-then-applies path, not direct mutation.

4. **`Apply(PlaythroughArchived)` replay path.** Sets `Status = GameStatus.Archived` and increments `_version`. The last-position snapshot fields on the event are not re-applied to live state — archive is terminal for play, so the snapshot exists for audit/projection consumption, not for rehydrating a playable session. Replay reconstructs the same `Archived` status as the command path.

5. **One-active-playthrough invariant enforcement.** `StartNewGameHandler.HandleAsync` enforces the invariant:
   - Generates one correlation id for the entire archive-old + create-new flow.
   - Loads all `Active` sessions via `IGameSessionRepository.GetByStatusAsync(GameStatus.Active)`.
   - Archives each via `ArchivePlaythrough("superseded-by-new-playthrough")` and stages the append via `StoreAsync` on the same `DbContext`.
   - Creates the new session and stages it on the same `DbContext`.
   - Commits everything in one `CommitAsync` (single EF `SaveChangesAsync` + transaction commit).
   - Marks events committed on all touched sessions after commit.

   This guarantees that after any successful call, at most one `Active` session exists in the persisted store. If the transaction fails, no archive and no create land — the pre-existing `Active` session(s) remain `Active` and the player has not lost their in-progress game.

6. **Archived sessions remain loadable.** `GetByIdAsync` loads archived sessions by id with `Archived` status. They are not filtered out of loads. This supports audit/history reads and any future "resume archived" or "view past playthrough" surface. The invariant constrains only `Active` sessions, not loadability.

## Options Considered and Rejected

- **Delete abandoned sessions on start-over.** Rejected: destroys audit/history. The player's past playthroughs are useful record; deletion is irreversible and loses the durable event stream.
- **Allow multiple `Active` sessions (no invariant).** Rejected: leaves the player stranded between in-progress games with no clear current playthrough. Violates the player-facing simplicity requirement.
- **Enforce the invariant with a DB unique constraint on `Active` status.** Rejected: a partial unique index on `WHERE Status = 'Active'` is store-specific, couples the invariant to table shape, and does not compose with the event-sourced append path. The invariant is enforced transactionally in the handler on the same `DbContext`, which is sufficient and stays within the ADR-0028 single-repository-port posture.
- **Direct status mutation without an event.** Rejected: violates ADR-0028. Archive is a lifecycle fact that must be replayable; it flows through `ProduceEvent` → `Apply` like every other migrated mutation.
- **Re-activate archived sessions on start-over.** Rejected: archive is terminal. A new playthrough is a new `GameSession` with a new id, not a resurrection of an archived one. This keeps the event stream and identity of each playthrough clean.
- **Add a new persistence table for archived sessions.** Rejected: `Archived` is a new enum value stored as a string in the existing Status column. No schema change is needed. Normalizing archived sessions into a separate table would couple the lifecycle to table shape without benefit.

## When a Rejected Option Would Have Been Better

- Deletion would only be better if abandoned playthroughs had zero audit/history value, which they do not.
- A DB-level unique constraint would only be better if the handler-level transactional enforcement were insufficient, which it is not (single `DbContext`, single commit).
- A separate archived-sessions table would only be better if archived sessions had a fundamentally different shape from active ones, which they do not — they are the same aggregate with a different status.

## Benefits

- The player always has exactly one current playthrough after start-over.
- Abandoned playthroughs remain durable and queryable for audit/history.
- Archive is fully event-sourced: replayable, auditable, consistent with ADR-0028.
- No persistence schema change required — the new enum value stores as a string in the existing column.
- The invariant is enforced in one transaction, so a failed start-over never destroys an in-progress game.

## Accepted Tradeoffs

- `StartNewGameHandler` does more work (load + archive all active sessions) than a plain create. The cost is bounded by the number of active sessions, which the invariant keeps at most one in steady state.
- The invariant is enforced at the application layer, not by a DB constraint. A path that creates an `Active` session outside `StartNewGameHandler` could violate it. Mitigation: session creation flows through the established command handler route (ADR-0002), and `GetByStatusAsync` makes violations inspectable.
- Archived sessions accumulate in the store over time. This is acceptable for a greenfield repo with dev database drop/recreate available; a future cleanup/retention policy is a separate decision.

## Risks

- A future session-creation path that bypasses `StartNewGameHandler` could create a second `Active` session. Mitigation: ADR-0002 keeps `GameSession` as the single command root and creation flows through handlers; the invariant is documented here and enforced in the canonical handler.
- If `GetByStatusAsync` ever returns stale reads under isolation anomalies, two concurrent start-over calls could each archive the other's session. Mitigation: the single UoW transaction and optimistic concurrency (ADR-0028 §7) bound this; the archive append uses the same concurrency check as every other command.

## Consequences for Future Work

- New session-creation paths must archive pre-existing `Active` sessions in the same transaction or delegate to `StartNewGameHandler` to preserve the invariant.
- Read paths that list "current playthrough" should filter on `GameStatus.Active`, not assume a single session exists unconditionally.
- A future "resume archived playthrough" or "view past playthroughs" surface can load archived sessions by id or by `GetByStatusAsync(GameStatus.Archived)` without new schema.
- A future retention/cleanup policy for archived sessions is a separate decision; this ADR only establishes that archive is not deletion.
- Projections that care about lifecycle (e.g. a playthrough list) should handle `PlaythroughArchived` to reflect the retired status.

## Implementation Status or Plan

Live. `GameStatus.Archived` (value `3`), `PlaythroughArchived` event, `GameSession.ArchivePlaythrough`, `Apply(PlaythroughArchived)`, `StartNewGameHandler` invariant enforcement, and persistence deserializer registration are all implemented and tested (BUNCH-102).

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Game/GameStatus.cs`
- `src/WildBunch.Domain/Events/PlaythroughArchived.cs`
- `src/WildBunch.Domain/Game/GameSession.cs` (`ArchivePlaythrough`, `Apply(PlaythroughArchived)`)
- `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`
- `src/WildBunch.Application/Games/Commands/StartNewGameHandler.cs`
- `src/WildBunch.Application/Abstractions/IGameSessionRepository.cs` (`GetByStatusAsync`)
- `src/WildBunch.Persistence/GameSessions/EfGameSessionRepository.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Events.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionRehydrator.cs`
- `src/WildBunch.Application/Games/Models/ArchivePlaythroughResultDto.cs`
- `docs/adr/ADR-0002-gamesession-is-the-command-aggregate-root.md`
- `docs/adr/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md`
- `docs/adr/ADR-0003-composed-jsonb-session-persistence.md`

## Proof of Implementation or Explicit Non-Implementation

`GameStatus.cs` defines `Archived = 3`. `PlaythroughArchived.cs` is a sealed record event carrying archive reason, last-position snapshot, and `StatusBeforeArchive`. `GameSession.ArchivePlaythrough` produces the event through `ProduceEvent`; `Apply(PlaythroughArchived)` sets `Status = Archived`. `StartNewGameHandler.HandleAsync` archives all pre-existing `Active` sessions and creates the new session in one correlation id and one UoW commit. The event is registered in the persistence deserializer. Integration and domain tests cover the one-active-playthrough invariant and the archive lifecycle (BUNCH-102).

## Review Triggers

- When a second session-creation path is introduced that does not delegate to `StartNewGameHandler`.
- When a "resume archived playthrough" feature is proposed (archive-is-terminal would need revisiting).
- When a retention/cleanup policy for archived sessions is needed.
- When a projection needs to surface archived playthroughs to the player.
