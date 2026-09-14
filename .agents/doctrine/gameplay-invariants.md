# Gameplay invariants

These domain truths remain stable across implementation workflows.

## Player and investigation state

- Wallet and inventory are concrete player state; do not replace them with
  generic supplies.
- Horse and saddle are separate concepts. Mounted travel requires a living,
  non-lame horse and a saddle.
- Water is not a generic stackable good.
- Hidden culprit truth remains internal. Player-facing state exposes only
  player-known clues, journal entries, warrants, and investigation results.
- The culprit is always a gang member, and every gang member is eligible unless
  the character is associated with but not part of the gang.
- Clue, journal, and wanted-poster flows remain stable unless directly in scope.

## Travel

- Travel is an active `JourneyLoop` that advances one trail day at a time and
  pauses for player choice; it is not an immediate multi-day town jump.
- Travel, journey, and encounter DTO shape is owned by `TravelMapper`;
  `GameSessionMapper` delegates rather than duplicating it.
- Travel state preserves origin, destination, route, progress, mode, player and
  horse condition, resources, and pending encounter state when applicable.

Gameplay mutations remain subject to the aggregate and event boundaries in
[architecture guardrails](architecture-guardrails.md).
