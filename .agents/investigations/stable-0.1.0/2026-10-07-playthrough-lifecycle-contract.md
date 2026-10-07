# Playthrough lifecycle contract: PG-002

**Status:** Working product decision record. Archive-on-confirmation is settled by the user. Implementation corrections and historical compatibility remain for the specification and plans.

## Confirmed start over

Start Over opens confirmation. Cancelling leaves the current playthrough unchanged. Confirming durably archives the current playthrough through the existing PlaythroughArchived event before returning the player to the starting page. This is a completed domain transition, not a deferred request contingent on creating another game.

After successful confirmation, closing the browser, signing out or waiting does not reverse the archive. Returning finds no active playthrough until the player creates another. Creation of a replacement is a separate action; failure or abandonment of that creation cannot undo the earlier archive. A fresh setup visit uses PG-001's new editable seed and default settings.

Event history is retained. An archive fact is immutable; any future restoration would require an explicitly designed new transition rather than deleting or rewriting it. This record does not add restoration to 0.1.0.

The server must commit the archive before the client treats it as successful. A failed or uncertain request must not be represented as completed solely by clearing browser state. On a lost response, refresh/retry must reconcile with authoritative archive state, with an already-completed archive remaining safe to retry. Ownership and active-playthrough discovery must be server-backed for public deployment; localStorage alone cannot deliver the promised cross-login lifecycle.

## Behavioral proof

Protect cancellation without mutation, confirmation persisting the archive event and terminal state, repeated confirmation without duplicate archive facts, archived command rejection, and returning after successful archive with no active playthrough despite creating no replacement. Cover command failure without false client success and authoritative reconciliation after a lost response. Verify retained history through reload/replay rather than tests that only inspect confirmation wording or mock a localStorage removal.

## Source anchors and dependencies

Current source includes GlobalOverlays' confirmation, useGameSessionMutations' archive command/cache handling, ArchivePlaythroughHandler, GameSession.ArchivePlaythrough and Apply(PlaythroughArchived). The current event-backed archive path matches the selected product boundary; browser/server recovery and account isolation still need implementation assessment. PG-001 owns new-game creation, and account ownership supplies the public per-user active-playthrough contract. See the [working feature inventory](2026-10-07-feature-inventory.md).
